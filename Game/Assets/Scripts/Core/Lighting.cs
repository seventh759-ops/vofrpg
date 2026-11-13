using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VividOfFade
{
    /// <summary>
    /// 明るさと暗闇。部屋全体（シーンの Global Light 2D）を暗くし、プレイヤーのランタンの光だけで照らす。
    /// 光の広さは「明るさ」（論理座標のマス）に合わせる。
    /// 火の玉・斬撃・ドロップなどは、暗闇でも見えるよう光の影響を受けない素材（Unlit）で描く。
    /// </summary>
    public sealed class Lighting : MonoBehaviour
    {
        const float AmbientIntensity = 0.22f;
        static readonly Color AmbientColor = new Color(0.55f, 0.6f, 0.85f);
        static readonly Color LanternColor = new Color(1f, 0.85f, 0.6f);

        static Material unlit;

        /// <summary>光の影響を受けない素材。見つからなければ null（そのときは普通の素材のまま）。</summary>
        public static Material Unlit
        {
            get
            {
                if (unlit != null) return unlit;
                // ビルドでシェーダーが削られないよう、Assets/Resources/GlowMaterial（Sprite-Unlit-Default を使うマテリアル）を先に探す
                unlit = Resources.Load<Material>("GlowMaterial");
                if (unlit != null) return unlit;
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader != null) unlit = new Material(shader) { name = "SpriteUnlit (runtime)" };
                return unlit;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => unlit = null;

        /// <summary>暗闇でも光って見せたい SpriteRenderer に使う。</summary>
        public static void MakeGlow(SpriteRenderer sr)
        {
            var m = Unlit;
            if (m != null) sr.sharedMaterial = m;
        }

        Light2D lantern;
        Light2D global;
        float radius;

        public void Init()
        {
            // シーンにある全体の光を暗くする（なければ作る）
            global = null;
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            {
                if (l.lightType == Light2D.LightType.Global) global = l;
            }
            if (global == null)
            {
                var go = new GameObject("Global Light 2D");
                global = go.AddComponent<Light2D>();
                global.lightType = Light2D.LightType.Global;
            }
            global.intensity = AmbientIntensity;
            global.color = AmbientColor;

            var lg = new GameObject("Lantern Light");
            lg.transform.SetParent(transform, false);
            lantern = lg.AddComponent<Light2D>();
            lantern.lightType = Light2D.LightType.Point;
            lantern.color = LanternColor;
            lantern.intensity = 1.25f;
            lantern.falloffIntensity = 0.6f;
        }

        /// <summary>区画に合わせて、全体の光の色と明るさを変える。</summary>
        public void SetAmbient(Color color, float intensity)
        {
            if (global == null) return;
            global.color = color;
            global.intensity = intensity;
        }

        /// <summary>light は論理座標のマス。光の円はクォータービューの見た目に合わせて少し大きめにする。</summary>
        public void Tick(Vector2 playerLogical, float light, float dt)
        {
            if (lantern == null) return;
            float target = light * 1.2f + 0.6f;
            radius = radius <= 0f ? target : Mathf.Lerp(radius, target, 1f - Mathf.Exp(-6f * dt));
            lantern.pointLightOuterRadius = radius;
            lantern.pointLightInnerRadius = radius * 0.45f;
            // ランタンらしく、少しだけ揺らぐ
            lantern.intensity = 1.2f + Mathf.Sin(Time.time * 7f) * 0.04f + Mathf.Sin(Time.time * 13f) * 0.03f;
            Vector2 p = Iso.ToScreen(playerLogical);
            lantern.transform.position = new Vector3(p.x, p.y + 0.3f, 0f);
        }
    }
}
