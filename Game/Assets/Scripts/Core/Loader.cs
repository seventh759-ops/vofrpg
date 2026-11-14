using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 起動の読み込み画面。ロゴと「NOW LOADING」と進み具合のバーを出しながら、重い準備を少しずつ進める：
    /// 曲（別のスレッドでまとめて作る）→ タイトル・町の背景の絵 → ゲーム本体（GameWorld）。終わったら、ふっと消える。
    /// </summary>
    public sealed class Loader : MonoBehaviour
    {
        float progress;      // 0〜1
        float shown;         // 表示しているバー（なめらかに追いかける）
        float fadeOut = -1f; // 消えていく途中（経過時間）
        const float FadeTime = 0.35f;
        Texture2D logo;

        void Start()
        {
            Application.targetFrameRate = 60;
            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            yield return null; // まず真っ暗な画面を1回出す
            logo = TitleArt.Logo;
            progress = 0.05f;
            yield return null;

            // 曲：計算だけ別のスレッドでまとめて動かし、終わったものから曲にする
            var songs = Music.StartupSongs();
            var tasks = new List<Task<float[]>>();
            foreach (var s in songs) tasks.Add(Task.Run(s.work));
            int done = 0;
            while (done < tasks.Count)
            {
                done = 0;
                foreach (var t in tasks) if (t.IsCompleted) done++;
                progress = 0.05f + 0.55f * done / Mathf.Max(1, tasks.Count);
                yield return null;
            }
            for (int i = 0; i < songs.Count; i++)
            {
                if (tasks[i].Status == TaskStatus.RanToCompletion) Music.StoreRendered(songs[i].name, tasks[i].Result);
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
            yield return null;
            while (shown < 0.99f) yield return null;
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
            if (Event.current.type == EventType.Repaint) shown = Mathf.MoveTowards(shown, progress, Time.unscaledDeltaTime * 2.5f);
            float a = fadeOut < 0f ? 1f : 1f - Mathf.Clamp01(fadeOut / FadeTime);
            float W = Screen.width, H = Screen.height;
            var old = GUI.color;
            Hud.Fill(new Rect(0, 0, W, H), new Color(0.02f, 0.02f, 0.04f, a));
            if (logo != null)
            {
                float sc = Mathf.Max(1f, Mathf.Floor(W * 0.7f / logo.width));
                var lr = new Rect((W - logo.width * sc) * 0.5f, H * 0.4f - logo.height * sc, logo.width * sc, logo.height * sc);
                GUI.color = new Color(1f, 1f, 1f, a * (0.6f + 0.25f * Mathf.Sin(Time.unscaledTime * 2f)));
                GUI.DrawTexture(new Rect(lr.x - sc * 2f, lr.y - sc * 2f, lr.width + sc * 4f, lr.height + sc * 4f), TitleArt.Glow);
                GUI.color = new Color(1f, 1f, 1f, a);
                GUI.DrawTexture(lr, logo);
                GUI.color = old;
            }
            float bw = W * 0.6f, bh = Mathf.Max(6f, H * 0.008f);
            var bar = new Rect((W - bw) * 0.5f, H * 0.5f, bw, bh);
            Hud.Fill(bar, new Color(0.08f, 0.075f, 0.11f, a));
            Hud.Fill(new Rect(bar.x, bar.y, bar.width * shown, bar.height), new Color(0.2f, 0.9f, 0.72f, a));
            Hud.Fill(new Rect(bar.x, bar.y, bar.width * shown, bar.height * 0.4f), new Color(0.85f, 1f, 0.95f, 0.4f * a));
            var fc = Hud.FrameGold; fc.a = a;
            Hud.OrnateFrame(bar, fc, Mathf.Max(2f, Mathf.Round(H * 0.0012f)), true, false);
            float blink = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f));
            NumberFont.DrawIn(new Rect(0, bar.yMax + H * 0.012f, W, H * 0.025f), "NOW LOADING", new Color(1f, 1f, 1f, a * blink));
        }
    }
}
