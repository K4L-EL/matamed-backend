using System.Text;
using System.Text.Json;

namespace Nex.Api.Services;

public record OpenAIImageResult(byte[] Data, string ContentType);
public record OpenAIChatResult(string Content, long TotalTokens, string? FinishReason);
public record OpenAIResponsesResult(string ResponseBody, bool IsSuccess, int StatusCode);

public interface IOpenAIDirectClient
{
    bool IsConfigured { get; }
    Task<OpenAIChatResult> ChatCompletionAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        double temperature = 0.4,
        int maxTokens = 8192,
        bool useMaxCompletionTokens = false,
        TimeSpan? timeout = null);

    Task<OpenAIResponsesResult> ResponsesAsync(
        string model,
        string instructions,
        string userPrompt,
        bool enableWebSearch,
        double temperature = 0.5,
        TimeSpan? timeout = null);

    Task<OpenAIImageResult> GenerateImageAsync(
        string prompt,
        string size = "1792x1024",
        string quality = "hd");
}

public class OpenAIDirectClient : IOpenAIDirectClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIDirectClient> _logger;

    private readonly string? _azureEndpoint;
    private readonly string? _azureApiKey;
    private readonly string? _azureDeploymentName;
    private readonly string? _directApiKey;
    private const int MaxRetries = 2;

    public OpenAIDirectClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OpenAIDirectClient> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _azureEndpoint = configuration["AZURE_OPENAI_ENDPOINT"] ?? configuration["AzureOpenAI:Endpoint"];
        _azureApiKey = configuration["AZURE_OPENAI_KEY"] ?? configuration["AzureOpenAI:ApiKey"];
        _azureDeploymentName = configuration["AZURE_OPENAI_DEPLOYMENT"] ?? configuration["AzureOpenAI:Deployment"] ?? "gpt-4o";
        _directApiKey = configuration["OPENAI_API_KEY"] ?? configuration["OpenAI:ApiKey"];
    }

    private bool UseAzure => !string.IsNullOrEmpty(_azureEndpoint) && !string.IsNullOrEmpty(_azureApiKey);
    public bool IsConfigured => UseAzure || !string.IsNullOrEmpty(_directApiKey);

    public async Task<OpenAIChatResult> ChatCompletionAsync(
        string model,
        string systemPrompt,
        string userPrompt,
        double temperature = 0.4,
        int maxTokens = 8192,
        bool useMaxCompletionTokens = false,
        TimeSpan? timeout = null)
    {
        if (!IsConfigured) throw new InvalidOperationException("OpenAI is not configured");

        var requestDict = new Dictionary<string, object>
        {
            ["messages"] = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            ["temperature"] = temperature
        };

        if (useMaxCompletionTokens)
            requestDict["max_completion_tokens"] = maxTokens;
        else
            requestDict["max_tokens"] = maxTokens;

        string url;
        Action<HttpRequestMessage> setAuth;

        if (UseAzure)
        {
            var baseUrl = _azureEndpoint!.TrimEnd('/');
            var deployment = _azureDeploymentName ?? "gpt-4o";
            url = $"{baseUrl}/openai/deployments/{deployment}/chat/completions?api-version=2024-08-01-preview";
            setAuth = req => req.Headers.Add("api-key", _azureApiKey!);
        }
        else
        {
            url = "https://api.openai.com/v1/chat/completions";
            requestDict["model"] = model;
            setAuth = req => req.Headers.Add("Authorization", $"Bearer {_directApiKey}");
        }

        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(4);
        var jsonPayload = JsonSerializer.Serialize(requestDict);

        HttpResponseMessage response = null!;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            setAuth(request);
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(effectiveTimeout);
            try
            {
                response = await _httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode) break;
                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
                        continue;
                    }
                }
                var errBody = await response.Content.ReadAsStringAsync();
                _logger.LogError("OpenAI chat error: {Status} {Body}", response.StatusCode, errBody);
                throw new Exception($"OpenAI chat error: {response.StatusCode}");
            }
            catch (Exception ex) when (attempt < MaxRetries && (ex is TaskCanceledException or HttpRequestException))
            {
                _logger.LogWarning(ex, "Chat API transient error, retrying");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
            }
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);
        var choice = jsonDoc.RootElement.GetProperty("choices")[0];
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? "";
        var finishReason = choice.TryGetProperty("finish_reason", out var fr) ? fr.GetString() : null;
        long totalTokens = 0;
        if (jsonDoc.RootElement.TryGetProperty("usage", out var usageEl) &&
            usageEl.TryGetProperty("total_tokens", out var tt))
            totalTokens = tt.GetInt64();
        return new OpenAIChatResult(content, totalTokens, finishReason);
    }

    public async Task<OpenAIResponsesResult> ResponsesAsync(
        string model,
        string instructions,
        string userPrompt,
        bool enableWebSearch,
        double temperature = 0.5,
        TimeSpan? timeout = null)
    {
        // The Responses API + web_search_preview tool requires the standard OpenAI API
        // (Azure does not yet support this surface). Fall back to direct OpenAI when configured.
        if (string.IsNullOrEmpty(_directApiKey))
        {
            // Without direct OpenAI access, we cannot do web search. Use chat completion as fallback.
            var chat = await ChatCompletionAsync(model, instructions, userPrompt, temperature, maxTokens: 8192, timeout: timeout);
            return new OpenAIResponsesResult(
                JsonSerializer.Serialize(new
                {
                    output = new[]
                    {
                        new
                        {
                            type = "message",
                            content = new[] { new { type = "output_text", text = chat.Content } }
                        }
                    }
                }),
                true, 200);
        }

        var url = "https://api.openai.com/v1/responses";
        var requestDict = new Dictionary<string, object>
        {
            ["model"] = model,
            ["instructions"] = instructions,
            ["input"] = userPrompt,
            ["temperature"] = temperature,
        };
        if (enableWebSearch)
        {
            requestDict["tools"] = new[] { new { type = "web_search_preview" } };
        }

        var effectiveTimeout = timeout ?? TimeSpan.FromMinutes(5);
        var jsonPayload = JsonSerializer.Serialize(requestDict);

        HttpResponseMessage response = null!;
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("Authorization", $"Bearer {_directApiKey}");
            request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using var cts = new CancellationTokenSource(effectiveTimeout);
            try
            {
                response = await _httpClient.SendAsync(request, cts.Token);
                if (response.IsSuccessStatusCode) break;
                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    if (attempt < MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
                        continue;
                    }
                }
                break;
            }
            catch (Exception ex) when (attempt < MaxRetries)
            {
                _logger.LogWarning(ex, "Responses API transient error, retrying");
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt + 1)));
            }
        }

        var body = await response.Content.ReadAsStringAsync();
        return new OpenAIResponsesResult(body, response.IsSuccessStatusCode, (int)response.StatusCode);
    }

    public async Task<OpenAIImageResult> GenerateImageAsync(
        string prompt,
        string size = "1792x1024",
        string quality = "hd")
    {
        if (string.IsNullOrEmpty(_directApiKey))
            throw new InvalidOperationException("OPENAI_API_KEY is required for DALL-E image generation");

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = "dall-e-3",
            ["prompt"] = prompt,
            ["n"] = 1,
            ["size"] = size,
            ["quality"] = quality,
            ["response_format"] = "url",
        };

        var jsonPayload = JsonSerializer.Serialize(requestBody);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/images/generations");
        request.Headers.Add("Authorization", $"Bearer {_directApiKey}");
        request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var response = await _httpClient.SendAsync(request, cts.Token);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("DALL-E API error: {Status} - {Body}", response.StatusCode, errorBody);
            throw new Exception($"DALL-E image generation failed: {response.StatusCode}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var jsonDoc = JsonDocument.Parse(responseContent);
        var imageUrl = jsonDoc.RootElement.GetProperty("data")[0].GetProperty("url").GetString()
            ?? throw new Exception("No image URL in DALL-E response");

        using var imageResponse = await _httpClient.GetAsync(imageUrl, cts.Token);
        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
        var contentType = imageResponse.Content.Headers.ContentType?.MediaType ?? "image/png";

        return new OpenAIImageResult(imageBytes, contentType);
    }
}
