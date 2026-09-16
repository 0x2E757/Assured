namespace Assured.Tests;

public class ResultTests
{
    [Fact]
    public void Value_HoldsValue()
    {
        var result = Result<int, string>.Value(5);

        Assert.True(result.HasValue());
        Assert.False(result.HasError());
        Assert.Equal(5, result.UnwrapValue());
    }

    [Fact]
    public void Error_HoldsError()
    {
        var result = Result<int, string>.Error("boom");

        Assert.False(result.HasValue());
        Assert.True(result.HasError());
        Assert.Equal("boom", result.UnwrapError());
    }

    [Fact]
    public void Default_HoldsNeither()
    {
        var result = default(Result<int, string>);

        Assert.False(result.HasValue());
        Assert.False(result.HasError());
    }

    [Fact]
    public void Value_AcceptsNull()
    {
        var result = Result<string?, int>.Value(null);

        Assert.True(result.HasValue());
        Assert.Null(result.UnwrapValue());
    }

    [Fact]
    public void Error_AcceptsNull()
    {
        var result = Result<int, string?>.Error(null);

        Assert.True(result.HasError());
        Assert.Null(result.UnwrapError());
    }

    [Fact]
    public void UnwrapValue_OnError_Throws()
    {
        var result = Result<int, string>.Error("boom");

        Assert.Throws<InvalidOperationException>(() => result.UnwrapValue());
    }

    [Fact]
    public void UnwrapValue_OnDefault_Throws()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapValue());
    }

    [Fact]
    public void UnwrapError_OnValue_Throws()
    {
        var result = Result<int, string>.Value(5);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapError());
    }

    [Fact]
    public void UnwrapError_OnDefault_Throws()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.UnwrapError());
    }

    [Fact]
    public void TryUnwrapValue_OnValue_ReturnsTrueAndValue()
    {
        var result = Result<int, string>.Value(5);

        Assert.True(result.TryUnwrapValue(out var value));
        Assert.Equal(5, value);
    }

    [Fact]
    public void TryUnwrapValue_OnNullValue_ReturnsTrueAndNull()
    {
        var result = Result<string?, int>.Value(null);

        Assert.True(result.TryUnwrapValue(out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryUnwrapValue_OnError_ReturnsFalse()
    {
        var result = Result<int, string>.Error("boom");

        Assert.False(result.TryUnwrapValue(out _));
    }

    [Fact]
    public void TryUnwrapValue_OnDefault_ReturnsFalse()
    {
        var result = default(Result<int, string>);

        Assert.False(result.TryUnwrapValue(out _));
    }

    [Fact]
    public void TryUnwrapError_OnError_ReturnsTrueAndError()
    {
        var result = Result<int, string>.Error("boom");

        Assert.True(result.TryUnwrapError(out var error));
        Assert.Equal("boom", error);
    }

    [Fact]
    public void TryUnwrapError_OnNullError_ReturnsTrueAndNull()
    {
        var result = Result<int, string?>.Error(null);

        Assert.True(result.TryUnwrapError(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryUnwrapError_OnValue_ReturnsFalse()
    {
        var result = Result<int, string>.Value(5);

        Assert.False(result.TryUnwrapError(out _));
    }

    [Fact]
    public void TryUnwrapError_OnDefault_ReturnsFalse()
    {
        var result = default(Result<int, string>);

        Assert.False(result.TryUnwrapError(out _));
    }

    [Fact]
    public void ValueOr_OnValue_ReturnsValue()
    {
        var result = Result<int, string>.Value(5);

        Assert.Equal(5, result.ValueOr(-1));
    }

    [Fact]
    public void ValueOr_OnNullValue_ReturnsNullNotFallback()
    {
        var result = Result<string?, int>.Value(null);

        Assert.Null(result.ValueOr("fallback"));
    }

    [Fact]
    public void ValueOr_OnError_ReturnsFallback()
    {
        var result = Result<int, string>.Error("boom");

        Assert.Equal(-1, result.ValueOr(-1));
    }

    [Fact]
    public void ValueOr_OnDefault_ReturnsFallback()
    {
        var result = default(Result<int, string>);

        Assert.Equal(-1, result.ValueOr(-1));
    }

    [Fact]
    public void ValueOrGenerate_OnValue_ReturnsValueWithoutCallingFallback()
    {
        var result = Result<int, string>.Value(5);
        var called = false;

        var value = result.ValueOrGenerate(() => { called = true; return -1; });

        Assert.Equal(5, value);
        Assert.False(called);
    }

    [Fact]
    public void ValueOrGenerate_OnNullValue_ReturnsNullWithoutCallingFallback()
    {
        var result = Result<string?, int>.Value(null);
        var called = false;

        var value = result.ValueOrGenerate(() => { called = true; return "fallback"; });

        Assert.Null(value);
        Assert.False(called);
    }

    [Fact]
    public void ValueOrGenerate_OnError_ReturnsGeneratedFallback()
    {
        var result = Result<int, string>.Error("boom");

        Assert.Equal(-1, result.ValueOrGenerate(() => -1));
    }

    [Fact]
    public void ValueOrGenerate_OnDefault_ReturnsGeneratedFallback()
    {
        var result = default(Result<int, string>);

        Assert.Equal(-1, result.ValueOrGenerate(() => -1));
    }

    [Fact]
    public void ToString_OnValue_ShowsValue()
    {
        Assert.Equal("Value(5)", Result<int, string>.Value(5).ToString());
    }

    [Fact]
    public void ToString_OnError_ShowsError()
    {
        Assert.Equal("Error(boom)", Result<int, string>.Error("boom").ToString());
    }

    [Fact]
    public void ToString_OnDefault_ShowsUndefined()
    {
        Assert.Equal("Result(Undefined)", default(Result<int, string>).ToString());
    }
}
