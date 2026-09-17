using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>The tracked symbol behind <paramref name="operation"/>: a local, a parameter or a field of type <c>Result</c>.</summary>
        private ISymbol? KeyOf(IOperation operation)
        {
            operation = StripImplicitConversions(operation);

            // An assignment expression such as (r = Next()) stands for the variable it has just assigned.
            if (operation is ISimpleAssignmentOperation assignment)
                operation = StripImplicitConversions(assignment.Target);

            if (!IsResult(operation.Type))
                return null;

            return Tracked(VariableOf(operation));
        }

        /// <summary>The tracked boolean local behind <paramref name="operation"/>, if any.</summary>
        private ISymbol? BoolLocalOf(IOperation operation)
        {
            return StripImplicitConversions(operation) is ILocalReferenceOperation { Type.SpecialType: SpecialType.System_Boolean } local
                ? Tracked(local.Local)
                : null;
        }

        /// <summary>
        /// Returns <c>true</c> if a write to <paramref name="target"/> certainly lands in the single variable found for
        /// it. A conditional reference may point elsewhere, including to storage that is not a variable at all.
        /// </summary>
        private static bool IsDefiniteTarget(IOperation target, Dictionary<CaptureId, List<ISymbol?>> captureTargets)
        {
            if (StripImplicitConversions(target) is not IFlowCaptureReferenceOperation reference)
                return true;

            return captureTargets.TryGetValue(reference.Id, out var captured) && captured.Count == 1;
        }

        /// <summary>
        /// The tracked variables a write to <paramref name="target"/> lands in. When the written value involves
        /// control flow, the graph refers to the target through a temporary; <paramref name="captureTargets"/> leads
        /// back to the variable, or to several when the temporary is a conditional reference.
        /// </summary>
        private List<ISymbol> TargetsOf(IOperation target, Dictionary<CaptureId, List<ISymbol?>> captureTargets)
        {
            var tracked = new List<ISymbol>();
            target = StripImplicitConversions(target);

            var isResult = IsResult(target.Type);
            var isBool = target.Type?.SpecialType == SpecialType.System_Boolean;

            if (!isResult && !isBool)
                return tracked;

            foreach (var variable in VariablesOf(target, captureTargets))
            {
                // Outcomes are kept for boolean locals only.
                if (isBool && variable is not ILocalSymbol)
                    continue;

                if (Tracked(variable) is { } kept)
                    tracked.Add(kept);
            }

            return tracked;
        }

        /// <summary>
        /// A variable is tracked unless something other than the analyzed code path can change it: a <c>ref</c> local
        /// is an alias of other storage, by-reference parameters of one type may alias each other, and the variables
        /// in <see cref="_untracked"/> are written from elsewhere.
        /// </summary>
        private ISymbol? Tracked(ISymbol? variable)
        {
            if (variable is null || _untracked.Contains(variable))
                return null;

            if (variable is ILocalSymbol { RefKind: not RefKind.None })
                return null;

            if (variable is IParameterSymbol { RefKind: not RefKind.None } parameter && MayAliasAnotherParameter(parameter))
                return null;

            return variable;
        }

        private static bool MayAliasAnotherParameter(IParameterSymbol parameter)
        {
            if (parameter.ContainingSymbol is not IMethodSymbol method)
                return false;

            foreach (var other in method.Parameters)
                if (!SymbolEqualityComparer.Default.Equals(other, parameter) && other.RefKind != RefKind.None && SymbolEqualityComparer.Default.Equals(other.Type, parameter.Type))
                    return true;

            return false;
        }

        /// <summary>The local, parameter or own field that <paramref name="operation"/> refers to, if it is one.</summary>
        private static ISymbol? VariableOf(IOperation operation)
        {
            return StripImplicitConversions(operation) switch
            {
                ILocalReferenceOperation local => local.Local,
                IParameterReferenceOperation parameter => parameter.Parameter,
                IFieldReferenceOperation { Instance: null or IInstanceReferenceOperation } field => field.Field,
                _ => null,
            };
        }

        /// <summary>The variables behind a write target, which may be a tuple of targets or a temporary of the graph.</summary>
        private static IEnumerable<ISymbol> VariablesOf(IOperation target, Dictionary<CaptureId, List<ISymbol?>>? captureTargets)
        {
            target = StripImplicitConversions(target);

            if (target is ITupleOperation tuple)
            {
                foreach (var element in tuple.Elements)
                    foreach (var variable in VariablesOf(element, captureTargets))
                        yield return variable;
            }
            else if (target is IFlowCaptureReferenceOperation reference)
            {
                if (captureTargets is not null && captureTargets.TryGetValue(reference.Id, out var captured))
                    foreach (var variable in captured)
                        if (variable is not null)
                            yield return variable;
            }
            else if (VariableOf(target) is { } single)
            {
                yield return single;
            }
        }

        /// <summary>
        /// Every variable assigned anywhere under <paramref name="root"/>, by any kind of assignment or by reference.
        /// <paramref name="captureTargets"/> is given when <paramref name="root"/> comes from a control flow graph.
        /// </summary>
        private static IEnumerable<ISymbol> WrittenSymbols(IOperation root, Dictionary<CaptureId, List<ISymbol?>>? captureTargets)
        {
            foreach (var operation in root.DescendantsAndSelf())
                foreach (var target in WriteTargetsOf(operation))
                    foreach (var variable in VariablesOf(target, captureTargets))
                        yield return variable;
        }

        /// <summary>The operands <paramref name="operation"/> itself writes to: an assignment target, or arguments passed by <c>ref</c> or <c>out</c>.</summary>
        private static IEnumerable<IOperation> WriteTargetsOf(IOperation operation)
        {
            switch (operation)
            {
                case IAssignmentOperation assignment:
                    yield return assignment.Target;
                    break;

                case IIncrementOrDecrementOperation step:
                    yield return step.Target;
                    break;

                case IArgumentOperation { Parameter: { RefKind: RefKind.Out or RefKind.Ref } } argument:
                    yield return argument.Value;
                    break;

                case IDynamicInvocationOperation dynamicCall:
                    for (var i = 0; i < dynamicCall.Arguments.Length; i++)
                        if (dynamicCall.GetArgumentRefKind(i) is RefKind.Out or RefKind.Ref)
                            yield return dynamicCall.Arguments[i];

                    break;

                case IDynamicObjectCreationOperation dynamicCreation:
                    for (var i = 0; i < dynamicCreation.Arguments.Length; i++)
                        if (dynamicCreation.GetArgumentRefKind(i) is RefKind.Out or RefKind.Ref)
                            yield return dynamicCreation.Arguments[i];

                    break;
            }
        }

        /// <summary>
        /// The variables the graph refers to through temporaries. A temporary that holds a reference is created where
        /// the variable is about to be written; one that holds a copy never appears as a write target. An entry is
        /// <c>null</c> where the temporary refers to something that is not a variable, such as an array element.
        /// </summary>
        private static Dictionary<CaptureId, List<ISymbol?>> CaptureTargetsOf(ControlFlowGraph graph)
        {
            var targets = new Dictionary<CaptureId, List<ISymbol?>>();

            foreach (var block in graph.Blocks)
            {
                foreach (var operation in block.Operations)
                {
                    if (operation is not IFlowCaptureOperation capture)
                        continue;

                    if (!targets.TryGetValue(capture.Id, out var variables))
                        targets[capture.Id] = variables = new List<ISymbol?>();

                    variables.Add(VariableOf(capture.Value));
                }
            }

            return targets;
        }

        /// <summary>Skips the implicit conversions the compiler inserts, but not user-defined operators.</summary>
        private static IOperation StripImplicitConversions(IOperation operation)
        {
            while (operation is IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion)
                operation = conversion.Operand;

            return operation;
        }

        private bool IsResult(ITypeSymbol? type)
        {
            return ResultMembers.IsResult(type, _resultType);
        }

        private bool IsResultMember(IMethodSymbol method)
        {
            return ResultMembers.IsResultMember(method, _resultType);
        }
    }
}
