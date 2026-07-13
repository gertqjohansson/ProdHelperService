using System.Text.Json;
using ProdHelperService.Controllers.Interface;

namespace ProdHelperService.TestApp;

// Calls IOeeController / IPlannerController directly, in-process — no Azure
// Relay, no RelayListener involved. Useful for quickly exercising controller
// logic without needing relay connectivity configured. The controllers are
// resolved from the DI container in Program.cs and injected here, the same
// way RelayListener gets them via IControllerDispatcher.
public class MainForm : Form
{
    private static readonly string[] Try1Parameters = ["id", "1,1", "Start", "2026-07-01", "end", "2026-07-12"];
    private static readonly string[] Try2Parameters = ["id", "5,2", "Start", "2026-07-01", "end", "2026-07-12", "break", "true"];

    private readonly IOeeController _oeeController;
    private readonly IPlannerController _plannerController;

    private readonly Button _try1Button;
    private readonly Button _try2Button;
    private readonly TextBox _output;

    public MainForm(IOeeController oeeController, IPlannerController plannerController)
    {
        _oeeController = oeeController;
        _plannerController = plannerController;

        Text = "ProdHelperService Test App";
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

        _try1Button.Click += (_, _) => CallController(
            "Oee/Calculate",
            () => _oeeController.Calculate(Try1Parameters));

        _try2Button.Click += (_, _) => CallController(
            "Planner/GetInteruption",
            () => _plannerController.GetInteruption(Try2Parameters));

        Controls.Add(_try1Button);
        Controls.Add(_try2Button);
        Controls.Add(_output);
    }

    private void CallController(string label, Func<object> call)
    {
        try
        {
            object result = call();
            string json = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            _output.Text = $"Called {label} directly (no relay/listener involved){Environment.NewLine}{Environment.NewLine}{json}";
        }
        catch (Exception ex)
        {
            _output.Text = $"Error calling {label}:{Environment.NewLine}{ex}";
        }
    }
}
