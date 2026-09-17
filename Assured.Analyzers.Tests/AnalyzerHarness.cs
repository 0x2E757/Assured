using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Assured.Analyzers.Tests;

/// <summary>
/// Compiles a consumer snippet against the real Assured assembly and runs one analyzer over it. A snippet that
/// does not compile, or an analyzer that throws, fails the test with the compiler's or the analyzer's own message.
/// </summary>
internal static class AnalyzerHarness
{
    private const string Prelude = """
        using System;
        using System.Collections.Generic;
        using Assured;

        """;

    /// <summary>Diagnostic id the compiler uses to report an analyzer that threw.</summary>
    private const string AnalyzerCrash = "AD0001";

    private static readonly ImmutableArray<MetadataReference> FrameworkReferences = BuildFrameworkReferences();
    private static readonly ImmutableArray<MetadataReference> References = FrameworkReferences.Add(MetadataReference.CreateFromFile(typeof(Result<,>).Assembly.Location));

    /// <summary>Runs <paramref name="analyzer"/> over <paramref name="source"/> compiled with the library referenced.</summary>
    public static Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnalyzer analyzer, LanguageVersion languageVersion = LanguageVersion.CSharp9)
    {
        return AnalyzeAsync(source, analyzer, References, languageVersion);
    }

    /// <summary>Runs <paramref name="analyzer"/> over <paramref name="source"/> compiled without the library.</summary>
    public static Task<ImmutableArray<Diagnostic>> AnalyzeWithoutLibraryAsync(string source, DiagnosticAnalyzer analyzer)
    {
        return AnalyzeAsync(source, analyzer, FrameworkReferences, LanguageVersion.CSharp9);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, DiagnosticAnalyzer analyzer, ImmutableArray<MetadataReference> references, LanguageVersion languageVersion)
    {
        var tree = CSharpSyntaxTree.ParseText(Prelude + source, new CSharpParseOptions(languageVersion));
        var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable);
        var compilation = CSharpCompilation.Create("Consumer", [tree], references, options);

        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

        if (errors.Count > 0)
            throw new InvalidOperationException("The snippet does not compile:\n" + string.Join("\n", errors));

        var diagnostics = await compilation.WithAnalyzers([analyzer]).GetAnalyzerDiagnosticsAsync();

        if (diagnostics.FirstOrDefault(d => d.Id == AnalyzerCrash) is { } crash)
            throw new InvalidOperationException("The analyzer threw:\n" + crash.GetMessage());

        // The compiler reports in no particular order; tests that expect several diagnostics rely on source order.
        return diagnostics.Sort((a, b) => a.Location.SourceSpan.Start.CompareTo(b.Location.SourceSpan.Start));
    }

    private static ImmutableArray<MetadataReference> BuildFrameworkReferences()
    {
        return ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .ToImmutableArray();
    }
}
