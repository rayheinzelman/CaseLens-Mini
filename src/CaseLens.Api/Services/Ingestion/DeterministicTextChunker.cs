using System.Text.RegularExpressions;

namespace CaseLens.Api.Services.Ingestion;

public sealed class DeterministicTextChunker : ITextChunker
{
    private const int WordsPerChunk = 200;
    private const int OverlapWords = 30;

    public string Normalize(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        return Regex.Replace(content, @"\s+", " ").Trim();
    }

    public IReadOnlyList<string> Chunk(string content)
    {
        var normalizedContent = Normalize(content);

        if (normalizedContent.Length == 0)
        {
            return Array.Empty<string>();
        }

        var words = normalizedContent.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);

        var chunks = new List<string>();
        var stepSize = WordsPerChunk - OverlapWords;

        for (var startIndex = 0;
             startIndex < words.Length;
             startIndex += stepSize)
        {
            var wordCount = Math.Min(
                WordsPerChunk,
                words.Length - startIndex);

            chunks.Add(string.Join(
                ' ',
                words,
                startIndex,
                wordCount));

            if (startIndex + wordCount >= words.Length)
            {
                break;
            }
        }

        return chunks;
    }
}