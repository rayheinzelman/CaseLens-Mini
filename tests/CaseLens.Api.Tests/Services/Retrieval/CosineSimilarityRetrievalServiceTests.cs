using CaseLens.Api.Services.Embeddings;
using CaseLens.Api.Services.Retrieval;

namespace CaseLens.Api.Tests.Services.Retrieval;

public sealed class CosineSimilarityRetrievalServiceTests
{
    public static TheoryData<string, string> KnownQuestions =>
        new()
        {
            {
                "When may police stop and frisk a person?",
                "392 U.S. 1 (1968)"
            },
            {
                "What standard governs excessive force during arrest?",
                "490 U.S. 386 (1989)"
            },
            {
                "When may police search a car incident to arrest?",
                "556 U. S. 332 (2009)"
            }
        };

    [Theory]
    [MemberData(nameof(KnownQuestions))]
    public async Task RetrieveAsync_KnownQuestion_RanksExpectedCaseFirst(
        string question,
        string expectedCitation)
    {
        var service = CreateService();

        var results = await service.RetrieveAsync(
            question,
            topK: 3);

        Assert.Equal(expectedCitation, results[0].Citation);
    }

    [Fact]
    public async Task RetrieveAsync_OrdersByDescendingSimilarity()
    {
        var service = CreateService();

        var results = await service.RetrieveAsync(
            "When may police stop and frisk a person?",
            topK: 3);

        Assert.Equal(
            [
                "392 U.S. 1 (1968)",
                "490 U.S. 386 (1989)",
                "556 U. S. 332 (2009)"
            ],
            results.Select(result => result.Citation));
    }

    [Fact]
    public async Task RetrieveAsync_InvalidTopK_Throws()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.RetrieveAsync("question", topK: 0));
    }

    private static CosineSimilarityRetrievalService CreateService()
    {
        var candidates = new[]
        {
            Candidate(
                1,
                "TERRY v. OHIO.",
                "392 U.S. 1 (1968)",
                [1.0f, 0.0f, 0.0f]),
            Candidate(
                2,
                "GRAHAM v. CONNOR ET AL",
                "490 U.S. 386 (1989)",
                [0.3f, 1.0f, 0.0f]),
            Candidate(
                3,
                "ARIZONA v. GANT",
                "556 U. S. 332 (2009)",
                [0.1f, 0.2f, 1.0f])
        };

        return new CosineSimilarityRetrievalService(
            new KnownQuestionEmbeddingGenerator(),
            new StubCandidateStore(candidates));
    }

    private static RetrievalCandidate Candidate(
        int id,
        string title,
        string citation,
        float[] embedding) =>
        new(
            id,
            id,
            title,
            citation,
            1,
            0,
            $"{title} representative text",
            embedding);

    private sealed class StubCandidateStore
        : IRetrievalCandidateStore
    {
        private readonly IReadOnlyList<RetrievalCandidate> _candidates;

        public StubCandidateStore(
            IReadOnlyList<RetrievalCandidate> candidates)
        {
            _candidates = candidates;
        }

        public Task<IReadOnlyList<RetrievalCandidate>>
            LoadEmbeddedAsync(
                CancellationToken cancellationToken = default) =>
            Task.FromResult(_candidates);
    }

    private sealed class KnownQuestionEmbeddingGenerator
        : IEmbeddingGenerator
    {
        public int Dimensions => 3;

        public Task<IReadOnlyList<float[]>> GenerateAsync(
            IReadOnlyList<string> inputs,
            CancellationToken cancellationToken = default)
        {
            var vectors = inputs
                .Select(input =>
                {
                    if (input.Contains(
                            "stop and frisk",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return new[] { 1.0f, 0.0f, 0.0f };
                    }

                    if (input.Contains(
                            "excessive force",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return new[] { 0.0f, 1.0f, 0.0f };
                    }

                    if (input.Contains(
                            "car incident",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return new[] { 0.0f, 0.0f, 1.0f };
                    }

                    return new[] { 1.0f, 1.0f, 1.0f };
                })
                .ToArray();

            return Task.FromResult<IReadOnlyList<float[]>>(vectors);
        }
    }
}
