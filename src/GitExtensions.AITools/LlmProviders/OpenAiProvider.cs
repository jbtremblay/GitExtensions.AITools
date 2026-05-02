using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace GitExtensions.AITools.LlmProviders;

internal sealed class OpenAiProvider : ILlmProvider
{
    private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
    private const string DefaultModel = "gpt-4o-mini";
    private const string DefaultApiUrl = "https://api.openai.com/v1/chat/completions";
    private const string ChatCompletionsPath = "/chat/completions";

    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _apiUrl;

    public string Name => "OpenAI-compatible";

    public OpenAiProvider(string apiKey, string? model = null, string? baseUrl = null)
    {
        _apiKey = apiKey;
        _model = string.IsNullOrWhiteSpace(model) ? DefaultModel : model;
        _apiUrl = ResolveApiUrl(baseUrl);
    }

    private static string ResolveApiUrl(string? baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return DefaultApiUrl;
        }

        string trimmed = baseUrl.TrimEnd('/');
        return trimmed.EndsWith(ChatCompletionsPath, StringComparison.OrdinalIgnoreCase)
            ? trimmed
            : trimmed + ChatCompletionsPath;
    }

    public (string message, bool isReady) GetStatus(string apiKey)
    {
        return string.IsNullOrWhiteSpace(apiKey)
            ? ("API key required", false)
            : ("Ready", true);
    }

    public async Task<string> GenerateAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        using HttpRequestMessage request = new(HttpMethod.Post, _apiUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var body = new
        {
            model = _model,
            max_tokens = 1024,
            temperature = 0.3,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
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
            throw new HttpRequestException($"OpenAI-compatible API error ({response.StatusCode}): {truncated}");
        }

        using JsonDocument doc = JsonDocument.Parse(responseBody);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("choices", out JsonElement choices)
            || choices.ValueKind != JsonValueKind.Array
            || choices.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenAI-compatible API response missing valid 'choices' array.");
        }

        if (choices[0].TryGetProperty("message", out JsonElement message)
            && message.TryGetProperty("content", out JsonElement messageContent))
        {
            return messageContent.GetString() ?? "";
        }

        throw new InvalidOperationException("OpenAI-compatible API response missing 'message.content' in first choice.");
    }
}
