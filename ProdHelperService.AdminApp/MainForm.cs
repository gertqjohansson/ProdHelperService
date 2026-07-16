using System.Net.Http.Json;
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

    private const int MenuStripHeight = 30;

    private readonly HttpClient _httpClient;
    private readonly AppSettings _settings;

    private readonly MenuStrip _menuStrip;
    private readonly Button _try1Button;
    private readonly Button _try2Button;
    private readonly TextBox _output;

    public MainForm(HttpClient httpClient, AppSettings settings)
    {
        _httpClient = httpClient;
        _settings = settings;

        Text = Strings.WindowTitle;
        Width = 640;
        Height = 470;
        StartPosition = FormStartPosition.CenterScreen;

        _menuStrip = BuildMenuStrip();

        _try1Button = new Button { Text = Strings.Try1ButtonText, Left = 20, Top = 20 + MenuStripHeight, Width = 120, Height = 32 };
        _try2Button = new Button { Text = Strings.Try2ButtonText, Left = 150, Top = 20 + MenuStripHeight, Width = 120, Height = 32 };
        _output = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(FontFamily.GenericMonospace, 9),
            Left = 20,
            Top = 65 + MenuStripHeight,
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
        Controls.Add(_menuStrip);
        MainMenuStrip = _menuStrip;
    }

    private MenuStrip BuildMenuStrip()
    {
        var languageMenu = new ToolStripMenuItem(Strings.LanguageMenuText);
        foreach (var (code, nativeName) in SupportedLanguages.All)
        {
            var item = new ToolStripMenuItem(nativeName) { Checked = code == _settings.Culture };
            item.Click += (_, _) =>
            {
                _settings.Culture = code;
                _settings.Save();
                Application.Restart();
            };
            languageMenu.DropDownItems.Add(item);
        }

        var menuStrip = new MenuStrip();
        menuStrip.Items.Add(languageMenu);
        return menuStrip;
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
