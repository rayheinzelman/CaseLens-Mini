namespace CaseLens.Api.Services.Ingestion;

public interface IPdfTextExtractor
{
    IReadOnlyList<ExtractedPage> ExtractPages(string filePath);
}