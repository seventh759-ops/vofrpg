using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>迷宮の中の部屋1つ（論理座標のマス）。</summary>
    public sealed class RoomArea
    {
        public int X, Y, W, H;
        public bool Activated;   // 入って敵が出た
        public bool IsBoss;
        public bool IsStart;
        public bool IsTreasure;  // 宝の間（敵が出ず、宝箱が置いてある）

        public Vector2 Center => new Vector2(X + W * 0.5f, Y + H * 0.5f);

        public bool Contains(Vector2 p, float margin = 0f) =>
            p.x >= X + margin && p.x <= X + W - margin && p.y >= Y + margin && p.y <= Y + H - margin;

        public bool Overlaps(RoomArea o, int gap) =>
            X - gap < o.X + o.W && X + W + gap > o.X && Y - gap < o.Y + o.H && Y + H + gap > o.Y;
    }

    /// <summary>
    /// 塔の1つの層（迷宮）。ランダムに並べた部屋を廊下でつなぐ。論理座標のマス (i, j) の中心は (i + 0.5, j + 0.5)。
    /// 画面ではひし形の床と、奥側の立体の壁・手前側の低い縁で描く。
    /// 敵が迷路を回り込んで追えるよう、プレイヤーまでの道のりの地図（フローフィールド）も持つ。
    /// </summary>
    public sealed class Room : MonoBehaviour
    {
        public const int MapSize = 64;

        const byte Wall = 0;
        const byte Floor = 1;

        readonly byte[] tiles = new byte[MapSize * MapSize];
        readonly int[] flow = new int[MapSize * MapSize];
        readonly Queue<int> queue = new Queue<int>();
        readonly List<GameObject> spawned = new List<GameObject>();
        int flowOrigin = -1;

        // 小さな地図（クォータービューの向きで描く。歩いて見た場所だけが出る）
        public const int MiniW = MapSize * 2;
        public const int MiniH = MapSize;
        readonly bool[] explored = new bool[MapSize * MapSize];
        Color32[] miniPixels;
        bool miniDirty;
        public Texture2D Minimap { get; private set; }

        public readonly List<RoomArea> Areas = new List<RoomArea>();
        public Vector2 StartPos { get; private set; }
        public Vector2 StairsPos { get; private set; }
        /// <summary>祭壇の位置（ボスの層にはない）。</summary>
        public bool HasAltar { get; private set; }
        public Vector2 AltarPos { get; private set; }
        public RoomArea BossArea { get; private set; }
        /// <summary>宝の間の宝箱の位置（宝の間がなければ空）。</summary>
        public readonly List<Vector2> ChestPositions = new List<Vector2>();
        /// <summary>この層の区画（見た目）。</summary>
        public ZoneSpec Zone { get; private set; }

        /// <summary>層を作り直す。boss なら「はじめの部屋 → 廊下 → 大きなボスの部屋」。</summary>
        public void Generate(int floor, bool boss, int seed)
        {
            var rng = new System.Random(seed);
            System.Array.Clear(tiles, 0, tiles.Length);
            Areas.Clear();
            BossArea = null;
            ChestPositions.Clear();
            flowOrigin = -1;
            System.Array.Clear(explored, 0, explored.Length);
            Zone = Zones.For(floor);

            if (boss) BuildBossFloor(rng);
            else BuildNormalFloor(rng, floor);

            Render();
            ResetMinimap();
        }

        void ResetMinimap()
        {
            if (Minimap == null)
            {
                Minimap = new Texture2D(MiniW, MiniH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = "Minimap" };
                miniPixels = new Color32[MiniW * MiniH];
            }
            for (int k = 0; k < miniPixels.Length; k++) miniPixels[k] = new Color32(0, 0, 0, 0);
            miniDirty = true;
            ApplyMinimap();
        }

        /// <summary>地図の上の位置（ピクセル）。論理座標を、画面と同じ斜めの向きに直す。</summary>
        public Vector2 MinimapPixel(Vector2 logical) =>
            new Vector2(logical.x - logical.y + MapSize, (logical.x + logical.y) * 0.5f);

        /// <summary>まわりのマスを「見た」ことにして、地図に描き足す。</summary>
        public void Reveal(Vector2 center, float radius)
        {
            int r = Mathf.CeilToInt(radius);
            int ci = Mathf.FloorToInt(center.x), cj = Mathf.FloorToInt(center.y);
            for (int j = cj - r; j <= cj + r; j++)
            {
                for (int i = ci - r; i <= ci + r; i++)
                {
                    if (i < 0 || j < 0 || i >= MapSize || j >= MapSize) continue;
                    int k = j * MapSize + i;
                    if (explored[k] || (i - ci) * (i - ci) + (j - cj) * (j - cj) > r * r) continue;
                    explored[k] = true;
                    if (!IsFloorTile(i, j)) continue;
                    // 1マスを横2ドットで描く
                    int px = i - j + MapSize - 1, py = (i + j) / 2;
                    var c = new Color32(150, 140, 175, 255);
                    SetMini(px, py, c);
                    SetMini(px + 1, py, c);
                    miniDirty = true;
                }
            }
        }

        void SetMini(int x, int y, Color32 c)
        {
            if (x < 0 || y < 0 || x >= MiniW || y >= MiniH) return;
            miniPixels[y * MiniW + x] = c;
        }

        public void ApplyMinimap()
        {
            if (!miniDirty || Minimap == null) return;
            Minimap.SetPixels32(miniPixels);
            Minimap.Apply(false);
            miniDirty = false;
        }

        void BuildNormalFloor(System.Random rng, int floor)
        {
            int target = Mathf.Min(5 + floor / 4, 8);
            for (int attempt = 0; attempt < 300 && Areas.Count < target; attempt++)
            {
                int w = rng.Next(9, 14);
                int h = rng.Next(9, 14);
                var a = new RoomArea { X = rng.Next(2, MapSize - w - 2), Y = rng.Next(2, MapSize - h - 2), W = w, H = h };
                bool ok = true;
                foreach (var o in Areas)
                {
                    if (!a.Overlaps(o, 6)) continue; // 部屋どうしを離す（廊下が長くなり、隣の部屋の敵と近づきにくい）
                    ok = false;
                    break;
                }
                if (ok) Areas.Add(a);
            }
            if (Areas.Count == 0) Areas.Add(new RoomArea { X = 20, Y = 20, W = 9, H = 9 });

            // 近い順につなぐ（はじめの部屋から順に、まだつないでいない一番近い部屋へ）
            var connected = new List<RoomArea> { Areas[0] };
            var rest = new List<RoomArea>(Areas);
            rest.RemoveAt(0);
            foreach (var a in Areas) Carve(a);
            while (rest.Count > 0)
            {
                RoomArea bestFrom = null, bestTo = null;
                float best = float.MaxValue;
                foreach (var c in connected)
                {
                    foreach (var r in rest)
                    {
                        float d = (c.Center - r.Center).sqrMagnitude;
                        if (d >= best) continue;
                        best = d;
                        bestFrom = c;
                        bestTo = r;
                    }
                }
                Corridor(bestFrom.Center, bestTo.Center, rng);
                connected.Add(bestTo);
                rest.Remove(bestTo);
            }

            var start = Areas[0];
            start.IsStart = true;
            StartPos = start.Center;
            // 階段は、はじめの部屋から一番遠い部屋に置く
            RoomArea far = start;
            float farD = 0f;
            foreach (var a in Areas)
            {
                float d = (a.Center - start.Center).sqrMagnitude;
                if (d <= farD) continue;
                farD = d;
                far = a;
            }
            StairsPos = far.Center;

            // 祭壇：はじめの部屋と階段の部屋以外の、どこかの部屋の隅寄り
            var candidates = new List<RoomArea>();
            foreach (var a in Areas)
            {
                if (a != start && a != far) candidates.Add(a);
            }
            HasAltar = candidates.Count > 0 && GameConfig.AltarOnFloor(floor); // ボスの層の1つ前に1つ
            RoomArea altarRoom = null;
            if (HasAltar)
            {
                altarRoom = candidates[rng.Next(candidates.Count)];
                AltarPos = new Vector2(altarRoom.X + 1.5f + rng.Next(Mathf.Max(1, altarRoom.W - 3)), altarRoom.Y + altarRoom.H - 1.5f);
            }

            // 宝の間：まれに、敵の出ない部屋に宝箱が並ぶ
            if (rng.NextDouble() < GameConfig.TreasureRoomChance)
            {
                var pool = new List<RoomArea>();
                foreach (var a in candidates)
                {
                    if (a != altarRoom && a.W >= 6 && a.H >= 5) pool.Add(a);
                }
                if (pool.Count > 0)
                {
                    var t = pool[rng.Next(pool.Count)];
                    t.IsTreasure = true;
                    var c = t.Center;
                    for (int k = 0; k < GameConfig.TreasureChests; k++)
                        ChestPositions.Add(new Vector2(c.x + (k - (GameConfig.TreasureChests - 1) * 0.5f) * 1.6f, c.y));
                }
            }
        }

        void BuildBossFloor(System.Random rng)
        {
            var start = new RoomArea { X = 6, Y = 6, W = 8, H = 8, IsStart = true };
            var boss = new RoomArea { X = 24, Y = 22, W = 16, H = 16, IsBoss = true };
            Areas.Add(start);
            Areas.Add(boss);
            Carve(start);
            Carve(boss);
            Corridor(start.Center, boss.Center, rng);
            StartPos = start.Center;
            StairsPos = boss.Center; // ボスを倒すと現れる
            BossArea = boss;
            HasAltar = false;
        }

        void Carve(RoomArea a)
        {
            for (int i = a.X; i < a.X + a.W; i++)
                for (int j = a.Y; j < a.Y + a.H; j++)
                    Set(i, j, Floor);
        }

        /// <summary>幅2マスの L 字の廊下。</summary>
        void Corridor(Vector2 from, Vector2 to, System.Random rng)
        {
            int x0 = (int)from.x, y0 = (int)from.y, x1 = (int)to.x, y1 = (int)to.y;
            bool horizontalFirst = rng.Next(2) == 0;
            if (horizontalFirst)
            {
                HLine(x0, x1, y0);
                VLine(y0, y1, x1);
            }
            else
            {
                VLine(y0, y1, x0);
                HLine(x0, x1, y1);
            }
        }

        void HLine(int x0, int x1, int y)
        {
            for (int x = Mathf.Min(x0, x1); x <= Mathf.Max(x0, x1); x++)
            {
                Set(x, y, Floor);
                Set(x, y + 1, Floor);
            }
        }

        void VLine(int y0, int y1, int x)
        {
            for (int y = Mathf.Min(y0, y1); y <= Mathf.Max(y0, y1); y++)
            {
                Set(x, y, Floor);
                Set(x + 1, y, Floor);
            }
        }

        void Set(int i, int j, byte v)
        {
            if (i < 1 || j < 1 || i >= MapSize - 1 || j >= MapSize - 1) return; // 外周は必ず壁
            tiles[j * MapSize + i] = v;
        }

        bool IsFloorTile(int i, int j) => i >= 0 && j >= 0 && i < MapSize && j < MapSize && tiles[j * MapSize + i] == Floor;

        // ---- 描く ----

        // 壁のマスの絵（キャラクターの手前の壁を透かすのに使う）。壁でないマスは null
        readonly SpriteRenderer[] wallRenderers = new SpriteRenderer[MapSize * MapSize];
        readonly List<int> fadedWalls = new List<int>();

        void Render()
        {
            foreach (var go in spawned) Destroy(go);
            spawned.Clear();
            System.Array.Clear(wallRenderers, 0, wallRenderers.Length);
            fadedWalls.Clear();

            var floorA = PixelArt.IsoFloorFor(Zone, false);
            var floorB = PixelArt.IsoFloorFor(Zone, true);
            var rim = PixelArt.IsoWallFor(Zone);
            for (int j = 0; j < MapSize; j++)
            {
                for (int i = 0; i < MapSize; i++)
                {
                    var p = new Vector2(i + 0.5f, j + 0.5f);
                    if (IsFloorTile(i, j))
                    {
                        Place((i + j) % 2 == 0 ? floorA : floorB, p, SortOrder.Floor);
                        continue;
                    }
                    // 床に接する壁だけを、どこも同じ高さで描く（高さがそろって見えるように）
                    bool touches = IsFloorTile(i - 1, j) || IsFloorTile(i + 1, j) || IsFloorTile(i, j - 1) || IsFloorTile(i, j + 1)
                                   || IsFloorTile(i - 1, j - 1) || IsFloorTile(i + 1, j + 1) || IsFloorTile(i + 1, j - 1) || IsFloorTile(i - 1, j + 1);
                    if (touches) wallRenderers[j * MapSize + i] = Place(rim, p, SortOrder.For(p));
                }
            }
        }

        SpriteRenderer Place(Sprite sprite, Vector2 logical, int order)
        {
            var go = new GameObject("Tile");
            go.transform.SetParent(transform, false);
            go.transform.position = Iso.ToScreen(logical);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            spawned.Add(go);
            return sr;
        }

        /// <summary>
        /// キャラクターの手前にあって重なっている壁を半透明にする（離れたら元に戻す）。毎フレーム呼ぶ。
        /// 手前＝画面でキャラクターより下にある壁。壁の絵は上に伸びるので、少し下の壁まで見る。
        /// </summary>
        public void FadeWallsNear(Vector2 player, float dt)
        {
            const float faded = 0.3f, speed = 6f;
            Vector2 ps = Iso.ToScreen(player);
            int pi = Mathf.FloorToInt(player.x), pj = Mathf.FloorToInt(player.y);

            // 今透けている壁のうち、もう重なっていないものを戻す
            for (int k = fadedWalls.Count - 1; k >= 0; k--)
            {
                int idx = fadedWalls[k];
                var sr = wallRenderers[idx];
                if (sr == null) { fadedWalls.RemoveAt(k); continue; }
                bool cover = Covers(sr.transform.position, ps);
                var c = sr.color;
                c.a = Mathf.MoveTowards(c.a, cover ? faded : 1f, speed * dt);
                sr.color = c;
                if (!cover && c.a >= 1f) fadedWalls.RemoveAt(k);
            }
            // 近くの壁で、新しく重なったものを透かし始める
            for (int j = pj - 3; j <= pj + 3; j++)
            {
                for (int i = pi - 3; i <= pi + 3; i++)
                {
                    if (i < 0 || j < 0 || i >= MapSize || j >= MapSize) continue;
                    int idx = j * MapSize + i;
                    var sr = wallRenderers[idx];
                    if (sr == null || fadedWalls.Contains(idx)) continue;
                    if (Covers(sr.transform.position, ps)) fadedWalls.Add(idx);
                }
            }
        }

        static bool Covers(Vector3 wallScreen, Vector2 playerScreen) =>
            wallScreen.y < playerScreen.y && playerScreen.y - wallScreen.y < 1.6f && Mathf.Abs(wallScreen.x - playerScreen.x) < 1.1f;

        // ---- 当たり判定 ----

        /// <summary>その点が床か。</summary>
        public bool Contains(Vector2 p) => IsFloorTile(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y));

        /// <summary>半径 r の円が床の上に収まるか（四隅で調べる）。</summary>
        public bool IsFree(Vector2 p, float r) =>
            Contains(new Vector2(p.x - r, p.y - r)) && Contains(new Vector2(p.x + r, p.y - r)) &&
            Contains(new Vector2(p.x - r, p.y + r)) && Contains(new Vector2(p.x + r, p.y + r));

        /// <summary>壁に沿って滑るように動く（x と y を別々に試す）。大きく動くときは細かく分ける。</summary>
        public Vector2 Move(Vector2 from, Vector2 delta, float r)
        {
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / 0.25f));
            Vector2 step = delta / steps;
            Vector2 p = from;
            for (int s = 0; s < steps; s++)
            {
                var nx = new Vector2(p.x + step.x, p.y);
                if (IsFree(nx, r)) p = nx;
                var ny = new Vector2(p.x, p.y + step.y);
                if (IsFree(ny, r)) p = ny;
            }
            return p;
        }

        /// <summary>その場所が床ならそのまま、壁なら一番近い床のマスの中心。</summary>
        public Vector2 Clamp(Vector2 p, float r)
        {
            if (IsFree(p, r)) return p;
            int ci = Mathf.FloorToInt(p.x), cj = Mathf.FloorToInt(p.y);
            for (int ring = 1; ring < 8; ring++)
            {
                float best = float.MaxValue;
                Vector2 found = p;
                for (int dj = -ring; dj <= ring; dj++)
                {
                    for (int di = -ring; di <= ring; di++)
                    {
                        if (Mathf.Abs(di) != ring && Mathf.Abs(dj) != ring) continue;
                        if (!IsFloorTile(ci + di, cj + dj)) continue;
                        var c = new Vector2(ci + di + 0.5f, cj + dj + 0.5f);
                        float d = (c - p).sqrMagnitude;
                        if (d >= best) continue;
                        best = d;
                        found = c;
                    }
                }
                if (best < float.MaxValue) return found;
            }
            return StartPos;
        }

        /// <summary>そのマスの部屋（廊下なら null）。</summary>
        public RoomArea AreaAt(Vector2 p)
        {
            foreach (var a in Areas)
            {
                if (a.Contains(p, 0.3f)) return a;
            }
            return null;
        }

        /// <summary>部屋の中の床の、ランダムな位置（avoid から minDist 以上離す）。</summary>
        /// <summary>部屋の中央寄り（端から frac の割合だけ内側）のランダムな位置。眠った敵を置くのに使う。</summary>
        public Vector2 RandomPointCentral(RoomArea a, float frac = 0.25f)
        {
            float mx = Mathf.Max(1f, a.W * frac), my = Mathf.Max(1f, a.H * frac);
            return new Vector2(Random.Range(a.X + mx, a.X + a.W - mx), Random.Range(a.Y + my, a.Y + a.H - my));
        }

        public Vector2 RandomPointIn(RoomArea a, Vector2 avoid, float minDist)
        {
            Vector2 p = a.Center;
            for (int k = 0; k < 20; k++)
            {
                p = new Vector2(Random.Range(a.X + 1f, a.X + a.W - 1f), Random.Range(a.Y + 1f, a.Y + a.H - 1f));
                if ((p - avoid).sqrMagnitude >= minDist * minDist) return p;
            }
            return p;
        }

        // ---- 追いかけるための道のりの地図 ----

        /// <summary>プレイヤーのいるマスからの道のり（マス数）を作り直す。マスが変わったときだけ計算する。</summary>
        public void UpdateFlow(Vector2 target)
        {
            int ti = Mathf.FloorToInt(target.x), tj = Mathf.FloorToInt(target.y);
            if (!IsFloorTile(ti, tj)) return;
            int origin = tj * MapSize + ti;
            if (origin == flowOrigin) return;
            flowOrigin = origin;

            for (int k = 0; k < flow.Length; k++) flow[k] = -1;
            queue.Clear();
            flow[origin] = 0;
            queue.Enqueue(origin);
            while (queue.Count > 0)
            {
                int cur = queue.Dequeue();
                int ci = cur % MapSize, cj = cur / MapSize;
                Visit(ci + 1, cj, flow[cur]);
                Visit(ci - 1, cj, flow[cur]);
                Visit(ci, cj + 1, flow[cur]);
                Visit(ci, cj - 1, flow[cur]);
            }
        }

        void Visit(int i, int j, int d)
        {
            if (!IsFloorTile(i, j)) return;
            int k = j * MapSize + i;
            if (flow[k] >= 0) return;
            flow[k] = d + 1;
            queue.Enqueue(k);
        }

        /// <summary>from から target へ向かう向き。近ければまっすぐ、遠ければ道のりの地図に沿って回り込む。</summary>
        public Vector2 FlowDir(Vector2 from, Vector2 target)
        {
            Vector2 direct = target - from;
            int fi = Mathf.FloorToInt(from.x), fj = Mathf.FloorToInt(from.y);
            if (!IsFloorTile(fi, fj)) return direct.sqrMagnitude > 0.0001f ? direct.normalized : Vector2.zero;
            int here = flow[fj * MapSize + fi];
            if (here <= 1) return direct.sqrMagnitude > 0.0001f ? direct.normalized : Vector2.zero;

            int best = here;
            Vector2 bestCenter = from;
            for (int dj = -1; dj <= 1; dj++)
            {
                for (int di = -1; di <= 1; di++)
                {
                    if (di == 0 && dj == 0) continue;
                    int ni = fi + di, nj = fj + dj;
                    if (!IsFloorTile(ni, nj)) continue;
                    // 斜めは、角を削らないよう両隣が床のときだけ
                    if (di != 0 && dj != 0 && (!IsFloorTile(fi + di, fj) || !IsFloorTile(fi, fj + dj))) continue;
                    int d = flow[nj * MapSize + ni];
                    if (d < 0 || d >= best) continue;
                    best = d;
                    bestCenter = new Vector2(ni + 0.5f, nj + 0.5f);
                }
            }
            Vector2 v = bestCenter - from;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector2.zero;
        }
    }
}
