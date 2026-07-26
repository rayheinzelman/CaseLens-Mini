using CaseLens.Api.Services.Retrieval;

namespace CaseLens.Api.Contracts.Retrieval;

public sealed record RetrievalResponse(
    string Question,
    IReadOnlyList<RetrievalResult> Results);
