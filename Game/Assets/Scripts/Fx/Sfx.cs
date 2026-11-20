using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 仮の効果音。音のファイルを使わず、起動時に簡単な波形を作る。本番の音ができたら差し替える。
    /// </summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int Rate = 44100;

        AudioSource source;
        AudioClip hit, pickup, ember, chime, fanfare, relic, boom, fountain, anvil, skillUp, levelUp;
        float lastHit;
        float lastEmber;

        void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;

            hit = Make("hit", new[] { 660f }, 0.035f, 0.25f, Wave.Square);
            pickup = Make("pickup", new[] { 880f, 1320f }, 0.05f, 0.3f, Wave.Square);
            ember = Make("ember", new[] { 1760f }, 0.03f, 0.15f, Wave.Sine);
            chime = Make("chime", new[] { 1046f, 1318f, 1568f }, 0.07f, 0.35f, Wave.Sine);
            fanfare = Make("fanfare", new[] { 784f, 988f, 1175f, 1568f }, 0.09f, 0.4f, Wave.Square);
            relic = Make("relic", new[] { 523f, 659f, 784f, 1046f, 1318f, 1568f, 2093f }, 0.08f, 0.4f, Wave.Sine);
            boom = Make("boom", new[] { 90f }, 0.18f, 0.5f, Wave.Noise);
            fountain = Make("fountain", new[] { 523f, 784f, 1046f }, 0.05f, 0.3f, Wave.Square);
            anvil = MakeAnvil();
            levelUp = MakeTeTee();
            skillUp = Make("skillUp", new[] { 1175f, 1568f, 2093f, 2637f, 3136f }, 0.045f, 0.28f, Wave.Sine); // きらりと駆け上がる（キャラのレベルアップとは別の音）
        }

        // ---- 同時に鳴らす数の上限 ----
        // 一度にたくさん倒すと、当たり・火種・爆発の音が何十も重なって割れるので、
        // 少し前（Window 秒）に鳴らし始めた数を数えて、多すぎるときは鳴らさない。
        // レベルアップ・装備が落ちた音など、聞き逃したくない音（important）は数えるだけで、いつも鳴らす。
        const float Window = 0.25f;     // この間に鳴り始めた音を「鳴っている」とみなす
        const int MaxVoices = 12;       // 全体でこの数まで
        const int MaxSame = 5;          // 同じ音はこの数まで
        const float SameGap = 0.04f;    // 同じ音は、この間隔より詰めて鳴らさない
        readonly System.Collections.Generic.List<(AudioClip clip, float at)> recent = new System.Collections.Generic.List<(AudioClip, float)>();

        /// <summary>効果音の音量の段階（0 はなし、1〜5）。5 が今までと同じ大きさ（Unity は 1 より大きくできないので、5 がいちばん大きい）。</summary>
        static readonly float[] Volumes = { 0f, 0.25f, 0.45f, 0.65f, 0.85f, 1f };

        /// <summary>効果音の音量を次の段階へ（なし → 1 → … → 5 → なし）。保存して、確かめの音を鳴らす。</summary>
        public void NextVolume()
        {
            Profile.SfxVolumeSetting = (Profile.SfxVolumeSetting + 1) % Volumes.Length;
            Play(pickup, 1f, true);
        }

        /// <summary>設定の画面に出す、今の音量（「オフ」か「■■■□□」）。</summary>
        public string VolumeLabel
        {
            get
            {
                int v = Profile.SfxVolumeSetting;
                if (v == 0) return Loc.T("OFF", "オフ");
                return new string('■', v) + new string('□', Volumes.Length - 1 - v);
            }
        }

        void Play(AudioClip clip, float volume = 1f, bool important = false)
        {
            if (clip == null) return;
            float master = Volumes[Mathf.Clamp(Profile.SfxVolumeSetting, 0, Volumes.Length - 1)];
            if (master <= 0f) return;
            volume *= master;
            float now = Time.unscaledTime;
            recent.RemoveAll(r => now - r.at > Window);
            if (!important)
            {
                int same = 0;
                float lastSame = -1f;
                foreach (var r in recent)
                {
                    if (r.clip != clip) continue;
                    same++;
                    lastSame = Mathf.Max(lastSame, r.at);
                }
                if (recent.Count >= MaxVoices || same >= MaxSame || (lastSame >= 0f && now - lastSame < SameGap)) return;
                // 数で抑えているので、音量は下げない（重なっても1つずつの大きさはそのまま）
            }
            recent.Add((clip, now));
            source.PlayOneShot(clip, volume);
        }

        public void Hit()
        {
            if (Time.unscaledTime - lastHit < 0.045f) return;
            lastHit = Time.unscaledTime;
            Play(hit, 0.6f);
        }

        public void Ember()
        {
            if (Time.unscaledTime - lastEmber < 0.03f) return;
            lastEmber = Time.unscaledTime;
            Play(ember, 0.6f);
        }

        public void Pickup() => Play(pickup);
        public void SkillUp() => Play(skillUp, 0.9f, true);
        /// <summary>レベルアップ：ほかの効果音と同じ電子音で「てててーん」（装備が落ちたときの音とは別）。</summary>
        public void LevelUp() => Play(levelUp, 1f, true);

        /// <summary>
        /// 「てててーん」：ほかの効果音（Make）と同じ作り方（矩形波の1音ずつ、鳴った瞬間がいちばん大きく、すっと減る）。
        /// ド・ド・ド→高いソ（ミを使わないので、どの区画の曲ともぶつかりにくい）。
        /// 0 Hz は休み。
        /// </summary>
        static AudioClip MakeTeTee()
        {
            float[] notes = { 1046.5f, 1046.5f, 1046.5f, 1568f };
            float[] lengths = { 0.07f, 0.07f, 0.07f, 0.32f };
            int total = 0;
            foreach (var l in lengths) total += (int)(Rate * l);
            var data = new float[total];
            int at = 0;
            for (int n = 0; n < notes.Length; n++)
            {
                int per = (int)(Rate * lengths[n]);
                if (notes[n] > 0f)
                {
                    for (int i = 0; i < per; i++)
                    {
                        float t = (float)i / Rate;
                        float env = 1f - (float)i / per;
                        data[at + i] = Pulse(notes[n] * t, 0.5f) * env * env * 0.35f;
                    }
                }
                at += per;
            }
            var clip = AudioClip.Create("tetee", data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Pulse(float phase, float duty) => Mathf.Repeat(phase, 1f) < duty ? 1f : -1f;
        public void Boom() => Play(boom, 0.7f);
        public void Fountain() => Play(fountain);

        /// <summary>鍛冶屋で強化したとき：金床を叩く「カン、カン」。</summary>
        public void Anvil() => Play(anvil, 0.8f, true);

        AudioClip unseal;

        /// <summary>階段の封印が解けた：仕掛けが外れる「ガッコン」。</summary>
        public void Unseal()
        {
            if (unseal == null) unseal = MakeUnseal();
            Play(unseal, 0.9f, true);
        }

        /// <summary>
        /// 仕掛けが外れる「ガッコン」：金具がかみ合う硬い「ガッ」と、少し遅れて重い石の「コン」、最後に低い余韻。
        /// </summary>
        static AudioClip MakeUnseal()
        {
            const float length = 1.9f;
            int len = (int)(Rate * length);
            var data = new float[len];
            uint seed = 4242u;
            float Noise()
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                return (seed & 0xffff) / 32768f - 1f;
            }
            // 「ガッ」：短い雑音（こもらせる）と、金属っぽく響く数本の音がすぐ消える
            float lp = 0f;
            float[] gf = { 120f, 260f, 540f, 900f };
            float[] ga = { 1f, 0.7f, 0.45f, 0.25f };
            for (int i = 0; i < (int)(Rate * 0.25f); i++)
            {
                float t = (float)i / Rate;
                lp += 0.12f * (Noise() - lp);
                float v = lp * 3f * Mathf.Exp(-t * 45f) + Mathf.Sin(2f * Mathf.PI * 48f * t) * Mathf.Exp(-t * 18f) * 0.9f; // 重い衝撃の低音
                for (int k = 0; k < gf.Length; k++) v += Mathf.Sin(2f * Mathf.PI * gf[k] * t) * ga[k] * Mathf.Exp(-t * (28f + k * 10f)) * 0.5f;
                data[i] += v * 0.35f;
            }
            // 「コン」：0.17秒後、重く低い音（少し音が下がる）と、ぶつかった瞬間の鈍い雑音
            int s2 = (int)(Rate * 0.17f);
            float ph = 0f, ph2 = 0f, lp2 = 0f;
            for (int i = 0; s2 + i < len; i++)
            {
                float t = (float)i / Rate;
                float f = 62f * (1f + 0.3f * Mathf.Exp(-t * 20f)); // さらに低く
                ph += f / Rate;
                ph2 += f * 2f / Rate;
                lp2 += 0.05f * (Noise() - lp2);
                float v = Mathf.Sin(2f * Mathf.PI * ph) * Mathf.Exp(-t * 5f)
                        + 0.6f * Mathf.Sin(2f * Mathf.PI * ph2) * Mathf.Exp(-t * 9f)        // スマホでも聞こえる2倍の音
                        + 0.3f * Mathf.Sin(2f * Mathf.PI * ph * 3f) * Mathf.Exp(-t * 14f)
                        + lp2 * 4f * Mathf.Exp(-t * 25f);
                // 低い余韻（石が落ち着く）
                v += 0.5f * Mathf.Sin(2f * Mathf.PI * 36f * t) * Mathf.Exp(-t * 2.2f) * Mathf.Min(1f, t / 0.03f); // 地響きのような余韻
                data[s2 + i] += v * 0.32f * Mathf.Clamp01((length - (s2 + i) / (float)Rate) / 0.2f);
            }
            var clip = AudioClip.Create("unseal", len, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        AudioClip tick, stop;
        float lastTick;

        /// <summary>スロットが回る「カチカチ」。</summary>
        public void Tick()
        {
            if (tick == null) tick = Make("tick", new[] { 2200f }, 0.012f, 0.2f, Wave.Square);
            if (Time.unscaledTime - lastTick < 0.05f) return;
            lastTick = Time.unscaledTime;
            Play(tick, 0.5f, true);
        }

        /// <summary>スロットが止まった「チン」。</summary>
        public void SlotStop()
        {
            if (stop == null) stop = Make("slotstop", new[] { 1568f, 2093f }, 0.06f, 0.35f, Wave.Sine);
            Play(stop, 0.8f, true);
        }

        /// <summary>金属を叩いた音（倍音が整数倍でない澄んだ響き＋叩いた瞬間の雑音）を2回。</summary>
        static AudioClip MakeAnvil()
        {
            int len = (int)(Rate * 0.7f);
            var data = new float[len];
            float[] partials = { 1180f, 1870f, 2640f, 3410f, 4300f };
            float[] amps = { 1f, 0.6f, 0.45f, 0.3f, 0.2f };
            void Strike(int start, float vol)
            {
                for (int i = 0; start + i < len; i++)
                {
                    float t = (float)i / Rate;
                    float v = 0f;
                    for (int k = 0; k < partials.Length; k++) v += Mathf.Sin(2f * Mathf.PI * partials[k] * t) * amps[k] * Mathf.Exp(-t * (7f + k * 3f));
                    if (t < 0.008f) v += (Random.value * 2f - 1f) * (1f - t / 0.008f) * 1.5f; // 叩いた瞬間
                    data[start + i] += v * vol * 0.22f;
                }
            }
            Strike(0, 1f);
            Strike((int)(Rate * 0.17f), 0.75f);
            var clip = AudioClip.Create("anvil", len, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public void Landed(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: Play(chime); break;                     // 黄色はたくさん落ちるので、上限の対象
                case Rarity.Legendary: Play(fanfare, 1f, true); break;   // 紫以上は聞き逃さないように、いつも鳴らす
                case Rarity.Relic: Play(relic, 1f, true); break;
                case Rarity.Mythic:
                case Rarity.Boss: Jackpot(); break;                       // 神話・ボスのセット：大当たりの音
            }
        }

        AudioClip jackpot;

        /// <summary>大当たり：鐘の連打「キンキンキン」がだんだん速く高くなり、高い鐘の和音「シャーン」、鈴が一気にこぼれる（約1.9秒）。</summary>
        public void Jackpot()
        {
            if (jackpot == null) jackpot = MakeJackpot();
            Play(jackpot, 1f, true);
        }

        static float Note(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static AudioClip MakeJackpot()
        {
            const float length = 1.9f;
            int len = (int)(Rate * length);
            var data = new float[len];
            float[] ratios = { 1f, 2.76f, 5.4f };
            float[] amps = { 1f, 0.5f, 0.25f };
            // 鐘の音（倍音が整数倍でない澄んだ響き。聞こえない高さの倍音は入れない）
            void Bell(float f, float at, float gain, float dur = 0.45f, float decay = 7f)
            {
                int s = (int)(Rate * at), n = (int)(Rate * dur);
                for (int i = 0; i < n && s + i < len; i++)
                {
                    float t = (float)i / Rate;
                    float v = 0f;
                    for (int k = 0; k < ratios.Length; k++)
                        if (f * ratios[k] < 20000f) v += amps[k] * Mathf.Sin(2f * Mathf.PI * f * ratios[k] * t);
                    data[s + i] += v * Mathf.Exp(-t * decay) * Mathf.Min(1f, t / 0.002f) * gain;
                }
            }
            // 1) 鐘の連打：3段に高くなりながら、だんだん速く、大きく
            int[] lo = { 91, 96, 100 }, hi = { 96, 100, 103 };
            int[] steps = { 5, 5, 6 };
            float tm = 0f;
            int idx = 0;
            for (int st = 0; st < steps.Length; st++)
                for (int k = 0; k < steps[st]; k++, idx++)
                {
                    Bell(Note(idx % 2 == 0 ? hi[st] : lo[st]), tm, 0.15f + 0.01f * idx, 0.25f);
                    tm += 0.05f - 0.0015f * idx;
                }
            // 2) 高い鐘の和音「シャーン」と、きらきら揺れる高い音
            float hit = tm;
            int[] chord = { 96, 100, 103, 108 }; // 連打より低い音を入れると「カーン」と浮くので、高い音だけ
            float[] chordGain = { 0.2f, 0.16f, 0.16f, 0.1f };
            for (int c = 0; c < chord.Length; c++) Bell(Note(chord[c]), hit, chordGain[c], 1.3f, 3.2f);
            {
                int s = (int)(Rate * hit), n = (int)(Rate * 1.1f);
                float f = Note(108);
                for (int i = 0; i < n && s + i < len; i++)
                {
                    float t = (float)i / Rate;
                    float trem = Mathf.Sin(2f * Mathf.PI * 18f * t) >= 0f ? 1f : 0.5f;
                    data[s + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 2.5f) * trem * 0.07f;
                }
            }
            // 3) 鈴が一気にこぼれる（2列で重ねる）
            int[] spill = { 103, 100, 96, 105, 98, 100, 91, 103, 96, 100, 98, 93, 103, 96, 100, 91, 98, 96, 103, 100, 96, 91, 98, 93 };
            for (int k = 0; k < spill.Length; k++)
            {
                Bell(Note(spill[k]), hit + 0.03f + k * 0.035f + (k % 3) * 0.007f, 0.10f);
                if (k % 2 == 0) Bell(Note(spill[k] + 12), hit + 0.05f + k * 0.035f, 0.05f, 0.3f, 10f);
            }
            // 終わりをなめらかに消して、大きさをそろえる
            float peak = 0.0001f;
            for (int i = 0; i < len; i++)
            {
                data[i] *= Mathf.Clamp01((length - (float)i / Rate) / 0.15f);
                peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            }
            for (int i = 0; i < len; i++) data[i] *= 0.5f / peak;
            var clip = AudioClip.Create("jackpot", len, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        enum Wave { Sine, Square, Noise }

        static AudioClip Make(string name, float[] notes, float noteLength, float volume, Wave wave)
        {
            int per = Mathf.Max(1, (int)(Rate * noteLength));
            var data = new float[per * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < per; i++)
                {
                    float t = (float)i / Rate;
                    float env = 1f - (float)i / per;
                    float phase = notes[n] * t;
                    float v;
                    switch (wave)
                    {
                        case Wave.Square: v = Mathf.Repeat(phase, 1f) < 0.5f ? 1f : -1f; break;
                        case Wave.Noise: v = Random.value * 2f - 1f; break;
                        default: v = Mathf.Sin(2f * Mathf.PI * phase); break;
                    }
                    data[n * per + i] = v * env * env * volume;
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
