namespace ProdHelperService.AdminApp;

public static class EmbeddedImages
{
    public static Bitmap LoadLoginBackground() => Load("Images.login-background.png");

    public static Bitmap FromBase64Png(string base64) =>
        new(new MemoryStream(Convert.FromBase64String(base64)));

    // Flag PNGs are embedded resources (Flags\{code}.png), pre-rasterized from the same
    // flag-icons SVGs the web client uses — see SupportedLanguages.cs.
    public static Image? LoadFlagImage(string flagResourceName)
    {
        string resourceName = $"ProdHelperService.AdminApp.Flags.{flagResourceName}.png";
        using Stream? resourceStream = typeof(EmbeddedImages).Assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null) return null;

        using var bitmap = new Bitmap(resourceStream);
        return new Bitmap(bitmap); // clone so the resource stream can be safely disposed
    }

    private static Bitmap Load(string manifestSuffix)
    {
        string resourceName = $"ProdHelperService.AdminApp.{manifestSuffix}";
        using Stream resourceStream = typeof(EmbeddedImages).Assembly.GetManifestResourceStream(resourceName)!;
        using var bitmap = new Bitmap(resourceStream);
        return new Bitmap(bitmap); // clone so the resource stream can be safely disposed
    }
}
