namespace Assured.Tests;

public class SuccessTests
{
    [Fact]
    public void Value_IsDefault()
    {
        Assert.Equal(default, Success.Value);
    }

    [Fact]
    public void Success_ImplementsIEquatable()
    {
        Assert.IsAssignableFrom<IEquatable<Success>>(Success.Value);
    }

    [Fact]
    public void Equals_AnySuccess_IsTrue()
    {
        Assert.True(Success.Value.Equals(default(Success)));
        Assert.True(Success.Value.Equals((object)default(Success)));
    }

    [Fact]
    public void Equals_OtherType_IsFalse()
    {
        Assert.False(Success.Value.Equals(0));
    }

    [Fact]
    public void Equals_Null_IsFalse()
    {
        Assert.False(Success.Value.Equals(null));
    }

    [Fact]
    public void GetHashCode_IsSameForAllInstances()
    {
        Assert.Equal(Success.Value.GetHashCode(), default(Success).GetHashCode());
    }

    [Fact]
    public void ToString_IsSuccess()
    {
        Assert.Equal("Success", Success.Value.ToString());
    }

    [Fact]
    public void ToString_ThroughVirtualDispatch_IsSuccess()
    {
        Assert.Equal("Success", ((object)Success.Value).ToString());
        Assert.Equal("Value(Success)", Result<Success, string>.Value(Success.Value).ToString());
    }

    [Fact]
    public void ImplicitConversion_ProducesValueResult()
    {
        Result<Success, string> result = Success.Value;

        Assert.True(result.HasValue());
        Assert.Equal(Result<Success, string>.Value(Success.Value), result);
    }
}
