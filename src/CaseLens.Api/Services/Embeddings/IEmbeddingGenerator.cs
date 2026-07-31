namespace CaseLens.Api.Services.Embeddings;

public interface IEmbeddingGenerator
{
    int Dimensions { get; }

    Task<IReadOnlyList<float[]>> GenerateAsync(
        IReadOnlyList<string> inputs,
        CancellationToken cancellationToken = default);
}
