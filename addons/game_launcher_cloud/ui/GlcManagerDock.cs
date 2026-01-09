#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace GameLauncherCloud;

/// <summary>
/// Main dock UI for Game Launcher Cloud Manager.
/// Provides UI for authentication and build & upload.
/// </summary>
[Tool]
public partial class GlcManagerDock : Control
{
    #region Constants

    private const bool ShowDeveloperOptions = false;

    #endregion

    #region Fields

    private GlcConfig _config = null!;
    private GlcApiClient _apiClient = null!;

    // UI References
    private Label? _statusLabel;
    private Label? _userLabel;
    private VBoxContainer? _loginSection;
    private VBoxContainer? _buildUploadSection;
    private LineEdit? _apiKeyInput;
    private Button? _loginButton;
    private Label? _loginMessage;
    private OptionButton? _appDropdown;
    private OptionButton? _presetDropdown;
    private TextEdit? _buildNotesInput;
    private VBoxContainer? _buildStatusContainer;
    private Label? _statusDetails;
    private Label? _statusSize;
    private Button? _buildUploadButton;
    private HBoxContainer? _separateBuildButtons;
    private Button? _buildOnlyButton;
    private Button? _uploadOnlyButton;
    private VBoxContainer? _progressContainer;
    private Label? _progressLabel;
    private ProgressBar? _progressBar;
    private Label? _buildMessage;
    private Button? _viewBuildButton;
    private Label? _environmentLabel;
    private Button? _logoutButton;

    // State
    private GlcApiClient.AppInfo[]? _availableApps;
    private List<ExportPresetInfo> _exportPresets = new();
    private bool _isLoggingIn;
    private bool _isLoadingApps;
    private bool _isBuilding;
    private bool _isUploading;
    private long _completedBuildId;
    private long _completedBuildAppId;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        // Load configuration
        _config = GlcConfigManager.LoadConfig();

        // Force Production environment when Developer options are disabled
        if (!ShowDeveloperOptions && _config.Environment != GlcEnvironment.Production)
        {
            _config.Environment = GlcEnvironment.Production;
            GlcConfigManager.SaveConfig(_config);
            GD.Print("[GLC] Forced environment to Production (Developer options disabled)");
        }

        // Initialize API client
        _apiClient = new GlcApiClient(_config.GetApiUrl(), _config.AuthToken ?? "");

        GD.Print($"[GLC] Manager dock initialized. Environment: {_config.Environment}");

        // Get UI references
        InitializeUiReferences();

        // Connect signals
        ConnectSignals();

        // Update UI state
        UpdateUiState();

        // Load export presets
        RefreshExportPresets();

