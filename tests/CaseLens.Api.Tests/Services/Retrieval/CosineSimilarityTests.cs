using CaseLens.Api.Services.Retrieval;

namespace CaseLens.Api.Tests.Services.Retrieval;

public sealed class CosineSimilarityTests
{
    [Fact]
    public void Calculate_IdenticalVectors_ReturnsOne()
    {
        var similarity = CosineSimilarity.Calculate(
            [1, 2, 3],
            [1, 2, 3]);

        Assert.Equal(1, similarity, precision: 10);
    }

    [Fact]
    public void Calculate_OrthogonalVectors_ReturnsZero()
    {
        var similarity = CosineSimilarity.Calculate(
            [1, 0],
            [0, 1]);

        Assert.Equal(0, similarity, precision: 10);
    }

    [Fact]
    public void Calculate_MismatchedDimensions_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CosineSimilarity.Calculate(
                [1, 0],
                [1, 0, 0]));
    }

    [Fact]
    public void Calculate_ZeroVector_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CosineSimilarity.Calculate(
                [0, 0],
                [1, 0]));
    }
}
