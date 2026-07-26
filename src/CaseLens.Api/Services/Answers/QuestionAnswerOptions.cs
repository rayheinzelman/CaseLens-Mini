namespace CaseLens.Api.Services.Answers;

public sealed class QuestionAnswerOptions
{
    public const string SectionName = "QuestionAnswering";

    public int TopK { get; init; } = 5;
    public double MinimumSimilarity { get; init; } = 0.25;
}
