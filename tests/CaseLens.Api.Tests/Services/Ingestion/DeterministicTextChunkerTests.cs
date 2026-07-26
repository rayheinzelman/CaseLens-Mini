using CaseLens.Api.Services.Ingestion;

namespace CaseLens.Api.Tests.Services.Ingestion;

public sealed class DeterministicTextChunkerTests
{
    [Fact]
    public void Normalize_CollapsesWhitespaceAndTrimsContent()
    {
        var chunker = new DeterministicTextChunker();

        var result = chunker.Normalize(
            "  The   court\r\nheld\tthat   the claim survived.  ");

        Assert.Equal(
            "The court held that the claim survived.",
            result);
    }

    [Fact]
    public void Chunk_SameContentProducesSameChunks()
    {
        var chunker = new DeterministicTextChunker();

        var content = string.Join(
            " ",
            Enumerable.Range(1, 500)
                .Select(number => $"word{number}"));

        var firstResult = chunker.Chunk(content);
        var secondResult = chunker.Chunk(content);

        Assert.Equal(firstResult, secondResult);
    }
}