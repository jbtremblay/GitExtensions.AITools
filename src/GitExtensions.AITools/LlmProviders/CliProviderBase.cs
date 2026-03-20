using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace GitExtensions.AITools.LlmProviders;

internal abstract class CliProviderBase : ILlmProvider
{
    private static readonly ConcurrentDictionary<Type, string> _resolvedPaths = new();

    public abstract string Name { get; }

    protected abstract string[] CliCandidates { get; }
    protected abstract string CliNotFoundMessage { get; }
    protected abstract void ConfigureArguments(ProcessStartInfo startInfo, string systemPrompt, string userPrompt);
    protected abstract string? BuildStdinContent(string systemPrompt, string userPrompt);

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        string cliPath = DetectCli();

        using Process process = new();
        string? stdinContent = BuildStdinContent(systemPrompt, userPrompt);
        bool usesStdin = stdinContent is not null;

        process.StartInfo = new ProcessStartInfo
        {
            FileName = cliPath,
            RedirectStandardInput = usesStdin,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        if (usesStdin)
        {
            process.StartInfo.StandardInputEncoding = Encoding.UTF8;
        }

        ConfigureArguments(process.StartInfo, systemPrompt, userPrompt);

        process.Start();

        if (usesStdin)
        {
            await process.StandardInput.WriteAsync(stdinContent);
            process.StandardInput.Close();
        }

        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        });

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        string error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            string detail = string.IsNullOrWhiteSpace(error) ? output : error;
            throw new InvalidOperationException(
                $"{Name} CLI exited with code {process.ExitCode}: {detail.Trim()}");
        }

        string result = output.Trim();

        if (result.Length == 0)
        {
            throw new InvalidOperationException(
                $"{Name} CLI returned empty output. Check your configuration (e.g. model format).");
        }

        return result;
    }

    public virtual (string message, bool isReady) GetStatus(string apiKey)
    {
        try
        {
            DetectCli();
            return ("Ready", true);
        }
        catch
        {
            return (CliNotFoundMessage, false);
        }
    }

    private string DetectCli()
    {
        Type type = GetType();

        if (_resolvedPaths.TryGetValue(type, out string? cached))
        {
            return cached;
        }

        foreach (string candidate in CliCandidates)
        {
            try
            {
                using Process process = new();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = candidate,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                process.StartInfo.ArgumentList.Add("--version");
                process.Start();
                process.WaitForExit(5000);

                if (process.ExitCode == 0)
                {
                    _resolvedPaths[type] = candidate;
                    return candidate;
                }
            }
            catch
            {
                // Try next candidate
            }
        }

        throw new InvalidOperationException(CliNotFoundMessage);
    }
}
