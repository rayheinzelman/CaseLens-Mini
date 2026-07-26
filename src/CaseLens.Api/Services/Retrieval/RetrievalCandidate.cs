namespace CaseLens.Api.Services.Retrieval;

public sealed record RetrievalCandidate(
    int ChunkId,
    int LegalDocumentId,
    string DocumentTitle,
    string Citation,
    int PageNumber,
    int ChunkIndex,
    string Content,
    float[] Embedding);
