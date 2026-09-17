using Microsoft.CodeAnalysis;

namespace Assured.Analyzers.Tests;

public class DelegateAllocationCaptureTests
{
    [Fact]
    public async Task NonCapturingLambda_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v => v + 1).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaReadingStaticField_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v => v + _staticField).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaReadingConstants_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M()
            {
                const int local = 2;
                return R.Map(v => v + Constant + local).ValueOr(0);
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaUsingItsOwnParameterAndLocals_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v =>
            {
                var doubled = v * 2;
                return doubled + 1;
            }).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NestedLambdaUsingItsOwnParameter_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v =>
            {
                Func<int, int> inner = w => w + 1;
                return inner(v);
            }).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task StaticLambda_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(static v => v + 1).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DelegateFromField_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(CachedStep).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task DelegateFromParameter_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(Func<int, int> f) => R.Map(f).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task CapturingLambda_PassedToUnrelatedMethod_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int Apply(Func<int, int> f) => f(1);
            int M(int k) => Apply(v => v + k + _field);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task NonDelegateArguments_AreIgnored()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.ValueOr(_field + k);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaReadingInstanceField_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v => v + _field).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LambdaWithExplicitThis_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v => v + this._field).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LambdaCallingInstanceMethod_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v => InstanceStep(v)).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task StatementLambdaCapturingThis_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v =>
            {
                var x = _field;
                return v + x;
            }).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LambdaCapturingParameter_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.Map(v => v + k).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LambdaCapturingLocal_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M()
            {
                var k = _staticField;
                return R.Map(v => v + k).ValueOr(0);
            }
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task LambdaInsideLocalFunction_CapturingItsParameter_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M()
            {
                return Inner(2);

                int Inner(int k) => R.Map(v => v + k).ValueOr(0);
            }
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task NestedLambdaCapturingThis_ReportsTheOuterOnce()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(v =>
            {
                Func<int> inner = () => _field;
                return inner();
            }).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task NestedLambdaCapturingOuterLocal_ReportsTheOuterOnce()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.Map(v =>
            {
                Func<int> inner = () => k;
                return v + inner();
            }).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task ExplicitDelegateCreation_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.Map(new Func<int, int>(v => v + k)).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task AnonymousMethod_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.Map(delegate (int v) { return v + k; }).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task ValueOrGenerate_CapturingThis_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.ValueOrGenerate(() => _field);
            """);

        Assert.Equal(DiagnosticIds.LambdaCaptures, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task Match_WithTwoCapturingLambdas_ReportsBoth()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k) => R.Match(v => v + k, e => _field);
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.LambdaCaptures, d.Id));
    }

    [Fact]
    public async Task Match_WithCaptureInErrorBranchOnly_ReportsThatLambda()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Match(v => v, e => _field);
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("e => _field", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task Diagnostic_NamesTheMemberAndThis()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Bind(v => StaticBind(v + _field)).ValueOr("").Length;
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("'Result.Bind'", diagnostic.GetMessage());
        Assert.Contains("captures 'this'", diagnostic.GetMessage());
        Assert.Equal("v => StaticBind(v + _field)", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task Diagnostic_NamesEveryCapturedVariableOnce()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int k)
            {
                var j = 1;
                return R.Map(v => v + k + j + k + _field).ValueOr(0);
            }
            """);

        Assert.Contains("captures 'k', 'j', 'this'", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task NameofOfOuterVariable_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int offset) => R.Map(static v => v + nameof(offset).Length).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaCallingStaticLocalFunction_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M()
            {
                return R.Map(v => Add(v)).ValueOr(0);

                static int Add(int x) => x + 1;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaCallingNonCapturingLocalFunction_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M()
            {
                return R.Map(v => Add(v)).ValueOr(0);

                int Add(int x) => x + 1;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task LambdaCallingCapturingLocalFunction_ReportsItsCaptures()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int offset)
            {
                return R.Map(v => Add(v)).ValueOr(0);

                int Add(int x) => x + offset;
            }
            """);

        Assert.Contains("captures 'offset'", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task LambdaCallingRecursiveLocalFunction_Terminates()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(int offset)
            {
                return R.Map(v => Down(v)).ValueOr(0);

                int Down(int x) => x <= 0 ? offset : Down(x - 1);
            }
            """);

        Assert.Contains("captures 'offset'", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task ConditionalArgument_WithCapturingLambdas_ReportsBoth()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(bool add, int offset) => R.Map<int>(add ? x => x + offset : x => x - offset).ValueOr(0);
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.LambdaCaptures, d.Id));
    }

    [Fact]
    public async Task ConditionalArgument_WithStoredDelegates_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M(bool first, Func<int, int> other) => R.Map(first ? CachedStep : other).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }
}
