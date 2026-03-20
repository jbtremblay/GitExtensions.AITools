namespace GitExtensions.AITools.LlmProviders;

internal interface ILlmProvider
{
    string Name { get; }
    Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken);
    (string message, bool isReady) GetStatus(string apiKey);
}
