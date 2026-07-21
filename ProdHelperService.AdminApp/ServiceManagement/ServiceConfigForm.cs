using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using ProdHelperService.Contracts.Service;
using ProdHelperService.ServiceManagement;

namespace ProdHelperService.AdminApp;

// Opened when ProdHelperService can't be reached (see Program.cs's EnsureServiceReachableAsync,
// which runs before login - this form must work with no session and no guarantee the HTTP API is
// up at all). Start/Stop control the Windows Service directly via IWindowsServiceInstaller (same
// local, no-HTTP mechanism EnsureServiceRegisteredAsync already uses); Get Service Version is the
// only thing here that goes over HTTP, to confirm the API is actually answering after a Start.
public class ServiceConfigForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private const int ContentLeft = 24;
    private const int ContentWidth = 512;
    private const int LanguageButtonWidth = 96;
    private const int LanguageButtonHeight = 32;
    private const int LanguageButtonMargin = 12;
    private const string JwtKeyAlphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    private readonly ServiceApiClient _serviceApiClient;
    private readonly IWindowsServiceInstaller _windowsServiceInstaller;
    private readonly AppSettings _settings;
    private readonly Panel _fieldsPanel;

    private readonly Button _startButton;
    private readonly Button _stopButton;
    private readonly Button _tryServiceButton;
    private readonly Label _reachabilityLabel;

    // Not readonly: assigned from BuildFieldSections(), a helper called from the constructor
    // rather than inline in its body (readonly fields can only be assigned directly within the
    // constructor itself).
    private TextBox _namespaceBox = null!;
    private TextBox _connectionNameBox = null!;
    private TextBox _keyNameBox = null!;
    private TextBox _relayKeyBox = null!;
    private TextBox _portBox = null!;
    private TextBox _dbConnectionStringBox = null!;
    private Button _tryConnectionButton = null!;
    private Label _connectionTestLabel = null!;
    private TextBox _jwtKeyBox = null!;
    private Button _generateJwtKeyButton = null!;
    private TextBox _accessTokenMinutesBox = null!;
    private TextBox _refreshTokenDaysBox = null!;
    private TextBox _emailConnectionStringBox = null!;
    private TextBox _senderAddressBox = null!;
    private TextBox _tokenTrackingBaseUrlBox = null!;
    private TextBox _tokenTrackingApiKeyBox = null!;
    private TextBox _tokenTrackingIntervalMinutesBox = null!;

    private readonly Label _validationLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    private string _originalJwtKey = string.Empty;

    public ServiceConfigForm(ServiceApiClient serviceApiClient, IWindowsServiceInstaller windowsServiceInstaller, AppSettings settings)
    {
        _serviceApiClient = serviceApiClient;
        _windowsServiceInstaller = windowsServiceInstaller;
        _settings = settings;

        Text = Strings.ServiceConfigDialogTitle;
        ClientSize = new Size(560, 700); // recomputed to fit content exactly at the end of this constructor
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;
        Padding = new Padding(24);

        Button languageButton = BuildLanguageButton();

        // Label.AutoSize defaults to false when a control is constructed in code (only true when
        // added via the designer) - without an explicitly measured Height, a Label sized for a
        // 16pt font falls back to the small generic DefaultSize and clips its own text. Same fix
        // LoginForm already uses for its title, applied here too, and extended to the (long,
        // wrapping) description label for the same reason.
        var title = new Label
        {
            Text = Strings.ServiceConfigDialogTitle,
            Left = ContentLeft,
            Top = 20,
            Width = ContentWidth - languageButton.Width - LanguageButtonMargin,
            Font = new Font(Font.FontFamily, 16, FontStyle.Regular),
            AutoSize = false,
        };
        title.Height = (int)Math.Ceiling(TextRenderer.MeasureText(title.Text, title.Font).Height * 1.15);
        languageButton.Top = title.Top + (title.Height - languageButton.Height) / 2;

        int y = title.Top + title.Height + 8;

        var description = new Label { Text = Strings.ServiceConfigDialogDescription, Left = ContentLeft, Top = y, Width = ContentWidth, AutoSize = false };
        Size measuredDescription = TextRenderer.MeasureText(description.Text, description.Font, new Size(ContentWidth, int.MaxValue), TextFormatFlags.WordBreak);
        description.Height = (int)Math.Ceiling(measuredDescription.Height * 1.15);
        y += description.Height + 16;

        // Initial visibility assumes unreachable (Start only) - the safe default, and also
        // literally why this form is usually opened in the first place (Program.cs's
        // EnsureServiceReachableAsync). CheckReachabilityAsync corrects this once the first check
        // resolves.
        int actionRowTop = y;
        _startButton = new Button { Text = Strings.ServiceStartButtonText, Left = ContentLeft, Top = actionRowTop, Width = 150, Height = 34, BackColor = TealPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Visible = true };
        _startButton.FlatAppearance.BorderSize = 0;
        _startButton.Click += OnStartClick;

        _stopButton = new Button { Text = Strings.ServiceStopButtonText, Left = ContentLeft, Top = actionRowTop, Width = 150, Height = 34, Visible = false };
        _stopButton.Click += OnStopClick;

        _tryServiceButton = new Button { Text = Strings.ServiceConfigGetVersionButtonText, Left = ContentLeft + 164, Top = actionRowTop, Width = 150, Height = 34, Visible = false };
        _tryServiceButton.Click += OnTryServiceClick;
        y += 34 + 12;

        _reachabilityLabel = new Label { Left = ContentLeft, Top = y, Width = ContentWidth, Height = 20, Text = "..." };
        y += 20 + 10;

        _fieldsPanel = new Panel { Left = ContentLeft, Top = y, Width = ContentWidth, Height = 400, AutoScroll = true };
        BuildFieldSections();
        y += _fieldsPanel.Height + 10;

        _validationLabel = new Label { Left = ContentLeft, Top = y, Width = ContentWidth, Height = 20, ForeColor = ErrorColor };
        y += 20 + 8;

        int buttonRowTop = y;
        _saveButton = new Button { Text = Strings.ServiceConfigSaveButtonText, Left = ContentLeft, Top = buttonRowTop, Width = 150, Height = 34, BackColor = TealPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button { Text = Strings.ServiceConfigCancelButtonText, Left = ContentLeft + 164, Top = buttonRowTop, Width = 150, Height = 34 };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        y += 34;

        // Size the window to fit exactly what was actually laid out, plus a bottom margin -
        // guaranteed correct regardless of font metrics or DPI, rather than a guessed constant.
        ClientSize = new Size(560, y + 24);

        Controls.Add(title);
        Controls.Add(languageButton);
        Controls.Add(description);
        Controls.Add(_startButton);
        Controls.Add(_stopButton);
        Controls.Add(_tryServiceButton);
        Controls.Add(_reachabilityLabel);
        Controls.Add(_fieldsPanel);
        Controls.Add(_validationLabel);
        Controls.Add(_saveButton);
        Controls.Add(_cancelButton);
        CancelButton = _cancelButton;

        Load += async (_, _) => await LoadCurrentConfigAsync();
        Load += async (_, _) => await CheckReachabilityAsync();
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
            Width = LanguageButtonWidth,
            Height = LanguageButtonHeight,
            FlatStyle = FlatStyle.Flat,
            BackColor = TealPrimary,
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        languageButton.FlatAppearance.BorderSize = 0;
        languageButton.Click += (_, _) => languageMenu.Show(languageButton, new Point(0, languageButton.Height));

        return languageButton;
    }

    private void BuildFieldSections()
    {
        int y = 8;

        y = AddSectionHeader(Strings.ServiceConfigRelaySectionHeader, y);
        _namespaceBox = AddField(Strings.ServiceConfigNamespaceLabel, ref y);
        _connectionNameBox = AddField(Strings.ServiceConfigConnectionNameLabel, ref y);
        _keyNameBox = AddField(Strings.ServiceConfigKeyNameLabel, ref y);
        _relayKeyBox = AddField(Strings.ServiceConfigKeyLabel, ref y);

        y += 12;
        y = AddSectionHeader(Strings.ServiceConfigLocalApiSectionHeader, y);
        _portBox = AddField(Strings.ServiceConfigPortLabel, ref y, width: 120, numeric: true);

        y += 12;
        y = AddSectionHeader(Strings.ServiceConfigDatabaseSectionHeader, y);
        _dbConnectionStringBox = AddField(Strings.ServiceConfigDatabaseConnectionStringLabel, ref y);

        _tryConnectionButton = new Button { Text = Strings.ServiceConfigTryConnectionButtonText, Left = 0, Top = y, Width = 150, Height = 30, Enabled = false };
        _tryConnectionButton.Click += OnTryConnectionClick;
        _fieldsPanel.Controls.Add(_tryConnectionButton);
        y += 36;
        _connectionTestLabel = new Label { Left = 0, Top = y, Width = 460, Height = 20 };
        _fieldsPanel.Controls.Add(_connectionTestLabel);
        y += 32;

        y += 12;
        y = AddSectionHeader(Strings.ServiceConfigJwtSectionHeader, y);
        _jwtKeyBox = AddField(Strings.ServiceConfigJwtKeyLabel, ref y);

        _generateJwtKeyButton = new Button { Text = Strings.ServiceConfigGenerateJwtKeyButtonText, Left = 0, Top = y, Width = 150, Height = 30, Enabled = false };
        _generateJwtKeyButton.Click += (_, _) => _jwtKeyBox.Text = RandomNumberGenerator.GetString(JwtKeyAlphabet, 48);
        _fieldsPanel.Controls.Add(_generateJwtKeyButton);
        y += 36;

        var accessLabel = new Label { Text = Strings.ServiceConfigAccessTokenMinutesLabel, Left = 0, Top = y, Width = 220 };
        var refreshLabel = new Label { Text = Strings.ServiceConfigRefreshTokenDaysLabel, Left = 240, Top = y, Width = 220 };
        _fieldsPanel.Controls.Add(accessLabel);
        _fieldsPanel.Controls.Add(refreshLabel);
        y += 18;
        _accessTokenMinutesBox = new TextBox { Left = 0, Top = y, Width = 100, Enabled = false };
        _accessTokenMinutesBox.KeyPress += DigitOnlyKeyPress;
        _refreshTokenDaysBox = new TextBox { Left = 240, Top = y, Width = 100, Enabled = false };
        _refreshTokenDaysBox.KeyPress += DigitOnlyKeyPress;
        _fieldsPanel.Controls.Add(_accessTokenMinutesBox);
        _fieldsPanel.Controls.Add(_refreshTokenDaysBox);
        y += 28;

        y += 12;
        y = AddSectionHeader(Strings.ServiceConfigEmailSectionHeader, y);
        _emailConnectionStringBox = AddField(Strings.ServiceConfigEmailConnectionStringLabel, ref y);
        _senderAddressBox = AddField(Strings.ServiceConfigSenderAddressLabel, ref y);

        y += 12;
        y = AddSectionHeader(Strings.ServiceConfigTokenTrackingSectionHeader, y);
        _tokenTrackingBaseUrlBox = AddField(Strings.ServiceConfigTokenTrackingBaseUrlLabel, ref y);
        _tokenTrackingApiKeyBox = AddField(Strings.ServiceConfigTokenTrackingApiKeyLabel, ref y);
        _tokenTrackingIntervalMinutesBox = AddField(Strings.ServiceConfigTokenTrackingIntervalMinutesLabel, ref y, width: 120, numeric: true);
    }

    private int AddSectionHeader(string text, int top)
    {
        var header = new Label
        {
            Text = text,
            Left = 0,
            Top = top,
            Width = 460,
            Font = new Font(Font.FontFamily, 10, FontStyle.Bold),
        };
        _fieldsPanel.Controls.Add(header);
        return top + 26;
    }

    private TextBox AddField(string labelText, ref int y, int width = 460, bool numeric = false)
    {
        var label = new Label { Text = labelText, Left = 0, Top = y, Width = width };
        y += 18;
        var textBox = new TextBox { Left = 0, Top = y, Width = width, Enabled = false };
        if (numeric) textBox.KeyPress += DigitOnlyKeyPress;
        _fieldsPanel.Controls.Add(label);
        _fieldsPanel.Controls.Add(textBox);
        y += 28;
        return textBox;
    }

    private static void DigitOnlyKeyPress(object? sender, KeyPressEventArgs e)
    {
        if (!char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)) e.Handled = true;
    }

    private string? ResolveAppSettingsPath() => AdminAppPaths.ResolveAppSettingsPath(_windowsServiceInstaller);

    private async Task LoadCurrentConfigAsync()
    {
        try
        {
            string path = ResolveAppSettingsPath() ?? throw new FileNotFoundException("appsettings.json not found.");
            string json = await File.ReadAllTextAsync(path);
            JsonNode? root = JsonNode.Parse(json) ?? throw new InvalidOperationException("appsettings.json is empty.");

            _namespaceBox.Text = root["Relay"]?["Namespace"]?.GetValue<string>() ?? string.Empty;
            _connectionNameBox.Text = root["Relay"]?["ConnectionName"]?.GetValue<string>() ?? string.Empty;
            _keyNameBox.Text = root["Relay"]?["KeyName"]?.GetValue<string>() ?? string.Empty;
            _relayKeyBox.Text = root["Relay"]?["Key"]?.GetValue<string>() ?? string.Empty;

            _portBox.Text = (root["LocalApi"]?["Port"]?.GetValue<int>() ?? 5080).ToString();

            _dbConnectionStringBox.Text = root["ConnectionStrings"]?["ProdHelperDb"]?.GetValue<string>() ?? string.Empty;

            _originalJwtKey = root["Jwt"]?["Key"]?.GetValue<string>() ?? string.Empty;
            _jwtKeyBox.Text = _originalJwtKey;
            _accessTokenMinutesBox.Text = (root["Jwt"]?["AccessTokenMinutes"]?.GetValue<int>() ?? 15).ToString();
            _refreshTokenDaysBox.Text = (root["Jwt"]?["RefreshTokenDays"]?.GetValue<int>() ?? 14).ToString();

            _emailConnectionStringBox.Text = root["Email"]?["ConnectionString"]?.GetValue<string>() ?? string.Empty;
            _senderAddressBox.Text = root["Email"]?["SenderAddress"]?.GetValue<string>() ?? string.Empty;

            _tokenTrackingBaseUrlBox.Text = root["TokenTracking"]?["BaseUrl"]?.GetValue<string>() ?? string.Empty;
            _tokenTrackingApiKeyBox.Text = root["TokenTracking"]?["ApiKey"]?.GetValue<string>() ?? string.Empty;
            _tokenTrackingIntervalMinutesBox.Text = (root["TokenTracking"]?["IntervalMinutes"]?.GetValue<int>() ?? 10).ToString();

            SetFieldsEnabled(true);
        }
        catch
        {
            _validationLabel.ForeColor = ErrorColor;
            _validationLabel.Text = Strings.ServiceConfigLoadFailedMessage;
            SetFieldsEnabled(false);
        }
    }

    private void SetFieldsEnabled(bool enabled)
    {
        foreach (Control control in _fieldsPanel.Controls)
        {
            if (control is TextBox textBox) textBox.Enabled = enabled;
        }
        _tryConnectionButton.Enabled = enabled;
        _generateJwtKeyButton.Enabled = enabled;
        _saveButton.Enabled = enabled;
    }

    private async void OnStartClick(object? sender, EventArgs e) =>
        await RunServiceActionAsync(Strings.ServiceStartingMessage, () => _windowsServiceInstaller.StartAsync(CancellationToken.None));

    private async void OnStopClick(object? sender, EventArgs e) =>
        await RunServiceActionAsync(Strings.ServiceStoppingMessage, () => _windowsServiceInstaller.StopAsync(CancellationToken.None));

    // Windows Service Control Manager access only - no HTTP - so this works whether or not the
    // API is currently reachable, which is the entire reason this form exists.
    private async Task RunServiceActionAsync(string busyMessage, Func<Task<ServiceOperationResult>> action)
    {
        _startButton.Enabled = false;
        _stopButton.Enabled = false;
        _reachabilityLabel.ForeColor = TealPrimary;
        _reachabilityLabel.Text = busyMessage;

        ServiceOperationResult result = await action();
        if (!result.Success)
        {
            MessageBox.Show(result.ErrorMessage, Strings.ServiceConfigDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        _startButton.Enabled = true;
        _stopButton.Enabled = true;

        // Starting/stopping the Windows Service doesn't guarantee Kestrel is actually
        // listening/configured correctly yet - always re-check rather than assume.
        await CheckReachabilityAsync();
    }

    private async void OnTryServiceClick(object? sender, EventArgs e)
    {
        string? version = await CheckReachabilityAsync();
        if (version is not null)
        {
            MessageBox.Show(Strings.ServiceConfigVersionMessage(version), Strings.ServiceConfigDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // Also drives Start/Stop/Test button visibility: unreachable -> Start only; reachable -> Stop
    // + Test (it's presumably already running, so offer to stop it or re-verify it). Returns the
    // reported version (null if unreachable) so OnTryServiceClick can show a version popup only
    // for an explicit user click, not for the automatic checks on Load/after Start-Stop.
    private async Task<string?> CheckReachabilityAsync()
    {
        _tryServiceButton.Enabled = false;
        _reachabilityLabel.ForeColor = TealPrimary;
        _reachabilityLabel.Text = Strings.ServiceConfigCheckingMessage;
        string? version;
        try
        {
            version = (await _serviceApiClient.GetVersionAsync(null)).Version;
            _reachabilityLabel.ForeColor = TealPrimary;
            _reachabilityLabel.Text = Strings.ServiceConfigReachableMessage;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or AuthApiException)
        {
            version = null;
            _reachabilityLabel.ForeColor = ErrorColor;
            _reachabilityLabel.Text = Strings.ServiceConfigUnreachableMessage;
        }
        finally
        {
            _tryServiceButton.Enabled = true;
        }

        bool reachable = version is not null;
        _startButton.Visible = !reachable;
        _stopButton.Visible = reachable;
        _tryServiceButton.Visible = reachable;
        return version;
    }

    private async void OnTryConnectionClick(object? sender, EventArgs e)
    {
        _tryConnectionButton.Enabled = false;
        _connectionTestLabel.ForeColor = TealPrimary;
        _connectionTestLabel.Text = Strings.ServiceConfigTestingConnectionMessage;
        try
        {
            await using var connection = new SqlConnection(_dbConnectionStringBox.Text);
            await connection.OpenAsync();
            _connectionTestLabel.ForeColor = TealPrimary;
            _connectionTestLabel.Text = Strings.ServiceConfigConnectionSuccessMessage;
        }
        catch (Exception ex)
        {
            _connectionTestLabel.ForeColor = ErrorColor;
            _connectionTestLabel.Text = Strings.ServiceConfigConnectionFailedMessage(ex.Message);
        }
        finally
        {
            _tryConnectionButton.Enabled = true;
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        _validationLabel.ForeColor = ErrorColor;
        _validationLabel.Text = string.Empty;

        if (!int.TryParse(_portBox.Text, out int port) || port is < 1 or > 65535)
        {
            _validationLabel.Text = Strings.ServiceConfigInvalidPortMessage;
            return;
        }
        if (!int.TryParse(_accessTokenMinutesBox.Text, out int accessMinutes) || accessMinutes < 1)
        {
            _validationLabel.Text = Strings.ServiceConfigInvalidAccessTokenMinutesMessage;
            return;
        }
        if (!int.TryParse(_refreshTokenDaysBox.Text, out int refreshDays) || refreshDays < 1)
        {
            _validationLabel.Text = Strings.ServiceConfigInvalidRefreshTokenDaysMessage;
            return;
        }
        if (!int.TryParse(_tokenTrackingIntervalMinutesBox.Text, out int tokenTrackingIntervalMinutes) || tokenTrackingIntervalMinutes < 1)
        {
            _validationLabel.Text = Strings.ServiceConfigInvalidIntervalMinutesMessage;
            return;
        }

        if (_jwtKeyBox.Text != _originalJwtKey)
        {
            DialogResult confirm = MessageBox.Show(
                Strings.ServiceConfigJwtKeyChangeWarningMessage,
                Strings.ServiceConfigDialogTitle,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirm != DialogResult.Yes) return;
        }

        SetBusy(true);
        _validationLabel.ForeColor = TealPrimary;
        _validationLabel.Text = Strings.ServiceConfigSavingMessage;

        bool saved = await TrySaveAsync(port, accessMinutes, refreshDays, tokenTrackingIntervalMinutes);

        SetBusy(false);
        if (!saved)
        {
            _validationLabel.ForeColor = ErrorColor;
            _validationLabel.Text = Strings.ServiceConfigSaveFailedMessage;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // Read-modify-write, same pattern as ServiceLifecycleManager.TryPersistPortAsync - only the
    // leaf keys this form edits are touched, everything else in the file (Cors, Translation,
    // Jwt:Issuer/Audience, ...) is preserved untouched.
    private async Task<bool> TrySaveAsync(int port, int accessMinutes, int refreshDays, int tokenTrackingIntervalMinutes)
    {
        try
        {
            string? path = ResolveAppSettingsPath();
            if (path is null) return false;

            string json = await File.ReadAllTextAsync(path);
            JsonNode? root = JsonNode.Parse(json);
            if (root is null) return false;

            root["Relay"] ??= new JsonObject();
            root["Relay"]!["Namespace"] = _namespaceBox.Text;
            root["Relay"]!["ConnectionName"] = _connectionNameBox.Text;
            root["Relay"]!["KeyName"] = _keyNameBox.Text;
            root["Relay"]!["Key"] = _relayKeyBox.Text;

            root["LocalApi"] ??= new JsonObject();
            root["LocalApi"]!["Port"] = port;

            root["ConnectionStrings"] ??= new JsonObject();
            root["ConnectionStrings"]!["ProdHelperDb"] = _dbConnectionStringBox.Text;

            root["Jwt"] ??= new JsonObject();
            root["Jwt"]!["Key"] = _jwtKeyBox.Text;
            root["Jwt"]!["AccessTokenMinutes"] = accessMinutes;
            root["Jwt"]!["RefreshTokenDays"] = refreshDays;

            root["Email"] ??= new JsonObject();
            root["Email"]!["ConnectionString"] = _emailConnectionStringBox.Text;
            root["Email"]!["SenderAddress"] = _senderAddressBox.Text;

            root["TokenTracking"] ??= new JsonObject();
            root["TokenTracking"]!["BaseUrl"] = _tokenTrackingBaseUrlBox.Text;
            root["TokenTracking"]!["ApiKey"] = _tokenTrackingApiKeyBox.Text;
            root["TokenTracking"]!["IntervalMinutes"] = tokenTrackingIntervalMinutes;

            await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetBusy(bool busy)
    {
        _saveButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
        foreach (Control control in _fieldsPanel.Controls)
        {
            if (control is TextBox textBox) textBox.Enabled = !busy;
        }
    }
}
