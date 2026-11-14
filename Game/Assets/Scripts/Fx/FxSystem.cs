using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>爆発などの、広がって消える輪。</summary>
    public sealed class FxSystem : MonoBehaviour
    {
        sealed class Burst
        {
            public SpriteRenderer Sr;
            public Transform Root;   // 斬撃：クォータービューの潰れを付ける親
            public float T;
            public float Duration;
            public float Radius;
            public Color Color;
            public bool Steady;      // 広がらずに脈打つ（ボスの攻撃の予告）
            public bool Soft;        // やわらかい光の柱（SoftPillar）
        }

        readonly List<Burst> active = new List<Burst>();
        readonly Stack<Burst> free = new Stack<Burst>();
        readonly List<Burst> slashes = new List<Burst>();
        readonly Stack<Burst> freeSlashes = new Stack<Burst>();
        readonly List<Burst> discs = new List<Burst>();
        readonly Stack<Burst> freeDiscs = new Stack<Burst>();
        readonly List<Burst> pillars = new List<Burst>();
        readonly Stack<Burst> freePillars = new Stack<Burst>();

        sealed class Particle
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;  // 床の上の位置と速さ（論理座標）
            public float H, Vh;       // 高さと上向きの速さ
            public float Gravity;
            public float T, Life;
            public Color Color;
            public float Scale0 = 1f, Scale1 = 1f; // 大きさ（はじめ → 終わり）
            public float Spin, Angle;              // 回る速さ（度/秒）と今の角度
            public float Drag = 3f;                // 床の上の速さの減り方
            public bool Custom;                    // 火花以外の絵（スキルの演出）
        }

        // ---- 時間差の演出（スキルの演出を、少しずつずらして出す） ----
        readonly List<(float t, System.Action a)> scheduled = new List<(float, System.Action)>();

        /// <summary>delay 秒後に a を実行する（ゲームの時間で。止めている間は進まない）。</summary>
        public void After(float delay, System.Action a)
        {
            if (delay <= 0f) { a(); return; }
            scheduled.Add((delay, a));
        }

        /// <summary>
        /// 絵の粒を1つ出す（スキルの演出）。pos は床の位置、h は高さ。vel は床の上の速さ、vh は上向きの速さ。
        /// 大きさは scale0 → scale1、spin で回る。flip で左右反転（残像）。
        /// </summary>
        public void Emit(Vector2 pos, float h, Sprite sprite, Color color, float life, Vector2 vel, float vh = 0f, float gravity = 0f,
            float scale0 = 1f, float scale1 = 1f, float spin = 0f, float drag = 3f, bool flip = false, float angle = 0f)
        {
            if (particles.Count >= MaxParticles) return;
            var p = TakeParticle();
            p.Sr.sprite = sprite;
            p.Sr.flipX = flip;
            p.Pos = pos;
            p.Vel = vel;
            p.H = h;
            p.Vh = vh;
            p.Gravity = gravity;
            p.T = 0f;
            p.Life = life;
            p.Color = color;
            p.Scale0 = scale0;
            p.Scale1 = scale1;
            p.Spin = spin;
            p.Angle = angle;
            p.Drag = drag;
            p.Custom = true;
            p.Sr.sortingOrder = 21000; // ほかの演出と同じく、いちばん手前（壁に隠れない）
            ApplyParticle(p);
            particles.Add(p);
        }

        Particle TakeParticle()
        {
            Particle p;
            if (freeParticles.Count > 0) p = freeParticles.Pop();
            else
            {
                var go = new GameObject("Spark");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.Spark;
                Lighting.MakeGlow(sr);
                p = new Particle { Sr = sr };
            }
            p.Sr.gameObject.SetActive(true);
            return p;
        }

        void ApplyParticle(Particle p)
        {
            p.Sr.transform.position = (Vector3)(Iso.ToScreen(p.Pos) + new Vector2(0f, p.H));
            float k = p.Life > 0f ? p.T / p.Life : 1f;
            var c = p.Color;
            c.a = p.Color.a * (1f - k);
            p.Sr.color = c;
            if (p.Custom)
            {
                float sc = Mathf.Lerp(p.Scale0, p.Scale1, k);
                p.Sr.transform.localScale = new Vector3(sc, sc, 1f);
                p.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, p.Angle);
            }
        }
        readonly List<Particle> particles = new List<Particle>();
        readonly Stack<Particle> freeParticles = new Stack<Particle>();
        const int MaxParticles = 400;

        // レベルアップの羽（頭の上で開いて、上がりながら消える）
        public const float LevelUpDuration = 1.9f;
        const float WingOpacity = 0.9f;
        readonly SpriteRenderer[] wingGlows = new SpriteRenderer[2]; // 羽の後ろの、大きな金色の光
        SpriteRenderer wingPillar, wingPillarFront; // 足元から羽まで伸びる光の柱（キャラクターの後ろと前に重ねて、包みこむ）
        float featherTimer;
        /// <summary>羽を付ける位置（プレイヤーの論理座標）。GameWorld が毎フレーム入れる。</summary>
        public Vector2 Anchor;
        Transform wingRoot;
        readonly Transform[] hinges = new Transform[2];
        readonly SpriteRenderer[] wings = new SpriteRenderer[2];
        float wingT = -1f;

        public void LevelUp()
        {
            if (wingRoot == null)
            {
                wingRoot = new GameObject("LevelUpWings").transform;
                wingRoot.SetParent(transform, false);
                for (int i = 0; i < 2; i++)
                {
                    hinges[i] = new GameObject(i == 0 ? "LeftHinge" : "RightHinge").transform;
                    hinges[i].SetParent(wingRoot, false);
                    var go = new GameObject("Wing");
                    go.transform.SetParent(hinges[i], false);
                    go.transform.localPosition = new Vector3(-0.6875f, -0.09f, 0f); // 付け根を蝶番に合わせる（羽は幅 22 ドット）
                    wings[i] = go.AddComponent<SpriteRenderer>();
                    wings[i].sprite = PixelArt.Wing;
                    Lighting.MakeGlow(wings[i]);
                    var glow = new GameObject("WingGlow");
                    glow.transform.SetParent(hinges[i], false);
                    glow.transform.localPosition = new Vector3(-0.6875f * 1.3f, -0.09f * 1.3f, 0f);
                    glow.transform.localScale = new Vector3(1.3f, 1.3f, 1f);
                    wingGlows[i] = glow.AddComponent<SpriteRenderer>();
                    wingGlows[i].sprite = PixelArt.Wing;
                    Lighting.MakeGlow(wingGlows[i]);
                }
                var pg = new GameObject("LevelUpPillar");
                pg.transform.SetParent(transform, false);
                wingPillar = pg.AddComponent<SpriteRenderer>();
                wingPillar.sprite = PixelArt.SoftPillar; // 足元が原点、幅1・高さ2ユニット
                Lighting.MakeGlow(wingPillar);
                var pf = new GameObject("LevelUpPillarFront");
                pf.transform.SetParent(transform, false);
                wingPillarFront = pf.AddComponent<SpriteRenderer>();
                wingPillarFront.sprite = PixelArt.SoftPillar;
                Lighting.MakeGlow(wingPillarFront);
                hinges[0].localPosition = new Vector3(-0.22f, 0f, 0f);
                hinges[1].localPosition = new Vector3(0.22f, 0f, 0f);
            }
            wingRoot.gameObject.SetActive(true);
            wingPillar.gameObject.SetActive(true);
            wingPillarFront.gameObject.SetActive(true);
            wingT = 0f;
            featherTimer = 0f;
            wingBurst = false;
            UpdateWings(0f);
        }

        // 羽の動き：畳んだまま足元から頭の上へ上がり → ぶわっと開いて → きらめきながら消える
        public const float WingRiseTime = 0.45f;  // 上がりきるまで
        const float WingHoldTime = 0.14f;         // 上がりきったあと、力をためる（少し縮んで沈む）
        const float WingOpenTime = 0.42f;         // 開いて、行き過ぎて、落ち着くまで
        public const float WingOpenAt = WingRiseTime + WingHoldTime; // 開き始め（「LV UP」もこのあと出る）
        const float WingFadeFrom = 1.15f;         // 消え始める

        /// <summary>羽がぶわっと開いた瞬間（画面を少し揺らす）。</summary>
        public event System.Action WingsOpened;
        const float WingTopHeight = 1.05f + (GameConfig.PlayerScale - 1f);        // 上がりきった高さ（頭の少し上）
        bool wingBurst;

        /// <summary>タイトルの「VIVID」の色（水色・紫・桃・橙・黄）。レベルアップのきらめきに使う。</summary>
        static readonly Color[] VividColors =
        {
            new Color(0.45f, 0.9f, 1f), new Color(0.7f, 0.6f, 1f), new Color(1f, 0.45f, 0.9f), new Color(1f, 0.7f, 0.3f), new Color(1f, 0.88f, 0.4f),
        };

        /// <summary>羽の付け根の、今の高さ（「LV UP」の文字を合わせる）。</summary>
        public float WingHeight { get; private set; }

        void UpdateWings(float dt)
        {
            if (wingT < 0f) return;
            wingT += dt;
            if (wingT >= LevelUpDuration)
            {
                wingT = -1f;
                wingRoot.gameObject.SetActive(false);
                wingPillar.gameObject.SetActive(false);
                wingPillarFront.gameObject.SetActive(false);
                return;
            }
            float r = Mathf.Clamp01(wingT / WingRiseTime);
            r = 1f - (1f - r) * (1f - r); // すっと上がって、頭の上で止まる
            // ため：上がりきったら、ぎゅっと縮んで少し沈む
            float hold = Mathf.Clamp01((wingT - WingRiseTime) / WingHoldTime);
            float squeeze = Mathf.Sin(hold * Mathf.PI * 0.5f) * (wingT < WingOpenAt ? 1f : 0f);
            // 開く：重く押し広げて、行き過ぎてから戻る（減衰するばね）
            float u = Mathf.Max(0f, wingT - WingOpenAt);
            float open = u <= 0f ? 0f : 1f - Mathf.Exp(-u * 7f) * Mathf.Cos(u * 12f);
            float angle = Mathf.LerpUnclamped(80f, -18f, open) + squeeze * 8f; // 畳んだ形から、少し上向きに広げる
            float scale = Mathf.LerpUnclamped(0.4f, 0.8f, open) - squeeze * 0.06f; // キャラクターの大きさに合わせる
            float dip = squeeze * 0.1f;
            float drift = (wingT > WingFadeFrom ? (wingT - WingFadeFrom) * 0.5f : 0f) - dip;
            open = Mathf.Clamp01(open);
            float alpha = Mathf.Clamp01(wingT / 0.1f) * (wingT > WingFadeFrom ? 1f - (wingT - WingFadeFrom) / (LevelUpDuration - WingFadeFrom) : 1f);
            WingHeight = Mathf.Lerp(0.1f, WingTopHeight, r) + drift;

            wingRoot.position = (Vector3)(Iso.ToScreen(Anchor) + new Vector2(0f, WingHeight));
            hinges[0].localRotation = Quaternion.Euler(0f, 0f, angle);
            hinges[0].localScale = new Vector3(scale, scale, 1f);
            hinges[1].localRotation = Quaternion.Euler(0f, 0f, -angle);
            hinges[1].localScale = new Vector3(-scale, scale, 1f);
            int order = SortOrder.For(Anchor) - 1; // キャラクターの後ろ
            // 光の柱：羽の付け根まで伸び、開いたら細くなって消える
            float pk = wingT < WingOpenAt ? 1f : Mathf.Clamp01(1f - (wingT - WingOpenAt) / 0.35f);
            float pillarH = Mathf.Max(0.05f, WingHeight + 0.75f) / 2f; // 上の方は薄れて消えるので、羽より少し高く
            float fadeIn = Mathf.Clamp01(wingT / 0.08f);
            // 後ろ：太く濃いめ／前：細く淡く（キャラクターが光に包まれて見える。足元から伸びるので浮いて見えない）
            wingPillar.transform.position = Iso.ToScreen(Anchor) + new Vector2(0f, -0.35f); // 足元より下から（足先まで光に包む）
            wingPillar.transform.localScale = new Vector3(1.3f * (0.5f + 0.5f * pk), pillarH, 1f);
            // 光の柱はタイトルの「VIVID」の色を順にめぐる（水色 → 紫 → 桃 → 橙 → 黄 → …）
            float hueT = Mathf.Repeat(wingT * 3f, VividColors.Length);
            int hi = Mathf.FloorToInt(hueT);
            var vc = Color.Lerp(VividColors[hi], VividColors[(hi + 1) % VividColors.Length], hueT - hi);
            wingPillar.color = new Color(vc.r, vc.g, vc.b, 0.6f * pk * fadeIn);
            wingPillar.sortingOrder = order - 2;
            wingPillarFront.transform.position = Iso.ToScreen(Anchor) + new Vector2(0f, -0.35f); // 足元より下から（足先まで光に包む）
            wingPillarFront.transform.localScale = new Vector3(0.95f * (0.5f + 0.5f * pk), pillarH, 1f);
            wingPillarFront.color = new Color(Mathf.Lerp(vc.r, 1f, 0.6f), Mathf.Lerp(vc.g, 1f, 0.6f), Mathf.Lerp(vc.b, 1f, 0.6f), 0.3f * pk * fadeIn); // 前は白っぽく
            wingPillarFront.sortingOrder = order + 3;
            float pulse = 0.5f + 0.5f * Mathf.Sin(wingT * 9f);
            for (int i = 0; i < 2; i++)
            {
                wings[i].color = new Color(1f, 1f, 1f, alpha * WingOpacity);
                wings[i].sortingOrder = order;
                wingGlows[i].color = new Color(1f, 0.92f, 0.65f, alpha * open * (0.3f + 0.25f * pulse)); // 開いてから光る
                wingGlows[i].sortingOrder = order - 1;
            }

            var white = new Color(1f, 1f, 0.97f);
            // 開いた瞬間：光の粒がぶわっと広がる
            if (!wingBurst && wingT >= WingOpenAt)
            {
                wingBurst = true;
                WingsOpened?.Invoke();
                Ring(Anchor, 1.6f, new Color(1f, 0.95f, 0.8f, 0.3f), 0.3f); // ごく弱い波紋
                // タイトルの「VIVID」と同じ虹の色で、光の粒がはじける
                foreach (var burst in VividColors) Sparks(Anchor, 4, burst, 2.2f, 0.6f, 1.1f, false, WingHeight);
                Sparks(Anchor, 6, white, 2.2f, 0.9f, 1.5f, true, WingHeight - 0.2f);
            }
            featherTimer -= dt;
            if (featherTimer > 0f) return;
            if (wingT < WingOpenAt)
            {
                // 上がる間：細い光の尾
                featherTimer = 0.05f;
                Sparks(Anchor + UnityEngine.Random.insideUnitCircle * 0.2f, 1, white, 0.2f, 0.4f, 0.3f, true, WingHeight - 0.1f);
            }
            else
            {
                // 開いたあと：羽のまわりがきらきらしながら消えていく
                featherTimer = 0.035f;
                var c = UnityEngine.Random.value < 0.4f ? white : VividColors[UnityEngine.Random.Range(0, VividColors.Length)];
                Sparks(Anchor + new Vector2(UnityEngine.Random.Range(-0.9f, 0.9f), UnityEngine.Random.Range(-0.3f, 0.3f)), 1, c, 0.4f, 0.7f, 0.6f, true,
                    WingHeight + UnityEngine.Random.Range(-0.2f, 0.5f));
            }
        }

        /// <summary>
        /// 扇形の斬撃。論理座標で aim の方向へ、reach の距離、arc 度の広さ。
        /// 子を論理の角度で回し、親で横長に潰すと、床の上の扇に見える（Iso.ToScreen と同じ変換）。
        /// </summary>
        public void Slash(Vector2 pos, Vector2 aim, float reach, float arcDeg, Color color)
        {
            var b = freeSlashes.Count > 0 ? freeSlashes.Pop() : CreateSlash();
            b.Root.gameObject.SetActive(true);
            b.Root.position = Iso.ToScreen(pos);
            float k = reach / 1f; // スプライトの半径は 1 ユニット
            b.Root.localScale = new Vector3(1.414f * k, 0.707f * k, 1f);
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + 45f;
            b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            // 扇のスプライトは 120 度。広さに合わせて縦に伸ばす（大まかな見た目）
            float w = Mathf.Clamp(arcDeg / 120f, 0.5f, 1.6f);
            b.Sr.transform.localScale = new Vector3(1f, w, 1f);
            b.T = 0f;
            b.Duration = 0.16f;
            b.Color = color;
            b.Sr.color = color;
            b.Sr.sortingOrder = 21000;
            slashes.Add(b);
        }

        Burst CreateSlash()
        {
            var root = new GameObject("Slash");
            root.transform.SetParent(transform, false);
            var child = new GameObject("Arc");
            child.transform.SetParent(root.transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.SlashArc;
            Lighting.MakeGlow(sr);
            return new Burst { Sr = sr, Root = root.transform };
        }

        /// <summary>床に広がる閃光（中心が濃い円）。スキルの「放った」瞬間に使う。</summary>
        public void Disc(Vector2 pos, float radius, Color color, float duration = 0.3f)
        {
            Burst b;
            if (freeDiscs.Count > 0) b = freeDiscs.Pop();
            else
            {
                var go = new GameObject("Disc");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.SoftDisc;
                Lighting.MakeGlow(sr);
                b = new Burst { Sr = sr };
            }
            b.Sr.gameObject.SetActive(true);
            b.Sr.transform.position = Iso.ToScreen(pos);
            b.T = 0f;
            b.Duration = duration;
            b.Radius = radius;
            b.Color = color;
            b.Steady = false;
            b.Sr.sortingOrder = 20990;
            discs.Add(b);
            Apply(b);
        }

        /// <summary>ボスの攻撃の予告：その場で脈打つ円（中を薄く塗り、縁をはっきり）。</summary>
        public void Marker(Vector2 pos, float radius, Color color, float duration)
        {
            Disc(pos, radius, new Color(color.r, color.g, color.b, color.a * 0.45f), duration);
            discs[discs.Count - 1].Steady = true;
            Apply(discs[discs.Count - 1]);
            Ring(pos, radius, color, duration);
            active[active.Count - 1].Steady = true;
            Apply(active[active.Count - 1]);
        }

        /// <summary>扇（ボスの扇状の攻撃の予告と、その一撃）。steady なら脈打ち、そうでなければ消えていく。</summary>
        public void Fan(Vector2 pos, Vector2 aim, float range, float arcDeg, Color color, float duration, bool steady)
        {
            var b = freeFans.Count > 0 ? freeFans.Pop() : CreateFan();
            b.Root.gameObject.SetActive(true);
            b.Root.position = Iso.ToScreen(pos);
            b.Root.localScale = new Vector3(1.414f * range, 0.707f * range, 1f);
            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + 45f;
            b.Sr.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            b.Sr.transform.localScale = new Vector3(1f, Mathf.Clamp(arcDeg / 120f, 0.2f, 1.6f), 1f);
            b.T = 0f;
            b.Duration = duration;
            b.Color = color;
            b.Steady = steady;
            b.Sr.color = color;
            b.Sr.sortingOrder = 20995;
            fans.Add(b);
        }

        readonly List<Burst> fans = new List<Burst>();
        readonly Stack<Burst> freeFans = new Stack<Burst>();

        Burst CreateFan()
        {
            var root = new GameObject("Fan");
            root.transform.SetParent(transform, false);
            var child = new GameObject("Shape");
            child.transform.SetParent(root.transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Fan;
            Lighting.MakeGlow(sr);
            return new Burst { Sr = sr, Root = root.transform };
        }

        /// <summary>立ちのぼる光の柱（落ちてくる技の着弾、強化など）。height はユニット。</summary>
        /// <summary>
        /// やわらかい光の柱（レベルアップの柱と同じ絵：中心ほど明るく、上へ行くほど消える）。width は幅（マス）。
        /// </summary>
        public void SoftPillar(Vector2 pos, Color color, float width, float height, float duration = 0.6f)
        {
            Pillar(pos, color, width, height, duration);
            var b = pillars[pillars.Count - 1];
            b.Sr.sprite = PixelArt.SoftPillar;
            b.Soft = true;
            b.Sr.transform.position = Iso.ToScreen(pos) + new Vector2(0f, -0.2f);
            b.Sr.transform.localScale = new Vector3(width, height / 2f, 1f);
        }

        public void Pillar(Vector2 pos, Color color, float width, float height, float duration = 0.4f)
        {
            Burst b;
            if (freePillars.Count > 0) b = freePillars.Pop();
            else
            {
                var go = new GameObject("Pillar");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PixelArt.Beam; // 1×32 ドット（16 ドットで 1 ユニット）、足元が原点
                Lighting.MakeGlow(sr);
                b = new Burst { Sr = sr };
            }
            b.Sr.gameObject.SetActive(true);
            b.Sr.sprite = PixelArt.Beam;
            b.Soft = false;
            b.Sr.transform.position = Iso.ToScreen(pos);
            b.T = 0f;
            b.Duration = duration;
            b.Radius = width;
            b.Color = color;
            b.Sr.transform.localScale = new Vector3(width * 16f, height / 2f, 1f);
            b.Sr.sortingOrder = 21000;
            pillars.Add(b);
        }

        /// <summary>
        /// 火花をまき散らす。speed は床の上を広がる速さ、pop は上へ跳ねる強さ。
        /// rising なら重さなしでふわっと立ちのぼる（強化・回復）。
        /// </summary>
        public void Sparks(Vector2 pos, int count, Color color, float speed, float life, float pop = 2f, bool rising = false, float startHeight = -1f)
        {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++)
            {
                var p = TakeParticle();
                p.Sr.sprite = PixelArt.Spark;
                p.Sr.flipX = false;
                p.Sr.transform.localScale = Vector3.one;
                p.Sr.transform.localRotation = Quaternion.identity;
                p.Custom = false;
                p.Drag = 3f;
                p.Spin = 0f;
                float a = UnityEngine.Random.value * Mathf.PI * 2f;
                float sp = speed * (0.4f + 0.6f * UnityEngine.Random.value);
                p.Pos = pos + (rising ? new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.5f * UnityEngine.Random.value : Vector2.zero);
                p.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (rising ? sp * 0.15f : sp);
                p.H = startHeight >= 0f ? startHeight + UnityEngine.Random.value * 0.15f : rising ? UnityEngine.Random.value * 0.3f : 0.2f; // 高さを決めて出せる（羽のまわり）
                p.Vh = rising ? pop * (0.6f + 0.4f * UnityEngine.Random.value) : pop * (0.5f + UnityEngine.Random.value);
                p.Gravity = rising ? 0f : 9f;
                p.T = 0f;
                p.Life = life * (0.7f + 0.3f * UnityEngine.Random.value);
                p.Color = color;
                p.Sr.sortingOrder = 21000;
                particles.Add(p);
            }
        }

        public void Ring(Vector2 pos, float radius, Color color, float duration = 0.25f)
        {
            var b = free.Count > 0 ? free.Pop() : Create();
            b.Sr.gameObject.SetActive(true);
            b.Sr.transform.position = Iso.ToScreen(pos);
            b.T = 0f;
            b.Duration = duration;
            b.Radius = radius;
            b.Color = color;
            b.Steady = false;
            b.Sr.sortingOrder = 21000;
            active.Add(b);
            Apply(b);
        }

        Burst Create()
        {
            var go = new GameObject("Burst");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.Ring;
            Lighting.MakeGlow(sr);
            return new Burst { Sr = sr };
        }

        public void Tick(float dt)
        {
            UpdateWings(dt);
            for (int i = 0; i < scheduled.Count; i++)
            {
                var (t, a) = scheduled[i];
                t -= dt;
                if (t > 0f) { scheduled[i] = (t, a); continue; }
                scheduled.RemoveAt(i);
                i--;
                a();
            }
            for (int i = fans.Count - 1; i >= 0; i--)
            {
                var b = fans[i];
                b.T += dt;
                if (b.T >= b.Duration) { b.Root.gameObject.SetActive(false); fans.RemoveAt(i); freeFans.Push(b); continue; }
                var c = b.Color;
                c.a = b.Steady ? b.Color.a * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(b.T * 8f))) : b.Color.a * (1f - b.T / b.Duration);
                b.Sr.color = c;
            }
            for (int i = discs.Count - 1; i >= 0; i--)
            {
                var b = discs[i];
                b.T += dt;
                if (b.T >= b.Duration) { b.Sr.gameObject.SetActive(false); discs.RemoveAt(i); freeDiscs.Push(b); continue; }
                Apply(b);
            }
            for (int i = pillars.Count - 1; i >= 0; i--)
            {
                var b = pillars[i];
                b.T += dt;
                if (b.T >= b.Duration) { b.Sr.gameObject.SetActive(false); pillars.RemoveAt(i); freePillars.Push(b); continue; }
                float t = b.T / b.Duration;
                var c = b.Color;
                c.a = b.Color.a * (1f - t);
                b.Sr.color = c;
                var sc = b.Sr.transform.localScale;
                b.Sr.transform.localScale = new Vector3(b.Radius * (b.Soft ? 1f : 16f) * (1f - 0.6f * t), sc.y, 1f); // 細くなりながら消える
            }
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.T += dt;
                if (p.T >= p.Life) { p.Sr.gameObject.SetActive(false); particles.RemoveAt(i); freeParticles.Push(p); continue; }
                p.Pos += p.Vel * dt;
                p.Vel *= 1f - Mathf.Min(1f, p.Drag * dt); // だんだん遅く
                p.Vh -= p.Gravity * dt;
                p.H = Mathf.Max(0f, p.H + p.Vh * dt);
                p.Angle += p.Spin * dt;
                ApplyParticle(p);
            }
            for (int i = slashes.Count - 1; i >= 0; i--)
            {
                var b = slashes[i];
                b.T += dt;
                if (b.T >= b.Duration)
                {
                    b.Root.gameObject.SetActive(false);
                    slashes.RemoveAt(i);
                    freeSlashes.Push(b);
                    continue;
                }
                var c = b.Color;
                c.a = b.Color.a * (1f - b.T / b.Duration);
                b.Sr.color = c;
            }

            for (int i = active.Count - 1; i >= 0; i--)
            {
                var b = active[i];
                b.T += dt;
                if (b.T >= b.Duration)
                {
                    b.Sr.gameObject.SetActive(false);
                    active.RemoveAt(i);
                    free.Push(b);
                    continue;
                }
                Apply(b);
            }
        }

        static void Apply(Burst b)
        {
            float t = Mathf.Clamp01(b.T / b.Duration);
            if (b.Steady)
            {
                float r0 = b.Radius;
                b.Sr.transform.localScale = new Vector3(r0 * 1.414f, r0 * 0.707f, 1f);
                var c0 = b.Color;
                c0.a = b.Color.a * (0.55f + 0.45f * Mathf.Abs(Mathf.Sin(b.T * 8f)));
                b.Sr.color = c0;
                return;
            }
            // スプライトは直径 2 ユニット（32 ドット / 16）
            float r = Mathf.Lerp(0.3f, 1f, Mathf.Sqrt(t)) * b.Radius;
            // 床の上の円は、クォータービューでは横長の楕円に見える
            b.Sr.transform.localScale = new Vector3(r * 1.414f, r * 0.707f, 1f);
            var c = b.Color;
            c.a = b.Color.a * (1f - t);
            b.Sr.color = c;
        }

        public void Clear()
        {
            scheduled.Clear();
            foreach (var b in fans) { b.Root.gameObject.SetActive(false); freeFans.Push(b); }
            fans.Clear();
            foreach (var b in discs) { b.Sr.gameObject.SetActive(false); freeDiscs.Push(b); }
            discs.Clear();
            foreach (var b in pillars) { b.Sr.gameObject.SetActive(false); freePillars.Push(b); }
            pillars.Clear();
            foreach (var p in particles) { p.Sr.gameObject.SetActive(false); freeParticles.Push(p); }
            particles.Clear();
            if (wingPillar != null) { wingPillar.gameObject.SetActive(false); wingPillarFront.gameObject.SetActive(false); }
            if (wingRoot != null) wingRoot.gameObject.SetActive(false);
            wingT = -1f;
            foreach (var b in slashes)
            {
                b.Root.gameObject.SetActive(false);
                freeSlashes.Push(b);
            }
            slashes.Clear();
            foreach (var b in active)
            {
                b.Sr.gameObject.SetActive(false);
                free.Push(b);
            }
            active.Clear();
        }
    }
}
