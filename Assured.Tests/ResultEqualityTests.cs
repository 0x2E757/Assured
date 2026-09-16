namespace Assured.Tests;

public class ResultEqualityTests
{
    [Fact]
    public void Result_ImplementsIEquatable()
    {
        Assert.IsAssignableFrom<IEquatable<Result<int, string>>>(Result<int, string>.Value(5));
    }

    [Fact]
    public void Equals_SameValues_IsTrue()
    {
        Assert.True(Result<int, string>.Value(5).Equals(Result<int, string>.Value(5)));
    }

    [Fact]
    public void Equals_DifferentValues_IsFalse()
    {
        Assert.False(Result<int, string>.Value(5).Equals(Result<int, string>.Value(6)));
    }

    [Fact]
    public void Equals_SameErrors_IsTrue()
    {
        Assert.True(Result<int, string>.Error("boom").Equals(Result<int, string>.Error("boom")));
    }

    [Fact]
    public void Equals_DifferentErrors_IsFalse()
    {
        Assert.False(Result<int, string>.Error("boom").Equals(Result<int, string>.Error("bang")));
    }

    [Fact]
    public void Equals_ValueAndErrorWithSameContent_IsFalse()
    {
        Assert.False(Result<int, int>.Value(1).Equals(Result<int, int>.Error(1)));
    }

    [Fact]
    public void Equals_ComparesContentsByValueNotByReference()
    {
        var left = string.Concat("bo", "om");
        var right = string.Concat("boo", "m");
        Assert.NotSame(left, right);

        Assert.True(Result<string, string>.Value(left).Equals(Result<string, string>.Value(right)));
        Assert.True(Result<string, string>.Error(left).Equals(Result<string, string>.Error(right)));
    }

    [Fact]
    public void Equals_NullValues_IsTrue()
    {
        Assert.True(Result<string?, int>.Value(null).Equals(Result<string?, int>.Value(null)));
    }

    [Fact]
    public void Equals_NullErrors_IsTrue()
    {
        Assert.True(Result<int, string?>.Error(null).Equals(Result<int, string?>.Error(null)));
    }

    [Fact]
    public void Equals_NullAndNonNullContent_IsFalse()
    {
        Assert.False(Result<string?, int>.Value(null).Equals(Result<string?, int>.Value("x")));
        Assert.False(Result<string?, int>.Value("x").Equals(Result<string?, int>.Value(null)));
        Assert.False(Result<int, string?>.Error(null).Equals(Result<int, string?>.Error("x")));
    }

    [Fact]
    public void Equals_Defaults_IsTrue()
    {
        Assert.True(default(Result<int, string>).Equals(default(Result<int, string>)));
    }

    [Fact]
    public void Equals_DefaultAndValue_IsFalse()
    {
        Assert.False(default(Result<int, string>).Equals(Result<int, string>.Value(0)));
    }

    [Fact]
    public void Equals_DefaultAndError_IsFalse()
    {
        Assert.False(default(Result<int, string?>).Equals(Result<int, string?>.Error(null)));
    }

    [Fact]
    public void Equals_BoxedEqualResult_IsTrue()
    {
        object boxed = Result<int, string>.Value(5);

        Assert.True(Result<int, string>.Value(5).Equals(boxed));
    }

    [Fact]
    public void Equals_BoxedDifferentResult_IsFalse()
    {
        object otherValue = Result<int, string>.Value(6);
        object error = Result<int, string>.Error("boom");

        Assert.False(Result<int, string>.Value(5).Equals(otherValue));
        Assert.False(Result<int, string>.Value(5).Equals(error));
    }

    [Fact]
    public void Equals_OtherType_IsFalse()
    {
        Assert.False(Result<int, string>.Value(5).Equals((object)5));
    }

    [Fact]
    public void Equals_Null_IsFalse()
    {
        Assert.False(Result<int, string>.Value(5).Equals((object?)null));
    }

    [Fact]
    public void GetHashCode_EqualValues_AreEqual()
    {
        Assert.Equal(Result<int, string>.Value(5).GetHashCode(), Result<int, string>.Value(5).GetHashCode());
    }

    [Fact]
    public void GetHashCode_EqualErrors_AreEqual()
    {
        Assert.Equal(Result<int, string>.Error("boom").GetHashCode(), Result<int, string>.Error("boom").GetHashCode());
    }

    [Fact]
    public void GetHashCode_NullContents_AreEqual()
    {
        Assert.Equal(Result<string?, int>.Value(null).GetHashCode(), Result<string?, int>.Value(null).GetHashCode());
        Assert.Equal(Result<int, string?>.Error(null).GetHashCode(), Result<int, string?>.Error(null).GetHashCode());
    }

    [Fact]
    public void GetHashCode_Defaults_AreEqual()
    {
        Assert.Equal(default(Result<int, string>).GetHashCode(), default(Result<int, string>).GetHashCode());
    }
}
