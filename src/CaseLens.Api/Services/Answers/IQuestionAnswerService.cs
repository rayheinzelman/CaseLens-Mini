using CaseLens.Api.Contracts.Questions;

namespace CaseLens.Api.Services.Answers;

public interface IQuestionAnswerService
{
    Task<QuestionResponse> AnswerAsync(
        string question,
        CancellationToken cancellationToken = default);
}
