namespace VividOfFade
{
    /// <summary>表示する言語。デフォルトは英語。</summary>
    public enum Language { English = 0, Japanese = 1 }

    /// <summary>
    /// 表示の言語の切り替え。文字は呼ぶ場所で英語と日本語を並べて書く（Loc.T("Bag", "鞄")）。
    /// 選んだ言語はセーブデータに保存する。日本語のフォントは後で登録する（それまでは □ になることがある）。
    /// </summary>
    public static class Loc
    {
        public static Language Current => (Language)Profile.LanguageSetting;

        public static bool Ja => Current == Language.Japanese;

        public static string T(string en, string ja) => Ja ? ja : en;

        public static string LanguageName => Ja ? "日本語" : "English";

        public static void Next()
        {
            Profile.LanguageSetting = Ja ? (int)Language.English : (int)Language.Japanese;
            Profile.Save();
        }
    }
}
