using System.Text.RegularExpressions;
using ProdHelperService.Contracts.Auth;

namespace ProdHelperService.AdminApp;

public class LoginForm : Form
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled);
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private const int LanguageButtonWidth = 96;
    private const int LanguageButtonHeight = 32;
    private const int LanguageButtonMargin = 12;

    private readonly AuthApiClient _authApiClient;
    private readonly AuthSession _session;
    private readonly AppSettings _settings;

    private readonly Panel _card;
    private readonly TextBox _emailBox;
    private readonly TextBox _passwordBox;
    private readonly Label _statusLabel;
    private readonly Button _loginButton;

    public LoginForm(AuthApiClient authApiClient, AuthSession session, AppSettings settings)
    {
        _authApiClient = authApiClient;
        _session = session;
        _settings = settings;

        Text = Strings.AuthLoginTitle;
        ClientSize = new Size(546, 560); // 420 * 1.3 - widened 30% per request
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackgroundImage = EmbeddedImages.LoadLoginBackground();
        BackgroundImageLayout = ImageLayout.Stretch;

        const int cardLeft = 40;
        const int cardWidth = 466; // ClientSize.Width - 2*cardLeft, keeps the original side margins
        const int contentLeft = 24;
        const int contentWidth = 418; // cardWidth - 2*contentLeft

        var title = new Label
        {
            Text = Strings.AuthLoginTitle,
            Left = contentLeft,
            Top = 20,
            Width = contentWidth,
            Font = new Font(Font.FontFamily, 16, FontStyle.Regular),
            AutoSize = false,
        };
        // The heading was clipping at its natural single-line height, so give it
        // an explicit box 10% taller than the text actually needs, and push
        // every control below it down by that same growth so nothing overlaps.
        int naturalTitleHeight = TextRenderer.MeasureText(title.Text, title.Font).Height;
        title.Height = (int)Math.Ceiling(naturalTitleHeight * 1.1);
        int layoutShift = title.Height - naturalTitleHeight;

        _card = new Panel
        {
            BackColor = Color.White,
            Left = cardLeft,
            Top = 90,
            Width = cardWidth,
            Height = 380 + layoutShift,
            Padding = new Padding(24),
        };

        var emailLabel = new Label { Text = Strings.AuthEmailLabel, Left = contentLeft, Top = 70 + layoutShift, Width = contentWidth };
        _emailBox = new TextBox { Left = contentLeft, Top = 90 + layoutShift, Width = contentWidth };

        var passwordLabel = new Label { Text = Strings.AuthPasswordLabel, Left = contentLeft, Top = 125 + layoutShift, Width = contentWidth };
        _passwordBox = new TextBox { Left = contentLeft, Top = 145 + layoutShift, Width = contentWidth, UseSystemPasswordChar = true };

        _statusLabel = new Label { Left = contentLeft, Top = 180 + layoutShift, Width = contentWidth, Height = 40, ForeColor = ErrorColor };

        _loginButton = new Button
        {
            Text = Strings.AuthLoginButtonText,
            Left = contentLeft,
            Top = 225 + layoutShift,
            Width = contentWidth,
            Height = 34,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
        };
        _loginButton.FlatAppearance.BorderSize = 0;
        _loginButton.Click += OnLoginClick;

        var switchLabel = new Label
        {
            Text = Strings.AuthNeedAccountText,
            Left = contentLeft,
            Top = 275 + layoutShift,
            Width = 150,
            TextAlign = ContentAlignment.MiddleLeft,
            Visible = false,
        };
        var registerLink = new LinkLabel
        {
            Text = Strings.AuthRegisterButtonText,
            Left = contentLeft + 150,
            Top = 275 + layoutShift,
            Width = contentWidth - 150,
            Visible = false,
        };
        registerLink.LinkClicked += OnRegisterLinkClicked;

        // Self-registration is only offered for first-run bootstrap (no users
        // in the database yet) - stays hidden until that's confirmed, and
        // fails safe (stays hidden) if the check can't be completed at all.
        Load += async (_, _) =>
        {
            try
            {
                HasUsersResponse hasUsers = await _authApiClient.HasUsersAsync();
                if (!hasUsers.HasUsers)
                {
                    switchLabel.Visible = true;
                    registerLink.Visible = true;
                }
            }
            catch
            {
                // API unreachable or errored - leave Register hidden.
            }
        };

        _card.Controls.Add(title);
        _card.Controls.Add(emailLabel);
        _card.Controls.Add(_emailBox);
        _card.Controls.Add(passwordLabel);
        _card.Controls.Add(_passwordBox);
        _card.Controls.Add(_statusLabel);
        _card.Controls.Add(_loginButton);
        _card.Controls.Add(switchLabel);
        _card.Controls.Add(registerLink);

        Controls.Add(_card);
        Controls.Add(BuildLanguageButton());
        AcceptButton = _loginButton;
    }

    private Button BuildLanguageButton()
    {
        var currentLanguage = SupportedLanguages.All.FirstOrDefault(l => l.Code == _settings.Culture);

        var languageMenu = new ContextMenuStrip { Renderer = new ToolStripProfessionalRenderer(new ProdHelperColorTable()) };
        foreach (var (code, nativeName, flagResourceName, _) in SupportedLanguages.All)
        {
            var item = new ToolStripMenuItem(nativeName, EmbeddedImages.LoadFlagImage(flagResourceName))
            {
                ForeColor = Color.White,
                Checked = code == _settings.Culture,
            };
            item.Click += (_, _) =>
            {
                _settings.Culture = code;
                _settings.Save();
                Application.Restart();
            };
            languageMenu.Items.Add(item);
        }

        var languageButton = new Button
        {
            Image = EmbeddedImages.LoadFlagImage(currentLanguage.FlagResourceName),
            ImageAlign = ContentAlignment.MiddleLeft,
            Text = "▾",
            TextAlign = ContentAlignment.MiddleRight,
            Left = ClientSize.Width - LanguageButtonWidth - LanguageButtonMargin,
            Top = LanguageButtonMargin,
            Width = LanguageButtonWidth,
            Height = LanguageButtonHeight,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(128, 24, 19, 12),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        languageButton.FlatAppearance.BorderSize = 0;
        languageButton.Click += (_, _) => languageMenu.Show(languageButton, new Point(0, languageButton.Height));

        return languageButton;
    }

    private async void OnLoginClick(object? sender, EventArgs e)
    {
        _statusLabel.ForeColor = ErrorColor;
        _statusLabel.Text = string.Empty;

        if (!EmailPattern.IsMatch(_emailBox.Text))
        {
            _statusLabel.Text = Strings.AuthEmailInvalidMessage;
            return;
        }

        _loginButton.Enabled = false;
        try
        {
            LoginResponse response = await _authApiClient.LoginAsync(new LoginRequest { Email = _emailBox.Text, Password = _passwordBox.Text });

            if (response.MfaRequired)
            {
                using var mfaForm = new MfaVerifyForm(_authApiClient, response.MfaToken!);
                if (mfaForm.ShowDialog(this) != DialogResult.OK) return;
                _session.SetFromTokens(mfaForm.Tokens!);
            }
            else
            {
                _session.SetFromTokens(response.Tokens!);
            }

            DialogResult = DialogResult.OK;
            Close();
        }
        catch (AuthApiException ex)
        {
            _statusLabel.Text = ex.Message;
        }
        finally
        {
            _loginButton.Enabled = true;
        }
    }

    private void OnRegisterLinkClicked(object? sender, EventArgs e)
    {
        using var registerForm = new RegisterForm(_authApiClient);
        if (registerForm.ShowDialog(this) == DialogResult.OK)
        {
            _emailBox.Text = registerForm.RegisteredEmail ?? _emailBox.Text;
            _statusLabel.ForeColor = TealPrimary;
            _statusLabel.Text = Strings.AuthRegisterSuccessMessage;
        }
    }
}
