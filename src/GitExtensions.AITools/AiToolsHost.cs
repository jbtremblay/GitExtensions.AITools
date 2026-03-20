using GitExtensions.AITools.LlmProviders;
using GitExtensions.Extensibility.Settings;

namespace GitExtensions.AITools;

internal sealed class AiToolsHost
{
    public AiToolsHost(SettingsSource settings, Image? icon)
    {
        Settings = settings;
        Icon = icon;
    }

    public SettingsSource Settings { get; }
    public Image? Icon { get; }

    public required BoolSetting EnabledSetting { get; init; }
    public required ChoiceSetting ProviderSetting { get; init; }
    public required PasswordSetting ApiKeySetting { get; init; }
    public required StringSetting ModelSetting { get; init; }

    public ILlmProvider? CreateProvider(out string? configError)
    {
        string provider = ProviderSetting.ValueOrDefault(Settings) ?? LlmProviderFactory.Anthropic;
        string apiKey = ApiKeySetting.ValueOrDefault(Settings) ?? "";
        string model = ModelSetting.ValueOrDefault(Settings) ?? "";

        configError = null;

        if (string.IsNullOrWhiteSpace(apiKey)
            && provider != LlmProviderFactory.GitHubCopilot
            && provider != LlmProviderFactory.ClaudeCode
            && provider != LlmProviderFactory.OpenCode)
        {
            configError = "No API key configured. Open Plugins > AI Tools to configure.";
            return null;
        }

        try
        {
            return LlmProviderFactory.Create(provider, apiKey, model);
        }
        catch (InvalidOperationException ex)
        {
            configError = ex.Message;
            return null;
        }
    }
}
