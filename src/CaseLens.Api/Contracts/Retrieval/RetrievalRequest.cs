namespace CaseLens.Api.Contracts.Retrieval;

public sealed record RetrievalRequest(
    string Question,
    int TopK = 5);
