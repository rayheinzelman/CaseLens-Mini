namespace CaseLens.Api.Services.Ingestion;

public interface ITextChunker
{
    string Normalize(string content);

    IReadOnlyList<string> Chunk(string content);
}