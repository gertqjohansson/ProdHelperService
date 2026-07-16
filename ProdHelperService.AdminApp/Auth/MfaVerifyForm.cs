using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class MfaVerifyForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly AuthApiClient _authApiClient;
    private readonly string _mfaToken;

    private readonly TextBox _codeBox;
    private readonly Label _statusLabel;
    private readonly Button _verifyButton;

    public TokenResponse? Tokens { get; private set; }

    public MfaVerifyForm(AuthApiClient authApiClient, string mfaToken)
    {
        _authApiClient = authApiClient;
        _mfaToken = mfaToken;

        Text = Strings.AuthMfaTitle;
        ClientSize = new Size(340, 220);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;

        var description = new Label { Text = Strings.AuthMfaDescription, Left = 20, Top = 16, Width = 300, Height = 40 };
        var codeLabel = new Label { Text = Strings.AuthMfaCodeLabel, Left = 20, Top = 60, Width = 300 };
        _codeBox = new TextBox { Left = 20, Top = 80, Width = 300, MaxLength = 6 };

        _statusLabel = new Label { Left = 20, Top = 112, Width = 300, Height = 30, ForeColor = ErrorColor };

        _verifyButton = new Button
        {
            Text = Strings.AuthVerifyButtonText,
            Left = 20,
            Top = 150,
            Width = 300,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _verifyButton.FlatAppearance.BorderSize = 0;
        _verifyButton.Click += OnVerifyClick;

        Controls.Add(description);
        Controls.Add(codeLabel);
        Controls.Add(_codeBox);
        Controls.Add(_statusLabel);
        Controls.Add(_verifyButton);
        AcceptButton = _verifyButton;
    }

    private async void OnVerifyClick(object? sender, EventArgs e)
    {
        _statusLabel.Text = string.Empty;
        _verifyButton.Enabled = false;
        try
        {
            Tokens = await _authApiClient.VerifyMfaAsync(new VerifyMfaRequest { MfaToken = _mfaToken, Code = _codeBox.Text });
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _verifyButton.Enabled = true;
        }
    }
}
