namespace CaseLens.Api.Contracts.Questions;

public sealed record QuestionResponse(
    string Answer,
    bool InsufficientEvidence,
    IReadOnlyList<QuestionSourceResponse> Sources);

public sealed record QuestionSourceResponse(
    string EvidenceId,
    int ChunkId,
    string DocumentTitle,
    string Citation,
    int PageNumber,
    int ChunkIndex,
    string Passage,
    double SimilarityScore);
