using GitCommands;
using GitExtensions.AITools.LlmProviders;
using GitExtensions.AITools.Properties;
using GitExtensions.Extensibility.Git;
using GitExtensions.Extensibility.Plugins;
using GitExtensions.Extensibility.Settings;
using GitExtensions.Extensibility.Translations;
using GitExtensions.Extensibility.Translations.Xliff;
using GitUIPluginInterfaces;
using System.ComponentModel.Composition;
using System.Diagnostics;

namespace GitExtensions.AITools;

[Export(typeof(IGitPlugin))]
[Export(typeof(IGitPluginForCommit))]
public class AiCommitMessagePlugin : GitPluginBase, IGitPluginForCommit
{
    private readonly BoolSetting _enabledSetting = new("AI commit message enabled", "Enabled", true);
    private readonly ChoiceSetting _providerSetting = new("AI provider", "Provider", LlmProviderFactory.ProviderNames, LlmProviderFactory.GitHubCopilot);
    private readonly PasswordSetting _apiKeySetting = new("AI API key", "API Key (optional for GitHub Copilot / Claude Code / OpenCode)", "");
    private readonly StringSetting _modelSetting = new("AI model override", "Model override (blank = provider default)", "");

    private readonly List<IAiFeature> _features = [];

    public AiCommitMessagePlugin() : base(true)
    {
        Id = new Guid("A1C0AA17-A1C0-4017-A1C0-A1C0AA17A1C0");
        Name = "AI Tools";
        Description = "AI-powered tools for Git Extensions (commit messages, and more)";
        Icon = Resources.Icon;
        Translate(AppSettings.CurrentTranslation);
        LoadPluginTranslations(AppSettings.CurrentTranslation);
    }

    private void LoadPluginTranslations(string translationName)
    {
        if (string.IsNullOrEmpty(translationName))
        {
            return;
        }

        string pluginDir = Path.GetDirectoryName(GetType().Assembly.Location)!;
        string xlfPath = Path.Combine(pluginDir, "Translation", $"{translationName}.xlf");

        TranslationFile? translation = TranslationSerializer.Deserialize(xlfPath);
        if (translation is not null)
        {
            TranslateItems(translation);

            foreach (IAiFeature feature in _features)
            {
                if (feature is ITranslate translatable)
                {
                    translatable.TranslateItems(translation);
                }
            }
        }
    }

    public override IEnumerable<ISetting> GetSettings()
    {
        Label statusLabel = new() { AutoSize = true };

        ComboBox providerCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList };
        providerCombo.Items.AddRange(LlmProviderFactory.ProviderNames.Cast<object>().ToArray());
        _providerSetting.CustomControl = providerCombo;

        TextBox apiKeyTextBox = new() { PasswordChar = '\u25CF' };
        _apiKeySetting.CustomControl = apiKeyTextBox;

        TextBox modelTextBox = new();
        _modelSetting.CustomControl = modelTextBox;

        providerCombo.SelectedIndexChanged += (_, _) => UpdateSettingsStatus(providerCombo, apiKeyTextBox, statusLabel, modelTextBox);
        apiKeyTextBox.TextChanged += (_, _) => UpdateSettingsStatus(providerCombo, apiKeyTextBox, statusLabel, modelTextBox);

        // LoadSetting populates controls after GetSettings returns —
        // trigger initial status check once the combo becomes visible.
        EventHandler? visibleHandler = null;
        visibleHandler = (_, _) =>
        {
            providerCombo.VisibleChanged -= visibleHandler;
            UpdateSettingsStatus(providerCombo, apiKeyTextBox, statusLabel, modelTextBox);
        };
        providerCombo.VisibleChanged += visibleHandler;

        List<ISetting> settings =
        [
            _enabledSetting,
            _providerSetting,
            new PseudoSetting(statusLabel, "Status"),
            _apiKeySetting,
            _modelSetting,
        ];

        foreach (IAiFeature feature in _features)
        {
            settings.AddRange(feature.GetSettings());
        }

        return settings;
    }

    private static void UpdateSettingsStatus(ComboBox providerCombo, TextBox apiKeyTextBox, Label statusLabel, TextBox modelTextBox)
    {
        string provider = "";
        string apiKey = "";

        try
        {
            if (statusLabel.IsDisposed)
            {
                return;
            }

            if (providerCombo.InvokeRequired)
            {
                providerCombo.Invoke(() => provider = providerCombo.SelectedItem?.ToString() ?? "");
            }
            else
            {
                provider = providerCombo.SelectedItem?.ToString() ?? "";
            }

            if (apiKeyTextBox.InvokeRequired)
            {
                apiKeyTextBox.Invoke(() => apiKey = apiKeyTextBox.Text);
            }
            else
            {
                apiKey = apiKeyTextBox.Text;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            return;
        }

        modelTextBox.PlaceholderText = provider switch
        {
            LlmProviderFactory.GitHubCopilot => "e.g. claude-haiku-4.5",
            LlmProviderFactory.ClaudeCode => "e.g. sonnet",
            LlmProviderFactory.OpenCode => "e.g. opencode/big-pickle",
            _ => "",
        };

        Task.Run(() => GetProviderStatus(provider, apiKey)).ContinueWith(
            task =>
            {
                if (task.IsCompletedSuccessfully)
                {
                    SetStatusLabel(statusLabel, task.Result.message, task.Result.isReady);
                }
            },
            TaskScheduler.Default);
    }

    private static (string message, bool isReady) GetProviderStatus(string provider, string apiKey)
    {
        try
        {
            ILlmProvider llmProvider = LlmProviderFactory.Create(provider, apiKey, model: null);
            return llmProvider.GetStatus(apiKey);
        }
        catch
        {
            return ("Unknown provider", false);
        }
    }

    private static void SetStatusLabel(Label label, string text, bool isReady)
    {
        if (label.IsDisposed)
        {
            return;
        }

        try
        {
            if (label.InvokeRequired)
            {
                label.Invoke(() =>
                {
                    label.Text = text;
                    label.ForeColor = isReady ? Color.Green : Color.Red;
                    label.Refresh();
                });
            }
            else
            {
                label.Text = text;
                label.ForeColor = isReady ? Color.Green : Color.Red;
                label.Refresh();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
        }
    }

    public override void Register(IGitUICommands gitUiCommands)
    {
        base.Register(gitUiCommands);

        AiToolsHost host = new(Settings, Icon)
        {
            EnabledSetting = _enabledSetting,
            ProviderSetting = _providerSetting,
            ApiKeySetting = _apiKeySetting,
            ModelSetting = _modelSetting,
        };

        CommitMessageFeature commitFeature = new(host);
        _features.Add(commitFeature);

        foreach (IAiFeature feature in _features)
        {
            feature.Register(gitUiCommands);
        }
    }

    public override void Unregister(IGitUICommands gitUiCommands)
    {
        foreach (IAiFeature feature in _features)
        {
            feature.Unregister(gitUiCommands);
        }

        _features.Clear();

        base.Unregister(gitUiCommands);
    }

    public override bool Execute(GitUIEventArgs args)
    {
        args.GitUICommands.StartSettingsDialog(this);
        return false;
    }
}
