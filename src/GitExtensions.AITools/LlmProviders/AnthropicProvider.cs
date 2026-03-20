using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class AnthropicProvider : ILlmProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string DefaultModel = "claude-sonnet-4-20250514";
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";

    private readonly string _apiKey;
    private readonly string _model;

    public string Name => "Anthropic (Claude)";

    public AnthropicProvider(string apiKey, string? model = null)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
    }

    public (string message, bool isReady) GetStatus(string apiKey)
    {
        return string.IsNullOrWhiteSpace(apiKey)
            ? ("API key required", false)
            : ("Ready", true);
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, ApiUrl);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");

        var body = new
        {
            model = _model,
            max_tokens = 1024,
            temperature = 0.3,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt }
            }
        };

        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        using HttpResponseMessage response = await HttpClient.SendAsync(request, cancellationToken);
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string truncated = responseBody.Length > 500 ? responseBody[..500] + "…" : responseBody;
            throw new HttpRequestException($"Anthropic API error ({response.StatusCode}): {truncated}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("content", out JsonElement content)
            || content.ValueKind != JsonValueKind.Array
            || content.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Anthropic API response missing valid 'content' array.");
        }

        if (content[0].TryGetProperty("text", out JsonElement text))
        {
            return text.GetString() ?? "";
        }

        throw new InvalidOperationException("Anthropic API response missing 'text' in content block.");
    }
}
