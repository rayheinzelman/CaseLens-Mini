using CaseLens.Api.Contracts.Questions;
using CaseLens.Api.Services.Answers;
using Microsoft.AspNetCore.Mvc;

namespace CaseLens.Api.Controllers;

[ApiController]
[Route("api/questions")]
public sealed class QuestionsController : ControllerBase
{
    private readonly IQuestionAnswerService _questionAnswerService;
    private readonly ILogger<QuestionsController> _logger;

    public QuestionsController(
        IQuestionAnswerService questionAnswerService,
        ILogger<QuestionsController> logger)
    {
        _questionAnswerService = questionAnswerService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType<QuestionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<QuestionResponse>> AskAsync(
        QuestionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            ModelState.AddModelError(
                nameof(request.Question),
                "Question is required.");
            return ValidationProblem(ModelState);
        }

        try
        {
            return Ok(await _questionAnswerService.AnswerAsync(
                request.Question.Trim(),
                cancellationToken));
        }
        catch (AnswerProviderException exception)
        {
            _logger.LogError(
                exception,
                "The answer provider failed while processing a question.");

            return Problem(
                statusCode: StatusCodes.Status502BadGateway,
                title: "Answer generation is temporarily unavailable.",
                detail: "The question could not be answered at this time.");
        }
    }
}
