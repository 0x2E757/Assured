namespace Assured.Tests;

public class ResultOperatorsTests
{
    [Fact]
    public void ImplicitFromValue_HoldsValue()
    {
        Result<int, string> result = 5;

        Assert.True(result.HasValue());
        Assert.Equal(5, result.UnwrapValue());
    }

    [Fact]
    public void ImplicitFromError_HoldsError()
    {
        Result<int, string> result = "boom";

        Assert.True(result.HasError());
        Assert.Equal("boom", result.UnwrapError());
    }

    [Fact]
    public void ImplicitFromNull_PicksTheNullableSide()
    {
        Result<string?, int> value = null;
        Result<int, string?> error = null;

        Assert.True(value.HasValue());
        Assert.True(error.HasError());
    }

    [Fact]
    public void ImplicitFromSourceConvertibleToBothSides_PicksTheMoreSpecificSide()
    {
        Result<object, string> result = "x";

        Assert.True(result.HasError());
    }

    [Fact]
    public void EqualityOperator_EqualResults_IsTrue()
    {
        Assert.True(Result<int, string>.Value(5) == Result<int, string>.Value(5));
        Assert.True(Result<int, string>.Error("boom") == Result<int, string>.Error("boom"));
        Assert.True(default(Result<int, string>) == default(Result<int, string>));
    }

    [Fact]
    public void EqualityOperator_DifferentResults_IsFalse()
    {
        Assert.False(Result<int, string>.Value(5) == Result<int, string>.Value(6));
        Assert.False(Result<int, string>.Value(5) == Result<int, string>.Error("boom"));
        Assert.False(Result<int, string>.Value(5) == default(Result<int, string>));
    }

    [Fact]
    public void EqualityOperator_ErrorsAndDefaults()
    {
        Assert.False(Result<int, string>.Error("boom") == Result<int, string>.Error("bang"));
        Assert.False(Result<int, string>.Error("boom") == default(Result<int, string>));
        Assert.False(Result<int, int>.Value(1) == Result<int, int>.Error(1));
        Assert.True(Result<int, string>.Error("boom") != Result<int, string>.Error("bang"));
        Assert.False(Result<int, string>.Error("boom") != Result<int, string>.Error("boom"));
        Assert.False(default(Result<int, string>) != default(Result<int, string>));
    }

    [Fact]
    public void EqualityOperator_WithRawOperand_ComparesAgainstConvertedResult()
    {
        Assert.True(Result<int, string>.Value(5) == 5);
        Assert.True(Result<int, string>.Error("boom") == "boom");
        Assert.False(Result<int, string>.Value(5) == "5");
    }

    [Fact]
    public void InequalityOperator_IsNegationOfEquality()
    {
        Assert.False(Result<int, string>.Value(5) != Result<int, string>.Value(5));
        Assert.True(Result<int, string>.Value(5) != Result<int, string>.Value(6));
        Assert.True(Result<int, string>.Value(5) != default(Result<int, string>));
    }
}
