using System.Text;
using GitExtensions.Extensibility;
using GitExtensions.Extensibility.Git;

namespace GitExtensions.AITools;

internal static class DiffCollector
{
    private const string TruncationMessage = "\n[Diff truncated due to length]";

    public static async Task<string> GetStagedDiffAsync(IGitModule module, CancellationToken cancellationToken, int maxLength = 8000)
    {
        Task<string> statTask = RunGitAsync(module, "diff --cached --stat", cancellationToken);
        Task<string> diffTask = RunGitAsync(module, "diff --cached", cancellationToken);
        await Task.WhenAll(statTask, diffTask);

        string stat = statTask.Result;

        if (string.IsNullOrWhiteSpace(stat))
        {
            return "";
        }

        string diff = diffTask.Result;

        if (string.IsNullOrWhiteSpace(diff))
        {
            return stat;
        }

        // Always keep the full stat summary; only truncate the raw diff portion
        int diffBudget = maxLength - stat.Length - 1 - TruncationMessage.Length;

        if (diffBudget <= 0)
        {
            return stat + "\n" + TruncationMessage;
        }

        if (diff.Length > diffBudget)
        {
            diff = diff[..diffBudget] + TruncationMessage;
        }

        return $"{stat}\n{diff}";
    }

    private static async Task<string> RunGitAsync(IGitModule module, string arguments, CancellationToken cancellationToken)
    {
        using IProcess process = module.GitExecutable.Start(
            arguments,
            redirectOutput: true,
            outputEncoding: Encoding.UTF8,
            throwOnErrorExit: false,
            cancellationToken: cancellationToken);

        string output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        return output;
    }
}
