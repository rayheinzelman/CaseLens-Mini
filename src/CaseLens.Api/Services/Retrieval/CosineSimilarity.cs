namespace CaseLens.Api.Services.Retrieval;

public static class CosineSimilarity
{
    public static double Calculate(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Count != right.Count)
        {
            throw new ArgumentException(
                "Vectors must have the same dimensions.");
        }

        if (left.Count == 0)
        {
            throw new ArgumentException(
                "Vectors must not be empty.");
        }

        double dotProduct = 0;
        double leftMagnitudeSquared = 0;
        double rightMagnitudeSquared = 0;

        for (var index = 0; index < left.Count; index++)
        {
            dotProduct += left[index] * right[index];
            leftMagnitudeSquared += left[index] * left[index];
            rightMagnitudeSquared += right[index] * right[index];
        }

        if (leftMagnitudeSquared == 0 ||
            rightMagnitudeSquared == 0)
        {
            throw new ArgumentException(
                "Vectors must have non-zero magnitude.");
        }

        return dotProduct /
            (Math.Sqrt(leftMagnitudeSquared) *
             Math.Sqrt(rightMagnitudeSquared));
    }
}
