namespace GitExtensions.AITools.LlmProviders;

internal static class LlmProviderFactory
{
    public const string GitHubCopilot = "GitHub Copilot";
    public const string ClaudeCode = "Claude Code";
    public const string Codex = "OpenAI Codex";
    public const string OpenCode = "OpenCode";
    public const string Anthropic = "Anthropic-compatible API";
    public const string OpenAI = "OpenAI-compatible API";

    public static readonly string[] ProviderNames = [GitHubCopilot, ClaudeCode, Codex, OpenCode, Anthropic, OpenAI];

    public static ILlmProvider Create(string providerName, string apiKey, string? model, string? baseUrl = null)
    {
        return providerName switch
        {
            Anthropic => new AnthropicProvider(apiKey, model, baseUrl),
            OpenAI => new OpenAiProvider(apiKey, model, baseUrl),
            GitHubCopilot => new GitHubCopilotProvider(model),
            ClaudeCode => new ClaudeCodeProvider(model),
            Codex => new CodexProvider(model),
            OpenCode => new OpenCodeProvider(model),
            _ => throw new InvalidOperationException(
                $"Unknown AI provider '{providerName}'. Open AI Tools settings and re-select a provider.")
        };
    }
}
