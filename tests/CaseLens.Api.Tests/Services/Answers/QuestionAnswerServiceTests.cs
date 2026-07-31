using CaseLens.Api.Services.Answers;
using CaseLens.Api.Services.Retrieval;
using Microsoft.Extensions.Options;

namespace CaseLens.Api.Tests.Services.Answers;

public sealed class QuestionAnswerServiceTests
{
    [Fact]
    public async Task AnswerAsync_SupportedQuestion_ReturnsValidatedSource()
    {
        var retrieval = new StubRetrievalService([
            Result(12, "TERRY v. OHIO.", "392 U.S. 1 (1968)", 0.87)
        ]);
        var generator = new StubAnswerGenerationService(
            new AnswerGenerationResult(
                "An officer must identify specific and articulable facts.",
                ["C1"],
                false));
        var service = CreateService(retrieval, generator);

        var response = await service.AnswerAsync(
            "When may police stop and frisk a person?");

        Assert.False(response.InsufficientEvidence);
        var source = Assert.Single(response.Sources);
        Assert.Equal("C1", source.EvidenceId);
        Assert.Equal(12, source.ChunkId);
        Assert.Equal("392 U.S. 1 (1968)", source.Citation);
    }

    [Fact]
    public async Task AnswerAsync_RemovesUnknownCitationIds()
    {
        var retrieval = new StubRetrievalService([
            Result(12, "TERRY v. OHIO.", "392 U.S. 1 (1968)", 0.87),
            Result(22, "GRAHAM v. CONNOR", "490 U.S. 386 (1989)", 0.71)
        ]);
        var generator = new StubAnswerGenerationService(
            new AnswerGenerationResult(
                "Supported answer.",
                ["C1", "C99", "C1"],
                false));
        var service = CreateService(retrieval, generator);

        var response = await service.AnswerAsync("supported question");

        var source = Assert.Single(response.Sources);
        Assert.Equal("C1", source.EvidenceId);
    }

    [Fact]
    public async Task AnswerAsync_OnlyUnknownCitationIds_Refuses()
    {
        var retrieval = new StubRetrievalService([
            Result(12, "TERRY v. OHIO.", "392 U.S. 1 (1968)", 0.87)
        ]);
        var generator = new StubAnswerGenerationService(
            new AnswerGenerationResult(
                "Invented answer.",
                ["C99"],
                false));
        var service = CreateService(retrieval, generator);

        var response = await service.AnswerAsync("question");

        Assert.True(response.InsufficientEvidence);
        Assert.Empty(response.Sources);
    }

    [Fact]
    public async Task AnswerAsync_LowSimilarityEvidence_RefusesWithoutProviderCall()
    {
        var retrieval = new StubRetrievalService([
            Result(12, "TERRY v. OHIO.", "392 U.S. 1 (1968)", 0.10)
        ]);
        var generator = new StubAnswerGenerationService(
            new AnswerGenerationResult("unused", [], false));
        var service = CreateService(retrieval, generator);

        var response = await service.AnswerAsync(
            "How should I draft my apartment lease?");

        Assert.True(response.InsufficientEvidence);
        Assert.Empty(response.Sources);
        Assert.Equal(0, generator.CallCount);
    }

    [Fact]
    public async Task AnswerAsync_ModelReportsInsufficientEvidence_Refuses()
    {
        var retrieval = new StubRetrievalService([
            Result(12, "TERRY v. OHIO.", "392 U.S. 1 (1968)", 0.87)
        ]);
        var generator = new StubAnswerGenerationService(
            new AnswerGenerationResult("Not enough evidence.", [], true));
        var service = CreateService(retrieval, generator);

        var response = await service.AnswerAsync("unsupported question");

        Assert.True(response.InsufficientEvidence);
        Assert.Empty(response.Sources);
    }

    private static QuestionAnswerService CreateService(
        IRetrievalService retrievalService,
        IAnswerGenerationService answerGenerationService) =>
        new(
            retrievalService,
            answerGenerationService,
            Options.Create(new QuestionAnswerOptions
            {
                TopK = 5,
                MinimumSimilarity = 0.25
            }));

    private static RetrievalResult Result(
        int chunkId,
        string title,
        string citation,
        double similarity) =>
        new(
            chunkId,
            1,
            title,
            citation,
            9,
            0,
            "Specific and articulable facts from the opinion.",
            similarity);

    private sealed class StubRetrievalService : IRetrievalService
    {
        private readonly IReadOnlyList<RetrievalResult> _results;

        public StubRetrievalService(IReadOnlyList<RetrievalResult> results) =>
            _results = results;

        public Task<IReadOnlyList<RetrievalResult>> RetrieveAsync(
            string question,
            int topK = 5,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_results);
    }

    private sealed class StubAnswerGenerationService
        : IAnswerGenerationService
    {
        private readonly AnswerGenerationResult _result;

        public StubAnswerGenerationService(AnswerGenerationResult result) =>
            _result = result;

        public int CallCount { get; private set; }

        public Task<AnswerGenerationResult> GenerateAsync(
            string question,
            IReadOnlyList<AnswerEvidence> evidence,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(_result);
        }
    }
}
