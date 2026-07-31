using System.Net;
using System.Text;
using CaseLens.Api.Services.Embeddings;
using Microsoft.Extensions.Options;

namespace CaseLens.Api.Tests.Services.Embeddings;

public sealed class OpenAiEmbeddingGeneratorTests
{
    [Fact]
    public async Task GenerateAsync_ReturnsVectorsInInputOrder()
    {
        const string responseJson =
            """
            {
              "data": [
                {
                  "index": 1,
                  "embedding": [0.4, 0.5, 0.6]
                },
                {
                  "index": 0,
                  "embedding": [0.1, 0.2, 0.3]
                }
              ]
            }
            """;

        var handler = new RecordingHttpMessageHandler(
            HttpStatusCode.OK,
            responseJson);

        var generator = CreateGenerator(handler);

        var result = await generator.GenerateAsync(
            ["first chunk", "second chunk"]);

        Assert.Equal([0.1f, 0.2f, 0.3f], result[0]);
        Assert.Equal([0.4f, 0.5f, 0.6f], result[1]);

        Assert.Equal(
            "Bearer test-api-key",
            handler.Authorization);

        Assert.Contains(
            "\"model\":\"text-embedding-3-small\"",
            handler.RequestBody);

        Assert.Contains(
            "\"dimensions\":3",
            handler.RequestBody);
    }

    [Fact]
    public async Task GenerateAsync_RejectsUnexpectedVectorLength()
    {
        const string responseJson =
            """
            {
              "data": [
                {
                  "index": 0,
                  "embedding": [0.1, 0.2]
                }
              ]
            }
            """;

        var generator = CreateGenerator(
            new RecordingHttpMessageHandler(
                HttpStatusCode.OK,
                responseJson));

        var exception = await Assert.ThrowsAsync<
            InvalidOperationException>(() =>
                generator.GenerateAsync(["a chunk"]));

        Assert.Contains(
            "2 dimensions but 3 were configured",
            exception.Message);
    }

    private static OpenAiEmbeddingGenerator CreateGenerator(
        HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress =
                new Uri("https://api.openai.com/v1/")
        };

        var options = Options.Create(
            new OpenAiEmbeddingOptions
            {
                ApiKey = "test-api-key",
                Model = "text-embedding-3-small",
                Dimensions = 3
            });

        return new OpenAiEmbeddingGenerator(
            httpClient,
            options);
    }

    private sealed class RecordingHttpMessageHandler
        : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _responseJson;

        public RecordingHttpMessageHandler(
            HttpStatusCode statusCode,
            string responseJson)
        {
            _statusCode = statusCode;
            _responseJson = responseJson;
        }

        public string Authorization { get; private set; } =
            string.Empty;

        public string RequestBody { get; private set; } =
            string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization =
                request.Headers.Authorization?.ToString()
                ?? string.Empty;

            RequestBody = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(
                    cancellationToken);

            return new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(
                    _responseJson,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
