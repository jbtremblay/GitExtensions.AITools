using System.Diagnostics;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class OpenCodeProvider : CliProviderBase
{
    private readonly string? _model;

    public override string Name => "OpenCode";

    protected override string[] CliCandidates => ["opencode", "opencode.cmd"];
    protected override string CliNotFoundMessage =>
        "OpenCode CLI not found on PATH. Install it from https://github.com/opencode-ai/opencode and ensure 'opencode' is available in your terminal.";

    public override (string message, bool isReady) GetStatus(string apiKey)
    {
        (string message, bool isReady) = base.GetStatus(apiKey);

        return isReady
            ? ("Ready", true)
            : (message, false);
    }

    public OpenCodeProvider(string? model = null)
    {
        _model = string.IsNullOrWhiteSpace(model) ? null : model;
    }

    protected override void ConfigureArguments(ProcessStartInfo startInfo, string systemPrompt, string userPrompt)
    {
        startInfo.ArgumentList.Add("run");

        if (_model is not null)
        {
            startInfo.ArgumentList.Add("-m");
            startInfo.ArgumentList.Add(_model);
        }
    }

    protected override string BuildStdinContent(string systemPrompt, string userPrompt)
    {
        return string.IsNullOrWhiteSpace(systemPrompt)
            ? userPrompt
            : systemPrompt + "\n\n" + userPrompt;
    }
}
