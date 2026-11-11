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
        }

        readonly List<Burst> active = new List<Burst>();
        readonly Stack<Burst> free = new Stack<Burst>();
        readonly List<Burst> slashes = new List<Burst>();
        readonly Stack<Burst> freeSlashes = new Stack<Burst>();

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
                    go.transform.localPosition = new Vector3(-0.5f, 0.05f, 0f); // 付け根を蝶番に合わせる（羽は幅1ユニット）
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
            float scale = Mathf.Lerp(0.6f, 1.2f, open);
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

        public void Ring(Vector2 pos, float radius, Color color, float duration = 0.25f)
        {
            var b = free.Count > 0 ? free.Pop() : Create();
            b.Sr.gameObject.SetActive(true);
            b.Sr.transform.position = Iso.ToScreen(pos);
            b.T = 0f;
            b.Duration = duration;
            b.Radius = radius;
            b.Color = color;
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
