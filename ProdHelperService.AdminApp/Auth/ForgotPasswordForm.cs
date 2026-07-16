using System.Text.RegularExpressions;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class ForgotPasswordForm : Form
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly AuthApiClient _authApiClient;

    private readonly TextBox _emailBox;
    private readonly Label _statusLabel;
    private readonly Button _sendButton;

    public string? ResetEmail { get; private set; }

    public ForgotPasswordForm(AuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;

        Text = Strings.AuthForgotPasswordTitle;
        ClientSize = new Size(360, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label { Text = Strings.AuthForgotPasswordTitle, Left = 24, Top = 20, Width = 312, Font = new Font(Font.FontFamily, 16, FontStyle.Regular) };
        var description = new Label { Text = Strings.AuthForgotPasswordDescription, Left = 24, Top = 60, Width = 312, Height = 40 };

        var emailLabel = new Label { Text = Strings.AuthEmailLabel, Left = 24, Top = 110, Width = 312 };
        _emailBox = new TextBox { Left = 24, Top = 130, Width = 312 };

        _statusLabel = new Label { Left = 24, Top = 165, Width = 312, Height = 40, ForeColor = ErrorColor };

        _sendButton = new Button
        {
            Text = Strings.AuthForgotPasswordButtonText,
            Left = 24,
            Top = 210,
            Width = 312,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _sendButton.FlatAppearance.BorderSize = 0;
        _sendButton.Click += OnSendClick;

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(emailLabel);
        Controls.Add(_emailBox);
        Controls.Add(_statusLabel);
        Controls.Add(_sendButton);
        AcceptButton = _sendButton;
    }

    private async void OnSendClick(object? sender, EventArgs e)
    {
        _statusLabel.Text = string.Empty;

        if (!EmailPattern.IsMatch(_emailBox.Text))
        {
            _statusLabel.Text = Strings.AuthEmailInvalidMessage;
            return;
        }

        _sendButton.Enabled = false;
        try
        {
            await _authApiClient.ForgotPasswordAsync(new ForgotPasswordRequest { Email = _emailBox.Text });

            using var resetForm = new ResetPasswordForm(_authApiClient, _emailBox.Text);
            bool resetSucceeded = resetForm.ShowDialog(this) == DialogResult.OK;
            ResetEmail = _emailBox.Text;
            DialogResult = resetSucceeded ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _sendButton.Enabled = true;
        }
    }
}
