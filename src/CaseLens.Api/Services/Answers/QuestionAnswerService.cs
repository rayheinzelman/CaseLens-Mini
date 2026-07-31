using CaseLens.Api.Contracts.Questions;
using CaseLens.Api.Services.Retrieval;
using Microsoft.Extensions.Options;

namespace CaseLens.Api.Services.Answers;

public sealed class QuestionAnswerService : IQuestionAnswerService
{
    public const string RefusalMessage =
        "The indexed opinions do not provide enough evidence to answer that question. CaseLens is a research aid, not legal advice.";

    private readonly IRetrievalService _retrievalService;
    private readonly IAnswerGenerationService _answerGenerationService;
    private readonly QuestionAnswerOptions _options;

    public QuestionAnswerService(
        IRetrievalService retrievalService,
        IAnswerGenerationService answerGenerationService,
        IOptions<QuestionAnswerOptions> options)
    {
        _retrievalService = retrievalService;
        _answerGenerationService = answerGenerationService;
        _options = options.Value;
    }

    public async Task<QuestionResponse> AnswerAsync(
        string question,
        CancellationToken cancellationToken = default)
    {
        var retrieved = await _retrievalService.RetrieveAsync(
            question,
            _options.TopK,
            cancellationToken);

        var evidence = retrieved
            .Where(result => result.Similarity >= _options.MinimumSimilarity)
            .Select((result, index) => new AnswerEvidence(
                $"C{index + 1}",
                result))
            .ToArray();

        if (evidence.Length == 0)
        {
            return Refusal();
        }

        var generated = await _answerGenerationService.GenerateAsync(
            question,
            evidence,
            cancellationToken);

        if (generated.InsufficientEvidence)
        {
            return Refusal();
        }

        // Create a dictionary for quick lookup of evidence supplied TO the answer generation service
        // This ensures that we only return sources that were actually used in generating the answer
        var evidenceById = evidence.ToDictionary(
            item => item.EvidenceId,
            StringComparer.Ordinal);

        // Remove duplicates
        // Remove IDs we never supplied
        // Convert valid IDs into source responses
        var validatedSources = generated.CitedEvidenceIds
            .Distinct(StringComparer.Ordinal)
            .Where(evidenceById.ContainsKey)
            .Select(id => MapSource(evidenceById[id]))
            .ToArray();

        // Reject answer if there are no valid sources to cite
        if (validatedSources.Length == 0)
        {
            return Refusal();
        }

        return new QuestionResponse(
            generated.Answer.Trim(),
            InsufficientEvidence: false,
            validatedSources);
    }

    private static QuestionSourceResponse MapSource(AnswerEvidence evidence)
    {
        var result = evidence.RetrievalResult;
        return new QuestionSourceResponse(
            evidence.EvidenceId,
            result.ChunkId,
            result.DocumentTitle,
            result.Citation,
            result.PageNumber,
            result.ChunkIndex,
            result.Content,
            result.Similarity);
    }

    private static QuestionResponse Refusal() =>
        new(
            RefusalMessage,
            InsufficientEvidence: true,
            Sources: Array.Empty<QuestionSourceResponse>());
}
