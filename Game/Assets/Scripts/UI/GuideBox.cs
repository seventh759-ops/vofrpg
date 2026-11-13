using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 案内役の会話欄（画面の下、顔つき）。初めての場面で一度だけ短く説明する。
    /// 表示中はゲームが止まる。タップか Enter／Space／Z で次へ。見た案内は保存され、二度と出ない。
    /// </summary>
    public sealed class GuideBox
    {
        /// <summary>案内役の名前（決まったらここを変える）。</summary>
        public const string Name = "？？？";

        readonly Hud hud;
        readonly Queue<string> lines = new Queue<string>();
        string current;
        float shownAt;

        public GuideBox(Hud hud)
        {
            this.hud = hud;
        }

        public bool Active => current != null;

        /// <summary>まだ見ていない案内なら出す（id ごとに一度だけ）。</summary>
        public void Show(string id, params string[] text)
        {
            if (Profile.TipSeen(id)) return;
            Profile.MarkTip(id);
            foreach (var t in text) lines.Enqueue(t);
            if (current == null) Next();
        }

        /// <summary>何度でも話す（1層の案内役に近づいたとき）。話している途中なら何もしない。</summary>
        public void Say(params string[] text)
        {
            if (current != null) return;
            foreach (var t in text) lines.Enqueue(t);
            Next();
        }

        public void Next()
        {
            current = lines.Count > 0 ? lines.Dequeue() : null;
            shownAt = Time.unscaledTime;
        }

        public void Clear()
        {
            lines.Clear();
            current = null;
        }

        public void Draw()
        {
            if (current == null) return;
            float W = Screen.width, H = Screen.height;
            float pad = W * 0.04f;
            float boxH = H * 0.16f;
            var box = new Rect(pad, H * 0.58f, W - pad * 2f, boxH);

            // 画面のどこを押しても次へ（後ろのボタンを押してしまわないように、全体をふさぐ）
            hud.Button(new Rect(0, 0, W, H), null, new Color(0f, 0f, 0f, 0.3f), hud.CenteredStyle, () =>
            {
                if (Time.unscaledTime - shownAt > 0.15f) Next();
            });
            hud.Button(box, null, new Color(0.05f, 0.04f, 0.08f, 0.92f), hud.CenteredStyle, () =>
            {
                if (Time.unscaledTime - shownAt > 0.15f) Next(); // 開いた瞬間の誤タップを防ぐ
            });
            Hud.Fill(new Rect(box.x, box.y, box.width, H * 0.004f), new Color(1f, 0.85f, 0.45f, 0.8f));

            float face = boxH * 0.8f;
            Hud.DrawSprite(new Rect(box.x + boxH * 0.1f, box.y + boxH * 0.1f, face, face), PixelArt.Guide);

            float tx = box.x + face + boxH * 0.25f;
            float tw = box.xMax - tx - pad * 0.5f;
            var name = new GUIStyle(hud.SmallStyle) { fontStyle = FontStyle.Bold };
            name.normal.textColor = new Color(1f, 0.85f, 0.45f);
            GUI.Label(new Rect(tx, box.y + boxH * 0.08f, tw, hud.SmallStyle.fontSize * 1.5f), Name, name);
            var text = new GUIStyle(hud.LabelStyle) { wordWrap = true };
            GUI.Label(new Rect(tx, box.y + boxH * 0.08f + hud.SmallStyle.fontSize * 1.6f, tw, boxH * 0.7f), current, text);

            var more = new GUIStyle(hud.SmallStyle) { alignment = TextAnchor.LowerRight };
            more.normal.textColor = new Color(1f, 1f, 1f, 0.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f));
            GUI.Label(new Rect(box.x, box.y, box.width - pad * 0.5f, boxH - boxH * 0.06f), lines.Count > 0 ? ">>" : "OK", more);
        }
    }
}
