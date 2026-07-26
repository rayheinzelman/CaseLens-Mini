namespace CaseLens.Api.Services.Ingestion;

public sealed record OpinionSource(
    string FilePath,
    string Title,
    string Citation);