using Microsoft.CodeAnalysis.CSharp;

namespace Assured.Analyzers.Tests;

public class DelegateAllocationMethodGroupTests
{
    [Fact]
    public async Task StaticMethodGroup_CSharp9_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(StaticStep).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task StaticMethodGroup_CSharp10_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(StaticStep).ValueOr(0);
            """, LanguageVersion.CSharp10);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task StaticMethodGroup_CSharp11_IsClean()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(StaticStep).ValueOr(0);
            """, LanguageVersion.CSharp11);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task InstanceMethodGroup_CSharp9_Reports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(InstanceStep).ValueOr(0);
            """);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task InstanceMethodGroup_CSharp11_StillReports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(InstanceStep).ValueOr(0);
            """, LanguageVersion.CSharp11);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task ChainWithTwoMethodGroups_ReportsBoth()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            Result<string, string> M() => R.Bind(StaticBind).MapError(StaticBindError);
            static string StaticBindError(string e) => e;
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticIds.MethodGroupAllocates, d.Id));
    }

    [Fact]
    public async Task MethodGroupAndCapturingLambda_ReportsEach()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Match(StaticStep, e => _field);
            """);

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal(DiagnosticIds.MethodGroupAllocates, diagnostics[0].Id);
        Assert.Equal(DiagnosticIds.LambdaCaptures, diagnostics[1].Id);
    }

    [Fact]
    public async Task Diagnostic_PointsAtTheMethodGroupAndNamesIt()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(InstanceStep).ValueOr(0);
            """);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("InstanceStep", diagnostic.Location.SourceTree!.GetText().ToString(diagnostic.Location.SourceSpan));
        Assert.Contains("'InstanceStep'", diagnostic.GetMessage());
        Assert.Contains("'Result.Map'", diagnostic.GetMessage());
        Assert.Contains("static readonly", diagnostic.GetMessage());
    }

    [Fact]
    public async Task WithoutLibraryReference_ReportsNothing()
    {
        var diagnostics = await AnalyzerHarness.AnalyzeWithoutLibraryAsync("""
            class C
            {
                int _f;
                int M(System.Func<int, int> f) => f(_f);
            }
            """, new DelegateAllocationAnalyzer());

        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task ExplicitDelegateCreation_CSharp11_StillReports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map(new Func<int, int>(StaticStep)).ValueOr(0);
            """, LanguageVersion.CSharp11);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }

    [Fact]
    public async Task TargetTypedDelegateCreation_CSharp11_StillReports()
    {
        var diagnostics = await DelegateAllocationHarness.Analyze("""
            int M() => R.Map<int>(new(StaticStep)).ValueOr(0);
            """, LanguageVersion.CSharp11);

        Assert.Equal(DiagnosticIds.MethodGroupAllocates, Assert.Single(diagnostics).Id);
    }
}
