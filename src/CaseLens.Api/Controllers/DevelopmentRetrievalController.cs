using CaseLens.Api.Contracts.Retrieval;
using CaseLens.Api.Services.Retrieval;
using Microsoft.AspNetCore.Mvc;

namespace CaseLens.Api.Controllers;

[ApiController]
[Route("api/development/retrieval")]
public sealed class DevelopmentRetrievalController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IRetrievalService _retrievalService;

    public DevelopmentRetrievalController(
        IWebHostEnvironment environment,
        IRetrievalService retrievalService)
    {
        _environment = environment;
        _retrievalService = retrievalService;
    }

    [HttpPost]
    [ProducesResponseType<RetrievalResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RetrievalResponse>> RetrieveAsync(
        RetrievalRequest request,
        CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            ModelState.AddModelError(
                nameof(request.Question),
                "Question is required.");
            return ValidationProblem(ModelState);
        }

        var results = await _retrievalService.RetrieveAsync(
            request.Question,
            request.TopK,
            cancellationToken);

        return Ok(new RetrievalResponse(request.Question, results));
    }
}
