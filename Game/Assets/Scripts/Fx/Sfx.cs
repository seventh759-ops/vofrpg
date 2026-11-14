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

        /// <summary>階段の封印が解けた：鍵を開けた「カチャ、カチ、ガチャン」。</summary>
        public void Unseal()
        {
            if (unseal == null) unseal = MakeUnseal();
            source.PlayOneShot(unseal, 0.9f);
        }

        /// <summary>
        /// 鍵を開けた音：鍵を差し込む小さな「カチャ」、回す「カチ、カチ」、かんぬきが外れる重い「ガチャン」と、低い余韻。
        /// </summary>
        static AudioClip MakeUnseal()
        {
            const float length = 1.5f;
            int len = (int)(Rate * length);
            var data = new float[len];
            uint seed = 777u;
            float Noise()
            {
                seed ^= seed << 13; seed ^= seed >> 17; seed ^= seed << 5;
                return (seed & 0xffff) / 32768f - 1f;
            }
            // 金属を叩いた短い音（at 秒から、partials の高さで、decay の速さで消える）
            void Metal(float at, float[] partials, float decay, float vol, float noise)
            {
                int start = (int)(Rate * at);
                float lp = 0f;
                for (int i = 0; start + i < len && i < Rate * 0.6f; i++)
                {
                    float t = (float)i / Rate;
                    float v = 0f;
                    for (int k = 0; k < partials.Length; k++) v += Mathf.Sin(2f * Mathf.PI * partials[k] * t) * Mathf.Exp(-t * decay * (1f + k * 0.4f)) / (1f + k * 0.5f);
                    lp += 0.4f * (Noise() - lp);
                    v += lp * noise * Mathf.Exp(-t * 120f);
                    data[start + i] += v * vol;
                }
            }
            // 差し込む「カチャ」（小さく高い音を3つ）
            Metal(0.00f, new[] { 2900f, 4100f, 5300f }, 90f, 0.10f, 0.6f);
            Metal(0.05f, new[] { 3300f, 4600f }, 100f, 0.08f, 0.5f);
            Metal(0.11f, new[] { 2600f, 3900f, 5100f }, 90f, 0.09f, 0.6f);
            // 回す「カチ、カチ」
            Metal(0.32f, new[] { 1900f, 2700f, 3600f }, 60f, 0.16f, 0.8f);
            Metal(0.44f, new[] { 2100f, 3000f, 3800f }, 60f, 0.16f, 0.8f);
            // かんぬきが外れる「ガチャン」：重い金属の響き＋低い「ドン」
            Metal(0.64f, new[] { 380f, 830f, 1460f, 2300f }, 14f, 0.32f, 1.2f);
            Metal(0.71f, new[] { 1700f, 2500f }, 70f, 0.10f, 0.5f); // ガチャン の後の小さなカタつき
            int s3 = (int)(Rate * 0.64f);
            for (int i = 0; s3 + i < len; i++)
            {
                float t = (float)i / Rate;
                float v = Mathf.Sin(2f * Mathf.PI * 70f * (1f + 0.3f * Mathf.Exp(-t * 30f)) * t) * Mathf.Exp(-t * 9f)
                        + 0.5f * Mathf.Sin(2f * Mathf.PI * 140f * t) * Mathf.Exp(-t * 14f)
                        + 0.35f * Mathf.Sin(2f * Mathf.PI * 42f * t) * Mathf.Exp(-t * 3.5f);
                data[s3 + i] += v * 0.3f * Mathf.Clamp01((length - (s3 + i) / (float)Rate) / 0.15f);
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
