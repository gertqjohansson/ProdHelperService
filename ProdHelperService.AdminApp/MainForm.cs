using System.Globalization;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using ProdHelperService.Contracts;

namespace ProdHelperService.AdminApp;

// Calls the controllers over HTTP, against the local Kestrel API that
// ProdHelperService hosts for Swagger/testing (see Program.cs in that
// project) — no Azure Relay, no RelayListener, and no direct reference to
// ProdHelperService.Controllers involved. ProdHelperService must be running
// (`dotnet run` in that project) for these calls to succeed.
public class MainForm : Form
{
    private static readonly string[] Try1Parameters = ["id", "1,1", "Start", "2026-07-01", "end", "2026-07-12"];
    private static readonly string[] Try2Parameters = ["id", "5,2", "Start", "2026-07-01", "end", "2026-07-12", "break", "true"];

    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");

    private const int TopBarHeight = 36;
    private const int FooterHeight = 28;

    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;
    private readonly CultureInfo _dateCulture;

    private readonly MenuStrip _menuStrip;
    private readonly Button _try1Button;
    private readonly Button _try2Button;
    private readonly TextBox _output;
    private readonly Panel _footer;
    private readonly Label _footerClock;
    private readonly System.Windows.Forms.Timer _footerTimer;

    public MainForm(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;
        _dateCulture = CultureInfo.GetCultureInfo(
            SupportedLanguages.All.FirstOrDefault(l => l.Code == _settings.Culture).DateCulture ?? "en-GB");

        Text = Strings.WindowTitle;
        Width = 640;
        Height = 440 + TopBarHeight + FooterHeight;
        StartPosition = FormStartPosition.CenterScreen;

        _menuStrip = BuildMenuStrip();
        _footer = BuildFooter(out _footerClock);
        _footerTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _footerTimer.Tick += (_, _) => UpdateFooterClock();

        _try1Button = new Button { Text = Strings.Try1ButtonText, Left = 20, Top = 20 + TopBarHeight, Width = 120, Height = 32 };
        _try2Button = new Button { Text = Strings.Try2ButtonText, Left = 150, Top = 20 + TopBarHeight, Width = 120, Height = 32 };
        _output = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(FontFamily.GenericMonospace, 9),
            Left = 20,
            Top = 65 + TopBarHeight,
            Width = 580,
            Height = 320,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
        };

        _try1Button.Click += async (_, _) => await CallController(
            ApiRoutes.OeeCalculate,
            new ParametersRequest { Parameters = Try1Parameters });

        _try2Button.Click += async (_, _) => await CallController(
            ApiRoutes.PlannerGetInteruption,
            new ParametersRequest { Parameters = Try2Parameters });

        Controls.Add(_try1Button);
        Controls.Add(_try2Button);
        Controls.Add(_output);
        Controls.Add(_footer);
        Controls.Add(_menuStrip);
        MainMenuStrip = _menuStrip;

        UpdateFooterClock();
        _footerTimer.Start();
        FormClosed += (_, _) => _footerTimer.Stop();
    }

    private MenuStrip BuildMenuStrip()
    {
        var menuStrip = new MenuStrip
        {
            BackColor = TealPrimary,
            ForeColor = Color.White,
            Renderer = new ToolStripProfessionalRenderer(new ProdHelperColorTable()),
            Padding = new Padding(8, 6, 8, 6),
        };

        var menuItem = new ToolStripMenuItem(Strings.MenuLabel, BuildHamburgerIcon()) { ForeColor = Color.White };
        var serviceItem = new ToolStripMenuItem("Service") { ForeColor = Color.White };
        serviceItem.Click += (_, _) => MessageBox.Show(
            Strings.ItemSelectedFormat("Service"), "Service", MessageBoxButtons.OK, MessageBoxIcon.Information);
        menuItem.DropDownItems.Add(serviceItem);

        var guestItem = new ToolStripMenuItem(Strings.GuestLabel, BuildUserIcon())
        {
            ForeColor = Color.White,
            Alignment = ToolStripItemAlignment.Right,
        };
        var languageItem = new ToolStripMenuItem(Strings.LanguageMenuText) { ForeColor = Color.White };
        foreach (var (code, nativeName, flagResourceName, _) in SupportedLanguages.All)
        {
            var item = new ToolStripMenuItem(nativeName, LoadFlagImage(flagResourceName))
            {
                ForeColor = Color.White,
                Checked = code == _settings.Culture,
            };
            item.Click += (_, _) =>
            {
                _settings.Culture = code;
                _settings.Save();
                Application.Restart();
            };
            languageItem.DropDownItems.Add(item);
        }
        guestItem.DropDownItems.Add(languageItem);

        menuStrip.Items.Add(menuItem);
        menuStrip.Items.Add(guestItem);
        return menuStrip;
    }

    private Panel BuildFooter(out Label clockLabel)
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = FooterHeight,
            BackColor = TealPrimary,
        };
        clockLabel = new Label
        {
            Dock = DockStyle.Right,
            Width = 220,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 12, 0),
        };
        footer.Controls.Add(clockLabel);
        return footer;
    }

    private void UpdateFooterClock()
    {
        _footerClock.Text = DateTime.Now.ToString("G", _dateCulture);
    }

    private static Bitmap BuildHamburgerIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        using var pen = new Pen(Color.White, 2);
        g.DrawLine(pen, 2, 4, 14, 4);
        g.DrawLine(pen, 2, 8, 14, 8);
        g.DrawLine(pen, 2, 12, 14, 12);
        return bmp;
    }

    private static Bitmap BuildUserIcon()
    {
        var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        using var brush = new SolidBrush(Color.White);
        g.FillEllipse(brush, 5, 1, 6, 6);
        g.FillEllipse(brush, 2, 8, 12, 10);
        return bmp;
    }

    // Flag PNGs are embedded resources (Flags\{code}.png), pre-rasterized from the same
    // flag-icons SVGs the web client uses — see SupportedLanguages.cs.
    private static Image? LoadFlagImage(string flagResourceName)
    {
        string resourceName = $"ProdHelperService.AdminApp.Flags.{flagResourceName}.png";
        using Stream? resourceStream = typeof(MainForm).Assembly.GetManifestResourceStream(resourceName);
        if (resourceStream is null) return null;

        using var bitmap = new Bitmap(resourceStream);
        return new Bitmap(bitmap); // clone so the resource stream can be safely disposed
    }

    private async Task CallController(string relativeUrl, ParametersRequest request)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(relativeUrl, request);
            response.EnsureSuccessStatusCode();

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            _output.Text = Strings.CallSuccessHeading(relativeUrl, _httpClient.BaseAddress)
                + Environment.NewLine + Environment.NewLine + json;
        }
        catch (Exception ex)
        {
            _output.Text = Strings.CallErrorHeading(relativeUrl) + Environment.NewLine + ex;
        }
    }
}
