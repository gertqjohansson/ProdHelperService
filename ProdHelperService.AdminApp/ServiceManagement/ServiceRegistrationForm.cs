using ProdHelperService.Contracts.Service;

namespace ProdHelperService.AdminApp;

public class ServiceRegistrationForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly ServiceApiClient _serviceApiClient;
    private readonly AuthSession _session;

    private readonly Label _statusLabel;
    private readonly Label _messageLabel;
    private readonly Button _registerButton;
    private readonly Button _unregisterButton;
    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Button _closeButton;

    public ServiceRegistrationForm(ServiceApiClient serviceApiClient, AuthSession session)
    {
        _serviceApiClient = serviceApiClient;
        _session = session;

        Text = Strings.ServiceDialogTitle;
        ClientSize = new Size(360, 290);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label { Text = Strings.ServiceDialogTitle, Left = 24, Top = 20, Width = 312, Font = new Font(Font.FontFamily, 16, FontStyle.Regular) };
        var description = new Label { Text = Strings.ServiceDialogDescription, Left = 24, Top = 60, Width = 312, Height = 40 };

        _statusLabel = new Label
        {
            Left = 24,
            Top = 110,
            Width = 312,
            Height = 24,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
            Text = "...",
        };

        _registerButton = BuildPrimaryButton(Strings.ServiceRegisterButtonText, 24);
        _registerButton.Click += async (_, _) => await RunActionAsync(Strings.ServiceRegisteringMessage, () => _serviceApiClient.RegisterAsync(_session.AccessToken!));

        _unregisterButton = BuildSecondaryButton(Strings.ServiceUnregisterButtonText, 24);
        _unregisterButton.Click += async (_, _) => await OnUnregisterClickAsync();

        _startButton = BuildPrimaryButton(Strings.ServiceStartButtonText, 186);
        _startButton.Click += async (_, _) => await RunActionAsync(Strings.ServiceStartingMessage, () => _serviceApiClient.StartAsync(_session.AccessToken!));

        _stopButton = BuildSecondaryButton(Strings.ServiceStopButtonText, 186);
        _stopButton.Click += async (_, _) => await RunActionAsync(Strings.ServiceStoppingMessage, () => _serviceApiClient.StopAsync(_session.AccessToken!));

        _messageLabel = new Label { Left = 24, Top = 182, Width = 312, Height = 40, ForeColor = ErrorColor };

        _closeButton = new Button { Text = Strings.ServiceCloseButtonText, Left = 24, Top = 232, Width = 150, Height = 34 };
        _closeButton.Click += (_, _) => Close();

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(_statusLabel);
        Controls.Add(_registerButton);
        Controls.Add(_unregisterButton);
        Controls.Add(_startButton);
        Controls.Add(_stopButton);
        Controls.Add(_messageLabel);
        Controls.Add(_closeButton);
        CancelButton = _closeButton;

        Load += async (_, _) => await RefreshStatusAsync();
    }

    private static Button BuildPrimaryButton(string text, int left)
    {
        var button = new Button
        {
            Text = text,
            Left = left,
            Top = 140,
            Width = 150,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Visible = false,
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }

    private static Button BuildSecondaryButton(string text, int left) =>
        new() { Text = text, Left = left, Top = 140, Width = 150, Height = 34, Visible = false };

    private async Task RefreshStatusAsync()
    {
        SetBusy(true);
        _messageLabel.ForeColor = ErrorColor;
        _messageLabel.Text = string.Empty;
        try
        {
            ServiceRegistrationStatusResponse status = await _serviceApiClient.GetRegistrationStatusAsync(_session.AccessToken!);
            ApplyStatus(status.State, status.IsRegistered);
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = string.Empty;
            _messageLabel.Text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyStatus(string state, bool isRegistered)
    {
        _statusLabel.Text = state switch
        {
            "NotRegistered" => Strings.ServiceStatusNotRegistered,
            "Running" => Strings.ServiceStatusRunning,
            "Stopped" => Strings.ServiceStatusStopped,
            _ => Strings.ServiceStatusPending, // StartPending / StopPending / Unknown
        };

        _registerButton.Visible = !isRegistered;
        _unregisterButton.Visible = isRegistered;
        _startButton.Visible = isRegistered && state == "Stopped";
        _stopButton.Visible = isRegistered && state == "Running";
    }

    private async Task OnUnregisterClickAsync()
    {
        DialogResult confirm = MessageBox.Show(
            Strings.ServiceUnregisterConfirmMessage,
            Strings.ServiceUnregisterConfirmTitle,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        await RunActionAsync(Strings.ServiceUnregisteringMessage, () => _serviceApiClient.UnregisterAsync(_session.AccessToken!));
    }

    private async Task RunActionAsync(string busyMessage, Func<Task<ServiceActionResponse>> action)
    {
        SetBusy(true);
        _messageLabel.ForeColor = TealPrimary;
        _messageLabel.Text = busyMessage;
        try
        {
            await action();
            _messageLabel.Text = string.Empty;
            await RefreshStatusAsync();
        }
        catch (AuthApiException ex)
        {
            _messageLabel.ForeColor = ErrorColor;
            _messageLabel.Text = ex.Message;
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _registerButton.Enabled = !busy;
        _unregisterButton.Enabled = !busy;
        _startButton.Enabled = !busy;
        _stopButton.Enabled = !busy;
        _closeButton.Enabled = !busy;
    }
}
