using CaseLens.Api.Services.Ingestion;

namespace CaseLens.Api.Tests.Services.Ingestion;

internal sealed class FakePdfTextExtractor : IPdfTextExtractor
{
    private readonly IReadOnlyList<ExtractedPage> _pages;

    public FakePdfTextExtractor(
        IReadOnlyList<ExtractedPage> pages)
    {
        _pages = pages;
    }

    public IReadOnlyList<ExtractedPage> ExtractPages(
        string filePath)
    {
        return _pages;
    }
}