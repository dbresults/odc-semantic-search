using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SemanticEngine.V2.Embeddings;

public sealed class EmbeddingClient
{
    private readonly HttpClient _http;
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly bool _isAzure;

    public EmbeddingClient(
        HttpClient httpClient,
        string endpoint,
        string apiKey,
        string model,
        bool isAzure)
    {
        _http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _model = model ?? throw new ArgumentNullException(nameof(model));
        _isAzure = isAzure;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input text is empty.", nameof(input));

        var url = BuildUrl();
        using var request = new HttpRequestMessage(HttpMethod.Post, url);

        if (_isAzure)
        {
            request.Headers.Add("api-key", _apiKey);
        }
        else
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        var payload = new { input = input, model = _model };
        request.Content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await _http.SendAsync(request).ConfigureAwait(false);

        // --- DEBUG: Catch the real error message from the server ---
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            throw new HttpRequestException(
                $"OpenAI API Error: {response.StatusCode} - {errorBody}. " +
                $"Attempted URL: {url}");
        }

        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

        var embedding = doc.RootElement
            .GetProperty("data")[0]
            .GetProperty("embedding");

        var result = new float[embedding.GetArrayLength()];
        int i = 0;
        foreach (var v in embedding.EnumerateArray())
        {
            result[i++] = v.GetSingle();
        }

        return result;
    }

    private string BuildUrl()
    {
        var baseEndpoint = _endpoint.Trim().TrimEnd('/');

        if (_isAzure)
        {
            baseEndpoint = RemoveTrailingSegment(baseEndpoint, "/openai");
            baseEndpoint = RemoveTrailingSegment(baseEndpoint, "/v1");
            const string apiVersion = "2024-10-21"; // stable GA; update when Azure releases a newer version
            return $"{baseEndpoint}/openai/deployments/{Uri.EscapeDataString(_model)}/embeddings?api-version={apiVersion}";
        }

        // Only append if it's a bare host
        if (baseEndpoint.Contains("/v1/embeddings")) return baseEndpoint;
        if (baseEndpoint.EndsWith("/v1")) return $"{baseEndpoint}/embeddings";
        
        return $"{baseEndpoint}/v1/embeddings";
    }

    private static string RemoveTrailingSegment(string value, string segment)
    {
        if (value.EndsWith(segment, StringComparison.OrdinalIgnoreCase))
            return value.Substring(0, value.Length - segment.Length).TrimEnd('/');
        return value;
    }
}