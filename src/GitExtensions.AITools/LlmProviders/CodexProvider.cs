using System.Diagnostics;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class CodexProvider : CliProviderBase
{
    private readonly string? _model;

    public override string Name => "OpenAI Codex";

    protected override string[] CliCandidates => ["codex", "codex.cmd"];
    protected override string CliNotFoundMessage =>
        "Codex CLI not found on PATH. Install it with 'npm install -g @openai/codex' and sign in with 'codex login'.";

    public CodexProvider(string? model = null)
    {
        _model = string.IsNullOrWhiteSpace(model) ? null : model;
    }

    protected override void ConfigureArguments(ProcessStartInfo startInfo, string systemPrompt, string userPrompt)
    {
        startInfo.ArgumentList.Add("exec");
        startInfo.ArgumentList.Add("--ephemeral");
        startInfo.ArgumentList.Add("--skip-git-repo-check");

        if (_model is not null)
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(_model);
        }

        startInfo.ArgumentList.Add("-");
    }

    protected override string? BuildStdinContent(string systemPrompt, string userPrompt)
    {
        return systemPrompt + "\n\n" + userPrompt;
    }
}
