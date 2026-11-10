using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>職業ごとのキャラクターの育ち具合。挑戦をまたいで残る。</summary>
    [Serializable]
    public sealed class CharacterData
    {
        public int ClassId;
        public int Level = 1;
        public int Exp;
        public int Points;
        public int[] Alloc = new int[Attributes.Count]; // 自分で振った能力値

        public void Repair()
        {
            if (Level < 1) Level = 1;
            if (Exp < 0) Exp = 0;
            if (Points < 0) Points = 0;
            if (Alloc == null || Alloc.Length != Attributes.Count)
            {
                var fixedAlloc = new int[Attributes.Count];
                if (Alloc != null) Array.Copy(Alloc, fixedAlloc, Mathf.Min(Alloc.Length, fixedAlloc.Length));
                Alloc = fixedAlloc;
            }
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int Version = 1;
        public List<CharacterData> Characters = new List<CharacterData>();
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

        public static CharacterData Get(ClassType type)
        {
            EnsureLoaded();
            foreach (var c in data.Characters)
            {
                if (c.ClassId == (int)type) return c;
            }
            var created = new CharacterData { ClassId = (int)type };
            data.Characters.Add(created);
            return created;
        }

        public static void Select(ClassType type)
        {
            Current = Get(type);
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
            foreach (var c in data.Characters) c.Repair();
        }
    }
}
