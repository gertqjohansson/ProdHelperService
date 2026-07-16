namespace ProdHelperService.AdminApp;

// Codes match client/src/i18n/languages.js — keep both lists in sync by hand
// when adding a language, there's no shared codegen across the JS/C# boundary.
internal static class SupportedLanguages
{
    public static readonly (string Code, string NativeName)[] All =
    [
        ("sv", "Svenska"),
        ("da", "Dansk"),
        ("nb", "Norsk (bokmål)"),
        ("fi", "Suomi"),
        ("de", "Deutsch"),
        ("en", "English"),
        ("fr", "Français"),
        ("it", "Italiano"),
        ("es", "Español"),
        ("pt-PT", "Português"),
        ("el", "Ελληνικά"),
        ("pl", "Polski"),
        ("cs", "Čeština"),
        ("sk", "Slovenčina"),
        ("hu", "Magyar"),
        ("bg", "Български"),
        ("hr", "Hrvatski"),
        ("sr-Latn", "Srpski (latinica)"),
        ("sl", "Slovenščina"),
        ("lt", "Lietuvių"),
        ("lv", "Latviešu"),
        ("et", "Eesti"),
        ("sq", "Shqip"),
    ];
}
