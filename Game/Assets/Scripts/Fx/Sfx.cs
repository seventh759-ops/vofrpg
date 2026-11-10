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
        AudioClip hit, pickup, ember, chime, fanfare, relic, boom, fountain;
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

        public void Landed(Rarity r)
        {
            switch (r)
            {
                case Rarity.Rare: source.PlayOneShot(chime); break;
                case Rarity.Legendary: source.PlayOneShot(fanfare); break;
                case Rarity.Relic: source.PlayOneShot(relic); break;
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
