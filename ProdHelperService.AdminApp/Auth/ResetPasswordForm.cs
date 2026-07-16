using System.Text.RegularExpressions;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class ResetPasswordForm : Form
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly AuthApiClient _authApiClient;

    private readonly TextBox _emailBox;
    private readonly TextBox _codeBox;
    private readonly TextBox _newPasswordBox;
    private readonly TextBox _confirmPasswordBox;
    private readonly Label _statusLabel;
    private readonly Button _resetButton;

    public ResetPasswordForm(AuthApiClient authApiClient, string email)
    {
        _authApiClient = authApiClient;

        Text = Strings.AuthResetPasswordTitle;
        ClientSize = new Size(360, 440);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label { Text = Strings.AuthResetPasswordTitle, Left = 24, Top = 20, Width = 312, Font = new Font(Font.FontFamily, 16, FontStyle.Regular) };
        var description = new Label { Text = Strings.AuthResetPasswordDescription, Left = 24, Top = 60, Width = 312, Height = 40 };

        var emailLabel = new Label { Text = Strings.AuthEmailLabel, Left = 24, Top = 110, Width = 312 };
        _emailBox = new TextBox { Left = 24, Top = 130, Width = 312, Text = email };

        var codeLabel = new Label { Text = Strings.AuthResetCodeLabel, Left = 24, Top = 163, Width = 312 };
        _codeBox = new TextBox { Left = 24, Top = 183, Width = 312 };

        var newPasswordLabel = new Label { Text = Strings.AuthNewPasswordLabel, Left = 24, Top = 216, Width = 312 };
        _newPasswordBox = new TextBox { Left = 24, Top = 236, Width = 312, UseSystemPasswordChar = true };

        var confirmLabel = new Label { Text = Strings.AuthConfirmNewPasswordLabel, Left = 24, Top = 269, Width = 312 };
        _confirmPasswordBox = new TextBox { Left = 24, Top = 289, Width = 312, UseSystemPasswordChar = true };

        _statusLabel = new Label { Left = 24, Top = 322, Width = 312, Height = 40, ForeColor = ErrorColor };

        _resetButton = new Button
        {
            Text = Strings.AuthResetPasswordButtonText,
            Left = 24,
            Top = 367,
            Width = 312,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _resetButton.FlatAppearance.BorderSize = 0;
        _resetButton.Click += OnResetClick;

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(emailLabel);
        Controls.Add(_emailBox);
        Controls.Add(codeLabel);
        Controls.Add(_codeBox);
        Controls.Add(newPasswordLabel);
        Controls.Add(_newPasswordBox);
        Controls.Add(confirmLabel);
        Controls.Add(_confirmPasswordBox);
        Controls.Add(_statusLabel);
        Controls.Add(_resetButton);
        AcceptButton = _resetButton;
    }

    private async void OnResetClick(object? sender, EventArgs e)
    {
        _statusLabel.Text = string.Empty;

        if (!EmailPattern.IsMatch(_emailBox.Text))
        {
            _statusLabel.Text = Strings.AuthEmailInvalidMessage;
            return;
        }
        if (_newPasswordBox.Text != _confirmPasswordBox.Text)
        {
            _statusLabel.Text = Strings.AuthPasswordMismatchMessage;
            return;
        }

        _resetButton.Enabled = false;
        try
        {
            await _authApiClient.ResetPasswordAsync(new ResetPasswordRequest
            {
                Email = _emailBox.Text,
                Token = _codeBox.Text,
                NewPassword = _newPasswordBox.Text,
            });

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _resetButton.Enabled = true;
        }
    }
}
