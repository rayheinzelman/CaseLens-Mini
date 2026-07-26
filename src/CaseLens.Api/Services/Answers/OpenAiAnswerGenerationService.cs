using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace CaseLens.Api.Services.Answers;

public sealed class OpenAiAnswerGenerationService : IAnswerGenerationService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly IAnswerPromptBuilder _promptBuilder;
    private readonly OpenAiAnswerOptions _options;

    public OpenAiAnswerGenerationService(
        HttpClient httpClient,
        IAnswerPromptBuilder promptBuilder,
        IOptions<OpenAiAnswerOptions> options)
    {
        _httpClient = httpClient;
        _promptBuilder = promptBuilder;
        _options = options.Value;
    }

    public async Task<AnswerGenerationResult> GenerateAsync(
        string question,
        IReadOnlyList<AnswerEvidence> evidence,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new AnswerProviderException(
                "The answer provider API key is not configured.");
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "chat/completions");
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        request.Content = JsonContent.Create(new
        {
            model = _options.Model,
            temperature = 0,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = _promptBuilder.Build(question, evidence)
                }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = "caselens_answer",
                    strict = true,
                    schema = new
                    {
                        type = "object",
                        properties = new
                        {
                            answer = new { type = "string" },
                            citedEvidenceIds = new
                            {
                                type = "array",
                                items = new { type = "string" }
                            },
                            insufficientEvidence = new { type = "boolean" }
                        },
                        required = new[]
                        {
                            "answer",
                            "citedEvidenceIds",
                            "insufficientEvidence"
                        },
                        additionalProperties = false
                    }
                }
            }
        });

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new AnswerProviderException(
                    $"The answer provider returned HTTP {(int)response.StatusCode}.");
            }

            var envelope = await response.Content.ReadFromJsonAsync<
                ChatCompletionEnvelope>(JsonOptions, cancellationToken);
            var content = envelope?.Choices.FirstOrDefault()?.Message.Content;

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new AnswerProviderException(
                    "The answer provider returned an empty response.");
            }

            var result = JsonSerializer.Deserialize<AnswerGenerationResult>(
                content,
                JsonOptions);

            return result ?? throw new AnswerProviderException(
                "The answer provider returned invalid structured output.");
        }
        catch (AnswerProviderException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is HttpRequestException or JsonException)
        {
            throw new AnswerProviderException(
                "The answer provider request failed.",
                exception);
        }
    }

    private sealed record ChatCompletionEnvelope(
        IReadOnlyList<ChatChoice> Choices);

    private sealed record ChatChoice(ChatMessage Message);

    private sealed record ChatMessage(
        [property: JsonPropertyName("content")] string Content);
}
