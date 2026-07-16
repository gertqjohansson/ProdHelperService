namespace ProdHelperService.AdminApp;

// Codes match client/src/i18n/languages.js — keep both lists in sync by hand
// when adding a language, there's no shared codegen across the JS/C# boundary.
//
// FlagResourceName is the embedded PNG's manifest suffix (Flags\{name}.png, pre-rasterized
// from the same flag-icons SVGs the web client uses). DateCulture is a region-qualified
// culture (e.g. "en-GB" not bare "en") used only for footer clock formatting — matches the
// web client's `dateLocale` field, for the same reason: bare language codes resolve to ICU's
// generic (often US-biased) date format rather than the region implied by the flag shown.
internal static class SupportedLanguages
{
    public static readonly (string Code, string NativeName, string FlagResourceName, string DateCulture)[] All =
    [
        ("sv", "Svenska", "se", "sv-SE"),
        ("da", "Dansk", "dk", "da-DK"),
        ("nb", "Norsk (bokmål)", "no", "nb-NO"),
        ("fi", "Suomi", "fi", "fi-FI"),
        ("de", "Deutsch", "de", "de-DE"),
        ("en", "English", "gb", "en-GB"),
        ("fr", "Français", "fr", "fr-FR"),
        ("it", "Italiano", "it", "it-IT"),
        ("es", "Español", "es", "es-ES"),
        ("pt-PT", "Português", "pt", "pt-PT"),
        ("el", "Ελληνικά", "gr", "el-GR"),
        ("pl", "Polski", "pl", "pl-PL"),
        ("cs", "Čeština", "cz", "cs-CZ"),
        ("sk", "Slovenčina", "sk", "sk-SK"),
        ("hu", "Magyar", "hu", "hu-HU"),
        ("bg", "Български", "bg", "bg-BG"),
        ("hr", "Hrvatski", "hr", "hr-HR"),
        ("sr-Latn", "Srpski (latinica)", "rs", "sr-Latn-RS"),
        ("sl", "Slovenščina", "si", "sl-SI"),
        ("lt", "Lietuvių", "lt", "lt-LT"),
        ("lv", "Latviešu", "lv", "lv-LV"),
        ("et", "Eesti", "ee", "et-EE"),
        ("sq", "Shqip", "al", "sq-AL"),
    ];
}
