namespace ProdHelperService.Translation;

// Thrown when the translation backend (LibreTranslate) is unreachable, errors out, or doesn't
// support the requested language pair - callers catch this specifically so a translation hiccup
// degrades gracefully instead of failing the whole request.
public class TranslationUnavailableException(string message) : Exception(message);
