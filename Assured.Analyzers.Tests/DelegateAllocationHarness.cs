using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Assured.Analyzers.Tests;

/// <summary>
/// Runs <see cref="DelegateAllocationAnalyzer"/> over members placed in a fixture class that already provides a
/// result to call combinators on, and members of every kind a lambda or a method group can refer to.
/// </summary>
internal static class DelegateAllocationHarness
{
    private const string Members = """
            static readonly Result<int, string> R = Result<int, string>.Value(7);
            static readonly Func<int, int> CachedStep = v => v + 1;
            int _field = 3;
            static int _staticField = 4;
            const int Constant = 5;

            static int StaticStep(int v) => v + 1;
            int InstanceStep(int v) => v + _field;
            static Result<string, string> StaticBind(int v) => Result<string, string>.Value(v.ToString());

        """;

    /// <summary>Compiles <paramref name="members"/> into the fixture class and returns the diagnostics in source order.</summary>
    public static Task<ImmutableArray<Diagnostic>> Analyze(string members, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        return AnalyzerHarness.AnalyzeAsync("class Fixture\n{\n" + Members + members + "\n}", new DelegateAllocationAnalyzer(), languageVersion);
    }
}
