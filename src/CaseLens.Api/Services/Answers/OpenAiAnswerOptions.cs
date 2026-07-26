namespace CaseLens.Api.Services.Answers;

public sealed class OpenAiAnswerOptions
{
    public const string SectionName = "OpenAI:Answers";

    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gpt-4o-mini";
}
