using ProdHelperService.Contracts.Service;

namespace ProdHelperService.AdminApp;

public class ServiceUrlForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly ServiceApiClient _serviceApiClient;
    private readonly HttpClient _sharedHttpClient;
    private readonly AuthSession _session;

    private readonly TextBox _portBox;
    private readonly Label _statusLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private int _currentPort;

    public ServiceUrlForm(ServiceApiClient serviceApiClient, HttpClient sharedHttpClient, AuthSession session)
    {
        _serviceApiClient = serviceApiClient;
        _sharedHttpClient = sharedHttpClient;
        _session = session;

        Text = Strings.ServiceUrlDialogTitle;
        ClientSize = new Size(360, 260);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label { Text = Strings.ServiceUrlDialogTitle, Left = 24, Top = 20, Width = 312, Font = new Font(Font.FontFamily, 16, FontStyle.Regular) };
        var description = new Label { Text = Strings.ServiceUrlDialogDescription, Left = 24, Top = 60, Width = 312, Height = 40 };

        var portLabel = new Label { Text = Strings.ServiceUrlPortLabel, Left = 24, Top = 110, Width = 312 };
        _portBox = new TextBox { Left = 24, Top = 130, Width = 312, Enabled = false, Text = "..." };
        _portBox.KeyPress += (_, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true;
        };

        _statusLabel = new Label { Left = 24, Top = 165, Width = 312, Height = 40, ForeColor = ErrorColor };

        _saveButton = new Button
        {
            Text = Strings.ServiceUrlSaveButtonText,
            Left = 24,
            Top = 210,
            Width = 150,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Enabled = false,
        };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button { Text = Strings.ServiceUrlCancelButtonText, Left = 186, Top = 210, Width = 150, Height = 34 };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(portLabel);
        Controls.Add(_portBox);
        Controls.Add(_statusLabel);
        Controls.Add(_saveButton);
        Controls.Add(_cancelButton);
        CancelButton = _cancelButton;

        Load += async (_, _) => await LoadCurrentPortAsync();
    }

    private async Task LoadCurrentPortAsync()
    {
        try
        {
            GetServiceInfoResponse info = await _serviceApiClient.GetInfoAsync(_session.AccessToken!);
            _currentPort = info.Port;
            _portBox.Text = info.Port.ToString();
            _portBox.Enabled = true;
            _saveButton.Enabled = true;
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        _statusLabel.ForeColor = ErrorColor;
        _statusLabel.Text = string.Empty;

        if (!int.TryParse(_portBox.Text, out int newPort) || newPort is < 1 or > 65535)
        {
            _statusLabel.Text = Strings.ServiceUrlInvalidPortMessage;
            return;
        }

        if (newPort == _currentPort)
        {
            _statusLabel.Text = Strings.ServiceUrlSamePortMessage;
            return;
        }

        _saveButton.Enabled = false;
        _cancelButton.Enabled = false;
        _statusLabel.ForeColor = TealPrimary;
        _statusLabel.Text = Strings.ServiceUrlRestartingMessage(newPort);

        try
        {
            UpdatePortResponse result = await _serviceApiClient.UpdatePortAsync(
                new UpdatePortRequest { NewPort = newPort }, _session.AccessToken!);

            _statusLabel.Text = Strings.ServiceUrlVerifyingMessage;
            await WaitForNewAddressAsync(result.NewPort);

            // The authoritative fix — every other consumer sharing this HttpClient
            // (AuthApiClient, ServiceApiClient, MainForm.CallController) picks this up too.
            _sharedHttpClient.BaseAddress = new Uri($"http://localhost:{result.NewPort}/");

            if (!result.SettingsFilePersisted)
            {
                MessageBox.Show(
                    Strings.ServiceUrlSettingsPersistFailedMessage,
                    Strings.ServiceUrlDialogTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.ForeColor = ErrorColor;
            _statusLabel.Text = ex.Message;
            _saveButton.Enabled = true;
            _cancelButton.Enabled = true;
        }
    }

    // Defense in depth beyond the server's own health check before it responded —
    // covers e.g. a local firewall blocking the new port from this machine specifically.
    private static async Task WaitForNewAddressAsync(int port)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                using HttpResponseMessage response = await client.GetAsync($"http://localhost:{port}/swagger/v1/swagger.json");
                if (response.IsSuccessStatusCode) return;
            }
            catch
            {
                // Not reachable yet — keep trying for the remaining attempts.
            }
            await Task.Delay(300);
        }
    }
}
