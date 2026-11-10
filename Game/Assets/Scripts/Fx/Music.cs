using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 仮の BGM。音のファイルを使わず、起動時に短いチップチューン風のループを作る（このゲーム用のオリジナルの旋律）。
    /// 通常とボス戦の2曲。ボスが出ると切り替わる（クロスフェード）。本番の曲ができたら AudioClip を差し替える。
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const int Rate = 22050;
        const float Volume = 0.35f;

        AudioSource normal;
        AudioSource boss;
        bool bossMode;
        bool muted;

        // 1小節 = 8分音符 8つ。-1 は休符。音の高さは MIDI の番号（69 = ラ）
        static readonly int[] normalLead =
        {
            69, -1, 72, -1, 76, -1, 74, 72,
            72, -1, 69, -1, 65, -1, -1, -1,
            67, -1, 72, -1, 76, -1, 79, 76,
            74, -1, -1, 71, 67, -1, -1, -1,
            76, -1, 74, -1, 72, -1, 71, 72,
            69, -1, 65, -1, 69, -1, 72, -1,
            71, -1, 72, -1, 74, -1, 76, -1,
            74, -1, 71, -1, 68, -1, -1, -1,
        };
        static readonly int[] normalRoots = { 45, 41, 48, 43, 45, 41, 48, 40 };

        static readonly int[] bossLead =
        {
            74, -1, 74, 77, -1, 74, 72, -1,
            74, -1, 69, -1, 72, -1, 74, -1,
            70, -1, 70, 74, -1, 77, 74, -1,
            72, -1, 76, -1, 79, -1, 76, -1,
            81, -1, 79, 77, -1, 76, 74, -1,
            77, -1, 76, 74, -1, 72, 69, -1,
            70, -1, 74, -1, 77, -1, 82, -1,
            81, -1, 79, -1, 76, -1, 73, -1,
        };
        static readonly int[] bossRoots = { 50, 50, 46, 48, 50, 50, 46, 45 };

        void Awake()
        {
            normal = MakeSource(Compose("bgm_normal", normalLead, normalRoots, 100f, false));
            boss = MakeSource(Compose("bgm_boss", bossLead, bossRoots, 150f, true));
            normal.volume = Volume;
            boss.volume = 0f;
            normal.Play();
            boss.Play();
        }

        AudioSource MakeSource(AudioClip clip)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            return src;
        }

        public void SetBoss(bool on)
        {
            bossMode = on;
        }

        public void ToggleMute()
        {
            muted = !muted;
        }

        void Update()
        {
            // 0.8 秒ほどで切り替える
            float step = Time.unscaledDeltaTime * Volume / 0.8f;
            float normalTarget = muted || bossMode ? 0f : Volume;
            float bossTarget = muted || !bossMode ? 0f : Volume;
            normal.volume = Mathf.MoveTowards(normal.volume, normalTarget, step);
            boss.volume = Mathf.MoveTowards(boss.volume, bossTarget, step);

            // ボス戦の曲は、始まるたびに頭から
            if (bossMode && boss.volume <= step && !muted) boss.time = 0f;
        }

        // ---- 曲を作る ----

        static AudioClip Compose(string name, int[] lead, int[] roots, float bpm, bool intense)
        {
            float eighth = 60f / bpm / 2f;
            int stepLen = Mathf.RoundToInt(eighth * Rate);
            int steps = lead.Length;
            var data = new float[stepLen * steps];

            for (int i = 0; i < steps; i++)
            {
                int start = i * stepLen;
                int bar = i / 8;
                int beat = i % 8;

                // 主旋律（矩形波）。次が休符なら少し長く鳴らす
                if (lead[i] >= 0)
                {
                    int len = stepLen * (i + 1 < steps && lead[i + 1] < 0 ? 2 : 1);
                    AddTone(data, start, len, Freq(lead[i]), 0.11f, Wave.Square);
                }

                // 低音（三角波）
                int root = roots[bar % roots.Length];
                int bassNote = intense
                    ? (beat == 2 || beat == 5 || beat == 7 ? root + 12 : root)
                    : (beat % 2 == 0 ? (beat % 4 == 0 ? root : root + 12) : -1);
                if (bassNote >= 0) AddTone(data, start, stepLen, Freq(bassNote - 12), 0.2f, Wave.Triangle);

                // 打楽器
                bool kick = intense ? beat % 2 == 0 : beat == 0 || beat == 4;
                bool snare = intense ? beat == 2 || beat == 6 : beat == 6 && bar % 2 == 1;
                bool hat = intense || beat % 2 == 1;
                if (kick) AddKick(data, start, 0.35f);
                if (snare) AddNoise(data, start, (int)(0.1f * Rate), 0.12f);
                if (hat) AddNoise(data, start, (int)(0.03f * Rate), 0.04f);
            }

            for (int i = 0; i < data.Length; i++) data[i] = Mathf.Clamp(data[i], -1f, 1f);
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        enum Wave { Square, Triangle }

        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static void AddTone(float[] data, int start, int len, float freq, float volume, Wave wave)
        {
            int attack = Mathf.Max(1, (int)(0.005f * Rate));
            int release = Mathf.Max(1, (int)(0.04f * Rate));
            for (int j = 0; j < len && start + j < data.Length; j++)
            {
                float t = (float)j / Rate;
                float phase = Mathf.Repeat(freq * t, 1f);
                float v = wave == Wave.Square
                    ? (phase < 0.25f ? 1f : -1f)
                    : 1f - 4f * Mathf.Abs(phase - 0.5f);
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / release) * (1f - 0.35f * j / len);
                data[start + j] += v * env * volume;
            }
        }

        static void AddKick(float[] data, int start, float volume)
        {
            int len = (int)(0.12f * Rate);
            float phase = 0f;
            for (int j = 0; j < len && start + j < data.Length; j++)
            {
                float k = (float)j / len;
                phase += Mathf.Lerp(120f, 40f, k) / Rate;
                data[start + j] += Mathf.Sin(phase * 2f * Mathf.PI) * (1f - k) * volume;
            }
        }

        static void AddNoise(float[] data, int start, int len, float volume)
        {
            // 毎回同じ並びにして、ループのたびに音が変わらないようにする
            uint seed = (uint)(start * 2654435761u + 1u);
            for (int j = 0; j < len && start + j < data.Length; j++)
            {
                seed ^= seed << 13;
                seed ^= seed >> 17;
                seed ^= seed << 5;
                float n = (seed & 0xffff) / 32768f - 1f;
                data[start + j] += n * (1f - (float)j / len) * volume;
            }
        }
    }
}
