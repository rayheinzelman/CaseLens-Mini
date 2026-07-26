namespace CaseLens.Api.Services.Ingestion;

public interface IDocumentIngestionService
{
    Task<IngestionResult> IngestAsync(
        OpinionSource source,
        CancellationToken cancellationToken = default);
}