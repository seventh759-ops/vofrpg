using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// 仮の BGM。音のファイルを使わず、コードで曲を組み立てる（どれもこのゲーム用のオリジナルの旋律）。
    /// 最初は「区画ごと」：塔の区画（灰の回廊・苔むす底…）ごとに曲が変わり、ボス戦はその曲を速く激しくした編曲になる。
    /// ほかに3つのテーマ（通常の曲＋ボス戦の曲）も選べる。曲が変わるときは、いったん小さくしてから切り替える。
    /// 音作り：重ねた鋸波の和音、こもらせたベース、ディレイ（反響）。音量や音程の揺れは控えめにしている。
    /// 本番の曲ができたら AudioClip を差し替える。
    /// </summary>
    public sealed class Music : MonoBehaviour
    {
        const int Rate = 22050;
        /// <summary>音量の段階（0 はなし、1〜5）。4 が前の版と同じ大きさ。</summary>
        static readonly float[] Volumes = { 0f, 0.1f, 0.2f, 0.3f, 0.4f, 0.55f };
        static float Volume => Volumes[Profile.MusicVolumeSetting];

        AudioSource normal;
        AudioSource boss;
        AudioSource bonus;   // 黄金のスライムがいる間の曲
        bool bossMode;
        bool goldenMode;
        bool muted;
        int theme = -1;
        int zoneSong;
        bool titleMode;
        bool townMode;
        AudioClip nextNormal, nextBoss;

        /// <summary>テーマの番号 0 は「区画ごと」。1 から先が固定のテーマ。</summary>
        const int ZoneMode = 0;
        static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>(); // 作った曲（起動の読み込み画面で先に作っておける）

        /// <summary>
        /// 遊ぶたびに作った曲を捨てる。エディタで「ドメインの再読み込みなし」で Play し直すと、前回の曲（もう壊れた物）が残って音が出なくなるため。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache() => clips.Clear();

        /// <summary>起動のときに先に作っておく曲（タイトル・町・最初に流れる区画の曲とそのボス戦）。名前と、作る処理（別のスレッドで動かせる）。</summary>
        public static List<(string name, System.Func<float[]> work)> StartupSongs()
        {
            var list = new List<(string, System.Func<float[]>)>();
            void Add(Song s) { if (!clips.TryGetValue(s.Name, out var c) || c == null) list.Add((s.Name, () => RenderData(s))); }
            Add(TitleSong);
            Add(Profile.AbyssUnlocked ? AbyssTownSong : TownSong);
            int t = ((Profile.MusicTheme % ThemeCount) + ThemeCount) % ThemeCount;
            if (t == ZoneMode) { Add(ZoneSongs[0]); Add(BossOf(ZoneSongs[0])); }
            else { Add(Themes[t - 1].Normal); Add(Themes[t - 1].Boss); }
            return list;
        }

        /// <summary>別のスレッドで作った曲のデータから、曲を作って取っておく（メインのスレッドで呼ぶ）。</summary>
        public static void StoreRendered(string name, float[] data)
        {
            if ((clips.TryGetValue(name, out var c) && c != null) || data == null) return;
            clips[name] = MakeClip(name, data);
        }

        static AudioClip MakeClip(string name, float[] mix)
        {
            var clip = AudioClip.Create("bgm_" + name, mix.Length, 1, Rate, false);
            clip.SetData(mix, 0);
            return clip;
        }

        public static int ThemeCount => Themes.Length + 1;
        public string ThemeName => theme == ZoneMode ? Loc.T("By zone", "区画ごと") : theme > 0 ? Themes[theme - 1].Name : "";

        void Awake()
        {
            normal = MakeSource();
            boss = MakeSource();
            bonus = MakeSource();
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
            index = ((index % ThemeCount) + ThemeCount) % ThemeCount;
            if (index == theme) return;
            theme = index;
            ApplySongs();
        }

        /// <summary>区画が変わった（「区画ごと」のときは曲を変える）。</summary>
        public void SetZone(FloorPattern pattern)
        {
            int i = Mathf.Clamp((int)pattern, 0, ZoneSongs.Length - 1);
            if (i == zoneSong) return;
            zoneSong = i;
            if (theme == ZoneMode) ApplySongs();
        }

        /// <summary>タイトル画面の間は専用の曲（テーマの設定に関係なく）。</summary>
        public void SetTitle(bool on)
        {
            if (on == titleMode) return;
            titleMode = on;
            ApplySongs();
        }

        /// <summary>町にいる間は町の曲（テーマの設定に関係なく）。</summary>
        bool abyssTown; // 深淵へ行けるようになったら、町の曲を深淵版に

        /// <summary>深淵へ行けるようになったか（町の曲が深淵版に変わる）。</summary>
        public void SetAbyssTown(bool on)
        {
            if (on == abyssTown) return;
            abyssTown = on;
            if (townMode) ApplySongs();
        }

        bool finalFloor; // アビスのいる最奥の層：ボス戦の曲はテーマに関係なく、アビス戦の曲

        /// <summary>アビスのいる最奥の層か（ボス戦の曲がアビス戦の曲になる）。</summary>
        public void SetFinalFloor(bool on)
        {
            if (on == finalFloor) return;
            finalFloor = on;
            ApplySongs();
        }

        public void SetTown(bool on)
        {
            if (on == townMode) return;
            townMode = on;
            ApplySongs();
        }

        void ApplySongs()
        {
            if (titleMode || townMode)
            {
                Queue(Get(titleMode ? TitleSong : abyssTown ? AbyssTownSong : TownSong), boss.clip);
                nextBoss = null;
            }
            else if (theme == ZoneMode)
            {
                var song = ZoneSongs[zoneSong];
                Queue(Get(song), Get(finalFloor ? AbyssBossSong : BossOf(song)));
            }
            else
            {
                var t = Themes[theme - 1];
                Queue(Get(t.Normal), Get(finalFloor ? AbyssBossSong : t.Boss));
            }
        }

        /// <summary>次に流す曲を決める。今の曲を小さくしきってから切り替える（Update）。</summary>
        void Queue(AudioClip n, AudioClip b)
        {
            nextNormal = normal.clip != n ? n : null;
            nextBoss = boss.clip != b ? b : null;
        }

        public void NextTheme()
        {
            SetTheme(theme + 1);
            Profile.MusicTheme = theme;
            Profile.Save();
        }

        AudioClip Get(Song song)
        {
            if (clips.TryGetValue(song.Name, out var clip) && clip != null) return clip; // 壊れた曲（前の Play の残り）なら作り直す
            clip = Render(song);
            clips[song.Name] = clip;
            return clip;
        }

        public void SetBoss(bool on)
        {
            bossMode = on;
        }

        /// <summary>黄金のスライムがいる間は、ボーナスタイムの曲（ボス戦の最中はボスの曲が優先）。</summary>
        public void SetGolden(bool on)
        {
            if (on && bonus.clip == null) bonus.clip = Get(GoldenSong);
            goldenMode = on;
        }

        public bool Muted => muted || Profile.MusicVolumeSetting == 0;

        /// <summary>M キー：一時的に消す／戻す。</summary>
        public void ToggleMute()
        {
            muted = !muted;
        }

        /// <summary>BGM の音量を次の段階へ（なし → 1 → 2 → 3 → 4 → 5 → なし）。保存する。</summary>
        public void NextVolume()
        {
            Profile.MusicVolumeSetting = (Profile.MusicVolumeSetting + 1) % Volumes.Length;
            muted = false;
        }

        /// <summary>設定の画面に出す、今の音量（「オフ」か「■■■□□」）。</summary>
        public string VolumeLabel
        {
            get
            {
                int v = Profile.MusicVolumeSetting;
                if (v == 0) return Loc.T("OFF", "オフ");
                return new string('■', v) + new string('□', Volumes.Length - 1 - v);
            }
        }

        void Update()
        {
            float step = Time.unscaledDeltaTime * Mathf.Max(Volume, 0.4f) / 0.8f; // 音量 0 でも、ちゃんと下げきれるように
            bool bonusOn = goldenMode && !bossMode;
            float normalTarget = muted || bossMode || bonusOn || nextNormal != null ? 0f : Volume;
            float bossTarget = muted || !bossMode || nextBoss != null ? 0f : Volume;
            float bonusTarget = muted || !bonusOn ? 0f : Volume;
            normal.volume = Mathf.MoveTowards(normal.volume, normalTarget, step);
            boss.volume = Mathf.MoveTowards(boss.volume, bossTarget, step);
            if (bonus.clip != null)
            {
                if (bonusOn && !bonus.isPlaying) { bonus.time = 0f; bonus.Play(); } // 出るたびに頭から
                bonus.volume = Mathf.MoveTowards(bonus.volume, bonusTarget, step * 2f);   // 切り替えは素早く
                if (!bonusOn && bonus.volume <= 0f && bonus.isPlaying) bonus.Stop();
            }
            if (nextNormal != null && normal.volume <= 0f) Swap(normal, ref nextNormal);
            if (nextBoss != null && boss.volume <= 0f) Swap(boss, ref nextBoss);
            if (bossMode && boss.volume <= step && !muted) boss.time = 0f; // ボス戦の曲は頭から
        }

        static void Swap(AudioSource src, ref AudioClip next)
        {
            src.clip = next;
            src.time = 0f;
            src.Play();
            next = null;
        }

        // ================= 曲のデータ =================

        enum LeadKind { None, Square, Saw, Flute, Bell, Organ }
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
            public bool Church;              // 教会の編成：和音・低音・分散和音をパイプオルガンに、聖歌隊と鐘、大聖堂の残響
            public float ChoirVol;           // 聖歌隊（和音を1オクターブ上で「あー」と歌う）
            public float BellVol;            // 4小節ごとに鳴る教会の鐘
            public float Reverb;             // 大聖堂の残響の量
            public float Gain = 1f;          // 曲全体の大きさ（静かな曲は下げる）
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

        // 区画ごとの曲で使う和音（先頭が根音）
        static readonly int[] Cm = { 60, 63, 67 }, Ab = { 56, 60, 63 }, Eb = { 63, 67, 70 }, Bb2 = { 58, 62, 65 }, Fm = { 53, 56, 60 }, G3 = { 55, 59, 62 };
        static readonly int[] Em2 = { 64, 67, 71 }, F2 = { 65, 69, 72 }, Bm = { 59, 62, 66 }, A2 = { 57, 61, 64 }, Fs = { 54, 58, 61 };
        static readonly int[] E2 = { 64, 68, 71 }, Fsm = { 66, 69, 73 }, D2 = { 62, 66, 69 };

        static readonly int[] Fm2 = { 65, 68, 72 }, Db2 = { 61, 65, 68 }, Ab2 = { 68, 72, 75 }, Eb2 = { 63, 67, 70 }, Bbm = { 58, 61, 65 }, C3 = { 60, 64, 67 };

        /// <summary>区画ごとの曲（FloorPattern の順：灰の回廊・苔むす底・残り火の炉・凍てる墓所・沈んだ書庫・虚ろの淵・深淵の4区画）。</summary>
        static readonly Song[] ZoneSongs =
        {
            // 灰の回廊：冷たい石の広間。ゆっくりした鐘の旋律と、遠くで鳴る太鼓
            new Song
            {
                Name = "zone_ashen", Bpm = 80f,
                Chords = new[] { Cm, Ab, Eb, Bb2, Cm, Ab, Fm, G3 },
                Lead = new[]
                {
                    72, -2, -2, -2, 75, -2, 74, -2,
                    72, -2, -2, -2, -1, -1, 68, -2,
                    70, -2, -2, -2, 75, -2, 79, -2,
                    77, -2, -2, -2, -1, -1, -1, -1,
                    72, -2, -2, -2, 75, -2, 77, -2,
                    79, -2, 77, -2, 75, -2, -2, -2,
                    72, -2, -2, 77, -2, -2, 75, -2,
                    74, -2, -2, -2, 71, -2, -2, -2,
                },
                Lead1 = LeadKind.Bell, LeadVol = 0.1f,
                Bass = BassKind.Sustain, BassVol = 0.16f, PadVol = 0.06f, ArpVol = 0.03f,
                Kick = "x.......x.......", Clap = "............x...", Hat = "..x...x...x...x.",
                KickVol = 0.26f, HatVol = 0.016f, Echo = 0.42f,
            },
            // 苔むす底：湿った洞。笛の旋律と、しずくのような分散和音
            new Song
            {
                Name = "zone_moss", Bpm = 92f,
                Chords = new[] { Dm, G3, Dm, G3, Am, C, G3, Dm },
                Lead = new[]
                {
                    69, -2, -2, 71, 72, -2, 71, 69,
                    67, -2, -2, -2, 71, -2, -2, -2,
                    69, -2, -2, 71, 72, -2, 74, -2,
                    76, -2, 74, -2, 71, -2, -2, -2,
                    72, -2, -2, -2, 76, -2, 74, 72,
                    71, -2, 72, -2, 67, -2, -2, -2,
                    71, -2, -2, 74, -2, 72, 71, -2,
                    69, -2, -2, -2, -1, -1, -1, -1,
                },
                Lead1 = LeadKind.Flute, LeadVol = 0.11f,
                Bass = BassKind.Roots, BassVol = 0.17f, PadVol = 0.045f, ArpVol = 0.045f,
                Kick = "x.....x...x.....", Hat = "x.x.x.x.x.x.x.x.",
                KickVol = 0.24f, HatVol = 0.014f, Echo = 0.32f,
            },
            // 残り火の炉：鍛冶場。重い低音と、金床を打つような刻み
            new Song
            {
                Name = "zone_forge", Bpm = 104f,
                Chords = new[] { Em2, Em2, F2, Em2, C, Dm, F2, F2 },
                Lead = new[]
                {
                    76, -2, -1, 76, 77, -2, 76, -2,
                    74, -2, 72, -2, 71, -2, -2, -2,
                    72, -2, -1, 72, 74, -2, 72, -2,
                    71, -2, 69, -2, 67, -2, -2, -2,
                    72, -2, -2, 76, 79, -2, -2, 77,
                    77, -2, 76, -2, 74, -2, -2, -2,
                    72, -2, 74, -2, 76, -2, 77, -2,
                    76, -2, -2, -2, -1, -1, -1, -1,
                },
                Lead1 = LeadKind.Square, LeadVol = 0.075f,
                Bass = BassKind.Pump, BassVol = 0.2f, PadVol = 0.04f, ArpVol = 0f,
                Kick = "x..x..x.x..x..x.", Snare = "....x.......x...", Hat = "..x...x...x...x.",
                KickVol = 0.38f, HatVol = 0.024f, Echo = 0.2f,
            },
            // 凍てる墓所：静かな氷の部屋。高い鐘と、ほとんど鳴らない太鼓
            new Song
            {
                Name = "zone_frost", Bpm = 70f,
                Chords = new[] { Bm, G3, D, A2, Bm, G3, Em2, Fs },
                Lead = new[]
                {
                    83, -2, -2, -2, -2, -2, 81, -2,
                    79, -2, -2, -2, 78, -2, -2, -2,
                    81, -2, -2, -2, 85, -2, 86, -2,
                    85, -2, -2, -2, -1, -1, -1, -1,
                    86, -2, -2, -2, 85, -2, 83, -2,
                    81, -2, -2, 79, -2, -2, 78, -2,
                    79, -2, -2, -2, 83, -2, -2, -2,
                    82, -2, -2, -2, -1, -1, 78, -2,
                },
                Lead1 = LeadKind.Bell, LeadVol = 0.09f,
                Bass = BassKind.Sustain, BassVol = 0.13f, PadVol = 0.075f, ArpVol = 0.035f,
                Kick = "x...............", Hat = "",
                KickVol = 0.2f, Echo = 0.45f,
            },
            // 沈んだ書庫：砂岩と古い金。異国風の音階の笛と、手で叩く太鼓のような刻み
            new Song
            {
                Name = "zone_archive", Bpm = 98f,
                Chords = new[] { Am, Bb2, Am, E2, Dm, Bb2, E2, E2 },
                Lead = new[]
                {
                    69, -2, 71, 72, -2, 71, 69, -2,
                    70, -2, -2, -2, 74, -2, 72, 70,
                    69, -2, 72, -2, 76, -2, 77, 76,
                    76, -2, -2, -2, 74, 72, 71, 68,
                    74, -2, -2, 77, -2, 76, 74, -2,
                    72, -2, 74, -2, 77, -2, 76, 74,
                    76, -2, -2, 74, -2, 72, 71, -2,
                    68, -2, -2, -2, 71, -2, -2, -2,
                },
                Lead1 = LeadKind.Flute, LeadVol = 0.11f,
                Bass = BassKind.Roots, BassVol = 0.17f, PadVol = 0.04f, ArpVol = 0.04f,
                Kick = "x.....x.x.......", Snare = "....x..x....x...", Hat = "x..x..x.x..x..x.",
                KickVol = 0.3f, HatVol = 0.02f, Echo = 0.28f,
            },
            // 虚ろの淵：星の消えていく闇。四つ打ちと、澄んだ矩形波の旋律
            new Song
            {
                Name = "zone_void", Bpm = 112f,
                Chords = new[] { Fsm, D2, A2, E2, Fsm, D2, E2, E2 },
                Lead = new[]
                {
                    78, -2, -2, 76, -2, -2, 73, -2,
                    74, -2, -2, -2, 69, -2, -2, -2,
                    73, -2, -2, 76, -2, -2, 81, -2,
                    80, -2, -2, -2, 76, -2, -2, -2,
                    78, -2, -2, 81, -2, -2, 85, -2,
                    83, -2, 81, -2, 78, -2, -2, -2,
                    76, -2, 78, -2, 80, -2, 83, -2,
                    80, -2, -2, -2, -1, -1, -1, -1,
                },
                Lead1 = LeadKind.Square, LeadVol = 0.07f,
                Bass = BassKind.Octaves, BassVol = 0.2f, PadVol = 0.055f, ArpVol = 0.04f,
                Kick = "x...x...x...x...", Clap = "....x.......x...", Hat = "..x...x...x...x.",
                KickVol = 0.36f, Echo = 0.3f,
            },
            // ---- 深淵 ----
            // 深淵の門：重い刻みと、鋸波の不穏な旋律。赤い裂け目の前に立つ緊張
            new Song
            {
                Name = "abyss_gate", Bpm = 124f,
                Chords = new[] { Cm, Ab, Fm, G3, Cm, Ab, Bb2, G3 },
                Lead = new[]
                {
                    72, -2, -2, 71, 72, -2, 75, -2,
                    74, -2, 72, -2, 68, -2, -2, -2,
                    72, -2, -2, 71, 72, -2, 77, -2,
                    75, -2, 74, -2, 71, -2, -2, -2,
                    79, -2, -2, 77, 75, -2, 77, -2,
                    80, -2, 79, -2, 75, -2, -2, -2,
                    77, -2, 75, -2, 74, -2, 75, 77,
                    74, -2, -2, -2, 71, -2, -1, -1,
                },
                Lead1 = LeadKind.Saw, LeadVol = 0.075f,
                Bass = BassKind.Pump, BassVol = 0.23f, PadVol = 0.05f, ArpVol = 0.03f,
                Kick = "x..x..x.x..x..x.", Snare = "....x.......x...", Hat = "x.x.x.x.x.x.x.x.",
                KickVol = 0.4f, HatVol = 0.022f, Duck = 0.2f, Echo = 0.22f,
            },
            // 星喰いの淵：星のない空。高い鐘の分散和音と、遠くで鳴る四つ打ち
            new Song
            {
                Name = "abyss_starless", Bpm = 116f,
                Chords = new[] { Em2, C, Am, Bm, Em2, C, Am, B },
                Lead = new[]
                {
                    83, -2, -2, -2, 79, -2, 76, -2,
                    84, -2, -2, -2, 79, -2, -2, -2,
                    81, -2, -2, 83, 84, -2, 88, -2,
                    86, -2, -2, -2, -1, -1, -1, -1,
                    83, -2, -2, -2, 88, -2, 86, -2,
                    84, -2, 83, -2, 79, -2, -2, -2,
                    81, -2, 83, -2, 84, -2, 86, -2,
                    87, -2, -2, -2, -1, -1, 83, -2,
                },
                Lead1 = LeadKind.Bell, LeadVol = 0.1f,
                Bass = BassKind.Octaves, BassVol = 0.19f, PadVol = 0.07f, ArpVol = 0.06f, ArpSaw = true,
                Kick = "x...x...x...x...", Clap = "....x.......x...", Hat = "..x...x...x...x.",
                KickVol = 0.32f, HatVol = 0.02f, Echo = 0.45f,
            },
            // 逆さ月の祭壇：儀式。笛の旋律と、低く響く太鼓
            new Song
            {
                Name = "abyss_moon", Bpm = 100f,
                Chords = new[] { Dm, Bb, Gm, A, Dm, Bb, Gm, A },
                Lead = new[]
                {
                    74, -2, -2, -2, 73, -2, 74, -2,
                    77, -2, -2, -2, 74, -2, -2, -2,
                    70, -2, -2, 69, 70, -2, 74, -2,
                    73, -2, -2, -2, -1, -1, 69, -2,
                    74, -2, 76, -2, 77, -2, 79, -2,
                    81, -2, -2, 79, 77, -2, -2, -2,
                    79, -2, 77, -2, 74, -2, 70, -2,
                    73, -2, -2, -2, -1, -1, -1, -1,
                },
                Lead1 = LeadKind.Flute, LeadVol = 0.11f,
                Bass = BassKind.Sustain, BassVol = 0.2f, PadVol = 0.08f, ArpVol = 0.035f,
                Kick = "x.....x.x.......", Snare = "........x.......", Hat = "....x.......x...",
                KickVol = 0.38f, HatVol = 0.02f, Echo = 0.4f,
            },
            // 深淵の玉座：最奥。速い四つ打ちに、鋸波の堂々とした旋律
            new Song
            {
                Name = "abyss_throne", Bpm = 132f,
                Chords = new[] { Fm2, Db2, Eb2, C3, Fm2, Db2, Bbm, C3 },
                Lead = new[]
                {
                    77, -2, -2, -2, 80, -2, 84, -2,
                    85, -2, -2, 84, 80, -2, -2, -2,
                    82, -2, -2, -2, 79, -2, 82, -2,
                    84, -2, -2, -2, 79, -2, -2, -2,
                    77, -2, 80, -2, 84, -2, 89, -2,
                    88, -2, -2, 85, 84, -2, -2, -2,
                    82, -2, 80, -2, 79, -2, 80, 82,
                    84, -2, -2, -2, -1, -1, -1, -1,
                },
                Lead1 = LeadKind.Saw, LeadVol = 0.08f,
                Bass = BassKind.Pump, BassVol = 0.24f, PadVol = 0.06f, ArpVol = 0.05f, ArpSaw = true,
                Kick = "x...x...x...x...", Snare = "....x.......x...", Hat = "x.xxx.xxx.xxx.xx",
                KickVol = 0.42f, HatVol = 0.025f, Duck = 0.25f, Echo = 0.2f,
            },
        };


        /// <summary>タイトル画面の曲：ゆっくりした笛の旋律と、広がる和音。灯りの消えゆく塔を見上げるイメージ。</summary>
        static readonly Song TitleSong = new Song
        {
            Name = "title", Bpm = 72f,
            Chords = new[] { Fm2, Db2, Ab2, Eb2, Fm2, Db2, Bbm, C3 },
            Lead = new[]
            {
                72, -2, -2, -2, 77, -2, -2, 75,
                73, -2, -2, -2, 72, -2, 68, -2,
                72, -2, -2, 75, -2, -2, 80, -2,
                79, -2, -2, -2, -1, -1, 75, -2,
                77, -2, -2, 79, 80, -2, 79, 77,
                80, -2, -2, -2, 77, -2, 73, -2,
                73, -2, 77, -2, -2, 73, 72, 70,
                72, -2, -2, -2, -1, -1, -1, -1,
            },
            Lead1 = LeadKind.Flute, LeadVol = 0.11f,
            Bass = BassKind.Sustain, BassVol = 0.15f, PadVol = 0.07f, ArpVol = 0.035f,
            Kick = "x.......x.......", Hat = "",
            KickVol = 0.2f, Echo = 0.4f,
        };

        /// <summary>町の曲：明るい長調の笛と、はじく分散和音。塔から戻ってひと息つくイメージ。</summary>
        static readonly Song TownSong = new Song
        {
            Name = "town", Bpm = 100f,
            Chords = new[] { F2, C, Dm, Bb2, F2, C, Bb2, C },
            Lead = new[]
            {
                72, -2, 74, 72, 69, -2, 72, -2,
                76, -2, -2, 74, 72, -2, -2, -2,
                74, -2, 77, -2, 76, 74, 72, -2,
                70, -2, -2, 72, 74, -2, -2, -2,
                72, -2, 74, 72, 69, -2, 65, -2,
                67, -2, 69, -2, 72, -2, 76, -2,
                74, -2, 72, -2, 70, -2, 69, 67,
                67, -2, -2, -2, -1, -1, -1, -1,
            },
            Lead1 = LeadKind.Flute, LeadVol = 0.11f,
            Bass = BassKind.Roots, BassVol = 0.16f, PadVol = 0.04f, ArpVol = 0.05f,
            Kick = "x.......x.......", Hat = "..x...x...x...x.",
            KickVol = 0.22f, HatVol = 0.014f, Echo = 0.25f,
        };

        /// <summary>
        /// 深淵へ行けるようになった後の町の曲：町の曲と同じ笛の旋律を、短調に沈めて遅くした編曲。
        /// 赤い月の下、静まりかえった町のイメージ。
        /// </summary>
        static readonly Song AbyssTownSong = new Song
        {
            Name = "town_abyss", Bpm = 78f,
            Chords = new[] { Fm2, Db2, Bbm, C3, Fm2, Db2, Bbm, C3 },
            Lead = new[]
            {
                72, -2, 73, 72, 68, -2, 72, -2,
                75, -2, -2, 73, 72, -2, -2, -2,
                73, -2, 77, -2, 75, 73, 72, -2,
                70, -2, -2, 72, 73, -2, -2, -2,
                72, -2, 73, 72, 68, -2, 65, -2,
                67, -2, 68, -2, 72, -2, 75, -2,
                73, -2, 72, -2, 70, -2, 68, 67,
                67, -2, -2, -2, -1, -1, -1, -1,
            },
            Lead1 = LeadKind.Flute, LeadVol = 0.1f,
            Bass = BassKind.Sustain, BassVol = 0.17f, PadVol = 0.07f, ArpVol = 0.035f,
            Kick = "x...............", Hat = "",
            KickVol = 0.22f, Echo = 0.45f,
        };

        /// <summary>黄金のスライムがいる間のボーナスタイムの曲：速い四つ打ちに、駆け上がるファンファーレの鐘。</summary>
        static readonly Song GoldenSong = new Song
        {
            Name = "golden", Bpm = 150f,
            Chords = new[] { C, G2, Am, F, C, G2, F, G2 },
            Lead = new[]
            {
                72, 76, 79, 84, 79, 76, 79, -2,
                79, -2, 74, 79, 83, -2, 79, -2,
                81, -2, 76, 81, 84, -2, 81, -2,
                77, 81, 84, -2, 81, -2, 77, -2,
                84, -2, 79, -2, 76, 79, 84, -2,
                86, -2, 83, -2, 79, 83, 86, -2,
                84, -2, 81, 77, 81, -2, 84, -2,
                86, 84, 83, 79, 74, -2, -2, -2,
            },
            Lead1 = LeadKind.Bell, LeadVol = 0.12f,
            Bass = BassKind.Octaves, BassVol = 0.2f, PadVol = 0.04f, ArpVol = 0.07f, ArpSaw = true,
            Kick = "x...x...x...x...", Clap = "....x.......x...", Hat = "..x...x...x...x.",
            KickVol = 0.38f, HatVol = 0.03f, Duck = 0.3f, Echo = 0.2f,
        };

        static readonly int[] OC = { 60, 64, 67 }, OE = { 64, 68, 71 }, OF = { 65, 69, 72 }, OFm = { 65, 68, 72 }, OAb = { 68, 72, 75 },
            OBb = { 70, 74, 77 }, OAm = { 69, 72, 76 }, ODm = { 62, 65, 69 }, OG = { 67, 71, 74 };

        /// <summary>
        /// アビス戦の曲：深淵の最奥なのに、天国のような明るさ。音数を絞った静かなパイプオルガンと、
        /// 聖歌隊、教会の鐘、大聖堂の残響。長調に、遠い和音（Ⅲ・♭Ⅵ・♭Ⅶ・短いⅣ）を混ぜて、この世ならぬ響きに。
        /// </summary>
        static readonly Song AbyssBossSong = new Song
        {
            Name = "abyss_final", Bpm = 92f,
            Chords = new[] { OC, OE, OF, OFm, OC, OAb, OBb, OC, OAm, OF, ODm, OG, OAb, OBb, OFm, OG },
            Lead = new[]
            {
                79, -2, -2, -2, 76, -2, -2, -2,
                83, -2, -2, -2, 80, -2, -2, -2,
                81, -2, -2, -2, 84, -2, -2, -2,
                80, -2, -2, -2, -2, -2, -1, -1,
                76, -2, -2, -2, 79, -2, -2, -2,
                84, -2, -2, -2, 80, -2, -2, -2,
                77, -2, -2, -2, 74, -2, -2, -2,
                79, -2, -2, -2, -2, -2, -1, -1,
                81, -2, -2, -2, 83, -2, -2, -2,
                81, -2, -2, -2, 77, -2, -2, -2,
                74, -2, -2, -2, 77, -2, -2, -2,
                83, -2, -2, -2, -2, -2, -1, -1,
                84, -2, -2, -2, 87, -2, -2, -2,
                86, -2, -2, -2, 82, -2, -2, -2,
                80, -2, -2, -2, 84, -2, -2, -2,
                86, -2, -2, -2, -2, -2, -1, -1,
            },
            Lead1 = LeadKind.Organ, LeadVol = 0.07f,
            Bass = BassKind.Sustain, BassVol = 0.16f, PadVol = 0.05f, ArpVol = 0f,
            Kick = "", Hat = "",
            Echo = 0.2f, Church = true, ChoirVol = 0.07f, BellVol = 0.08f, Reverb = 0.6f, Gain = 0.75f,
        };

        /// <summary>区画の曲のボス戦版：同じ和音と旋律を、速く、四つ打ちと刻む低音で。</summary>
        static Song BossOf(Song s) => new Song
        {
            Name = s.Name + "_boss",
            Bpm = Mathf.Min(150f, Mathf.Max(132f, s.Bpm * 1.35f)),
            Chords = s.Chords,
            Lead = s.Lead,
            Lead1 = s.Lead1 == LeadKind.Square ? LeadKind.Square : LeadKind.Saw,
            LeadVol = s.Lead1 == LeadKind.Square ? 0.08f : 0.09f,
            Bass = BassKind.Pump, BassVol = 0.24f, PadVol = 0.04f, ArpVol = 0.05f, ArpSaw = true,
            Kick = "x...x...x...x...", Snare = "....x.......x...", Hat = "..x...x...x...x.",
            KickVol = 0.42f, HatVol = 0.025f, Echo = 0.18f,
        };

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
                    KickVol = 0.42f, Echo = 0.22f,
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
                    KickVol = 0.4f, Echo = 0.15f,
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
                    KickVol = 0.38f, Echo = 0.3f,
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
                    KickVol = 0.42f, HatVol = 0.02f, Echo = 0.15f,
                },
            },
        };

        // ================= 曲を作る =================

        static AudioClip Render(Song s) => MakeClip(s.Name, RenderData(s));

        /// <summary>曲のデータを作る（計算だけなので、別のスレッドでも動かせる）。</summary>
        static float[] RenderData(Song s)
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
                    foreach (int n in chord)
                    {
                        if (s.Church) AddOrgan(music, barStart, step * 16, Freq(n), s.PadVol, false);
                        else AddPad(music, barStart, step * 16, Freq(n), s.PadVol);
                    }
                }

                // 聖歌隊（和音の1オクターブ上）と、4小節ごとの鐘
                if (s.ChoirVol > 0f)
                {
                    foreach (int n in chord) AddChoir(music, barStart, step * 16, Freq(n + 12), s.ChoirVol);
                }
                if (s.BellVol > 0f && bar % 4 == 0) AddChurchBell(echoBus, barStart, step * 32, Freq(chord[0] + 12), s.BellVol);

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
                    if (note >= 0 && s.Church) AddOrgan(music, barStart + k * step, length, Freq(note - 12), s.BassVol, false, true);
                    else if (note >= 0) AddBass(music, barStart + k * step, length, Freq(note - 12), s.BassVol, s.Bass == BassKind.Pump);
                }

                // 分散和音（16分音符で和音の音を上へたどる）
                if (s.ArpVol > 0f)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        int n = chord[k % chord.Length] + 12 * (1 + (k / chord.Length) % 2);
                        if (s.Church) { if (k % 2 == 0) AddOrgan(echoBus, barStart + k * step, step * 2, Freq(n), s.ArpVol, false); } // 8分音符で、やわらかい管で
                        else AddPluck(echoBus, barStart + k * step, step * 2, Freq(n), s.ArpVol, s.ArpSaw);
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

            float[] hall = s.Reverb > 0f ? Cathedral(music, echoBus, wet) : null;

            var mix = new float[len];
            float peak = 0.0001f;
            for (int i = 0; i < len; i++)
            {
                mix[i] = (music[i] + echoBus[i] + wet[i]) * duck[i] + drums[i] + (hall != null ? hall[i] * s.Reverb : 0f);
                peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            }
            float norm = 0.9f / peak * s.Gain;
            for (int i = 0; i < len; i++) mix[i] = Mathf.Clamp(mix[i] * norm, -1f, 1f);
            return mix;
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
                float x = (Saw(freq * 1.0012f * t) + Saw(freq * 0.9988f * t)) * 0.5f; // ずれは小さく（揺れすぎないように）
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
                float vib = 1f + 0.0025f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Clamp01((t - 0.25f) * 3f);
                float ph = freq * vib * t;
                float x;
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / release);
                switch (kind)
                {
                    case LeadKind.Square: x = Mathf.Repeat(ph, 1f) < 0.3f ? 1f : -1f; break;
                    case LeadKind.Saw: x = (Saw(ph) + Saw(ph * 1.0015f)) * 0.5f; break;
                    case LeadKind.Organ: // 主旋律のオルガン：やわらかい笛の管を重ねて、ゆれ（トレモロ）を少し
                        x = (Mathf.Sin(2f * Mathf.PI * ph) + 0.4f * Mathf.Sin(4f * Mathf.PI * ph) + 0.12f * Mathf.Sin(6f * Mathf.PI * ph)
                            + 0.1f * Mathf.Sin(8f * Mathf.PI * ph)) * 0.6f
                            * (1f + 0.06f * Mathf.Sin(2f * Mathf.PI * 6f * t));
                        break;
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

        /// <summary>
        /// パイプオルガン：何本かの管（16'・8'・4'・2 2/3'・2'）を重ねた音。立ち上がりはすぐ、消えるのもすぐ。
        /// bright で高い管を強めに（分散和音）、pedal で足鍵盤の太い低音に。
        /// </summary>
        static void AddOrgan(float[] buf, int start, int len, float freq, float vol, bool bright, bool pedal = false)
        {
            float[] ratio = pedal ? PedalRatio : OrganRatio;
            float[] amp = pedal ? PedalAmp : bright ? OrganBrightAmp : OrganAmp;
            float total = 0f;
            foreach (float v in amp) total += v;
            int attack = (int)(0.012f * Rate), release = System.Math.Min(len / 3, (int)(0.04f * Rate));
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float x = 0f;
                for (int h = 0; h < ratio.Length; h++)
                {
                    float f = freq * ratio[h];
                    if (f > Rate * 0.45f) break;
                    x += amp[h] * Mathf.Sin(2f * Mathf.PI * f * t);
                }
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / System.Math.Max(1, release));
                Add(buf, start + j, x / total * env * vol);
            }
        }

        static readonly float[] OrganRatio = { 0.5f, 1f, 2f, 3f, 4f, 6f };
        static readonly float[] OrganAmp = { 0.35f, 1f, 0.6f, 0.25f, 0.3f, 0.08f };
        static readonly float[] OrganBrightAmp = { 0f, 1f, 0.8f, 0.4f, 0.6f, 0.3f };
        static readonly float[] PedalRatio = { 1f, 2f, 3f, 4f };
        static readonly float[] PedalAmp = { 1f, 0.7f, 0.2f, 0.2f };

        /// <summary>聖歌隊：ゆれる2つの鋸波を、「あ」の響き（700Hz と 1100Hz あたり）だけ通す。ゆっくり立ち上がる。</summary>
        static void AddChoir(float[] buf, int start, int len, float freq, float vol)
        {
            float f1 = 2f * Mathf.Sin(Mathf.PI * 730f / Rate), f2 = 2f * Mathf.Sin(Mathf.PI * 1090f / Rate);
            float l1 = 0f, b1 = 0f, l2 = 0f, b2 = 0f;
            int attack = (int)(0.4f * Rate), release = (int)(0.35f * Rate);
            float pa = 0f, pb = 0f;
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float vib = 1f + 0.004f * Mathf.Sin(2f * Mathf.PI * 5f * t);
                pa += freq * vib * 1.003f / Rate;
                pb += freq * vib * 0.997f / Rate;
                float x = (Saw(pa) + Saw(pb)) * 0.5f;
                // 2つの帯域フィルタ（状態変数フィルタ）
                float h1 = x - l1 - 0.25f * b1; b1 += f1 * h1; l1 += f1 * b1;
                float h2 = x - l2 - 0.3f * b2; b2 += f2 * h2; l2 += f2 * b2;
                float y = b1 * 0.6f + b2 * 0.4f;
                float env = Mathf.Min(1f, (float)j / attack) * Mathf.Min(1f, (float)(len - j) / release);
                Add(buf, start + j, y * env * vol * 0.5f);
            }
        }

        /// <summary>教会の鐘：低いうなり・短三度の響きなど、少しずれた倍音を重ねて、長く残す。</summary>
        static void AddChurchBell(float[] buf, int start, int len, float freq, float vol)
        {
            float[] r = { 0.5f, 1f, 1.19f, 1.5f, 2f, 2.51f, 3.01f };
            float[] a = { 0.5f, 1f, 0.5f, 0.3f, 0.45f, 0.2f, 0.15f };
            float[] d = { 0.6f, 0.9f, 1.4f, 1.6f, 1.8f, 2.6f, 3.2f };
            for (int j = 0; j < len; j++)
            {
                float t = (float)j / Rate;
                float x = 0f;
                for (int h = 0; h < r.Length; h++) x += a[h] * Mathf.Sin(2f * Mathf.PI * freq * r[h] * t) * Mathf.Exp(-t * d[h]);
                Add(buf, start + j, x * Mathf.Min(1f, j / 30f) * vol * 0.4f);
            }
        }

        /// <summary>大聖堂の残響：4本の遅れの輪（櫛形）と2本の全域通過。ループのつなぎ目で途切れないよう2周回す。</summary>
        static float[] Cathedral(float[] a, float[] b, float[] c)
        {
            int len = a.Length;
            var input = new float[len];
            for (int i = 0; i < len; i++) input[i] = (a[i] + b[i] + c[i]) * 0.25f;
            var output = new float[len];
            int[] combs = { 1557, 1617, 1491, 1422 };
            foreach (int dl in combs)
            {
                var y = new float[len];
                float lp = 0f;
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 0; i < len; i++)
                    {
                        int j = (i - dl + len) % len;
                        lp += 0.35f * (y[j] - lp); // 高い音ほど早く消える
                        y[i] = input[i] + lp * 0.86f;
                    }
                }
                for (int i = 0; i < len; i++) output[i] += y[i];
            }
            foreach (int dl in new[] { 225, 556 })
            {
                var y = new float[len];
                var x = (float[])output.Clone();
                for (int pass = 0; pass < 2; pass++)
                {
                    for (int i = 0; i < len; i++)
                    {
                        int j = (i - dl + len) % len;
                        y[i] = -0.5f * x[i] + x[j] + 0.5f * y[j];
                    }
                }
                output = y;
            }
            return output;
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
