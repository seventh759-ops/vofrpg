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
            public float T;
            public float Duration;
            public float Radius;
            public Color Color;
        }

        readonly List<Burst> active = new List<Burst>();
        readonly Stack<Burst> free = new Stack<Burst>();

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
            return new Burst { Sr = sr };
        }

        public void Tick(float dt)
        {
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
            foreach (var b in active)
            {
                b.Sr.gameObject.SetActive(false);
                free.Push(b);
            }
            active.Clear();
        }
    }
}
