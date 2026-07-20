using System.Text.Json;
using System.Text.Json.Nodes;
using ProdHelperService.ServiceManagement;

namespace ProdHelperService.AdminApp;

// Configures Storage:UploadPath in ProdHelperService's appsettings.json - the folder a future web
// app upload feature will store documents in. This form only sets the path; the upload feature
// itself is separate, later work.
public class UploadFolderForm : Form
{
    private static readonly Color TealPrimary = ColorTranslator.FromHtml("#27627B");
    private static readonly Color ErrorColor = ColorTranslator.FromHtml("#D24D2A");

    private const int ContentLeft = 24;
    private const int ContentWidth = 432;

    private readonly IWindowsServiceInstaller _windowsServiceInstaller;

    private readonly TextBox _pathBox;
    private readonly Button _browseButton;
    private readonly Label _validationLabel;
    private readonly Button _saveButton;
    private readonly Button _cancelButton;

    public UploadFolderForm(IWindowsServiceInstaller windowsServiceInstaller)
    {
        _windowsServiceInstaller = windowsServiceInstaller;

        Text = Strings.UploadFolderDialogTitle;
        ClientSize = new Size(480, 260); // recomputed to fit content exactly at the end of this constructor
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Padding = new Padding(24);

        // Label.AutoSize defaults to false when constructed in code, so an unmeasured Height would
        // clip a 16pt title - same fix already used in ServiceConfigForm/LoginForm.
        var title = new Label
        {
            Text = Strings.UploadFolderDialogTitle,
            Left = ContentLeft,
            Top = 20,
            Width = ContentWidth,
            Font = new Font(Font.FontFamily, 16, FontStyle.Regular),
            AutoSize = false,
        };
        title.Height = (int)Math.Ceiling(TextRenderer.MeasureText(title.Text, title.Font).Height * 1.15);

        int y = title.Top + title.Height + 8;

        var description = new Label { Text = Strings.UploadFolderDialogDescription, Left = ContentLeft, Top = y, Width = ContentWidth, AutoSize = false };
        Size measuredDescription = TextRenderer.MeasureText(description.Text, description.Font, new Size(ContentWidth, int.MaxValue), TextFormatFlags.WordBreak);
        description.Height = (int)Math.Ceiling(measuredDescription.Height * 1.15);
        y += description.Height + 16;

        var pathLabel = new Label { Left = ContentLeft, Top = y, Width = ContentWidth, Height = 20, AutoSize = false, Text = Strings.UploadFolderPathLabel };
        y += 24;

        // Browse sits below the path box (not beside it) so its own translated text - which can
        // run noticeably longer than the English "Browse..." - always has full width to render
        // without being squeezed against the textbox.
        _pathBox = new TextBox { Left = ContentLeft, Top = y, Width = ContentWidth };
        y += 28;

        _browseButton = new Button { Text = Strings.UploadFolderBrowseButtonText, Left = ContentLeft, Top = y, Width = 160, Height = 32 };
        _browseButton.Click += OnBrowseClick;
        y += 32 + 20;

        _validationLabel = new Label { Left = ContentLeft, Top = y, Width = ContentWidth, Height = 40, AutoSize = false, ForeColor = ErrorColor };
        y += 40 + 16;

        int buttonRowTop = y;
        _saveButton = new Button { Text = Strings.UploadFolderSaveButtonText, Left = ContentLeft, Top = buttonRowTop, Width = 150, Height = 34, BackColor = TealPrimary, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        _saveButton.FlatAppearance.BorderSize = 0;
        _saveButton.Click += OnSaveClick;

        _cancelButton = new Button { Text = Strings.UploadFolderCancelButtonText, Left = ContentLeft + 164, Top = buttonRowTop, Width = 150, Height = 34 };
        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };
        y += 34;

        ClientSize = new Size(480, y + 32);

        Controls.Add(title);
        Controls.Add(description);
        Controls.Add(pathLabel);
        Controls.Add(_pathBox);
        Controls.Add(_browseButton);
        Controls.Add(_validationLabel);
        Controls.Add(_saveButton);
        Controls.Add(_cancelButton);
        CancelButton = _cancelButton;

        Load += async (_, _) => await LoadCurrentPathAsync();
    }

