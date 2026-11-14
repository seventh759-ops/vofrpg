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
        AudioClip hit, pickup, ember, chime, fanfare, relic, boom, fountain, anvil;
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
        }

        public void Hit()
        {
            if (Time.unscaledTime - lastHit < 0.045f) return;
            lastHit = Time.unscaledTime;
            source.PlayOneShot(hit, 0.6f);
        }

        public void Ember()
        {
            if (Time.unscaledTime - lastEmber < 0.03f) return;
            lastEmber = Time.unscaledTime;
            source.PlayOneShot(ember, 0.6f);
        }

        public void Pickup() => source.PlayOneShot(pickup);
        public void Boom() => source.PlayOneShot(boom, 0.7f);
        public void Fountain() => source.PlayOneShot(fountain);

        /// <summary>鍛冶屋で強化したとき：金床を叩く「カン、カン」。</summary>
        public void Anvil() => source.PlayOneShot(anvil, 0.8f);

        AudioClip unseal;

        /// <summary>階段の封印が解けた：仕掛けが外れる「ガッコン」。</summary>
        public void Unseal()
        {
            if (unseal == null) unseal = MakeUnseal();
            source.PlayOneShot(unseal, 0.9f);
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
            source.PlayOneShot(tick, 0.5f);
        }

        /// <summary>スロットが止まった「チン」。</summary>
        public void SlotStop()
        {
            if (stop == null) stop = Make("slotstop", new[] { 1568f, 2093f }, 0.06f, 0.35f, Wave.Sine);
            source.PlayOneShot(stop, 0.8f);
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
                case Rarity.Rare: source.PlayOneShot(chime); break;
                case Rarity.Legendary: source.PlayOneShot(fanfare); break;
                case Rarity.Relic:
                case Rarity.Mythic: source.PlayOneShot(relic); break;
            }
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
