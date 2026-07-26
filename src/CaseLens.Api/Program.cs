using CaseLens.Api.Data;
using CaseLens.Api.Services.Embeddings;
using CaseLens.Api.Services.Ingestion;
using CaseLens.Api.Services.Retrieval;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var connectionString =
    builder.Configuration.GetConnectionString("CaseLensMiniDatabase")
    ?? throw new InvalidOperationException(
        "Connection string 'CaseLensDatabase' was not found.");

builder.Services.AddDbContext<CaseLensDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<
    IPdfTextExtractor,
    PdfPigTextExtractor>();

builder.Services.AddScoped<
    ITextChunker,
    DeterministicTextChunker>();

builder.Services.AddScoped<
    IDocumentIngestionService,
    DocumentIngestionService>();

builder.Services.Configure<OpenAiEmbeddingOptions>(
    builder.Configuration.GetSection(
        OpenAiEmbeddingOptions.SectionName));

builder.Services.AddHttpClient<
    IEmbeddingGenerator,
    OpenAiEmbeddingGenerator>(httpClient =>
    {
        httpClient.BaseAddress =
            new Uri("https://api.openai.com/v1/");
    });

builder.Services.AddScoped<
    IChunkEmbeddingBackfillService,
    ChunkEmbeddingBackfillService>();

builder.Services.AddScoped<
    IRetrievalCandidateStore,
    EfRetrievalCandidateStore>();

builder.Services.AddScoped<
    IRetrievalService,
    CosineSimilarityRetrievalService>();

builder.Services.AddScoped<
    IRetrievalEvaluator,
    RetrievalEvaluator>();

var app = builder.Build();

if (args.Contains("--ingest", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();

    var ingestionService =
        scope.ServiceProvider
            .GetRequiredService<IDocumentIngestionService>();

    var opinionsDirectory = Path.Combine(
        AppContext.BaseDirectory,
        "Data",
        "Opinions");

    var opinionSources = new[]
    {
        new OpinionSource(
            Path.Combine(opinionsDirectory, "arizonavgant.pdf"),
            "ARIZONA v. GANT",
            "556 U. S. 332 (2009)"),

        new OpinionSource(
            Path.Combine(opinionsDirectory, "grahamvconnor.pdf"),
            "GRAHAM v. CONNOR ET AL",
            "490 U.S. 386 (1989)"),

        new OpinionSource(
            Path.Combine(opinionsDirectory, "terryvohio.pdf"),
            "TERRY v. OHIO.",
            "392 U.S. 1 (1968)")
    };

    foreach (var source in opinionSources.OrderBy(
                 source => source.FilePath,
                 StringComparer.OrdinalIgnoreCase))
    {
        var result = await ingestionService.IngestAsync(source);

        Console.WriteLine(
            $"{result.SourceFileName}: " +
            $"{result.PageCount} pages, " +
            $"{result.ChunkCount} chunks, " +
            $"skipped={result.WasSkipped}");
    }

    var embeddingBackfillService =
        scope.ServiceProvider.GetRequiredService<
            IChunkEmbeddingBackfillService>();

    var embeddingResult =
        await embeddingBackfillService.PopulateMissingAsync();

    Console.WriteLine(
        $"Embeddings: {embeddingResult.TotalChunkCount} chunks, " +
        $"{embeddingResult.GeneratedEmbeddingCount} generated, " +
        $"{embeddingResult.ExistingEmbeddingCount} already present, " +
        $"{embeddingResult.Dimensions} dimensions.");

    return;
}

if (args.Contains(
        "--evaluate-retrieval",
        StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();

    var evaluator = scope.ServiceProvider
        .GetRequiredService<IRetrievalEvaluator>();

    var report = await evaluator.EvaluateAsync();

    foreach (var result in report.Results)
    {
        Console.WriteLine(
            $"[{(result.Passed ? "PASS" : "FAIL")}] " +
            $"{result.Question}");
        Console.WriteLine(
            $"  Expected: {result.ExpectedCitation}");
        Console.WriteLine(
            $"  Retrieved: {string.Join(", ", result.RetrievedCitations)}");
    }

    Environment.ExitCode = report.Passed ? 0 : 1;
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
