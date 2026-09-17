using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers
{
    /// <summary>
    /// Reports calls to <c>Result</c> members that receive a delegate allocated on every call: a lambda that
    /// captures state, or a method group the compiler does not cache.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DelegateAllocationAnalyzer : DiagnosticAnalyzer
    {
        private const string Category = "Performance";

        // The named member exists only in newer Roslyn; the numeric value is the language version itself.
        private const LanguageVersion CSharp11 = (LanguageVersion)1100;

        private static readonly DiagnosticDescriptor CapturesRule = new(
            DiagnosticIds.LambdaCaptures,
            "Lambda passed to a Result combinator captures state",
            "Lambda passed to '{0}' captures {1} and allocates on every call; use TryUnwrapValue with plain code, or pass a delegate created once",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "A lambda that uses a local, a parameter or an instance member of the enclosing code captures it. The C# compiler cannot cache such a delegate: a captured 'this' costs a delegate per call, captured locals cost a closure object and a delegate. Lambdas that capture nothing are cached and cost nothing.",
            helpLinkUri: DiagnosticIds.HelpLink);

        private static readonly DiagnosticDescriptor MethodGroupRule = new(
            DiagnosticIds.MethodGroupAllocates,
            "Method group passed to a Result combinator allocates a delegate",
            "Method group '{0}' passed to '{1}' allocates a delegate on every call; store the delegate in a static readonly field",
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: "Converting a method group to a delegate allocates. Static method groups are cached by the compiler starting with C# 11; instance method groups are never cached.",
            helpLinkUri: DiagnosticIds.HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(CapturesRule, MethodGroupRule);

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

            context.RegisterOperationAction(operationContext => AnalyzeInvocation(operationContext, resultType), OperationKind.Invocation);
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context, INamedTypeSymbol resultType)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;

            if (!ResultMembers.IsResultMember(method, resultType))
                return;

            var languageVersion = invocation.Syntax.SyntaxTree.Options is CSharpParseOptions options ? options.LanguageVersion : LanguageVersion.Default;
            var member = method.ContainingType.Name + "." + method.Name;

            // Every Result member taking a delegate is covered, whatever its name.
            foreach (var argument in invocation.Arguments)
            {
                if (argument.Parameter?.Type.TypeKind != TypeKind.Delegate)
                    continue;

                foreach (var creation in DelegateCreationsIn(argument.Value))
                {
                    switch (creation.Target)
                    {
                        case IAnonymousFunctionOperation lambda when LambdaCaptures.Of(lambda) is { Count: > 0 } captured:
                            context.ReportDiagnostic(Diagnostic.Create(CapturesRule, lambda.Syntax.GetLocation(), member, string.Join(", ", captured)));
                            break;

                        case IMethodReferenceOperation methodReference when !IsCached(creation, methodReference.Method, languageVersion):
                            context.ReportDiagnostic(Diagnostic.Create(MethodGroupRule, methodReference.Syntax.GetLocation(), methodReference.Method.Name, member));
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// The delegates created right in the argument: the argument itself, or the alternatives of a conditional,
        /// coalescing or switch expression. A delegate created earlier and stored in a field, a local or a parameter
        /// is not a delegate creation and is not reported.
        /// </summary>
        private static IEnumerable<IDelegateCreationOperation> DelegateCreationsIn(IOperation value)
        {
            switch (StripImplicitConversions(value))
            {
                case IDelegateCreationOperation creation:
                    yield return creation;
                    break;

                case IConditionalOperation conditional:
                    foreach (var creation in DelegateCreationsIn(conditional.WhenTrue))
                        yield return creation;

                    if (conditional.WhenFalse is not null)
                        foreach (var creation in DelegateCreationsIn(conditional.WhenFalse))
                            yield return creation;

                    break;

                case ICoalesceOperation coalesce:
                    foreach (var creation in DelegateCreationsIn(coalesce.Value))
                        yield return creation;

                    foreach (var creation in DelegateCreationsIn(coalesce.WhenNull))
                        yield return creation;

                    break;

                case ISwitchExpressionOperation switchExpression:
                    foreach (var arm in switchExpression.Arms)
                        foreach (var creation in DelegateCreationsIn(arm.Value))
                            yield return creation;

                    break;
            }
        }

        /// <summary>
        /// Starting with C# 11 the compiler caches the delegate it creates when converting a static method group.
        /// An explicit <c>new</c> of a delegate type, with or without the type name, is not such a conversion and allocates in every language version.
        /// </summary>
        private static bool IsCached(IDelegateCreationOperation creation, IMethodSymbol method, LanguageVersion languageVersion)
        {
            return method.IsStatic && languageVersion >= CSharp11 && creation.Syntax is not BaseObjectCreationExpressionSyntax;
        }

        private static IOperation StripImplicitConversions(IOperation operation)
        {
            while (operation is IConversionOperation { IsImplicit: true } conversion)
                operation = conversion.Operand;

            return operation;
        }
    }
}
