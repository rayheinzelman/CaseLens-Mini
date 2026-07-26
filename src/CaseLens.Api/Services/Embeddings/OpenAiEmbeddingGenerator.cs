using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CaseLens.Api.Services.Embeddings;

public sealed class OpenAiEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiEmbeddingOptions _options;

    public OpenAiEmbeddingGenerator(
        HttpClient httpClient,
        IOptions<OpenAiEmbeddingOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public int Dimensions => _options.Dimensions;

    public async Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        ValidateConfiguration();

        if (inputs.Count == 0)
        {
            return Array.Empty<float[]>();
        }

        if (inputs.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException(
                "Embedding inputs cannot be empty or whitespace.",
                nameof(inputs));
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "embeddings");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _options.ApiKey);

        request.Content = JsonContent.Create(
            new EmbeddingRequest(
                inputs,
                _options.Model,
                _options.Dimensions,
                "float"));

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                "OpenAI embedding generation failed with HTTP " +
                $"{(int)response.StatusCode}: {responseBody}");
        }

        var embeddingResponse =
            JsonSerializer.Deserialize<EmbeddingResponse>(responseBody)
            ?? throw new InvalidOperationException(
                "OpenAI returned an empty embedding response.");

        if (embeddingResponse.Data.Count != inputs.Count)
        {
            throw new InvalidOperationException(
                "OpenAI returned a different number of embeddings " +
                "than requested.");
        }

        var embeddings = new float[inputs.Count][];

        foreach (var item in embeddingResponse.Data)
        {
            if (item.Index < 0 ||
                item.Index >= embeddings.Length ||
                embeddings[item.Index] is not null)
            {
                throw new InvalidOperationException(
                    "OpenAI returned an invalid embedding index.");
            }

            if (item.Embedding.Length != Dimensions)
            {
                throw new InvalidOperationException(
                    $"OpenAI returned {item.Embedding.Length} dimensions " +
                    $"but {Dimensions} were configured.");
            }

            embeddings[item.Index] = item.Embedding;
        }

        if (embeddings.Any(embedding => embedding is null))
        {
            throw new InvalidOperationException(
                "OpenAI did not return an embedding for every input.");
        }

        return embeddings;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenAI embedding API key was not configured. Set " +
                "'OpenAI:Embeddings:ApiKey' with user secrets.");
        }

        if (string.IsNullOrWhiteSpace(_options.Model))
        {
            throw new InvalidOperationException(
                "OpenAI embedding model was not configured.");
        }

        if (_options.Dimensions <= 0)
        {
            throw new InvalidOperationException(
                "OpenAI embedding dimensions must be greater than zero.");
        }
    }

    private sealed record EmbeddingRequest(
        [property: JsonPropertyName("input")]
        IReadOnlyList<string> Input,

        [property: JsonPropertyName("model")]
        string Model,

        [property: JsonPropertyName("dimensions")]
        int Dimensions,

        [property: JsonPropertyName("encoding_format")]
        string EncodingFormat);

    private sealed record EmbeddingResponse(
        [property: JsonPropertyName("data")]
        IReadOnlyList<EmbeddingItem> Data);

    private sealed record EmbeddingItem(
        [property: JsonPropertyName("index")]
        int Index,

        [property: JsonPropertyName("embedding")]
        float[] Embedding);
}
