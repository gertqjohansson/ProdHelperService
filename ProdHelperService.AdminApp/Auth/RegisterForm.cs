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
    private readonly TextBox _adminPasswordBox;
    private readonly Label _statusLabel;
    private readonly Button _registerButton;

    public string? RegisteredEmail { get; private set; }

    public RegisterForm(AuthApiClient authApiClient)
    {
        _authApiClient = authApiClient;

        Text = Strings.AuthRegisterTitle;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        var title = new Label
        {
            Text = Strings.AuthRegisterTitle,
            Left = 24,
            Top = 20,
            Width = 312,
            Font = new Font(Font.FontFamily, 16, FontStyle.Regular),
            AutoSize = false,
        };
        // The heading was clipping at its natural single-line height, so give it
        // an explicit box 10% taller than the text actually needs, and push
        // every control below it down by that same growth so nothing overlaps.
        int naturalTitleHeight = TextRenderer.MeasureText(title.Text, title.Font).Height;
        title.Height = (int)Math.Ceiling(naturalTitleHeight * 1.1);
        int layoutShift = title.Height - naturalTitleHeight;

        ClientSize = new Size(360, 473 + layoutShift);

        var emailLabel = new Label { Text = Strings.AuthEmailLabel, Left = 24, Top = 65 + layoutShift, Width = 312 };
        _emailBox = new TextBox { Left = 24, Top = 85 + layoutShift, Width = 312 };

        var displayNameLabel = new Label { Text = Strings.AuthDisplayNameLabel, Left = 24, Top = 118 + layoutShift, Width = 312 };
        _displayNameBox = new TextBox { Left = 24, Top = 138 + layoutShift, Width = 312 };

        var passwordLabel = new Label { Text = Strings.AuthPasswordLabel, Left = 24, Top = 171 + layoutShift, Width = 312 };
        _passwordBox = new TextBox { Left = 24, Top = 191 + layoutShift, Width = 312, UseSystemPasswordChar = true };

        var confirmLabel = new Label { Text = Strings.AuthConfirmPasswordLabel, Left = 24, Top = 224 + layoutShift, Width = 312 };
        _confirmPasswordBox = new TextBox { Left = 24, Top = 244 + layoutShift, Width = 312, UseSystemPasswordChar = true };

        var adminPasswordLabel = new Label { Text = Strings.AuthAdminPasswordLabel, Left = 24, Top = 277 + layoutShift, Width = 312 };
        _adminPasswordBox = new TextBox { Left = 24, Top = 297 + layoutShift, Width = 312, UseSystemPasswordChar = true };

        _statusLabel = new Label { Left = 24, Top = 333 + layoutShift, Width = 312, Height = 40, ForeColor = ErrorColor };

        _registerButton = new Button
        {
            Text = Strings.AuthRegisterButtonText,
            Left = 24,
            Top = 378 + layoutShift,
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
        Controls.Add(adminPasswordLabel);
        Controls.Add(_adminPasswordBox);
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
                AdminPassword = _adminPasswordBox.Text,
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
