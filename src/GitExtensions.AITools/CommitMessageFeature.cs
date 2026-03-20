using GitExtensions.AITools.LlmProviders;
using GitExtensions.Extensibility.Git;
using System.Diagnostics;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using ResourceManager;
using System.Reflection;

namespace GitExtensions.AITools;

internal sealed class CommitMessageFeature : IAiFeature, ITranslate
{
    private const string TemplateKey = "AI: Generate commit message";
    private const string LegacyTemplateKey = "AI commit template";

    private readonly AiToolsHost _host;
    private readonly BoolSetting _autoFillSetting = new("AI commit message auto-fill", "Auto-fill on stage/unstage", true);
    private readonly StringSetting _commitTypesSetting = new("AI commit types", "Commit types (comma-separated)", CommitMessageGenerator.DefaultCommitTypes, true);
    private readonly MultilineStringSetting _customInstructionsSetting = new("AI custom instructions", "Custom instructions (appended to built-in prompt)", "");

    private readonly TranslationString _triggerText = new("AI: Generate commit message...");
    private readonly TranslationString _noApiKeyMessage = new("[AI Tools: No API key configured. Open Plugins > AI Tools to configure.]");
    private readonly TranslationString _generatingMessage = new("Generating AI commit message...");
    private readonly TranslationString _cancelledMessage = new("[AI Commit Message: Generation was cancelled.]");
    private readonly TranslationString _errorMessage = new("[AI Commit Message Error: {0}]");

    private CancellationTokenSource? _cancellationTokenSource;
    private Task<string>? _pendingGeneration;
    private ILlmProvider? _currentProvider;
    private string? _currentCustomInstructions;
    private string? _currentCommitTypes;
    private IGitUICommands? _currentGitUiCommands;
    private Control? _messageControl;
    private bool _listeningToTextChanged;
    private Button? _commitButton;
    private Button? _commitAndPushButton;
    private FileSystemWatcher? _indexWatcher;
    private System.Threading.Timer? _debounceTimer;
    private long _watcherStartTicks;
    private string? _configError;
    private readonly object _stateLock = new();
    private bool _isGenerating;
    private bool _regenerateRequested;
    private bool _buttonsDisabled;
    private IGitModule? _currentModule;

    public CommitMessageFeature(AiToolsHost host)
    {
        _host = host;
    }

    public IEnumerable<ISetting> GetSettings()
    {
        return [_autoFillSetting, _commitTypesSetting, _customInstructionsSetting];
    }

    public void Register(IGitUICommands gitUiCommands)
    {
        MigrateEmptySetting(_commitTypesSetting, _host.Settings);

        gitUiCommands.RemoveCommitTemplate(LegacyTemplateKey);

        gitUiCommands.PreCommit += OnPreCommit;
        gitUiCommands.PostCommit += OnPostCommit;
    }

    public void Unregister(IGitUICommands gitUiCommands)
    {
        StopIndexWatcher();
        CancelPendingWork();
        gitUiCommands.PreCommit -= OnPreCommit;
        gitUiCommands.PostCommit -= OnPostCommit;
    }

    public void Translate()
    {
        // Handled by the plugin's translation infrastructure
    }

    public void AddTranslationItems(ITranslation translation)
    {
        TranslationUtils.AddTranslationItemsFromFields("AiCommitMessagePlugin", this, translation);
    }

    public void TranslateItems(ITranslation translation)
    {
        TranslationUtils.TranslateItemsFromFields("AiCommitMessagePlugin", this, translation);
    }

    public void Dispose()
    {
        // Cleanup handled by Unregister
    }

    private void OnPreCommit(object? sender, GitUIEventArgs e)
    {
        if (!_host.EnabledSetting.ValueOrDefault(_host.Settings))
        {
            return;
        }

        string commitPrefixes = _commitTypesSetting.ValueOrDefault(_host.Settings);
        string customInstructions = _customInstructionsSetting.ValueOrDefault(_host.Settings);

        _configError = null;
        _currentProvider = _host.CreateProvider(out string? configError);

        if (_currentProvider is null)
        {
            _configError = configError is not null
                ? $"[AI Tools] {configError}"
                : _noApiKeyMessage.Text;
        }

        _currentCustomInstructions = customInstructions;
        _currentCommitTypes = commitPrefixes;
        _currentGitUiCommands = e.GitUICommands;
        _messageControl = null;

        if (_autoFillSetting.ValueOrDefault(_host.Settings))
        {
            StartIndexWatcher(e.GitUICommands.Module);
        }

        if (_configError is not null)
        {
            e.GitUICommands.AddCommitTemplate(TemplateKey, () => _configError, _host.Icon);
        }
        else
        {
            e.GitUICommands.AddCommitTemplate(TemplateKey, () => GetGeneratedMessage(), _host.Icon);
        }
    }

