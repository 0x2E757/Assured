using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Assured.Analyzers.Tests;

/// <summary>
/// Runs <see cref="UncheckedUnwrapAnalyzer"/> over members placed in a fixture class that already provides
/// results in every state the tests need: <c>Load</c> is unknown, <c>Other</c> is an error, and so on.
/// </summary>
internal static class UncheckedUnwrapHarness
{
    private const string Members = """
            Result<int, string> _field;
            static Result<int, string> _staticField;
            Result<int, string> Prop => Other();

            static Result<int, string> Load(int x) => x > 0 ? x : "neg";
            static Result<int, string> Other() => "other";
            static Result<string, string> Next(int v) => Result<string, string>.Value(v.ToString());
            static void Fill(out Result<int, string> r) => r = Other();
            static void Touch(ref Result<int, string> r) => r = Other();
            static void TouchAndTake(ref Result<int, string> r, Result<int, string> ignored) => r = Other();
            static void TouchAndTake(ref Result<int, string> r, int ignored) => r = Other();
            static void Flip(ref bool b) => b = !b;
            static int Fail(Result<int, string> ignored) => throw new Exception();
            static bool Coin() => DateTime.Now.Ticks % 2 == 0;
            static void Work() { }

        """;

    /// <summary>Compiles <paramref name="members"/> into the fixture class and returns the diagnostics in source order.</summary>
    public static Task<ImmutableArray<Diagnostic>> Analyze(string members)
    {
        return AnalyzerHarness.AnalyzeAsync("class Fixture\n{\n" + Members + members + "\n}", new UncheckedUnwrapAnalyzer());
    }
}
