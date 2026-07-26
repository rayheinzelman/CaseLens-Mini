using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace CaseLens.Api.Services.Ingestion;

public sealed class PdfPigTextExtractor : IPdfTextExtractor
{
    public IReadOnlyList<ExtractedPage> ExtractPages(string filePath)
    {
        using var document = PdfDocument.Open(filePath);

        return document.GetPages()
            .OrderBy(page => page.Number)
            .Select(page => new ExtractedPage(
                page.Number,
                ContentOrderTextExtractor.GetText(page)))
            .ToList();
    }
}