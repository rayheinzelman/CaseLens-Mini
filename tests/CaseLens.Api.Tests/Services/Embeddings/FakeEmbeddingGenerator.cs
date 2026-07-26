using CaseLens.Api.Services.Embeddings;

namespace CaseLens.Api.Tests.Services.Embeddings;

internal sealed class FakeEmbeddingGenerator
    : IEmbeddingGenerator
{
    public FakeEmbeddingGenerator(int dimensions)
    {
        Dimensions = dimensions;
    }

    public int Dimensions { get; }

    public List<IReadOnlyList<string>> Requests { get; } = [];

    public Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(inputs.ToList());

        IReadOnlyList<float[]> embeddings = inputs
            .Select((_, index) =>
                Enumerable.Repeat(
                        (float)(index + 1),
                        Dimensions)
                    .ToArray())
            .ToList();

        return Task.FromResult(embeddings);
    }
}
