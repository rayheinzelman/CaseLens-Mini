namespace CaseLens.Api.Services.Answers;

public interface IAnswerPromptBuilder
{
    string Build(
        string question,
        IReadOnlyList<AnswerEvidence> evidence);
}