    private void OnPostCommit(object? sender, GitUIPostActionEventArgs e)
    {
        CleanUp(e.GitUICommands);
    }

    private void CleanUp(IGitUICommands gitUiCommands)
    {
        StopIndexWatcher();
        CancelPendingWork();
        _currentModule = null;
        UnhookTextChanged();
        SetCommitButtonsEnabled(true);
        _configError = null;
        _currentProvider = null;
        _currentCustomInstructions = null;
        _currentCommitTypes = null;
        _currentGitUiCommands = null;
        _messageControl = null;
        _commitButton = null;
        _commitAndPushButton = null;
        gitUiCommands.RemoveCommitTemplate(TemplateKey);
    }

    private string GetGeneratedMessage()
    {
        EnsureTextChangedHooked();
        return _triggerText.Text;
    }

    private void EnsureTextChangedHooked()
    {
        if (_listeningToTextChanged)
        {
            return;
        }

        _messageControl ??= FindMessageControl();
        if (_messageControl is null)
        {
            return;
        }

        _messageControl.TextChanged += OnMessageTextChanged;
        _listeningToTextChanged = true;
    }

    private void UnhookTextChanged()
    {
        if (_listeningToTextChanged && _messageControl is not null && !_messageControl.IsDisposed)
        {
            _messageControl.TextChanged -= OnMessageTextChanged;
        }

        _listeningToTextChanged = false;
    }

    private void OnMessageTextChanged(object? sender, EventArgs e)
    {
        if (sender is not Control control || control.Text != _triggerText.Text)
        {
            return;
        }

        if (_currentProvider is not null && _currentGitUiCommands is not null)
        {
            StartGeneration(_currentGitUiCommands.Module, autoFill: true);
        }
    }

    private void StartGeneration(IGitModule module, bool autoFill)
    {
        lock (_stateLock)
        {
            _currentModule = module;

            if (_isGenerating)
            {
                _regenerateRequested = true;
                try { _cancellationTokenSource?.Cancel(); } catch { }
                return;
            }

            _regenerateRequested = false;
            _isGenerating = true;
        }

        // Cancel old work outside lock (CTS.Cancel may invoke callbacks)
        CancellationTokenSource? oldCts = Interlocked.Exchange(ref _cancellationTokenSource, null);
        _pendingGeneration = null;
        if (oldCts is not null)
        {
            try { oldCts.Cancel(); } finally { oldCts.Dispose(); }
        }

        BeginGeneration(module, autoFill);
    }

    private void BeginGeneration(IGitModule module, bool autoFill)
    {
        CancellationTokenSource cts = new();
        _cancellationTokenSource = cts;
        CancellationToken ct = cts.Token;

        if (autoFill && !_buttonsDisabled)
        {
            _messageControl ??= FindMessageControl();
            SetCommitMessage(_messageControl, _generatingMessage.Text);

            _commitButton ??= FindButton("Commit");
            _commitAndPushButton ??= FindButton("CommitAndPush");
            _buttonsDisabled = true;
            SetCommitButtonsEnabled(false);
        }

        CommitMessageGenerator generator = new(
            _currentProvider!,
            _currentCommitTypes ?? CommitMessageGenerator.DefaultCommitTypes,
            _currentCustomInstructions);

        _pendingGeneration = Task.Run(() => GenerateSafeAsync(generator, module, ct), ct);

        if (autoFill)
        {
            _pendingGeneration.ContinueWith(
                task =>
                {
                    IGitModule? moduleForRegen = null;
                    lock (_stateLock)
                    {
                        _isGenerating = false;

                        if (_regenerateRequested && _currentModule is not null && _currentProvider is not null)
                        {
                            moduleForRegen = _currentModule;
                            _regenerateRequested = false;
                            _isGenerating = true;
                        }
                    }

                    if (moduleForRegen is not null)
                    {
                        BeginGeneration(moduleForRegen, autoFill: true);
                        return;
                    }

                    if (task.IsCompletedSuccessfully)
                    {
                        string message = task.Result;
                        _messageControl ??= FindMessageControl();
                        SetCommitMessage(_messageControl, string.IsNullOrEmpty(message) ? "" : message);
                    }

                    if (_buttonsDisabled)
                    {
                        _buttonsDisabled = false;
                        SetCommitButtonsEnabled(true);
                    }
                },
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);
        }
    }

