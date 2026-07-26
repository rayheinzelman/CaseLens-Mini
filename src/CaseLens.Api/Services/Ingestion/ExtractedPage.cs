namespace CaseLens.Api.Services.Ingestion;

public sealed record ExtractedPage(
    int PageNumber,
    string Content
    );