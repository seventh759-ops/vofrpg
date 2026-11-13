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
        }
        readonly List<Particle> particles = new List<Particle>();
        readonly Stack<Particle> freeParticles = new Stack<Particle>();
        const int MaxParticles = 400;

        // レベルアップの羽（頭の上で開いて、上がりながら消える）
        public const float LevelUpDuration = 1.5f;
        const float WingOpacity = 0.7f; // 羽は半透明（後ろのキャラクターや文字が透けて見える）
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
                }
                hinges[0].localPosition = new Vector3(-0.22f, 0f, 0f);
                hinges[1].localPosition = new Vector3(0.22f, 0f, 0f);
            }
            wingRoot.gameObject.SetActive(true);
            wingT = 0f;
            UpdateWings(0f);
        }

        void UpdateWings(float dt)
        {
            if (wingT < 0f) return;
            wingT += dt;
            if (wingT >= LevelUpDuration)
            {
                wingT = -1f;
                wingRoot.gameObject.SetActive(false);
                return;
            }
            const float openTime = 0.35f, fadeFrom = 1f;
            float open = Mathf.Clamp01(wingT / openTime);
            open = 1f - (1f - open) * (1f - open) * (1f - open); // 勢いよく開いて、ゆっくり止まる
            float flap = wingT > openTime ? Mathf.Sin((wingT - openTime) * 14f) * 8f * Mathf.Clamp01(1f - (wingT - openTime) * 1.2f) : 0f;
            float angle = Mathf.Lerp(75f, -18f, open) + flap; // 下に畳んだ形から、少し上向きに広げる
            float scale = Mathf.Lerp(0.5f, 0.95f, open);
            float rise = wingT > fadeFrom ? (wingT - fadeFrom) * 0.8f : 0f;
            float alpha = wingT > fadeFrom ? 1f - (wingT - fadeFrom) / (LevelUpDuration - fadeFrom) : 1f;

            wingRoot.position = (Vector3)(Iso.ToScreen(Anchor) + new Vector2(0f, 0.75f + rise));
            hinges[0].localRotation = Quaternion.Euler(0f, 0f, angle);
            hinges[0].localScale = new Vector3(scale, scale, 1f);
            hinges[1].localRotation = Quaternion.Euler(0f, 0f, -angle);
            hinges[1].localScale = new Vector3(-scale, scale, 1f);
            int order = SortOrder.For(Anchor) - 1; // キャラクターの後ろ
            for (int i = 0; i < 2; i++)
            {
                wings[i].color = new Color(1f, 1f, 1f, alpha * WingOpacity);
                wings[i].sortingOrder = order;
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
        public void Sparks(Vector2 pos, int count, Color color, float speed, float life, float pop = 2f, bool rising = false)
        {
            for (int i = 0; i < count && particles.Count < MaxParticles; i++)
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
                float a = UnityEngine.Random.value * Mathf.PI * 2f;
                float sp = speed * (0.4f + 0.6f * UnityEngine.Random.value);
                p.Pos = pos + (rising ? new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.5f * UnityEngine.Random.value : Vector2.zero);
                p.Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (rising ? sp * 0.15f : sp);
                p.H = rising ? UnityEngine.Random.value * 0.3f : 0.2f;
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
                b.Sr.transform.localScale = new Vector3(b.Radius * 16f * (1f - 0.6f * t), sc.y, 1f); // 細くなりながら消える
            }
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.T += dt;
                if (p.T >= p.Life) { p.Sr.gameObject.SetActive(false); particles.RemoveAt(i); freeParticles.Push(p); continue; }
                p.Pos += p.Vel * dt;
                p.Vel *= 1f - Mathf.Min(1f, 3f * dt); // だんだん遅く
                p.Vh -= p.Gravity * dt;
                p.H = Mathf.Max(0f, p.H + p.Vh * dt);
                p.Sr.transform.position = (Vector3)(Iso.ToScreen(p.Pos) + new Vector2(0f, p.H));
                var c = p.Color;
                c.a = p.Color.a * (1f - p.T / p.Life);
                p.Sr.color = c;
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
            foreach (var b in fans) { b.Root.gameObject.SetActive(false); freeFans.Push(b); }
            fans.Clear();
            foreach (var b in discs) { b.Sr.gameObject.SetActive(false); freeDiscs.Push(b); }
            discs.Clear();
            foreach (var b in pillars) { b.Sr.gameObject.SetActive(false); freePillars.Push(b); }
            pillars.Clear();
            foreach (var p in particles) { p.Sr.gameObject.SetActive(false); freeParticles.Push(p); }
            particles.Clear();
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
