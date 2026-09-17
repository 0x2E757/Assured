using Microsoft.CodeAnalysis;

namespace Assured.Analyzers.Tests;

public class UncheckedUnwrapAnalyzerTests
{
    [Fact]
    public async Task UnwrapValue_OnUnknownLocal_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.UnwrapValue();
            }
            """);

        Assert.Contains("may hold an error", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task UnwrapValue_OnCallResult_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x) => Load(x).UnwrapValue();
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task UnwrapValue_OnParameter_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(Result<int, string> r) => r.UnwrapValue();
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task UnwrapValue_OnField_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M() => _field.UnwrapValue();
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task UnwrapError_OnUnknown_Reports()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            string M(int x) => Load(x).UnwrapError();
            """);

        Assert.Contains("may hold a value", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task UnwrapValue_OnDefault_ReportsUninitialized()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                Result<int, string> r = default;
                return r.UnwrapValue();
            }
            """);

        Assert.Contains("uninitialized", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task UnwrapValue_OnParameterlessConstructor_ReportsUninitialized()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M()
            {
                var r = new Result<int, string>();
                return r.UnwrapValue();
            }
            """);

        Assert.Contains("uninitialized", Assert.Single(diagnostics).GetMessage());
    }

    [Fact]
    public async Task TwoUncheckedCalls_ReportsBothInSourceOrder()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.UnwrapValue() + Other().UnwrapError().Length;
            }
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.Contains("'UnwrapValue'", diagnostics[0].GetMessage());
        Assert.Contains("'UnwrapError'", diagnostics[1].GetMessage());
    }

    [Fact]
    public async Task Diagnostic_HasIdSeverityAndLocationOfTheCall()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x)
            {
                var r = Load(x);
                return r.UnwrapValue() + 1;
            }
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticIds.UncheckedUnwrap, diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Equal("r.UnwrapValue()", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public async Task ExpressionBodiedProperty_IsAnalyzed()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int P => _field.UnwrapValue();
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task FieldInitializer_IsAnalyzed()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int _initialized = Other().UnwrapValue();
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task Constructor_IsAnalyzed()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            Fixture()
            {
                _field = Other();
                Console.WriteLine(_field.UnwrapValue());
            }
            """);

        Assert.Single(diagnostics);
    }

    [Fact]
    public async Task GenericResult_IsTracked()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            static T M<T>(Result<T, string> r) => r.HasValue() ? r.UnwrapValue() : default!;
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WithoutUnwrapCalls_ReportsNothing()
    {
        var diagnostics = await UncheckedUnwrapHarness.Analyze("""
            int M(int x) => Load(x).Map(v => v + 1).ValueOr(0);
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task WithoutLibraryReference_ReportsNothing()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeWithoutLibraryAsync("""
            class C
            {
                int M(int x) => x;
            }
            """, new UncheckedUnwrapAnalyzer());

        Assert.Empty(diagnostics);
    }
}
