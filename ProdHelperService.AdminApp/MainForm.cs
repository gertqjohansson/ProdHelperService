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

    private readonly HttpClient _httpClient;

    private readonly Button _try1Button;
    private readonly Button _try2Button;
    private readonly TextBox _output;

    public MainForm(HttpClient httpClient)
    {
        _httpClient = httpClient;

        Text = "ProdHelperService Admin App";
        Width = 640;
        Height = 440;
        StartPosition = FormStartPosition.CenterScreen;

        _try1Button = new Button { Text = "Try 1", Left = 20, Top = 20, Width = 120, Height = 32 };
        _try2Button = new Button { Text = "Try 2", Left = 150, Top = 20, Width = 120, Height = 32 };
        _output = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(FontFamily.GenericMonospace, 9),
            Left = 20,
            Top = 65,
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
    }

    private async Task CallController(string relativeUrl, ParametersRequest request)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(relativeUrl, request);
            response.EnsureSuccessStatusCode();

            using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
            _output.Text = $"Called {relativeUrl} via HTTP ({_httpClient.BaseAddress}){Environment.NewLine}{Environment.NewLine}{json}";
        }
        catch (Exception ex)
        {
            _output.Text = $"Error calling {relativeUrl}:{Environment.NewLine}{ex}";
        }
    }
}
