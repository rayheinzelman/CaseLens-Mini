namespace CaseLens.Api.Services.Answers;

public interface IAnswerGenerationService
{
    Task<AnswerGenerationResult> GenerateAsync(
        string question,
        IReadOnlyList<AnswerEvidence> evidence,
        CancellationToken cancellationToken = default);
}
