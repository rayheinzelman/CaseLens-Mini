namespace CaseLens.Api.Services.Answers;

public sealed class AnswerProviderException : Exception
{
    public AnswerProviderException(string message)
        : base(message) { }

    public AnswerProviderException(string message, Exception innerException)
        : base(message, innerException) { }
}
