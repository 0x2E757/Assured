using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Assured.Analyzers.Flow;

namespace Assured.Analyzers
{
    /// <summary>
    /// Reports <c>UnwrapValue</c> and <c>UnwrapError</c> calls on a result whose state is not proven by a check.
    /// The analysis itself lives in <see cref="ResultFlow"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class UncheckedUnwrapAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor Rule = new(
            DiagnosticIds.UncheckedUnwrap,
            "Unwrap on an unchecked Result",
            "'{0}' is called on a result that {1}; guard with HasValue()/HasError() or use TryUnwrapValue/ValueOr",
            "Correctness",
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "UnwrapValue throws when the result holds an error or is uninitialized; UnwrapError throws when it holds a value. The analyzer tracks the result's possible states through branches and assignments and reports calls that are not proven safe.",
            helpLinkUri: DiagnosticIds.HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(OnCompilationStart);
        }

        private static void OnCompilationStart(CompilationStartAnalysisContext context)
        {
            // Without a reference to the library there is nothing to analyze, and the analyzer costs nothing.
            var resultType = context.Compilation.GetTypeByMetadataName(ResultMembers.TypeName);

            if (resultType is null)
                return;

            context.RegisterOperationBlockAction(blockContext => AnalyzeBlocks(blockContext, resultType));
        }

        private static void AnalyzeBlocks(OperationBlockAnalysisContext context, INamedTypeSymbol resultType)
        {
            foreach (var block in context.OperationBlocks)
                if (ContainsUnwrap(block, resultType))
                    new ResultFlow(resultType, block, context.ReportDiagnostic, context.CancellationToken).Run(context.GetControlFlowGraph(block));
        }

        /// <summary>
        /// The only diagnostic needs an unwrap call, so a block without one is skipped before its control flow
        /// graph is built. Lambdas and local functions are part of the block's operation tree and are included.
        /// </summary>
        private static bool ContainsUnwrap(IOperation block, INamedTypeSymbol resultType)
        {
            foreach (var operation in block.DescendantsAndSelf())
                if (operation is IInvocationOperation { TargetMethod: { Name: ResultMembers.UnwrapValue or ResultMembers.UnwrapError } method } && ResultMembers.IsResultMember(method, resultType))
                    return true;

            return false;
        }
    }
}
