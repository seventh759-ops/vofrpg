using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 起動の画面。ブランドロゴ「TOMOSHIBI WORKS」（ランタンに火がともる）を見せている間に、裏で重い準備を進める：
    /// 曲（1曲ずつ順番に）→ タイトル・町の背景の絵 → ゲーム本体（GameWorld）。終わったら、ふっと消える。
    /// </summary>
    public sealed class Loader : MonoBehaviour
    {
        float progress;      // 0〜1
        float shown;         // 表示しているバー（なめらかに追いかける）
        float fadeOut = -1f; // 消えていく途中（経過時間）
        const float FadeTime = 0.35f;
        Texture2D logo;
        float brandT;         // ブランドロゴを出してからの時間
        bool loaded;          // 準備が終わった
        const float BrandMin = 2.6f; // ブランドロゴを最低これだけ見せる（準備が早く終わっても）

        /// <summary>起動のロゴを出している間（ゲームには入力を渡さない）。</summary>
        public static bool Active { get; private set; }

        void OnDestroy() => Active = false;

        void Start()
        {
            Active = true;
            Application.targetFrameRate = 60;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            yield return null; // まず真っ暗な画面を1回出す
            logo = TitleArt.Logo;
            progress = 0.05f;
            yield return null;

            // 曲：1曲ずつ順番に作る（計算は裏で動かして、その間もロゴの火はゆらめき続ける）
            var songs = Music.StartupSongs();
            for (int k = 0; k < songs.Count; k++)
            {
                var task = Task.Run(songs[k].work);
                while (!task.IsCompleted) yield return null;
                if (task.Status == TaskStatus.RanToCompletion) Music.StoreRendered(songs[k].name, task.Result);
                progress = 0.05f + 0.6f * (k + 1) / songs.Count;
                yield return null;
            }
            progress = 0.65f;
            yield return null;

            // 背景の絵（タイトル・町）
            var t1 = Profile.AbyssUnlocked ? TitleScene.AbyssTexture : TitleScene.Texture;
            progress = 0.75f;
            yield return null;
            var t2 = Profile.AbyssUnlocked ? TownScene.AbyssTexture : TownScene.Texture;
            progress = 0.85f;
            yield return null;

            // ゲーム本体
            new GameObject("GameWorld").AddComponent<GameWorld>();
            progress = 1f;
            loaded = true;
            yield return null;
            while (shown < 0.99f || brandT < BrandMin) yield return null; // ロゴを見せきるまで（タップで飛ばせる）
            fadeOut = 0f;
            while (fadeOut < FadeTime)
            {
                fadeOut += Time.unscaledDeltaTime;
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnGUI()
        {
            GUI.depth = -1000; // ゲームの画面より手前
            if (Event.current.type == EventType.Repaint)
            {
                shown = Mathf.MoveTowards(shown, progress, Time.unscaledDeltaTime * 2.5f);
                brandT += Time.unscaledDeltaTime;
            }
            if (loaded && Event.current.type == EventType.MouseDown) brandT = BrandMin; // 準備が終わっていれば、タップで飛ばせる
            float a = fadeOut < 0f ? 1f : 1f - Mathf.Clamp01(fadeOut / FadeTime);
            float W = Screen.width, H = Screen.height;
            var old = GUI.color;
            Hud.Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.02f, 0.04f, a));
            // ブランドロゴ：ランタンがふわっと現れ、少しして中に火がともり、ゆらめく。下に「TOMOSHIBI WORKS」
            float appear = Mathf.Clamp01(brandT / 0.6f) * a;
            var lantern = PixelArt.BrandLantern.texture;
            float sc = Mathf.Max(2f, Mathf.Floor(Mathf.Min(W, H) * 0.16f / lantern.height));
            var lr = new Rect(Mathf.Round((W - lantern.width * sc) * 0.5f), Mathf.Round(H * 0.42f - lantern.height * sc), lantern.width * sc, lantern.height * sc);
            float lit = Mathf.Clamp01((brandT - 0.7f) / 0.4f);                                   // 火がともる
            float flick = 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 9f) * Mathf.Sin(Time.unscaledTime * 3.7f + 1f);
            if (lit > 0f)
            {
                GUI.color = new Color(1f, 0.7f, 0.3f, 0.55f * lit * flick * a);
                float g = lr.width * 2.6f;
                GUI.DrawTexture(new Rect(lr.center.x - g * 0.5f, lr.center.y - g * 0.5f, g, g), TitleArt.SoftGlow); // 灯りのにじみ
            }
            GUI.color = new Color(1f, 1f, 1f, appear);
            GUI.DrawTexture(lr, lantern);
            if (lit > 0f)
            {
                var flame = PixelArt.Flame.texture;
                float fs = sc * (0.75f + 0.1f * flick); // ガラスの中に収まる大きさ
                var fr = new Rect(lr.center.x - flame.width * fs * 0.5f, lr.y + lr.height * 0.6f - flame.height * fs * 0.5f, flame.width * fs, flame.height * fs);
                GUI.color = new Color(1f, 1f, 1f, lit * a);
                GUI.DrawTexture(fr, flame);
            }
            GUI.color = old;
            float textA = Mathf.Clamp01((brandT - 0.9f) / 0.5f) * a;
            NumberFont.DrawIn(new Rect(0, lr.yMax + H * 0.025f, W, H * 0.032f), "TOMOSHIBI WORKS", new Color(1f, 0.93f, 0.8f, textA));

            // 準備がまだ終わっていなければ、下に小さく進み具合
            if (!loaded || shown < 0.99f)
            {
                float bw = W * 0.4f, bh = Mathf.Max(4f, H * 0.004f);
                var bar = new Rect((W - bw) * 0.5f, H * 0.85f, bw, bh);
                Hud.Fill(bar, new Color(0.08f, 0.075f, 0.11f, a));
                Hud.Fill(new Rect(bar.x, bar.y, bar.width * shown, bar.height), new Color(1f, 0.75f, 0.35f, 0.8f * a));
                float blink = 0.4f + 0.4f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f));
                NumberFont.DrawIn(new Rect(0, bar.yMax + H * 0.008f, W, H * 0.016f), "NOW LOADING", new Color(1f, 1f, 1f, a * blink));
            }
        }
    }
}