    private void StartIndexWatcher(IGitModule module)
    {
        StopIndexWatcher();

        try
        {
            string gitDir = module.WorkingDirGitDir.TrimEnd('\\', '/');
            _watcherStartTicks = Environment.TickCount64;
            _indexWatcher = new FileSystemWatcher(gitDir, "index*")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true,
            };

            _debounceTimer = new System.Threading.Timer(OnDebounceTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

            _indexWatcher.Changed += OnGitIndexChanged;
            _indexWatcher.Created += OnGitIndexChanged;
            _indexWatcher.Renamed += OnGitIndexRenamed;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }

    private void StopIndexWatcher()
    {
        Interlocked.Exchange(ref _debounceTimer, null)?.Dispose();

        if (_indexWatcher is not null)
        {
            _indexWatcher.EnableRaisingEvents = false;
            _indexWatcher.Changed -= OnGitIndexChanged;
            _indexWatcher.Created -= OnGitIndexChanged;
            _indexWatcher.Renamed -= OnGitIndexRenamed;
            _indexWatcher.Dispose();
            _indexWatcher = null;
        }
    }

    private void OnGitIndexRenamed(object sender, RenamedEventArgs e)
    {
        if (string.Equals(e.Name, "index", StringComparison.OrdinalIgnoreCase))
        {
            OnGitIndexChanged(sender, e);
        }
    }

    private void OnGitIndexChanged(object sender, FileSystemEventArgs e)
    {
        if (!string.Equals(e.Name, "index", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Ignore index events during the first 3 seconds after watcher start —
        // GE touches the index during dialog initialization.
        if (Environment.TickCount64 - Interlocked.Read(ref _watcherStartTicks) < 3000)
        {
            return;
        }

        System.Threading.Timer? timer = _debounceTimer;
        timer?.Change(1500, Timeout.Infinite);
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        try { OnGitIndexChangedCore(); }
        catch (Exception ex) { Debug.WriteLine(ex.Message); }
    }

    private void OnGitIndexChangedCore()
    {
        if (_currentGitUiCommands is null)
        {
            return;
        }

        if (_currentProvider is null)
        {
            if (_configError is not null)
            {
                _messageControl ??= FindMessageControl();
                SetCommitMessage(_messageControl, _configError);
            }

            return;
        }

        StartGeneration(_currentGitUiCommands.Module, autoFill: true);
    }

    private static Control? FindMessageControl()
    {
        try
        {
            Form? formCommit = Application.OpenForms
                .Cast<Form>()
                .FirstOrDefault(f => f.GetType().Name == "FormCommit");

            if (formCommit is null)
            {
                return null;
            }

            FieldInfo? messageField = formCommit.GetType()
                .GetField("Message", BindingFlags.Instance | BindingFlags.NonPublic);

            return messageField?.GetValue(formCommit) as Control;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private static Button? FindButton(string fieldName)
    {
        try
        {
            Form? formCommit = Application.OpenForms
                .Cast<Form>()
                .FirstOrDefault(f => f.GetType().Name == "FormCommit");

            if (formCommit is null)
            {
                return null;
            }

            FieldInfo? field = formCommit.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            return field?.GetValue(formCommit) as Button;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            return null;
        }
    }

    private void SetCommitButtonsEnabled(bool enabled)
    {
        SetButtonEnabled(_commitButton, enabled);
        SetButtonEnabled(_commitAndPushButton, enabled);
    }

    private static void SetButtonEnabled(Button? button, bool enabled)
    {
        if (button is null || button.IsDisposed)
        {
            return;
        }

        try
        {
            if (button.InvokeRequired)
            {
                button.BeginInvoke(() => button.Enabled = enabled);
            }
            else
            {
                button.Enabled = enabled;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private static void SetCommitMessage(Control? messageControl, string message)
    {
        if (messageControl is null || messageControl.IsDisposed)
        {
            return;
        }

        try
        {
            if (messageControl.InvokeRequired)
            {
                messageControl.BeginInvoke(() => messageControl.Text = message);
            }
            else
            {
                messageControl.Text = message;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    private async Task<string> GenerateSafeAsync(
        CommitMessageGenerator generator, IGitModule module, CancellationToken cancellationToken)
    {
        try
        {
            return await generator.GenerateAsync(module, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return _cancelledMessage.Text;
        }
        catch (Exception ex)
        {
            return string.Format(_errorMessage.Text, ex.Message);
        }
    }

    private void CancelPendingWork()
    {
        lock (_stateLock)
        {
            _isGenerating = false;
            _regenerateRequested = false;
        }

        CancellationTokenSource? old = Interlocked.Exchange(ref _cancellationTokenSource, null);
        _pendingGeneration = null;
        if (old is not null)
        {
            try { old.Cancel(); } finally { old.Dispose(); }
        }
    }

    private static void MigrateEmptySetting(StringSetting setting, SettingsSource settings)
    {
        if (string.IsNullOrEmpty(setting[settings]))
        {
            setting[settings] = null;
        }
    }
}
