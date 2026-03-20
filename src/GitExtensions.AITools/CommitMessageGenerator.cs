using System.Text.RegularExpressions;
using GitExtensions.AITools.LlmProviders;
using GitExtensions.Extensibility.Git;

namespace GitExtensions.AITools;

internal sealed partial class CommitMessageGenerator
{
    public const string DefaultCommitTypes = "feat, fix, refactor, docs, test, chore, style, perf, ci, build";

    public const string DefaultSystemPrompt = """
        You are a commit message generator. Given a git diff, produce ONLY the commit message text — nothing else.
        Follow the Conventional Commits 1.0.0 specification.

        FORMAT:
        - Subject line: `<type>[(<scope>)][!]: <description>` — 72 chars max, imperative mood.
        - Allowed types: {types}
        - Scope: derive from the most relevant directory or module in the diff. Omit if the change is project-wide.
        - Breaking changes: append `!` before the `:` when the change introduces an incompatible API or behavior change. Optionally add a `BREAKING CHANGE: <details>` footer to explain.
        - Body: add a blank line + short body ONLY when the diff spans multiple files or the reason isn't obvious from the subject. Keep the body under 3 lines.
        - Footers: you may add Conventional Commits footers such as `BREAKING CHANGE`, `Fixes`, `Refs`, or `Closes` after the body (separated by a blank line). Do NOT add git trailers (Co-Authored-By, Signed-off-by, etc.).
        - If the branch name contains a ticket reference (e.g. PROJ-123), mention it in the body or as a `Refs` footer.

        EXAMPLES:

        ---
        fix(auth): handle expired tokens in refresh flow
        ---
        feat(api): add pagination to list endpoints

        Supports cursor-based pagination on /users and /orders.
        ---
        refactor: rename IdentityService to AuthService
        ---
        feat(api)!: remove deprecated /v1/users endpoint

        The /v1/users endpoint has been removed in favor of /v2/users.

        BREAKING CHANGE: clients using /v1/users must migrate to /v2/users
        ---

        HARD RULES:
        - Output ONLY the commit message. No markdown, no quotes, no code fences, no preamble, no explanation.
        - Do NOT wrap the message in quotes or backticks.
        - Do NOT add git trailers (Co-Authored-By, Signed-off-by, etc.).
        - Do NOT start with "Here is", "Sure", or any conversational text.
        """;

    private readonly ILlmProvider _provider;
    private readonly string _systemPrompt;

    public CommitMessageGenerator(ILlmProvider provider, string commitTypes, string? customInstructions)
    {
        _provider = provider;

        string basePrompt = DefaultSystemPrompt.Replace("{types}", commitTypes);

        _systemPrompt = string.IsNullOrWhiteSpace(customInstructions)
            ? basePrompt
            : $"{basePrompt}\n\nADDITIONAL INSTRUCTIONS:\n{customInstructions}";
    }

    public async Task<string> GenerateAsync(IGitModule module, CancellationToken cancellationToken)
    {
        string diff = await DiffCollector.GetStagedDiffAsync(module, cancellationToken);

        if (string.IsNullOrWhiteSpace(diff))
        {
            return "[No staged changes found. Stage some changes before generating a commit message.]";
        }

        string branch = module.GetSelectedBranch();
        string userPrompt = string.IsNullOrWhiteSpace(branch)
            ? $"Generate a commit message for the following changes:\n\n{diff}"
            : $"Branch: {branch}\n\nGenerate a commit message for the following changes:\n\n{diff}";

        string response = await _provider.GenerateAsync(_systemPrompt, userPrompt, cancellationToken);
        return CleanResponse(response);
    }

    internal static string CleanResponse(string response)
    {
        string result = response.Trim();

        if (result.Length == 0)
        {
            return result;
        }

        // Remove markdown code fences
        result = CodeFenceRegex().Replace(result, "").Trim();

        // Remove wrapping quotes (single or double)
        if (result.Length >= 2
            && ((result[0] == '"' && result[^1] == '"')
                || (result[0] == '\'' && result[^1] == '\'')))
        {
            result = result[1..^1].Trim();
        }

        // Remove leading preamble lines
        result = PreambleRegex().Replace(result, "").TrimStart();

        // Remove trailing trailers
        result = TrailerRegex().Replace(result, "").TrimEnd();

        return result;
    }

    [GeneratedRegex(@"^```\w*\s*\n?|```\s*$", RegexOptions.Multiline)]
    private static partial Regex CodeFenceRegex();

    [GeneratedRegex(@"^(here\s+is|here's|sure[,!.]?\s*)\s*.*?:\s*\n?", RegexOptions.IgnoreCase)]
    private static partial Regex PreambleRegex();

    [GeneratedRegex(@"\n(Co-Authored-By|Signed-off-by|Reviewed-by|Acked-by|Tested-by):.*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex TrailerRegex();
}
