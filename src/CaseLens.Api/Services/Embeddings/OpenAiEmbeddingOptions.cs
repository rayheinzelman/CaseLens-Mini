namespace CaseLens.Api.Services.Embeddings;

public sealed class OpenAiEmbeddingOptions
{
    public const string SectionName = "OpenAI:Embeddings";

    public string ApiKey { get; set; } = string.Empty;

    public string Model { get; set; } = "text-embedding-3-small";

    public int Dimensions { get; set; } = 1536;
}
