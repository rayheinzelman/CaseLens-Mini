namespace CaseLens.Api.Services.Answers;

public sealed record AnswerGenerationResult(
    string Answer,
    IReadOnlyList<string> CitedEvidenceIds,
    bool InsufficientEvidence);
