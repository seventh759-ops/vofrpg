using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace VividOfFade
{
    /// <summary>
    /// タッチ・マウス・キーボード・ゲームパッドの入力をまとめる。
    /// スマホ：押した場所を中心にした仮想スティックで移動。短くはじいて離すと回避。
    /// 毎フレーム GameWorld から Poll() を呼ぶ（座標はすべて画面のピクセル、左下が原点）。
    /// </summary>
    public sealed class PlayerInputReader
    {
        sealed class Pointer
        {
            public Vector2 Start;
            public Vector2 Pos;
            public float StartTime;
            public bool Reserved; // ボタン類の上で押された
            public bool Attack;   // 攻撃ボタンの上で押された
        }

        const int NoId = int.MinValue;
        const int MouseId = -1;
        const float FlickMaxTime = 0.25f;
        const float TapMaxTime = 0.3f;

        public Vector2 Move { get; private set; }
        public bool AttackHeld { get; private set; }
        public bool ToggleModePressed { get; private set; }
        public bool ConfirmPressed { get; private set; }
        public bool BagPressed { get; private set; }
        public bool CancelPressed { get; private set; }
        public bool StatsPressed { get; private set; }
        public bool SkillsPressed { get; private set; }
        public bool MusicPressed { get; private set; }
        public bool HelpPressed { get; private set; }
        public bool NextMusicPressed { get; private set; }
        /// <summary>数字キー 1〜3 を押したときの番号（0 始まり）。押していなければ -1。</summary>
        public int NumberPressed { get; private set; } = -1;

        public bool StickActive => stickId != NoId;
        public Vector2 StickOrigin { get; private set; }
        public Vector2 StickKnob { get; private set; }
        public float StickRadius { get; private set; }

        /// <summary>この位置で押されたらスティックにしない（ボタン類）。</summary>
        public Func<Vector2, bool> IsReserved = _ => false;
        /// <summary>この位置で押されたら攻撃ボタンとして扱う。</summary>
        public Func<Vector2, bool> IsAttackArea = _ => false;
        /// <summary>短く触って離した。引数は押し始めた位置。</summary>
        public event Action<Vector2> Tapped;

        readonly Dictionary<int, Pointer> pointers = new Dictionary<int, Pointer>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> released = new List<int>();
        int stickId = NoId;
        bool dodgeQueued;
        Vector2 dodgeDir;

        public bool ConsumeDodge(out Vector2 dir)
        {
            dir = dodgeDir;
            if (!dodgeQueued) return false;
            dodgeQueued = false;
            return true;
        }

        public void ClearQueued()
        {
            dodgeQueued = false;
        }

        public void Poll()
        {
            float now = Time.unscaledTime;
            StickRadius = Screen.dpi > 0 ? Screen.dpi * 0.4f : Screen.height * 0.06f;

            // --- 画面に触れている指（なければマウスの左ボタン）を集める ---
            seen.Clear();
            bool anyTouch = false;
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                foreach (TouchControl t in touchscreen.touches)
                {
                    if (!t.press.isPressed) continue;
                    anyTouch = true;
                    Track(t.touchId.ReadValue(), t.position.ReadValue(), now);
                }
            }
            var mouse = Mouse.current;
            if (!anyTouch && mouse != null && mouse.leftButton.isPressed)
            {
                Track(MouseId, mouse.position.ReadValue(), now);
            }

            released.Clear();
            foreach (var kv in pointers)
            {
                if (!seen.Contains(kv.Key)) released.Add(kv.Key);
            }
            foreach (int id in released)
            {
                Release(id, pointers[id], now);
                pointers.Remove(id);
            }

            // --- 仮想スティック ---
            Vector2 move = Vector2.zero;
            bool attack = false;
            foreach (var p in pointers.Values)
            {
                if (p.Attack) attack = true;
            }
            if (StickActive && pointers.TryGetValue(stickId, out var sp))
            {
                Vector2 d = sp.Pos - sp.Start;
                if (d.magnitude > StickRadius * 0.15f)
                {
                    move = Vector2.ClampMagnitude(d / StickRadius, 1f);
                }
                StickOrigin = sp.Start;
                StickKnob = sp.Start + Vector2.ClampMagnitude(d, StickRadius);
            }

            // --- キーボード（PC・エディタ） ---
            ToggleModePressed = false;
            ConfirmPressed = false;
            BagPressed = false;
            CancelPressed = false;
            StatsPressed = false;
            SkillsPressed = false;
            MusicPressed = false;
            HelpPressed = false;
            NextMusicPressed = false;
            NumberPressed = -1;
            var kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (k != Vector2.zero) move = k.normalized;

                if (kb.spaceKey.wasPressedThisFrame)
                {
                    dodgeQueued = true;
                    dodgeDir = move;
                }
                if (kb.jKey.isPressed || kb.zKey.isPressed) attack = true;
                ToggleModePressed = kb.tKey.wasPressedThisFrame;
                ConfirmPressed = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
                BagPressed = kb.bKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
                CancelPressed = kb.escapeKey.wasPressedThisFrame;
                StatsPressed = kb.cKey.wasPressedThisFrame;
                SkillsPressed = kb.kKey.wasPressedThisFrame;
                MusicPressed = kb.mKey.wasPressedThisFrame;
                HelpPressed = kb.hKey.wasPressedThisFrame;
                NextMusicPressed = kb.nKey.wasPressedThisFrame;
                if (kb.digit1Key.wasPressedThisFrame) NumberPressed = 0;
                else if (kb.digit2Key.wasPressedThisFrame) NumberPressed = 1;
                else if (kb.digit3Key.wasPressedThisFrame) NumberPressed = 2;
            }
            if (mouse != null && mouse.rightButton.isPressed) attack = true;

            // --- ゲームパッド ---
            var pad = Gamepad.current;
            if (pad != null)
            {
                Vector2 s = pad.leftStick.ReadValue();
                if (s.magnitude > 0.2f) move = Vector2.ClampMagnitude(s, 1f);
                if (pad.buttonSouth.wasPressedThisFrame)
                {
                    dodgeQueued = true;
                    dodgeDir = move;
                }
                if (pad.buttonWest.isPressed || pad.rightTrigger.isPressed) attack = true;
                if (pad.buttonNorth.wasPressedThisFrame) ToggleModePressed = true;
                if (pad.startButton.wasPressedThisFrame) ConfirmPressed = true;
                if (pad.selectButton.wasPressedThisFrame) BagPressed = true;
                if (pad.buttonEast.wasPressedThisFrame) CancelPressed = true;
            }

            Move = move;
            AttackHeld = attack;
        }

        void Track(int id, Vector2 pos, float now)
        {
            seen.Add(id);
            if (!pointers.TryGetValue(id, out var p))
            {
                p = new Pointer
                {
                    Start = pos,
                    Pos = pos,
                    StartTime = now,
                    Reserved = IsReserved(pos),
                    Attack = IsAttackArea(pos),
                };
                pointers[id] = p;
                if (!p.Reserved && !p.Attack && stickId == NoId) stickId = id;
            }
            p.Pos = pos;
        }

        void Release(int id, Pointer p, float now)
        {
            float duration = now - p.StartTime;
            Vector2 d = p.Pos - p.Start;
            float dist = d.magnitude;

            if (id == stickId)
            {
                stickId = NoId;
                // 短くはじいて離した → その方向へ回避
                if (duration < FlickMaxTime && dist > StickRadius * 0.5f)
                {
                    dodgeQueued = true;
                    dodgeDir = d / dist;
                    return;
                }
            }

            // ボタンの上で押した指は、ゆっくり離してもタップとして扱う
            bool tap = p.Reserved ? dist < StickRadius * 0.5f : duration < TapMaxTime && dist < StickRadius * 0.3f;
            if (tap) Tapped?.Invoke(p.Start);
        }
    }
}
