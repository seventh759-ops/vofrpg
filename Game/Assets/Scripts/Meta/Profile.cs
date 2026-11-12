using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>キャラクター1人の育ち具合（3つの枠のどれか。職業は作るときに選ぶ）。挑戦をまたいで残る。</summary>
    [Serializable]
    public sealed class CharacterData
    {
        public int Slot = -1;                         // 枠（0〜2）。-1 は前の版のデータ（読み込み時に枠を割り当てる）
        public int ClassId;
        public int Level = 1;
        public int Exp;
        public int Points;
        public int[] Alloc = new int[Attributes.Count]; // 自分で振った能力値

        /// <summary>レベルアップのたびに、そのときの WIS で積み上がった最大 MP。</summary>
        public int MpGrown;
        /// <summary>MpGrown を導入する前のセーブを一度だけ見積もり直したか。</summary>
        public bool MpGrowthReady;

        // スキル（覚えるのはレベルで自動。強さは使った回数で上がる）
        public int[] SkillLv = { 1, 1, 1, 1, 1, 1, 1, 1 };
        public int[] SkillUses = new int[SkillInfo.PerClass];
        public int[] Slots = { 0, -1, -1 };            // ボタンに付けたスキルの番号（-1 は空き）
        public bool[] SlotAuto = new bool[SkillInfo.Slots];

        // 持ち帰り（帰還したときだけ書き換わる。死んだときは前回の帰還のまま）
        public List<Item> Loadout = new List<Item>(); // 装備欄5つ（空きは Level 0 の印）
        public List<Item> Stash = new List<Item>();   // 倉庫。次の挑戦では鞄に入った状態で始まる
        public int Embers;                            // 持ち帰った火種の合計
        public int CarrySlots = 3;                    // 今回拾った品から持ち帰れる数
        public int BestFloor;                         // たどり着いた一番深い層
        public int BossCleared;                       // 倒した一番深いボスの層（その次の層がチェックポイント）
        public RunSave Run = new RunSave();           // 中断した挑戦
        public int Advanced = -1;                     // 上位職（-1 はまだ。0 か 1）
        public bool Third;                            // 転生して3次職になった
        public int HpPotions;                         // 回復薬（町の商人で買う）
        public int MpPotions;                         // 魔力の薬

        public void Repair()
        {
            if (Level < 1) Level = 1;
            if (Exp < 0) Exp = 0;
            if (Points < 0) Points = 0;
            if (MpGrown < 0) MpGrown = 0;
            if (Loadout == null) Loadout = new List<Item>();
            if (Stash == null) Stash = new List<Item>();
            if (CarrySlots < 1) CarrySlots = 3;
            CarrySlots = Mathf.Min(CarrySlots, Town.MaxCarrySlots);
            if (Advanced < -1 || Advanced > 1) Advanced = -1;
            if (Advanced < 0) Third = false;
            HpPotions = Mathf.Clamp(HpPotions, 0, Town.MaxPotions);
            MpPotions = Mathf.Clamp(MpPotions, 0, Town.MaxPotions);
            foreach (var it in Loadout) it?.Repair();
            foreach (var it in Stash) it?.Repair();
            if (Run == null) Run = new RunSave();
            Run.Repair();
            Stash.RemoveAll(it => it == null || it.IsEmptyMarker);
            Alloc = Fit(Alloc, Attributes.Count, 0);
            SkillLv = Fit(SkillLv, SkillInfo.PerClass, 1);
            SkillUses = Fit(SkillUses, SkillInfo.PerClass, 0);
            Slots = Fit(Slots, SkillInfo.Slots, -1);
            if (SlotAuto == null || SlotAuto.Length != SkillInfo.Slots)
            {
                var fixedAuto = new bool[SkillInfo.Slots];
                if (SlotAuto != null) Array.Copy(SlotAuto, fixedAuto, Mathf.Min(SlotAuto.Length, fixedAuto.Length));
                SlotAuto = fixedAuto;
            }
            for (int i = 0; i < SkillLv.Length; i++) SkillLv[i] = Mathf.Clamp(SkillLv[i], 1, SkillInfo.MaxSkillLevel);
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i] < -1 || Slots[i] >= SkillInfo.PerClass) Slots[i] = -1;
            }
        }

        static int[] Fit(int[] source, int length, int fill)
        {
            if (source != null && source.Length == length) return source;
            var result = new int[length];
            for (int i = 0; i < length; i++) result[i] = source != null && i < source.Length ? source[i] : fill;
            return result;
        }
    }

    /// <summary>中断した挑戦（アプリを閉じても、その層の最初から続けられる）。</summary>
    [Serializable]
    public sealed class RunSave
    {
        public bool Active;
        public int Floor = 1;
        public List<Item> Equipped = new List<Item>(); // 5つ（空きは Level 0 の印）
        public List<Item> Bag = new List<Item>();
        public List<bool> BagCarried = new List<bool>(); // 鞄の品が前の挑戦から持ち込んだ物か
        public int Embers;
        public List<int> Boons = new List<int>();
        public float HpRatio = 1f;
        public float MpRatio = 1f;

        public void Repair()
        {
            if (Equipped == null) Equipped = new List<Item>();
            if (Bag == null) Bag = new List<Item>();
            if (BagCarried == null) BagCarried = new List<bool>();
            if (Boons == null) Boons = new List<int>();
            foreach (var it in Equipped) it?.Repair();
            foreach (var it in Bag) it?.Repair();
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public int Bgm;                                       // 0 = 区画ごと（前の版の MusicTheme は番号の意味が違うので使わない）
        public int Language;                                  // 0 = English（デフォルト）, 1 = 日本語
        public List<string> TipsSeen = new List<string>(); // 案内役が説明し終えた場面
        public List<CharacterData> Characters = new List<CharacterData>();
        public List<Item> Storage = new List<Item>();      // 町の倉庫（全職業で共有）
    }

    /// <summary>
    /// セーブデータ（端末の persistentDataPath に JSON で保存）。
    /// 今はキャラクターのレベル・経験値・能力値だけを保存する。装備の持ち帰りは M5。
    /// </summary>
    public static class Profile
    {
        static SaveData data;

        public static CharacterData Current { get; private set; }

        static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            data = null;
            Current = null;
        }

        public const int SlotCount = 3;

        // 枠を選んでいないときに使う、保存しない仮のキャラクター（タイトルの後ろの画面など）
        static readonly Dictionary<int, CharacterData> blanks = new Dictionary<int, CharacterData>();

        /// <summary>
        /// その職業の「今のキャラクター」。選んでいるキャラクターがその職業ならそれを、
        /// そうでなければ保存しない仮のキャラクター（Lv1）を返す。
        /// </summary>
        public static CharacterData Get(ClassType type)
        {
            EnsureLoaded();
            if (Current != null && Current.ClassId == (int)type) return Current;
            if (!blanks.TryGetValue((int)type, out var blank))
            {
                blank = new CharacterData { ClassId = (int)type, MpGrowthReady = true };
                blanks[(int)type] = blank;
            }
            return blank;
        }

        /// <summary>枠のキャラクター（空なら null）。</summary>
        public static CharacterData InSlot(int slot)
        {
            EnsureLoaded();
            foreach (var c in data.Characters)
            {
                if (c.Slot == slot) return c;
            }
            return null;
        }

        public static void SelectSlot(int slot)
        {
            Current = InSlot(slot);
        }

        /// <summary>空いた枠に、選んだ職業でキャラクターを作る。</summary>
        public static CharacterData CreateInSlot(int slot, ClassType type)
        {
            EnsureLoaded();
            data.Characters.RemoveAll(c => c.Slot == slot);
            var created = new CharacterData { Slot = slot, ClassId = (int)type, MpGrowthReady = true };
            data.Characters.Add(created);
            Current = created;
            Save();
            return created;
        }

        /// <summary>枠のキャラクターを消す（倉庫は全キャラクターで共有なので残る）。</summary>
        public static void DeleteSlot(int slot)
        {
            EnsureLoaded();
            data.Characters.RemoveAll(c => c.Slot == slot);
            if (Current != null && Current.Slot == slot) Current = null;
            Save();
        }

        /// <summary>今のキャラクターを、同じ枠・同じ職業のまっさらな状態に作り直す（すべてリセット）。</summary>
        public static void ResetCurrent()
        {
            EnsureLoaded();
            if (Current == null) return;
            int slot = Current.Slot, cls = Current.ClassId;
            data.Characters.Remove(Current);
            var fresh = new CharacterData { Slot = slot, ClassId = cls, MpGrowthReady = true };
            data.Characters.Add(fresh);
            Current = fresh;
        }

        /// <summary>挑戦の開始用：今のキャラクターがその職業でなければ、仮のキャラクターにする（保存しない）。</summary>
        public static void Select(ClassType type)
        {
            if (Current == null || Current.ClassId != (int)type) Current = Get(type);
        }

        /// <summary>町の倉庫（全職業で共有）。</summary>
        public static List<Item> Storage
        {
            get
            {
                EnsureLoaded();
                return data.Storage;
            }
        }

        /// <summary>選んだ BGM のテーマ（どの職業でも共通）。</summary>
        public static int MusicTheme
        {
            get
            {
                EnsureLoaded();
                return data.Bgm;
            }
            set
            {
                EnsureLoaded();
                data.Bgm = value;
            }
        }

        /// <summary>表示の言語（Loc で使う）。</summary>
        public static int LanguageSetting
        {
            get
            {
                EnsureLoaded();
                return data.Language;
            }
            set
            {
                EnsureLoaded();
                data.Language = value;
            }
        }

        public static bool TipSeen(string id)
        {
            EnsureLoaded();
            return data.TipsSeen.Contains(id);
        }

        public static void MarkTip(string id)
        {
            EnsureLoaded();
            if (!data.TipsSeen.Contains(id)) data.TipsSeen.Add(id);
            Save();
        }

        public static void Save()
        {
            if (data == null) return;
            try
            {
                // 書きかけで壊れないよう、一時ファイルに書いてから置き換える
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Save failed: {e.Message}");
            }
        }

        static void EnsureLoaded()
        {
            if (data != null) return;
            try
            {
                if (File.Exists(FilePath)) data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Load failed, starting fresh: {e.Message}");
            }
            if (data == null) data = new SaveData();
            if (data.Characters == null) data.Characters = new List<CharacterData>();
            if (data.TipsSeen == null) data.TipsSeen = new List<string>();
            if (data.Storage == null) data.Storage = new List<Item>();
            foreach (var it in data.Storage) it?.Repair();
            data.Storage.RemoveAll(it => it == null || it.IsEmptyMarker);
            foreach (var c in data.Characters) c.Repair();
            // 前の版（職業ごとに1人）のデータは、空いている枠に順に入れる。入らない分は消す
            var used = new bool[SlotCount];
            foreach (var c in data.Characters) if (c.Slot >= 0 && c.Slot < SlotCount) used[c.Slot] = true;
            foreach (var c in data.Characters)
            {
                if (c.Slot >= 0 && c.Slot < SlotCount) continue;
                c.Slot = -1;
                for (int i = 0; i < SlotCount; i++)
                {
                    if (used[i]) continue;
                    used[i] = true;
                    c.Slot = i;
                    break;
                }
            }
            data.Characters.RemoveAll(c => c.Slot < 0);
        }
    }
}
