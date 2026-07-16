using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class MfaSetupForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly AuthApiClient _authApiClient;
    private readonly AuthSession _session;
    private readonly Panel _contentPanel;

    public MfaSetupForm(AuthApiClient authApiClient, AuthSession session)
    {
        _authApiClient = authApiClient;
        _session = session;

        Text = Strings.AuthMfaSetupTitle;
        ClientSize = new Size(380, 500);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;

        _contentPanel = new Panel { Left = 0, Top = 0, Width = ClientSize.Width, Height = ClientSize.Height };
        Controls.Add(_contentPanel);

        Load += async (_, _) => await LoadContentAsync();
    }

    private async Task LoadContentAsync()
    {
        if (_session.MfaEnabled)
        {
            BuildDisableView();
        }
        else
        {
            await BuildSetupViewAsync();
        }
    }

    private async Task BuildSetupViewAsync()
    {
        var loadingLabel = new Label { Text = "...", Left = 20, Top = 20, Width = 340 };
        _contentPanel.Controls.Add(loadingLabel);

        AuthenticatorSetupResponse? setupInfo = null;
        string? error = null;
        try
        {
            setupInfo = await _authApiClient.SetupAuthenticatorAsync(_session.AccessToken!);
        }
        catch (AuthApiException ex)
        {
            error = ex.Message;
        }

        _contentPanel.Controls.Clear();

        if (setupInfo is null)
        {
            _contentPanel.Controls.Add(new Label { Text = error, Left = 20, Top = 20, Width = 340, ForeColor = ErrorColor });
            AddCloseButton(60, closesWithOk: false);
            return;
        }

        var description = new Label { Text = Strings.AuthMfaSetupDescription, Left = 20, Top = 10, Width = 340, Height = 60 };
        var qrBox = new PictureBox
        {
            Image = EmbeddedImages.FromBase64Png(setupInfo.QrCodePngBase64),
            Left = 90,
            Top = 75,
            Width = 200,
            Height = 200,
            SizeMode = PictureBoxSizeMode.Zoom,
        };
        var keyLabel = new Label
        {
            Text = setupInfo.SharedKey,
            Left = 20,
            Top = 285,
            Width = 340,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font(FontFamily.GenericMonospace, 10),
        };
        var codeLabel = new Label { Text = Strings.AuthMfaCodeLabel, Left = 20, Top = 315, Width = 340 };
        var codeBox = new TextBox { Left = 20, Top = 335, Width = 340, MaxLength = 6 };
        var statusLabel = new Label { Left = 20, Top = 365, Width = 340, Height = 30, ForeColor = ErrorColor };

        var enableButton = new Button
        {
            Text = Strings.AuthMfaEnableButtonText,
            Left = 20,
            Top = 400,
            Width = 340,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        enableButton.FlatAppearance.BorderSize = 0;
        enableButton.Click += async (_, _) =>
        {
            statusLabel.Text = string.Empty;
            enableButton.Enabled = false;
            try
            {
                EnableAuthenticatorResponse result = await _authApiClient.EnableAuthenticatorAsync(
                    new EnableAuthenticatorRequest { Code = codeBox.Text }, _session.AccessToken!);
                await RefreshSessionAsync();
                ShowRecoveryCodes(result.RecoveryCodes);
            }
            catch (AuthApiException ex)
            {
                statusLabel.Text = ex.Message;
            }
            finally
            {
                enableButton.Enabled = true;
            }
        };

        _contentPanel.Controls.Add(description);
        _contentPanel.Controls.Add(qrBox);
        _contentPanel.Controls.Add(keyLabel);
        _contentPanel.Controls.Add(codeLabel);
        _contentPanel.Controls.Add(codeBox);
        _contentPanel.Controls.Add(statusLabel);
        _contentPanel.Controls.Add(enableButton);
        AddCloseButton(445, closesWithOk: false);
    }

    private void BuildDisableView()
    {
        var description = new Label { Text = Strings.AuthMfaAlreadyEnabledMessage, Left = 20, Top = 20, Width = 340, Height = 50 };
        var passwordLabel = new Label { Text = Strings.AuthPasswordLabel, Left = 20, Top = 80, Width = 340 };
        var passwordBox = new TextBox { Left = 20, Top = 100, Width = 340, UseSystemPasswordChar = true };
        var statusLabel = new Label { Left = 20, Top = 135, Width = 340, Height = 30, ForeColor = ErrorColor };

        var disableButton = new Button
        {
            Text = Strings.AuthMfaDisableButtonText,
            Left = 20,
            Top = 175,
            Width = 340,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        disableButton.FlatAppearance.BorderSize = 0;
        disableButton.Click += async (_, _) =>
        {
            statusLabel.Text = string.Empty;
            disableButton.Enabled = false;
            try
            {
                await _authApiClient.DisableAuthenticatorAsync(
                    new DisableAuthenticatorRequest { Password = passwordBox.Text }, _session.AccessToken!);
                await RefreshSessionAsync();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (AuthApiException ex)
            {
                statusLabel.Text = ex.Message;
            }
            finally
            {
                disableButton.Enabled = true;
            }
        };

        _contentPanel.Controls.Add(description);
        _contentPanel.Controls.Add(passwordLabel);
        _contentPanel.Controls.Add(passwordBox);
        _contentPanel.Controls.Add(statusLabel);
        _contentPanel.Controls.Add(disableButton);
        AddCloseButton(225, closesWithOk: false);
    }

    private void ShowRecoveryCodes(string[] codes)
    {
        _contentPanel.Controls.Clear();
        var description = new Label { Text = Strings.AuthMfaRecoveryCodesDescription, Left = 20, Top = 10, Width = 340, Height = 60 };
        var codesBox = new TextBox
        {
            Left = 20,
            Top = 80,
            Width = 340,
            Height = 200,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font(FontFamily.GenericMonospace, 10),
            Text = string.Join(Environment.NewLine, codes),
        };
        _contentPanel.Controls.Add(description);
        _contentPanel.Controls.Add(codesBox);
        // The Enable call already succeeded at this point, so closing from
        // here always reports OK — the session's MFA state genuinely changed.
        AddCloseButton(300, closesWithOk: true);
    }

    private void AddCloseButton(int top, bool closesWithOk)
    {
        var closeButton = new Button { Text = Strings.AuthCloseButtonText, Left = 20, Top = top, Width = 340, Height = 30 };
        closeButton.Click += (_, _) =>
        {
            DialogResult = closesWithOk ? DialogResult.OK : DialogResult.Cancel;
            Close();
        };
        _contentPanel.Controls.Add(closeButton);
    }

    private async Task RefreshSessionAsync()
    {
        if (_session.RefreshToken is null) return;
        TokenResponse refreshed = await _authApiClient.RefreshAsync(new RefreshRequest { RefreshToken = _session.RefreshToken });
        _session.SetFromTokens(refreshed);
    }
}
