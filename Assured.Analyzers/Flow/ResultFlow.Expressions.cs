using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>Possible states of the result produced by <paramref name="expression"/>.</summary>
        private ResultStates StateOf(IOperation expression, FlowState state)
        {
            switch (expression)
            {
                case IConversionOperation { OperatorMethod: { } operatorMethod } conversion when IsResultMember(operatorMethod) && conversion.Type is INamedTypeSymbol target:
                    return StateOfImplicitOperator(operatorMethod, target);

                case IConversionOperation { OperatorMethod: null } conversion:
                    return StateOf(conversion.Operand, state);

                case ISimpleAssignmentOperation assignment:
                    // The value of an assignment expression is what the target holds now; the assignment has been applied.
                    return KeyOf(assignment.Target) is { } assigned ? state.Get(assigned) : StateOf(assignment.Value, state);

                case IDefaultValueOperation:
                case IObjectCreationOperation { Arguments.Length: 0 }:
                    return ResultStates.Undefined;

                case IFlowCaptureReferenceOperation reference:
                    return state.Get(reference.Id);

                case IInvocationOperation invocation when IsResultMember(invocation.TargetMethod):
                    return StateOfMember(invocation, state);
            }

            return KeyOf(expression) is { } key ? state.Get(key) : ResultStates.Any;
        }

        /// <summary>
        /// The implicit operators from <c>TValue</c> and <c>TError</c>: the operator's parameter type tells which
        /// side of <paramref name="target"/> the result is created on.
        /// </summary>
        private static ResultStates StateOfImplicitOperator(IMethodSymbol operatorMethod, INamedTypeSymbol target)
        {
            var parameter = operatorMethod.Parameters[0].Type;

            if (SymbolEqualityComparer.Default.Equals(parameter, target.TypeArguments[0]))
                return ResultStates.Value;

            if (SymbolEqualityComparer.Default.Equals(parameter, target.TypeArguments[1]))
                return ResultStates.Error;

            return ResultStates.Any;
        }

        /// <summary>State of the result returned by a <c>Result</c> member.</summary>
        private ResultStates StateOfMember(IInvocationOperation invocation, FlowState state)
        {
            switch (invocation.TargetMethod.Name)
            {
                case ResultMembers.Value:
                    return ResultStates.Value;

                case ResultMembers.Error:
                    return ResultStates.Error;

                case ResultMembers.Map:
                case ResultMembers.MapError:
                    // Map keeps the state: a value stays a value, an error stays an error, undefined stays undefined.
                    return invocation.Instance is { } instance ? StateOf(instance, state) : ResultStates.Any;

                case ResultMembers.Bind:
                    // An error and undefined pass through; a value becomes whatever next returns, which is unknown.
                    if (invocation.Instance is null)
                        return ResultStates.Any;

                    var input = StateOf(invocation.Instance, state);
                    return (input & ResultStates.Value) != 0 ? ResultStates.Any : input;

                default:
                    return ResultStates.Any;
            }
        }
    }
}
