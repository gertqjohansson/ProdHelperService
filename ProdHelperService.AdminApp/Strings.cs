using System.Globalization;
using System.Resources;

namespace ProdHelperService.AdminApp;

// Hand-written ResourceManager wrapper instead of a VS-generated
// Strings.Designer.cs — that generator only runs at design time inside
// Visual Studio, not as part of `dotnet build`, which this app relies on
// exclusively. Satellite assemblies (Strings.{culture}.resx) still build
// automatically under plain `dotnet build`/`dotnet run`.
internal static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("ProdHelperService.AdminApp.Strings", typeof(Strings).Assembly);

    public static string WindowTitle => Get("WindowTitle");
    public static string Try1ButtonText => Get("Try1ButtonText");
    public static string Try2ButtonText => Get("Try2ButtonText");
    public static string LanguageMenuText => Get("LanguageMenuText");
    public static string MenuLabel => Get("MenuLabel");
    public static string GuestLabel => Get("GuestLabel");

    public static string CallSuccessHeading(string relativeUrl, Uri? baseAddress) =>
        string.Format(CultureInfo.CurrentUICulture, Get("CallSuccessHeading"), relativeUrl, baseAddress);

    public static string CallErrorHeading(string relativeUrl) =>
        string.Format(CultureInfo.CurrentUICulture, Get("CallErrorHeading"), relativeUrl);

    public static string ItemSelectedFormat(string item) =>
        string.Format(CultureInfo.CurrentUICulture, Get("ItemSelectedFormat"), item);

    private static string Get(string name) =>
        ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? name;
}
