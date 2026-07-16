using System.Text.RegularExpressions;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class RegisterForm : Form
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private readonly AuthApiClient _authApiClient;

    private readonly TextBox _emailBox;
    private readonly TextBox _displayNameBox;
    private readonly TextBox _passwordBox;
    private readonly TextBox _confirmPasswordBox;
    private readonly Label _statusLabel;
    private readonly Button _registerButton;

    public string? RegisteredEmail { get; private set; }

    public RegisterForm(AuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;

        Text = Strings.AuthRegisterTitle;
        ClientSize = new Size(360, 420);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label { Text = Strings.AuthRegisterTitle, Left = 24, Top = 20, Width = 312, Font = new Font(Font.FontFamily, 16, FontStyle.Regular) };

        var emailLabel = new Label { Text = Strings.AuthEmailLabel, Left = 24, Top = 65, Width = 312 };
        _emailBox = new TextBox { Left = 24, Top = 85, Width = 312 };

        var displayNameLabel = new Label { Text = Strings.AuthDisplayNameLabel, Left = 24, Top = 118, Width = 312 };
        _displayNameBox = new TextBox { Left = 24, Top = 138, Width = 312 };

        var passwordLabel = new Label { Text = Strings.AuthPasswordLabel, Left = 24, Top = 171, Width = 312 };
        _passwordBox = new TextBox { Left = 24, Top = 191, Width = 312, UseSystemPasswordChar = true };

        var confirmLabel = new Label { Text = Strings.AuthConfirmPasswordLabel, Left = 24, Top = 224, Width = 312 };
        _confirmPasswordBox = new TextBox { Left = 24, Top = 244, Width = 312, UseSystemPasswordChar = true };

        _statusLabel = new Label { Left = 24, Top = 280, Width = 312, Height = 40, ForeColor = ErrorColor };

        _registerButton = new Button
        {
            Text = Strings.AuthRegisterButtonText,
            Left = 24,
            Top = 325,
            Width = 312,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _registerButton.FlatAppearance.BorderSize = 0;
        _registerButton.Click += OnRegisterClick;

        Controls.Add(title);
        Controls.Add(emailLabel);
        Controls.Add(_emailBox);
        Controls.Add(displayNameLabel);
        Controls.Add(_displayNameBox);
        Controls.Add(passwordLabel);
        Controls.Add(_passwordBox);
        Controls.Add(confirmLabel);
        Controls.Add(_confirmPasswordBox);
        Controls.Add(_statusLabel);
        Controls.Add(_registerButton);
        AcceptButton = _registerButton;
    }

    private async void OnRegisterClick(object? sender, EventArgs e)
    {
        _statusLabel.Text = string.Empty;

        if (!EmailPattern.IsMatch(_emailBox.Text))
        {
            _statusLabel.Text = Strings.AuthEmailInvalidMessage;
            return;
        }
        if (_passwordBox.Text != _confirmPasswordBox.Text)
        {
            _statusLabel.Text = Strings.AuthPasswordMismatchMessage;
            return;
        }

        _registerButton.Enabled = false;
        try
        {
            await _authApiClient.RegisterAsync(new RegisterRequest
            {
                Email = _emailBox.Text,
                Password = _passwordBox.Text,
                DisplayName = string.IsNullOrWhiteSpace(_displayNameBox.Text) ? null : _displayNameBox.Text,
            });

            RegisteredEmail = _emailBox.Text;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _registerButton.Enabled = true;
        }
    }
}