        // Load apps if authenticated
        if (_config.IsAuthenticated)
        {
            _ = LoadAppsAsync();
        }
    }

    private void InitializeUiReferences()
    {
        // Header
        _statusLabel = GetNodeOrNull<Label>("MainContainer/Header/HeaderContent/StatusContainer/StatusLabel");
        _userLabel = GetNodeOrNull<Label>("MainContainer/Header/HeaderContent/StatusContainer/UserLabel");

        // Login section
        _loginSection = GetNodeOrNull<VBoxContainer>("MainContainer/ScrollContainer/ContentContainer/LoginSection");
        _apiKeyInput = GetNodeOrNull<LineEdit>("MainContainer/ScrollContainer/ContentContainer/LoginSection/ApiKeyInput");
        _loginButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/LoginSection/LoginButton");
        _loginMessage = GetNodeOrNull<Label>("MainContainer/ScrollContainer/ContentContainer/LoginSection/LoginMessage");

        // Build & Upload section
        _buildUploadSection = GetNodeOrNull<VBoxContainer>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection");
        _appDropdown = GetNodeOrNull<OptionButton>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/AppDropdown");
        _presetDropdown = GetNodeOrNull<OptionButton>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ExportPresetContainer/PresetDropdown");
        _buildNotesInput = GetNodeOrNull<TextEdit>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildNotesContainer/BuildNotesInput");
        _buildStatusContainer = GetNodeOrNull<VBoxContainer>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildStatusContainer");
        _statusDetails = GetNodeOrNull<Label>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildStatusContainer/StatusHeader/StatusInfo/StatusDetails");
        _statusSize = GetNodeOrNull<Label>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildStatusContainer/StatusHeader/StatusInfo/StatusSize");
        _buildUploadButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ActionButtons/BuildUploadButton");
        _separateBuildButtons = GetNodeOrNull<HBoxContainer>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ActionButtons/SeparateBuildButtons");
        _buildOnlyButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ActionButtons/SeparateBuildButtons/BuildOnlyButton");
        _uploadOnlyButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ActionButtons/SeparateBuildButtons/UploadOnlyButton");
        _progressContainer = GetNodeOrNull<VBoxContainer>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ProgressContainer");
        _progressLabel = GetNodeOrNull<Label>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ProgressContainer/ProgressLabel");
        _progressBar = GetNodeOrNull<ProgressBar>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ProgressContainer/ProgressBar");
        _buildMessage = GetNodeOrNull<Label>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildMessage");
        _viewBuildButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ViewBuildButton");

        // Footer
        _environmentLabel = GetNodeOrNull<Label>("MainContainer/Footer/EnvironmentLabel");
        _logoutButton = GetNodeOrNull<Button>("MainContainer/Footer/LogoutButton");

        // Restore saved values
        if (_apiKeyInput != null && !string.IsNullOrEmpty(_config.GetApiKey()))
        {
            _apiKeyInput.Text = _config.GetApiKey();
        }

        if (_buildNotesInput != null && !string.IsNullOrEmpty(_config.BuildNotes))
        {
            _buildNotesInput.Text = _config.BuildNotes;
        }
    }

    private void ConnectSignals()
    {
        // Login
        _loginButton?.Connect("pressed", Callable.From(OnLoginPressed));

        var getApiKeyButton = GetNodeOrNull<LinkButton>("MainContainer/ScrollContainer/ContentContainer/LoginSection/ApiKeyHint/GetApiKeyButton");
        getApiKeyButton?.Connect("pressed", Callable.From(() => OS.ShellOpen($"{_config.GetFrontendUrl()}/user/api-keys")));

        // App selection
        var loadAppsButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/LoadAppsButton");
        loadAppsButton?.Connect("pressed", Callable.From(() => _ = LoadAppsAsync()));

        var refreshAppsButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/AppButtons/RefreshAppsButton");
        refreshAppsButton?.Connect("pressed", Callable.From(() => _ = LoadAppsAsync()));

        var manageAppButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/AppButtons/ManageAppButton");
        manageAppButton?.Connect("pressed", Callable.From(OnManageAppPressed));

        _appDropdown?.Connect("item_selected", Callable.From<long>(OnAppSelected));

        // Export presets
        var refreshPresetsButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ExportPresetContainer/RefreshPresetsButton");
        refreshPresetsButton?.Connect("pressed", Callable.From(RefreshExportPresets));

        _presetDropdown?.Connect("item_selected", Callable.From<long>(OnPresetSelected));

        // Build actions
        _buildUploadButton?.Connect("pressed", Callable.From(OnBuildUploadPressed));
        _buildOnlyButton?.Connect("pressed", Callable.From(OnBuildOnlyPressed));
        _uploadOnlyButton?.Connect("pressed", Callable.From(OnUploadOnlyPressed));

        var openExportButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/ActionButtons/OpenExportButton");
        openExportButton?.Connect("pressed", Callable.From(() => EditorInterface.Singleton.PopupDialogCentered(CreateExportHintDialog())));

        var showInExplorerButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/BuildStatusContainer/ShowInExplorerButton");
        showInExplorerButton?.Connect("pressed", Callable.From(OnShowInExplorerPressed));

        _viewBuildButton?.Connect("pressed", Callable.From(OnViewBuildPressed));

        // Links
        var websiteButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/LinksSection/LinksButtons/WebsiteButton");
        websiteButton?.Connect("pressed", Callable.From(() => OS.ShellOpen("https://gamelauncher.cloud")));

        var docsButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/LinksSection/LinksButtons/DocsButton");
        docsButton?.Connect("pressed", Callable.From(() => OS.ShellOpen("https://help.gamelauncher.cloud")));

        var discordButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/LinksSection/LinksButtons/DiscordButton");
        discordButton?.Connect("pressed", Callable.From(() => OS.ShellOpen("https://discord.com/invite/FpWvUQ2CJP")));

        var cliLinkButton = GetNodeOrNull<LinkButton>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/CliWarning/WarningContainer/CliLinkButton");
        cliLinkButton?.Connect("pressed", Callable.From(() => OS.ShellOpen("https://help.gamelauncher.cloud/applications/cli-releases")));

        // Footer
        _logoutButton?.Connect("pressed", Callable.From(OnLogoutPressed));
    }

    #endregion

    #region UI State

    private void UpdateUiState()
    {
        var isAuthenticated = _config.IsAuthenticated;

        // Update status
        if (_statusLabel != null)
        {
            _statusLabel.Text = isAuthenticated ? "✓ Connected" : "Not connected";
            _statusLabel.Modulate = isAuthenticated ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.7f, 0.4f);
        }

        if (_userLabel != null)
        {
            _userLabel.Text = isAuthenticated ? _config.UserEmail : "";
            _userLabel.Visible = isAuthenticated;
        }

        // Show/hide sections
        if (_loginSection != null)
        {
            _loginSection.Visible = !isAuthenticated;
        }

        if (_buildUploadSection != null)
        {
            _buildUploadSection.Visible = isAuthenticated;
        }

        if (_logoutButton != null)
        {
            _logoutButton.Visible = isAuthenticated;
        }

        // Update environment label
        if (_environmentLabel != null)
        {
            _environmentLabel.Text = $"Environment: {_config.Environment}";
        }

        // Update build status
        UpdateBuildStatus();
    }

    private void UpdateBuildStatus()
    {
        var buildInfo = GlcBuildManager.GetExistingBuild();
        var hasBuild = buildInfo != null;

        if (_buildStatusContainer != null)
        {
            _buildStatusContainer.Visible = hasBuild;
        }

        if (hasBuild && buildInfo != null)
        {
            if (_statusDetails != null)
            {
                _statusDetails.Text = $"Last build: {buildInfo.LastModified:yyyy-MM-dd HH:mm:ss}";
            }

            if (_statusSize != null)
            {
                if (buildInfo.IsCompressed)
                {
                    var ratio = buildInfo.UncompressedSize > 0
                        ? (1 - (double)buildInfo.Size / buildInfo.UncompressedSize) * 100
                        : 0;
                    _statusSize.Text = $"Files: {buildInfo.FileCount} | Uncompressed: {GlcMultipartUploadHelper.FormatFileSize(buildInfo.UncompressedSize)} | Compressed: {GlcMultipartUploadHelper.FormatFileSize(buildInfo.Size)} ({ratio:F1}% saved)";
                }
                else
                {
                    _statusSize.Text = $"Files: {buildInfo.FileCount} | Size: {GlcMultipartUploadHelper.FormatFileSize(buildInfo.Size)} (Not compressed)";
                }
            }
        }

        // Show separate build/upload buttons if build exists
        if (_buildUploadButton != null)
        {
            _buildUploadButton.Visible = !hasBuild;
        }

        if (_separateBuildButtons != null)
        {
            _separateBuildButtons.Visible = hasBuild;
        }
    }

    private void SetProgress(bool visible, string message = "", float progress = 0f)
    {
        CallDeferred(MethodName.SetProgressDeferred, visible, message, progress);
    }

    private void SetProgressDeferred(bool visible, string message, float progress)
    {
        if (_progressContainer != null)
        {
            _progressContainer.Visible = visible;
        }

        if (_progressLabel != null)
        {
            _progressLabel.Text = message;
        }

        if (_progressBar != null)
        {
            _progressBar.Value = progress * 100;
        }
    }

    private void SetMessage(string message, bool isError = false)
    {
        CallDeferred(MethodName.SetMessageDeferred, message, isError);
    }

    private void SetMessageDeferred(string message, bool isError)
    {
        if (_buildMessage != null)
        {
            _buildMessage.Text = message;
            _buildMessage.Modulate = isError ? new Color(1f, 0.4f, 0.4f) : new Color(0.4f, 1f, 0.4f);
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        CallDeferred(MethodName.SetButtonsEnabledDeferred, enabled);
    }

    private void SetButtonsEnabledDeferred(bool enabled)
    {
        if (_buildUploadButton != null)
        {
            _buildUploadButton.Disabled = !enabled;
        }

        if (_buildOnlyButton != null)
        {
            _buildOnlyButton.Disabled = !enabled;
        }

        if (_uploadOnlyButton != null)
        {
            _uploadOnlyButton.Disabled = !enabled;
        }
    }

    #endregion

    #region Login

    private async void OnLoginPressed()
    {
        if (_apiKeyInput == null || string.IsNullOrWhiteSpace(_apiKeyInput.Text))
        {
            if (_loginMessage != null)
            {
                _loginMessage.Text = "Please enter your API Key";
                _loginMessage.Modulate = new Color(1f, 0.7f, 0.4f);
            }
            return;
        }

        _isLoggingIn = true;

        if (_loginButton != null)
        {
            _loginButton.Disabled = true;
            _loginButton.Text = "Logging in...";
        }

        if (_loginMessage != null)
        {
            _loginMessage.Text = "";
        }

        var (success, message, response) = await _apiClient.LoginWithApiKeyAsync(_apiKeyInput.Text);

        _isLoggingIn = false;

        if (_loginButton != null)
        {
            _loginButton.Disabled = false;
            _loginButton.Text = "Connect Account";
        }

        if (success && response != null)
        {
            // Save configuration
            _config.SetApiKey(_apiKeyInput.Text);
            _config.AuthToken = response.Token;
            _config.UserId = response.Id;
            _config.UserEmail = response.Email;
            _config.UserPlan = response.Subscription?.Plan?.Name ?? "Free";
            GlcConfigManager.SaveConfig(_config);

            _apiClient.SetAuthToken(response.Token);

            if (_loginMessage != null)
            {
                _loginMessage.Text = "Login successful!";
                _loginMessage.Modulate = new Color(0.4f, 1f, 0.4f);
            }

            UpdateUiState();

            // Load apps
            _ = LoadAppsAsync();
        }
        else
        {
            if (_loginMessage != null)
            {
                _loginMessage.Text = message;
                _loginMessage.Modulate = new Color(1f, 0.4f, 0.4f);
            }
        }
    }

    private void OnLogoutPressed()
    {
        // Confirm logout
        var dialog = new ConfirmationDialog();
        dialog.DialogText = "Are you sure you want to logout?";
        dialog.OkButtonText = "Yes";
        dialog.CancelButtonText = "No";
        dialog.Confirmed += () =>
        {
            GlcConfigManager.ClearAuth();
            _config = GlcConfigManager.LoadConfig();
            _apiClient.SetAuthToken("");
            _availableApps = null;

            UpdateUiState();

            if (_appDropdown != null)
            {
                _appDropdown.Clear();
            }
        };

        AddChild(dialog);
        dialog.PopupCentered();
    }

    #endregion

    #region App Management

    private async Task LoadAppsAsync()
    {
        if (_isLoadingApps)
        {
            return;
        }

        _isLoadingApps = true;

        var loadAppsButton = GetNodeOrNull<Button>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/LoadAppsButton");
        if (loadAppsButton != null)
        {
            loadAppsButton.Disabled = true;
            loadAppsButton.Text = "⏳ Loading...";
        }

        var (success, message, apps) = await _apiClient.GetAppListAsync();

        _isLoadingApps = false;

        if (loadAppsButton != null)
        {
            loadAppsButton.Disabled = false;
            loadAppsButton.Text = "📱 Load My Apps";
        }

        if (success && apps != null && apps.Length > 0)
        {
            _availableApps = apps;

            if (_appDropdown != null)
            {
                _appDropdown.Clear();
                foreach (var app in apps)
                {
                    var label = $"{app.Name} ({app.BuildCount} builds)";
                    if (!app.IsOwnedByUser)
                    {
                        label += " [Team]";
                    }
                    _appDropdown.AddItem(label);
                }

                // Restore previously selected app
                if (_config.SelectedAppId > 0)
                {
                    for (var i = 0; i < apps.Length; i++)
                    {
                        if (apps[i].Id == _config.SelectedAppId)
                        {
                            _appDropdown.Selected = i;
                            break;
                        }
                    }
                }
            }

            // Hide load button, show dropdown
            if (loadAppsButton != null)
            {
                loadAppsButton.Visible = false;
            }

            var appButtons = GetNodeOrNull<HBoxContainer>("MainContainer/ScrollContainer/ContentContainer/BuildUploadSection/AppSelectionContainer/AppButtons");
            if (appButtons != null)
            {
                appButtons.Visible = true;
            }

            if (_appDropdown != null)
            {
                _appDropdown.Visible = true;
            }
        }
        else
        {
            SetMessage(message, true);
        }
    }

    private void OnAppSelected(long index)
    {
        if (_availableApps == null || index < 0 || index >= _availableApps.Length)
        {
            return;
        }

        var app = _availableApps[index];
        _config.SelectedAppId = app.Id;
        _config.SelectedAppName = app.Name;
        GlcConfigManager.SaveConfig(_config);
    }

    private void OnManageAppPressed()
    {
        if (_availableApps == null || _appDropdown == null)
        {
            return;
        }

        var selectedIndex = _appDropdown.Selected;
        if (selectedIndex < 0 || selectedIndex >= _availableApps.Length)
        {
            return;
        }

        var appId = _availableApps[selectedIndex].Id;
        OS.ShellOpen($"{_config.GetFrontendUrl()}/apps/id/{appId}/overview");
    }

    #endregion

    #region Export Presets

    private void RefreshExportPresets()
    {
        _exportPresets = GlcBuildManager.GetExportPresets();

        if (_presetDropdown != null)
        {
            _presetDropdown.Clear();

            if (_exportPresets.Count == 0)
            {
                _presetDropdown.AddItem("No export presets found");
                _presetDropdown.Disabled = true;
            }
            else
            {
                _presetDropdown.Disabled = false;
                foreach (var preset in _exportPresets)
                {
                    _presetDropdown.AddItem(preset.ToString());
                }

                // Restore previously selected preset
                if (_config.SelectedExportPreset >= 0 && _config.SelectedExportPreset < _exportPresets.Count)
                {
                    _presetDropdown.Selected = _config.SelectedExportPreset;
                }
            }
        }
    }

    private void OnPresetSelected(long index)
    {
        _config.SelectedExportPreset = (int)index;
        GlcConfigManager.SaveConfig(_config);
    }

    #endregion

    #region Build & Upload

    private void OnBuildUploadPressed()
    {
        _ = BuildAndUploadAsync();
    }

    private void OnBuildOnlyPressed()
    {
        _ = BuildOnlyAsync();
    }

    private void OnUploadOnlyPressed()
    {
        _ = UploadOnlyAsync();
    }

    private async Task BuildAndUploadAsync()
    {
        if (!ValidateBuildRequirements())
        {
            return;
        }

        _isBuilding = true;
        SetButtonsEnabled(false);
        SetMessage("");

        // Step 1: Export
        SetProgress(true, "Exporting project...", 0.1f);
        var (exportSuccess, exportMessage, outputPath) = await GlcBuildManager.ExportProjectAsync(
            _presetDropdown?.Selected ?? 0,
            msg => SetProgress(true, msg, 0.2f));

        if (!exportSuccess)
        {
            SetProgress(false);
            SetMessage($"Export failed: {exportMessage}", true);
            SetButtonsEnabled(true);
            _isBuilding = false;
            return;
        }

        // Step 2: Compress
        SetProgress(true, "Compressing build...", 0.4f);
        var (compressSuccess, compressMessage, zipPath, uncompressedSize) = await GlcBuildManager.CompressBuildAsync(
            msg => SetProgress(true, msg, 0.5f));

        if (!compressSuccess || zipPath == null)
        {
            SetProgress(false);
            SetMessage($"Compression failed: {compressMessage}", true);
            SetButtonsEnabled(true);
            _isBuilding = false;
            return;
        }

        _isBuilding = false;
        UpdateBuildStatus();

        // Step 3: Upload
        await UploadBuildAsync(zipPath, uncompressedSize);
    }

    private async Task BuildOnlyAsync()
    {
        if (!ValidateBuildRequirements())
        {
            return;
        }

        _isBuilding = true;
        SetButtonsEnabled(false);
        SetMessage("");

        // Step 1: Export
        SetProgress(true, "Exporting project...", 0.2f);
        var (exportSuccess, exportMessage, outputPath) = await GlcBuildManager.ExportProjectAsync(
            _presetDropdown?.Selected ?? 0,
            msg => SetProgress(true, msg, 0.3f));

        if (!exportSuccess)
        {
            SetProgress(false);
            SetMessage($"Export failed: {exportMessage}", true);
            SetButtonsEnabled(true);
            _isBuilding = false;
            return;
        }

        // Step 2: Compress
        SetProgress(true, "Compressing build...", 0.6f);
        var (compressSuccess, compressMessage, zipPath, uncompressedSize) = await GlcBuildManager.CompressBuildAsync(
            msg => SetProgress(true, msg, 0.8f));

        SetProgress(false);

        if (compressSuccess)
        {
            SetMessage($"Build compressed successfully! Size: {GlcMultipartUploadHelper.FormatFileSize(new System.IO.FileInfo(zipPath!).Length)}");
        }
        else
        {
            SetMessage($"Compression failed: {compressMessage}", true);
        }

        _isBuilding = false;
        SetButtonsEnabled(true);
        UpdateBuildStatus();
    }

    private async Task UploadOnlyAsync()
    {
        var buildInfo = GlcBuildManager.GetExistingBuild();
        if (buildInfo == null)
        {
            SetMessage("No build found. Please build first.", true);
            return;
        }

        if (!buildInfo.IsCompressed)
        {
            // Need to compress first
            SetProgress(true, "Compressing build...", 0.2f);
            var (compressSuccess, compressMessage, zipPath, uncompressedSize) = await GlcBuildManager.CompressBuildAsync(
                msg => SetProgress(true, msg, 0.4f));

            if (!compressSuccess || zipPath == null)
            {
                SetProgress(false);
                SetMessage($"Compression failed: {compressMessage}", true);
                return;
            }

            buildInfo = GlcBuildManager.GetExistingBuild();
        }

        await UploadBuildAsync(buildInfo!.Path, buildInfo.UncompressedSize);
    }

    private async Task UploadBuildAsync(string zipPath, long? uncompressedSize)
    {
        if (_availableApps == null || _appDropdown == null)
        {
            SetMessage("Please select an app first.", true);
            return;
        }

        var selectedIndex = _appDropdown.Selected;
        if (selectedIndex < 0 || selectedIndex >= _availableApps.Length)
        {
            SetMessage("Please select a valid app.", true);
            return;
        }

        var app = _availableApps[selectedIndex];
        var fileInfo = new System.IO.FileInfo(zipPath);
        var fileSize = fileInfo.Length;

        _isUploading = true;
        SetButtonsEnabled(false);

        // Get build notes
        var buildNotes = _buildNotesInput?.Text ?? "";
        if (string.IsNullOrWhiteSpace(buildNotes))
        {
            buildNotes = "Uploaded from Godot Extension";
        }

        // Save build notes
        _config.BuildNotes = buildNotes;
        GlcConfigManager.SaveConfig(_config);

        // Step 1: Start upload
        SetProgress(true, "Step 1/3: Requesting upload URL...", 0.1f);
        var (startSuccess, startMessage, uploadResponse) = await _apiClient.StartUploadAsync(
            app.Id, fileInfo.Name, fileSize, uncompressedSize, buildNotes);

        if (!startSuccess || uploadResponse == null)
        {
            SetProgress(false);
            SetMessage($"Failed to start upload: {startMessage}", true);
            SetButtonsEnabled(true);
            _isUploading = false;
            return;
        }

        // Step 2: Upload file
        SetProgress(true, "Step 2/3: Uploading...", 0.2f);

        bool uploadSuccess;
        string uploadMessage;
        List<GlcApiClient.PartETag>? parts = null;

        if (uploadResponse.PartUrls != null && uploadResponse.PartUrls.Count > 0)
        {
            // Multipart upload
            (uploadSuccess, uploadMessage, parts) = await _apiClient.UploadMultipartAsync(
                zipPath,
                uploadResponse.PartUrls,
                (progress, msg) => SetProgress(true, $"Step 2/3: {msg}", 0.2f + progress * 0.6f));
        }
        else
        {
            // Single part upload
            (uploadSuccess, uploadMessage) = await _apiClient.UploadFileAsync(
                uploadResponse.UploadUrl,
                zipPath,
                progress => SetProgress(true, $"Step 2/3: Uploading... {progress * 100:F0}%", 0.2f + progress * 0.6f));
        }

        if (!uploadSuccess)
        {
            SetProgress(false);
            SetMessage($"Upload failed: {uploadMessage}", true);
            SetButtonsEnabled(true);
            _isUploading = false;
            return;
        }

        // Step 3: Notify file ready
        SetProgress(true, "Step 3/3: Finalizing...", 0.9f);
        var (notifySuccess, notifyMessage) = await _apiClient.NotifyFileReadyAsync(
            uploadResponse.AppBuildId,
            uploadResponse.Key,
            uploadResponse.UploadId,
            parts);

        SetProgress(false);

        if (notifySuccess)
        {
            _completedBuildId = uploadResponse.AppBuildId;
            _completedBuildAppId = app.Id;

            SetMessage($"Build #{uploadResponse.AppBuildId} uploaded successfully!");

            if (_viewBuildButton != null)
            {
                _viewBuildButton.Visible = true;
                _viewBuildButton.Text = $"View Build #{uploadResponse.AppBuildId} in Dashboard";
            }
        }
        else
        {
            SetMessage($"Upload completed but finalization failed: {notifyMessage}", true);
        }

        SetButtonsEnabled(true);
        _isUploading = false;
        UpdateBuildStatus();
    }

    private bool ValidateBuildRequirements()
    {
        if (_availableApps == null || _availableApps.Length == 0)
        {
            SetMessage("Please load your apps first.", true);
            return false;
        }

        if (_appDropdown == null || _appDropdown.Selected < 0)
        {
            SetMessage("Please select an app.", true);
            return false;
        }

        if (_exportPresets.Count == 0)
        {
            SetMessage("No export presets found. Please configure export settings first.", true);
            return false;
        }

        return true;
    }

    #endregion

    #region Actions

    private void OnShowInExplorerPressed()
    {
        var buildInfo = GlcBuildManager.GetExistingBuild();
        if (buildInfo != null)
        {
            OS.ShellOpen(System.IO.Path.GetDirectoryName(buildInfo.Path) ?? buildInfo.Path);
        }
    }

    private void OnViewBuildPressed()
    {
        if (_completedBuildId > 0)
        {
            OS.ShellOpen($"{_config.GetFrontendUrl()}/apps/id/{_completedBuildAppId}/builds");
        }
    }

    private AcceptDialog CreateExportHintDialog()
    {
        var dialog = new AcceptDialog
        {
            Title = "Export Configuration",
            DialogText = "To configure export presets:\n\n1. Go to Project > Export in the menu\n2. Add and configure your export presets\n3. Click 'Refresh Presets' in the GLC Manager dock",
            Size = new Vector2I(400, 200)
        };
        return dialog;
    }

    #endregion
}
#endif
