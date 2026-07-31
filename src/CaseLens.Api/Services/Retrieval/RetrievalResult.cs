namespace CaseLens.Api.Services.Retrieval;

public sealed record RetrievalResult(
    int ChunkId,
    int LegalDocumentId,
    string DocumentTitle,
    string Citation,
    int PageNumber,
    int ChunkIndex,
    string Content,
    double Similarity);
