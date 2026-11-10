using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 仮の BGM。音のファイルを使わず、コードで曲を組み立てる（どれもこのゲーム用のオリジナルの旋律）。
    /// 3つのテーマ（通常の曲＋それに合わせたボス戦の曲）から選べる。ボスが出ると切り替わる。
    /// 音作り：重ねた鋸波の和音、こもらせたベース、ディレイ（反響）、キックに合わせた音量のうねり。
    /// 本番の曲ができたら AudioClip を差し替える。
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const int Rate = 22050;
        const float Volume = 0.4f;

        AudioSource normal;
        AudioSource boss;
        bool bossMode;
        bool muted;
        int theme = -1;
        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        public static int ThemeCount => Themes.Length;
        public string ThemeName => theme >= 0 ? Themes[theme].Name : "";

        void Awake()
        {
            normal = MakeSource();
            boss = MakeSource();
            SetTheme(Profile.MusicTheme);
        }

        AudioSource MakeSource()
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0f;
            return src;
        }

        /// <summary>テーマを変える（初めて選んだテーマはその場で曲を作る）。</summary>
        public void SetTheme(int index)
        {
            index = ((index % Themes.Length) + Themes.Length) % Themes.Length;
            if (index == theme) return;
            theme = index;
            var t = Themes[index];
            normal.clip = Get(t.Normal);
            boss.clip = Get(t.Boss);
            normal.Play();
            boss.Play();
            normal.volume = 0f;
            boss.volume = 0f;
        }

        public void NextTheme()
        {
            SetTheme(theme + 1);
            Profile.MusicTheme = theme;
            Profile.Save();
        }

        AudioClip Get(Song song)
        {
            if (clips.TryGetValue(song.Name, out var clip)) return clip;
            clip = Render(song);
            clips[song.Name] = clip;
            return clip;
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
            float step = Time.unscaledDeltaTime * Volume / 0.8f;
            float normalTarget = muted || bossMode ? 0f : Volume;
            float bossTarget = muted || !bossMode ? 0f : Volume;
            normal.volume = Mathf.MoveTowards(normal.volume, normalTarget, step);
            boss.volume = Mathf.MoveTowards(boss.volume, bossTarget, step);
            if (bossMode && boss.volume <= step && !muted) boss.time = 0f; // ボス戦の曲は頭から
        }

        // ================= 曲のデータ =================

        enum LeadKind { None, Square, Saw, Flute, Bell }
        enum BassKind { Sustain, Roots, Octaves, Pump }

        sealed class Song
        {
            public string Name;
            public float Bpm;
            public int[][] Chords;           // 1小節ごとの和音（MIDI の番号）。低音は先頭の音の1オクターブ下
            public int[] Lead;               // 8分音符ごと。-1 休符、-2 前の音を伸ばす
            public LeadKind Lead1 = LeadKind.None;
            public float LeadVol = 0.12f;
            public BassKind Bass = BassKind.Roots;
            public float BassVol = 0.22f;
            public float PadVol = 0.05f;
            public float ArpVol = 0.05f;
            public bool ArpSaw;
            public string Kick = "";         // 16分音符ごと（1小節 16 文字）。x で鳴らす
            public string Snare = "";
            public string Clap = "";
            public string Hat = "";
            public float KickVol = 0.4f;
            public float HatVol = 0.035f;
            public float Duck;               // キックに合わせて、ほかの音を下げる量（0〜1）
            public float Echo = 0.25f;       // 旋律と分散和音の反響の量
        }

        sealed class Theme
        {
            public string Name;
            public Song Normal;
            public Song Boss;
        }

        static readonly int[] Dm = { 62, 65, 69 }, Bb = { 58, 62, 65 }, Gm = { 55, 58, 62 }, A = { 57, 61, 64 }, C = { 60, 64, 67 };
        static readonly int[] Em = { 64, 67, 71 }, Cmaj = { 60, 64, 67 }, G = { 62, 67, 71 }, D = { 62, 66, 69 }, B = { 59, 63, 66 };
        static readonly int[] Am = { 57, 60, 64 }, F = { 57, 60, 65 }, C2 = { 55, 60, 64 }, G2 = { 55, 59, 62 }, E = { 56, 59, 64 };

        static readonly Theme[] Themes =
        {
            new Theme
            {
                Name = "Nocturne",
                Normal = new Song
                {
                    Name = "nocturne", Bpm = 84f,
                    Chords = new[] { Dm, Dm, Bb, Bb, Gm, Gm, A, A },
                    Lead = new[]
                    {
                        74, -2, -2, -2, 77, -2, 76, -2,
                        74, -2, -2, -2, -1, -1, -1, -1,
                        70, -2, -2, -2, 74, -2, 72, -2,
                        70, -2, -2, -2, -1, -1, 69, -2,
                        67, -2, -2, -2, 70, -2, 74, -2,
                        72, -2, 70, -2, 69, -2, -2, -2,
                        69, -2, -2, 73, -2, -2, 76, -2,
                        73, -2, -2, -2, -1, -1, -1, -1,
                    },
                    Lead1 = LeadKind.Bell, LeadVol = 0.1f,
                    Bass = BassKind.Sustain, BassVol = 0.16f, PadVol = 0.07f, ArpVol = 0.035f,
                    Kick = "x.......x.......", Snare = "", Clap = "............x...", Hat = "..x...x...x...x.",
                    KickVol = 0.28f, HatVol = 0.02f, Echo = 0.4f,
                },
                Boss = new Song
                {
                    Name = "nocturne_boss", Bpm = 132f,
                    Chords = new[] { Dm, Dm, Bb, C, Dm, Dm, Bb, A },
                    Lead = new[]
                    {
                        74, -2, 77, -2, 79, -2, 81, -2,
                        79, 77, -2, 74, -2, -1, 72, 74,
                        77, -2, -2, 74, -2, 70, -2, -2,
                        72, -2, 76, -2, 79, -2, -2, -2,
                        81, -2, 79, -2, 77, -2, 74, -2,
                        77, -2, 76, 74, -2, 72, -2, 69,
                        70, -2, 74, -2, 77, -2, 82, -2,
                        81, -2, -2, 79, -2, 76, 73, -2,
                    },
                    Lead1 = LeadKind.Saw, LeadVol = 0.09f,
                    Bass = BassKind.Pump, BassVol = 0.24f, PadVol = 0.05f, ArpVol = 0.04f, ArpSaw = true,
                    Kick = "x...x...x...x...", Clap = "....x.......x...", Hat = "..x...x...x...x.",
                    KickVol = 0.42f, Duck = 0.5f, Echo = 0.22f,
                },
            },
            new Theme
            {
                Name = "Wayfarer",
                Normal = new Song
                {
                    Name = "wayfarer", Bpm = 96f,
                    Chords = new[] { Em, Cmaj, G, D, Em, Cmaj, D, Em },
                    Lead = new[]
                    {
                        71, -2, -2, 74, 76, -2, 74, 71,
                        72, -2, -2, -2, 67, -2, 69, 71,
                        74, -2, -2, 71, 67, -2, 71, 74,
                        78, -2, -2, -2, 76, -2, 74, -2,
                        76, -2, -2, 79, 78, -2, 76, 74,
                        72, -2, 74, 76, -2, -2, 72, 71,
                        69, -2, 71, 74, -2, 72, 71, 69,
                        71, -2, -2, -2, -1, -1, -1, -1,
                    },
                    Lead1 = LeadKind.Flute, LeadVol = 0.12f,
                    Bass = BassKind.Roots, BassVol = 0.18f, PadVol = 0.045f, ArpVol = 0.05f,
                    Kick = "x.......x.....x.", Hat = "x.x.x.x.x.x.x.x.",
                    KickVol = 0.26f, HatVol = 0.018f, Echo = 0.28f,
                },
                Boss = new Song
                {
                    Name = "wayfarer_boss", Bpm = 140f,
                    Chords = new[] { Em, Em, Cmaj, D, Em, Em, Cmaj, B },
                    Lead = new[]
                    {
                        76, -2, -2, 79, -2, -2, 83, -2,
                        81, -2, 79, -2, 78, -2, 76, -2,
                        79, -2, -2, 76, -2, -2, 72, -2,
                        74, -2, 78, -2, 81, -2, -2, -2,
                        83, -2, 81, 79, -2, 76, -2, 79,
                        78, -2, 76, -2, 74, -2, 71, -2,
                        72, -2, 76, -2, 79, -2, 84, -2,
                        83, -2, -2, -2, 75, -2, 78, -2,
                    },
                    Lead1 = LeadKind.Saw, LeadVol = 0.09f,
                    Bass = BassKind.Octaves, BassVol = 0.22f, PadVol = 0.04f, ArpVol = 0.06f, ArpSaw = true,
                    Kick = "x..x..x.x..x..x.", Snare = "....x.......x...", Hat = "x.x.x.x.x.x.x.x.",
                    KickVol = 0.4f, Duck = 0.2f, Echo = 0.15f,
                },
            },
            new Theme
            {
                Name = "Neon Descent",
                Normal = new Song
                {
                    Name = "neon", Bpm = 118f,
                    Chords = new[] { Am, F, C2, G2, Am, F, G2, E },
                    Lead = new[]
                    {
                        76, -2, -1, 76, 74, -2, 72, -2,
                        72, -2, 74, -2, 77, -2, 76, 74,
                        72, -2, -1, 72, 71, -2, 67, -2,
                        71, -2, 72, -2, 74, -2, -2, -2,
                        76, -2, -1, 76, 79, -2, 81, -2,
                        77, -2, 76, -2, 74, -2, 72, -2,
                        71, -2, 72, -2, 74, -2, 76, -2,
                        75, -2, -2, -2, 71, -2, 68, -2,
                    },
                    Lead1 = LeadKind.Square, LeadVol = 0.08f,
                    Bass = BassKind.Octaves, BassVol = 0.22f, PadVol = 0.05f, ArpVol = 0.045f,
                    Kick = "x...x...x...x...", Clap = "....x.......x...", Hat = "..x...x...x...x.",
                    KickVol = 0.38f, Duck = 0.45f, Echo = 0.3f,
                },
                Boss = new Song
                {
                    Name = "neon_boss", Bpm = 150f,
                    Chords = new[] { Am, Am, F, G2, Am, Am, F, E },
                    Lead = new[]
                    {
                        81, -2, 79, -2, 76, -2, 79, 81,
                        -1, 81, -2, 79, 76, -2, 74, 76,
                        77, -2, 76, -2, 74, -2, 72, 74,
                        76, -2, -2, 74, -2, 71, -2, -2,
                        81, -2, 84, -2, 83, -2, 81, 79,
                        81, -2, -2, 76, -2, 79, -2, 76,
                        77, -2, 79, -2, 81, -2, 84, -2,
                        83, -2, -2, 80, -2, 76, -2, -2,
                    },
                    Lead1 = LeadKind.Saw, LeadVol = 0.09f,
                    Bass = BassKind.Pump, BassVol = 0.24f, PadVol = 0.04f, ArpVol = 0.05f, ArpSaw = true,
                    Kick = "x...x...x...x...", Snare = "....x.......x...", Hat = "xxxxxxxxxxxxxxxx",
                    KickVol = 0.42f, HatVol = 0.02f, Duck = 0.55f, Echo = 0.15f,
                },
            },
        };

        // ================= 曲を作る =================

        static AudioClip Render(Song s)
        {
            float sixteenth = 60f / s.Bpm / 4f;
            int step = Mathf.RoundToInt(sixteenth * Rate);
            int bars = s.Chords.Length;
            int len = step * 16 * bars;

            var music = new float[len];  // 和音・低音（キックで下げる）
            var echoBus = new float[len]; // 旋律・分散和音（反響を付けて、キックで下げる）
            var drums = new float[len];
            var duck = new float[len];
            for (int i = 0; i < len; i++) duck[i] = 1f;

            for (int bar = 0; bar < bars; bar++)
            {
                int[] chord = s.Chords[bar];
                int barStart = bar * 16 * step;

                // 和音（重ねた鋸波。ゆっくり立ち上がる）
                if (s.PadVol > 0f)
                {
                    foreach (int n in chord) AddPad(music, barStart, step * 16, Freq(n), s.PadVol);
                }

                // 低音
                int root = chord[0] - 12;
                for (int k = 0; k < 16; k++)
                {
                    int note = -1;
                    int length = step * 2;
                    switch (s.Bass)
                    {
                        case BassKind.Sustain: if (k == 0) { note = root; length = step * 16; } break;
                        case BassKind.Roots: if (k % 4 == 0) { note = root; length = step * 3; } break;
                        case BassKind.Octaves: if (k % 2 == 0) note = (k / 2) % 2 == 0 ? root : root + 12; break;
                        case BassKind.Pump: if (k % 2 == 0) note = root; break;
                    }
                    if (note >= 0) AddBass(music, barStart + k * step, length, Freq(note - 12), s.BassVol, s.Bass == BassKind.Pump);
                }

                // 分散和音（16分音符で和音の音を上へたどる）
                if (s.ArpVol > 0f)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        int n = chord[k % chord.Length] + 12 * (1 + (k / chord.Length) % 2);
                        AddPluck(echoBus, barStart + k * step, step * 2, Freq(n), s.ArpVol, s.ArpSaw);
                    }
                }

                // 打楽器
                for (int k = 0; k < 16; k++)
                {
                    int at = barStart + k * step;
                    if (Hit(s.Kick, k))
                    {
                        AddKick(drums, at, s.KickVol);
                        if (s.Duck > 0f) Duck(duck, at, (int)(sixteenth * 3f * Rate), s.Duck);
                    }
                    if (Hit(s.Snare, k)) AddSnare(drums, at, 0.13f);
                    if (Hit(s.Clap, k)) AddClap(drums, at, 0.12f);
                    if (Hit(s.Hat, k)) AddHat(drums, at, (int)(0.03f * Rate), s.HatVol, (uint)(bar * 16 + k));
                }
            }

            // 旋律
            if (s.Lead1 != LeadKind.None)
            {
                int eighth = step * 2;
                for (int i = 0; i < s.Lead.Length; i++)
                {
                    int n = s.Lead[i];
                    if (n < 0) continue;
                    int span = 1;
                    while (i + span < s.Lead.Length && s.Lead[i + span] == -2) span++;
                    AddLead(echoBus, i * eighth, span * eighth, Freq(n), s.LeadVol, s.Lead1);
                }
            }

            // 反響（3/16 拍遅れ。ループのつなぎ目でも途切れないよう、2周ぶん回す）
            int delay = step * 3;
            var wet = new float[len];
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < len; i++)
                {
                    int j = (i - delay + len) % len;
                    wet[i] = echoBus[j] * s.Echo + wet[j] * 0.45f;
                }
            }

            var mix = new float[len];
            float peak = 0.0001f;
            for (int i = 0; i < len; i++)
            {
                mix[i] = (music[i] + echoBus[i] + wet[i]) * duck[i] + drums[i];
                peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            }
            float norm = 0.9f / peak;
            for (int i = 0; i < len; i++) mix[i] = Mathf.Clamp(mix[i] * norm, -1f, 1f);

            var clip = AudioClip.Create("bgm_" + s.Name, len, 1, Rate, false);
            clip.SetData(mix, 0);
            return clip;
        }

        static bool Hit(string pattern, int k) => !string.IsNullOrEmpty(pattern) && k < pattern.Length && pattern[k] == 'x';

        static float Freq(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

        static float Saw(float phase) => 2f * Mathf.Repeat(phase, 1f) - 1f;

        static float Lp(float a) => 1f - Mathf.Exp(-2f * Mathf.PI * a / Rate);

        static void AddPad(float[] buf, int start, int len, float freq, float vol)
        {
            float a = Lp(1400f);
            float y = 0f;
            int attack = (int)(0.35f * Rate), release = (int)(0.3f * Rate);
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float x = (Saw(freq * 1.004f * t) + Saw(freq * 0.996f * t)) * 0.5f;
                y += a * (x - y);
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / release);
                Add(buf, start + j, y * env * vol);
            }
        }

        static void AddBass(float[] buf, int start, int len, float freq, float vol, bool pump)
        {
            float a = Lp(pump ? 500f : 700f);
            float y = 0f;
            int release = (int)(0.02f * Rate);
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float x = pump ? Saw(freq * t) : 1f - 4f * Mathf.Abs(Mathf.Repeat(freq * t, 1f) - 0.5f); // 鋸波／三角波
                y += a * (x - y);
                float env = Mathf.Min(1f, j / 30f) * Mathf.Min(1f, (float)(len - j) / release);
                Add(buf, start + j, y * env * vol * (pump ? 1.6f : 1f));
            }
        }

        static void AddPluck(float[] buf, int start, int len, float freq, float vol, bool saw)
        {
            float a = Lp(2400f);
            float y = 0f;
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float x = saw ? Saw(freq * t) : Mathf.Sin(2f * Mathf.PI * freq * t);
                y += a * (x - y);
                float env = Mathf.Exp(-t * 18f) * Mathf.Min(1f, j / 20f);
                Add(buf, start + j, y * env * vol);
            }
        }

        static void AddLead(float[] buf, int start, int len, float freq, float vol, LeadKind kind)
        {
            float a = Lp(kind == LeadKind.Saw ? 2200f : 5000f);
            float y = 0f;
            int attack = kind == LeadKind.Flute ? (int)(0.05f * Rate) : 40;
            int release = (int)(0.06f * Rate);
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                // 少し遅れてかかるビブラート
                float vib = 1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01((t - 0.15f) * 4f);
                float ph = freq * vib * t;
                float x;
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / release);
                switch (kind)
                {
                    case LeadKind.Square: x = Mathf.Repeat(ph, 1f) < 0.3f ? 1f : -1f; break;
                    case LeadKind.Saw: x = (Saw(ph) + Saw(ph * 1.005f)) * 0.5f; break;
                    case LeadKind.Flute: x = Mathf.Sin(2f * Mathf.PI * ph) + 0.15f * Mathf.Sin(4f * Mathf.PI * ph); break;
                    default: // Bell：澄んだ音に、高い倍音を少し。ゆっくり消える
                        x = Mathf.Sin(2f * Mathf.PI * ph) + 0.35f * Mathf.Sin(2f * Mathf.PI * ph * 2.76f) * Mathf.Exp(-t * 6f);
                        env *= Mathf.Exp(-t * 1.5f);
                        break;
                }
                y += a * (x - y);
                Add(buf, start + j, y * env * vol);
            }
        }

        static void AddKick(float[] buf, int start, float vol)
        {
            int len = (int)(0.18f * Rate);
            float phase = 0f;
            for (int j = 0; j < len; j++)
            {
                float k = (float)j / len;
                phase += Mathf.Lerp(130f, 42f, Mathf.Sqrt(k)) / Rate;
                Add(buf, start + j, Mathf.Sin(phase * 2f * Mathf.PI) * (1f - k) * (1f - k) * vol);
            }
        }

        static void AddSnare(float[] buf, int start, float vol)
        {
            int len = (int)(0.14f * Rate);
            uint seed = (uint)start * 747796405u + 2891336453u;
            for (int j = 0; j < len; j++)
            {
                float k = (float)j / len;
                float tone = Mathf.Sin(2f * Mathf.PI * 185f * j / Rate) * (1f - k) * 0.5f;
                Add(buf, start + j, (Noise(ref seed) * 0.8f + tone) * (1f - k) * (1f - k) * vol);
            }
        }

        static void AddClap(float[] buf, int start, float vol)
        {
            // 少しずらした3つの短いノイズ
            uint seed = (uint)start * 2654435761u + 1u;
            int burst = (int)(0.012f * Rate);
            int tail = (int)(0.12f * Rate);
            for (int b = 0; b < 3; b++)
            {
                for (int j = 0; j < burst + (b == 2 ? tail : 0); j++)
                {
                    float env = b == 2 ? Mathf.Exp(-(float)j / (0.035f * Rate)) : 1f - (float)j / burst;
                    Add(buf, start + b * burst + j, Noise(ref seed) * env * vol);
                }
            }
        }

        static void AddHat(float[] buf, int start, int len, float vol, uint salt)
        {
            uint seed = salt * 1103515245u + 12345u;
            float prev = 0f;
            for (int j = 0; j < len; j++)
            {
                float n = Noise(ref seed);
                float hp = n - prev; // 高い音だけ残す
                prev = n;
                Add(buf, start + j, hp * (1f - (float)j / len) * vol);
            }
        }

        static void Duck(float[] duck, int start, int len, float amount)
        {
            for (int j = 0; j < len; j++)
            {
                int i = (start + j) % duck.Length;
                float k = (float)j / len;
                duck[i] = Mathf.Min(duck[i], 1f - amount * (1f - k) * (1f - k));
            }
        }

        static float Noise(ref uint seed)
        {
            seed ^= seed << 13;
            seed ^= seed >> 17;
            seed ^= seed << 5;
            return (seed & 0xffff) / 32768f - 1f;
        }

        static void Add(float[] buf, int i, float v)
        {
            buf[i % buf.Length] += v; // 曲の最後からはみ出した音は頭に回す（ループで途切れない）
        }
    }
}