    // Best-effort - a missing file or missing value just leaves the field blank rather than
    // showing an error, since there's nothing to load yet on a fresh install (unlike
    // ServiceConfigForm, where a load failure is a genuine problem worth surfacing).
    private async Task LoadCurrentPathAsync()
    {
        try
        {
            string? path = AdminAppPaths.ResolveAppSettingsPath(_windowsServiceInstaller);
            if (path is null) return;

            string json = await File.ReadAllTextAsync(path);
            JsonNode? root = JsonNode.Parse(json);
            _pathBox.Text = root?["Storage"]?["UploadPath"]?.GetValue<string>() ?? string.Empty;
        }
        catch
        {
            // Leave the field blank.
        }
    }

    private void OnBrowseClick(object? sender, EventArgs e)
    {
        try
        {
            using var dialog = new FolderBrowserDialog
            {
                // UseDescriptionForTitle is not set - it has no effect on old-style dialogs, per
                // its own documentation, and AutoUpgradeEnabled is false below.
                Description = Strings.UploadFolderDialogDescription,
                SelectedPath = Directory.Exists(_pathBox.Text) ? _pathBox.Text : string.Empty,
                // The modernized (Vista-style) COM-based picker that's the default since .NET
                // Core 3.0 is prone to hanging when shown from an elevated process (AdminApp runs
                // elevated - see app.manifest) - Microsoft's own documented remedy for
                // compatibility/reliability issues with it is falling back to the older,
                // non-COM dialog via AutoUpgradeEnabled = false. Kept even after fixing the root
                // STA cause in Program.cs (missing SynchronizationContext) as defense in depth,
                // since the modern dialog is inherently more COM-heavy.
                AutoUpgradeEnabled = false,
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pathBox.Text = dialog.SelectedPath;
            }
        }
        catch (Exception ex)
        {
            // FolderBrowserDialog failing outright (rather than just being cancelled) shouldn't
            // happen, but silently doing nothing on click - with no error, nothing in any log -
            // is much harder to diagnose than just showing what went wrong.
            MessageBox.Show(ex.Message, Strings.UploadFolderDialogTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void OnSaveClick(object? sender, EventArgs e)
    {
        _validationLabel.ForeColor = ErrorColor;
        _validationLabel.Text = string.Empty;

        string path = _pathBox.Text.Trim();
        if (path.Length == 0)
        {
            _validationLabel.Text = Strings.UploadFolderInvalidPathMessage;
            return;
        }

        SetBusy(true);
        _validationLabel.ForeColor = TealPrimary;
        _validationLabel.Text = Strings.UploadFolderSavingMessage;

        bool saved = await TrySaveAsync(path);

        SetBusy(false);
        if (!saved)
        {
            _validationLabel.ForeColor = ErrorColor;
            _validationLabel.Text = Strings.UploadFolderSaveFailedMessage;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    // Read-modify-write, same pattern as ServiceConfigForm.TrySaveAsync - only Storage:UploadPath
    // is touched, everything else in the file is preserved untouched.
    private async Task<bool> TrySaveAsync(string path)
    {
        try
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);

            string? settingsPath = AdminAppPaths.ResolveAppSettingsPath(_windowsServiceInstaller);
            if (settingsPath is null) return false;

            string json = await File.ReadAllTextAsync(settingsPath);
            JsonNode? root = JsonNode.Parse(json);
            if (root is null) return false;

            root["Storage"] ??= new JsonObject();
            root["Storage"]!["UploadPath"] = path;

            await File.WriteAllTextAsync(settingsPath, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void SetBusy(bool busy)
    {
        _pathBox.Enabled = !busy;
        _browseButton.Enabled = !busy;
        _saveButton.Enabled = !busy;
        _cancelButton.Enabled = !busy;
    }
}
