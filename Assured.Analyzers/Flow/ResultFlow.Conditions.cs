using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>
        /// Program states after <paramref name="condition"/> turned out true or false. The returned states are
        /// snapshots to be combined with the current state through <see cref="FlowState.Intersect"/>.
        /// </summary>
        private FlowBranches Resolve(IOperation condition, FlowState state)
        {
            switch (StripImplicitConversions(condition))
            {
                case ILiteralOperation { ConstantValue: { HasValue: true, Value: bool literal } }:
                    return FlowBranches.Constant(literal, state);

                case ISimpleAssignmentOperation assignment:
                    // The value of an assignment expression is what the target holds now; the assignment has been applied,
                    // so resolving the assigned expression again would read the target after the change.
                    return BoolLocalOf(assignment.Target) is { } assigned && state.GetBool(assigned) is { } stored
                        ? stored
                        : Resolve(assignment.Value, state);

                case IUnaryOperation { OperatorKind: UnaryOperatorKind.Not } not:
                    return Resolve(not.Operand, state).Swap();

                case IFlowCaptureReferenceOperation reference when state.GetBool(reference.Id) is { } captured:
                    return captured;

                case ILocalReferenceOperation local when state.GetBool(local.Local) is { } known:
                    return known;

                case IBinaryOperation { OperatorKind: BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals } comparison:
                    return ResolveComparison(comparison, state);

                case IIsPatternOperation { Pattern: IConstantPatternOperation constant } pattern when BoolLiteral(constant.Value) is { } literal:
                    return literal ? Resolve(pattern.Value, state) : Resolve(pattern.Value, state).Swap();

                case IIsPatternOperation { Pattern: INegatedPatternOperation { Pattern: IConstantPatternOperation constant } } pattern when BoolLiteral(constant.Value) is { } literal:
                    return literal ? Resolve(pattern.Value, state).Swap() : Resolve(pattern.Value, state);

                case IInvocationOperation invocation when IsResultMember(invocation.TargetMethod) && invocation.Instance is { } instance && KeyOf(instance) is { } key:
                    return ResolveCheck(invocation.TargetMethod.Name, key, state);
            }

            return FlowBranches.Unknown(state);
        }

        /// <summary>Outcomes of <c>x == true</c>, <c>x != false</c> and the like; anything else tells nothing.</summary>
        private FlowBranches ResolveComparison(IBinaryOperation comparison, FlowState state)
        {
            IOperation operand;
            bool literal;

            if (BoolLiteral(comparison.RightOperand) is { } right)
            {
                operand = comparison.LeftOperand;
                literal = right;
            }
            else if (BoolLiteral(comparison.LeftOperand) is { } left)
            {
                operand = comparison.RightOperand;
                literal = left;
            }
            else
            {
                return FlowBranches.Unknown(state);
            }

            var inner = Resolve(operand, state);
            var flip = !literal ^ (comparison.OperatorKind == BinaryOperatorKind.NotEquals);
            return flip ? inner.Swap() : inner;
        }

        /// <summary>Outcomes of a state check on a tracked result; a method that is not a check tells nothing.</summary>
        private static FlowBranches ResolveCheck(string method, ISymbol key, FlowState state)
        {
            var current = state.Get(key);

            switch (method)
            {
                case ResultMembers.HasValue:
                case ResultMembers.TryUnwrapValue:
                    return new FlowBranches(
                        Narrowed(state, key, current & ResultStates.Value),
                        Narrowed(state, key, current & (ResultStates.Error | ResultStates.Undefined)));

                case ResultMembers.HasError:
                case ResultMembers.TryUnwrapError:
                    return new FlowBranches(
                        Narrowed(state, key, current & ResultStates.Error),
                        Narrowed(state, key, current & (ResultStates.Value | ResultStates.Undefined)));

                default:
                    return FlowBranches.Unknown(state);
            }
        }

        /// <summary>A copy of <paramref name="state"/> where <paramref name="key"/> is narrowed to <paramref name="states"/>, or <c>null</c> if that leaves nothing.</summary>
        private static FlowState? Narrowed(FlowState state, ISymbol key, ResultStates states)
        {
            // This outcome is impossible given the current knowledge.
            if (states == ResultStates.None)
                return null;

            var narrowed = state.Clone();
            narrowed.Narrow(key, states);
            return narrowed;
        }

        private static bool? BoolLiteral(IOperation operation)
        {
            return StripImplicitConversions(operation) is ILiteralOperation { ConstantValue: { HasValue: true, Value: bool literal } }
                ? literal
                : null;
        }
    }
}
