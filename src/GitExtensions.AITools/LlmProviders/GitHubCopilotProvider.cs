using System.Diagnostics;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class GitHubCopilotProvider : CliProviderBase
{
    private readonly string? _model;

    public override string Name => "GitHub Copilot";

    protected override string[] CliCandidates => ["copilot", "copilot.cmd"];
    protected override string CliNotFoundMessage =>
        "Copilot CLI not found on PATH. Install it from https://github.com/features/copilot/cli and ensure 'copilot' is available in your terminal.";

    public GitHubCopilotProvider(string? model = null)
    {
        _model = string.IsNullOrWhiteSpace(model) ? null : model;
    }

    protected override void ConfigureArguments(ProcessStartInfo startInfo, string systemPrompt, string userPrompt)
    {
        string fullPrompt = string.IsNullOrWhiteSpace(systemPrompt)
            ? userPrompt
            : systemPrompt + "\n\n" + userPrompt;

        startInfo.ArgumentList.Add("-p");
        startInfo.ArgumentList.Add(fullPrompt);
        startInfo.ArgumentList.Add("--silent");
        startInfo.ArgumentList.Add("--output-format");
        startInfo.ArgumentList.Add("text");
        startInfo.ArgumentList.Add("--no-custom-instructions");
        startInfo.ArgumentList.Add("--deny-tool");
        startInfo.ArgumentList.Add("shell");
        startInfo.ArgumentList.Add("--deny-tool");
        startInfo.ArgumentList.Add("write");
        startInfo.ArgumentList.Add("--deny-tool");
        startInfo.ArgumentList.Add("read");
        startInfo.ArgumentList.Add("--deny-tool");
        startInfo.ArgumentList.Add("url");
        startInfo.ArgumentList.Add("--deny-tool");
        startInfo.ArgumentList.Add("memory");

        if (_model is not null)
        {
            startInfo.ArgumentList.Add("--model");
            startInfo.ArgumentList.Add(_model);
        }
    }

    protected override string? BuildStdinContent(string systemPrompt, string userPrompt)
    {
        return null;
    }
}
