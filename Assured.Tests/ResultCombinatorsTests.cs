namespace Assured.Tests;

public class ResultCombinatorsTests
{
    private static readonly Func<int, int> Increment = v => v + 1;
    private static readonly Func<int, Result<int, string>> Wrap = v => Result<int, string>.Value(v);
    private static readonly Func<string, string> Same = e => e;
    private static readonly Func<int, int> Identity = v => v;
    private static readonly Func<string, int> Zero = _ => 0;

    [Fact]
    public void Match_OnValue_CallsOnValue()
    {
        var result = Result<int, string>.Value(2);

        Assert.Equal(20, result.Match(v => v * 10, _ => -1));
    }

    [Fact]
    public void Match_OnError_CallsOnError()
    {
        var result = Result<int, string>.Error("boom");

        Assert.Equal(4, result.Match(v => v * 10, e => e.Length));
    }

    [Fact]
    public void Match_OnDefault_Throws()
    {
        var result = default(Result<int, string>);

        Assert.Throws<InvalidOperationException>(() => result.Match(v => v, _ => 0));
    }

    [Fact]
    public void Match_OnNullContents_PassesNullToCallback()
    {
        Assert.True(Result<string?, int>.Value(null).Match(v => v is null, _ => false));
        Assert.True(Result<int, string?>.Error(null).Match(_ => false, e => e is null));
    }

    [Fact]
    public void Match_CallsOnlyTheSelectedCallback()
    {
        var onValueCalls = 0;
        var onErrorCalls = 0;
        int OnValue(int v) { onValueCalls++; return v; }
        int OnError(string e) { onErrorCalls++; return -1; }

        Result<int, string>.Value(5).Match(OnValue, OnError);
        Assert.Equal((1, 0), (onValueCalls, onErrorCalls));

        Result<int, string>.Error("boom").Match(OnValue, OnError);
        Assert.Equal((1, 1), (onValueCalls, onErrorCalls));

        Assert.Throws<InvalidOperationException>(() => default(Result<int, string>).Match(OnValue, OnError));
        Assert.Equal((1, 1), (onValueCalls, onErrorCalls));
    }

    [Fact]
    public void Map_OnValue_TransformsValue()
    {
        var result = Result<string, int>.Value("abc");

        Assert.Equal(Result<int, int>.Value(3), result.Map(s => s.Length));
    }

    [Fact]
    public void Map_OnNullValue_CallsMapWithNull()
    {
        var result = Result<string?, int>.Value(null);

        Assert.Equal(Result<bool, int>.Value(true), result.Map(v => v is null));
    }

    [Fact]
    public void Map_OnError_KeepsErrorWithoutCallingMap()
    {
        var result = Result<string, int>.Error(7);
        var called = false;

        var mapped = result.Map(s => { called = true; return s.Length; });

        Assert.Equal(Result<int, int>.Error(7), mapped);
        Assert.False(called);
    }

    [Fact]
    public void Map_OnDefault_StaysDefault()
    {
        var result = default(Result<string, int>);

        Assert.Equal(default, result.Map(s => s.Length));
    }

    [Fact]
    public void MapError_OnError_TransformsError()
    {
        var result = Result<int, string>.Error("boom");

        Assert.Equal(Result<int, int>.Error(4), result.MapError(e => e.Length));
    }

    [Fact]
    public void MapError_OnNullError_CallsMapWithNull()
    {
        var result = Result<int, string?>.Error(null);

        Assert.Equal(Result<int, bool>.Error(true), result.MapError(e => e is null));
    }

    [Fact]
    public void MapError_OnValue_KeepsValueWithoutCallingMap()
    {
        var result = Result<int, string>.Value(5);
        var called = false;

        var mapped = result.MapError(e => { called = true; return e.Length; });

        Assert.Equal(Result<int, int>.Value(5), mapped);
        Assert.False(called);
    }

    [Fact]
    public void MapError_OnDefault_StaysDefault()
    {
        var result = default(Result<int, string>);

        Assert.Equal(default, result.MapError(e => e.Length));
    }

    [Fact]
    public void Bind_OnValue_ReturnsNextValue()
    {
        var result = Result<int, string>.Value(2);

        Assert.Equal(Result<string, string>.Value("2"), result.Bind(v => Result<string, string>.Value(v.ToString())));
    }

    [Fact]
    public void Bind_OnValue_ReturnsNextError()
    {
        var result = Result<int, string>.Value(2);

        Assert.Equal(Result<string, string>.Error("inner"), result.Bind(_ => Result<string, string>.Error("inner")));
    }

    [Fact]
    public void Bind_OnNullValue_CallsNextWithNull()
    {
        var result = Result<string?, int>.Value(null);

        Assert.Equal(Result<bool, int>.Value(true), result.Bind(v => Result<bool, int>.Value(v is null)));
    }

    [Fact]
    public void Bind_OnError_KeepsErrorWithoutCallingNext()
    {
        var result = Result<int, string>.Error("outer");
        var called = false;

        var bound = result.Bind(_ => { called = true; return Result<string, string>.Value("x"); });

        Assert.Equal(Result<string, string>.Error("outer"), bound);
        Assert.False(called);
    }

    [Fact]
    public void Bind_OnDefault_StaysDefault()
    {
        var result = default(Result<int, string>);

        Assert.Equal(default, result.Bind(_ => Result<string, string>.Value("x")));
    }

    [Fact]
    public void Bind_Chain_StopsAtFirstError()
    {
        var log = new List<string>();

        Result<int, string> Step(string name, int value, bool fail)
        {
            log.Add(name);
            return fail ? name : value + 1;
        }

        var result = Step("a", 0, false)
            .Bind(v => Step("b", v, true))
            .Bind(v => Step("c", v, false));

        Assert.Equal("b", result.UnwrapError());
        Assert.Equal(["a", "b"], log);
    }

    [Fact]
    public void Combinators_DoNotAllocate()
    {
        var result = Result<int, string>.Value(7);
        var sum = 0;

        // Delegates are created once above, so the loop measures only the library's code path.
        // Warm up so that tiered compilation is already in place.
        for (var i = 0; i < 1000; i++)
            sum += result.Map(Increment).MapError(Same).Bind(Wrap).Match(Identity, Zero);

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100_000; i++)
            sum += result.Map(Increment).MapError(Same).Bind(Wrap).Match(Identity, Zero);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Any allocation on the hot path costs at least 24 bytes per iteration; a small constant is runtime noise.
        Assert.NotEqual(0, sum);
        Assert.True(allocated < 100_000, $"Allocated {allocated} bytes");
    }
}
