using System.Diagnostics;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class ClaudeCodeProvider : CliProviderBase
{
    private readonly string? _model;

    public override string Name => "Claude Code";

    protected override string[] CliCandidates => ["claude"];
    protected override string CliNotFoundMessage =>
        "Claude Code CLI not found on PATH. Install it from https://docs.anthropic.com/en/docs/claude-code and ensure 'claude' is available in your terminal.";

    public ClaudeCodeProvider(string? model = null)
    {
        _model = string.IsNullOrWhiteSpace(model) ? null : model;
    }

    protected override void ConfigureArguments(ProcessStartInfo startInfo, string systemPrompt, string userPrompt)
    {
        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");

        if (_model is not null)
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(_model);
        }

        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            startInfo.ArgumentList.Add("--append-system-prompt");
            startInfo.ArgumentList.Add(systemPrompt);
        }
    }

    protected override string BuildStdinContent(string systemPrompt, string userPrompt)
    {
        return userPrompt;
    }
}
