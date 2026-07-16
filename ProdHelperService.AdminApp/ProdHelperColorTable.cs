namespace ProdHelperService.AdminApp;

// Recolors the native MenuStrip/ToolStripDropDown to the same teal/near-black/gold palette
// the web client uses (see client/src/index.css) — same three colors, no gradients, to read
// as one flat brand bar rather than the default Windows 3D-ish menu chrome.
internal sealed class ProdHelperColorTable : ProfessionalColorTable
{
    private static readonly Color Teal = ColorTranslator.FromHtml("#27627B");
    private static readonly Color TealDark = ColorTranslator.FromHtml("#1B4455");
    private static readonly Color NearBlack = ColorTranslator.FromHtml("#18130C");
    private static readonly Color Gold = ColorTranslator.FromHtml("#AA8A55");

    public override Color MenuStripGradientBegin => Teal;
    public override Color MenuStripGradientEnd => Teal;

    public override Color ToolStripDropDownBackground => NearBlack;
    public override Color ImageMarginGradientBegin => NearBlack;
    public override Color ImageMarginGradientMiddle => NearBlack;
    public override Color ImageMarginGradientEnd => NearBlack;

    public override Color MenuBorder => TealDark;
    public override Color MenuItemBorder => Gold;

    public override Color MenuItemSelected => Gold;
    public override Color MenuItemSelectedGradientBegin => Gold;
    public override Color MenuItemSelectedGradientEnd => Gold;

    public override Color MenuItemPressedGradientBegin => Gold;
    public override Color MenuItemPressedGradientEnd => Gold;

    public override Color ToolStripBorder => TealDark;
    public override Color SeparatorDark => TealDark;
    public override Color SeparatorLight => TealDark;
}
