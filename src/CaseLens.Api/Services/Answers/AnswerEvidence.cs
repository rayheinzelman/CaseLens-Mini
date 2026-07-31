using CaseLens.Api.Services.Retrieval;

namespace CaseLens.Api.Services.Answers;

public sealed record AnswerEvidence(
    string EvidenceId,
    RetrievalResult RetrievalResult);
