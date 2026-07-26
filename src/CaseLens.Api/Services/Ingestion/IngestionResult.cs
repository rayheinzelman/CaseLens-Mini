namespace CaseLens.Api.Services.Ingestion;

public sealed record IngestionResult(
    string SourceFileName,
    int PageCount,
    int ChunkCount,
    bool WasSkipped);