using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>
        /// Applies the effect of an operation to <paramref name="state"/>, walking in evaluation order: operands
        /// first, then the operation itself. Unwrap calls are checked only in the reporting phase.
        /// </summary>
        private void Visit(Step step, IOperation operation, FlowState state)
        {
            switch (operation)
            {
                case IFlowAnonymousFunctionOperation lambda:
                    // The lambda body is not part of this graph. A lambda sees the state at the point of its creation.
                    if (step.Report)
                        Run(step.Graph.GetAnonymousFunctionControlFlowGraph(lambda), state.Clone());

                    return;

                case ISimpleAssignmentOperation assignment:
                    // The target is evaluated first: its receiver and indexes run before the assigned value does.
                    Visit(step, assignment.Target, state);
                    Visit(step, assignment.Value, state);
                    Assign(step, assignment, state);
                    return;

                case IFlowCaptureOperation capture:
                    Visit(step, capture.Value, state);

                    if (IsResult(capture.Value.Type))
                        state.Set(capture.Id, StateOf(capture.Value, state));
                    else if (capture.Value.Type?.SpecialType == SpecialType.System_Boolean)
                        state.SetBool(capture.Id, Resolve(capture.Value, state));

                    return;
            }

            foreach (var child in operation.Children)
                Visit(step, child, state);

            if (step.Report && operation is IInvocationOperation invocation && IsResultMember(invocation.TargetMethod))
                CheckUnwrap(invocation, state);

            // A simple assignment is handled above; every other write leaves nothing to know about its target. For a
            // call this happens when it runs, that is after all of its arguments have been evaluated.
            if (operation is IArgumentOperation)
                return;

            foreach (var child in operation.Children)
                if (child is IArgumentOperation argument)
                    foreach (var target in WriteTargetsOf(argument))
                        Forget(step, target, state);

            foreach (var target in WriteTargetsOf(operation))
                Forget(step, target, state);
        }

        /// <summary>Records what a simple assignment puts into its target.</summary>
        private void Assign(Step step, ISimpleAssignmentOperation assignment, FlowState state)
        {
            var targets = TargetsOf(assignment.Target, step.CaptureTargets);

            // A conditional reference may be any of several variables, so none of them is known afterwards.
            if (targets.Count != 1 || !IsDefiniteTarget(assignment.Target, step.CaptureTargets))
            {
                Forget(step, assignment.Target, state);
                return;
            }

            if (IsResult(assignment.Target.Type))
                state.Assign(targets[0], StateOf(assignment.Value, state));
            else
                state.SetBool(targets[0], Resolve(assignment.Value, state));

            step.Changed(state);
        }

        /// <summary>Drops the knowledge about every tracked variable written through <paramref name="target"/>.</summary>
        private void Forget(Step step, IOperation target, FlowState state)
        {
            if (StripImplicitConversions(target) is ITupleOperation tuple)
            {
                foreach (var element in tuple.Elements)
                    Forget(step, element, state);

                return;
            }

            foreach (var variable in TargetsOf(target, step.CaptureTargets))
            {
                state.Assign(variable, ResultStates.Any);
                state.ForgetBool(variable);
            }

            step.Changed(state);
        }

        /// <summary>Reports an unwrap call unless the receiver is proven to hold what the call expects.</summary>
        private void CheckUnwrap(IInvocationOperation invocation, FlowState state)
        {
            var name = invocation.TargetMethod.Name;

            if (name != ResultMembers.UnwrapValue && name != ResultMembers.UnwrapError)
                return;

            if (invocation.Instance is null)
                return;

            var (expected, wrongContent) = name == ResultMembers.UnwrapValue
                ? (ResultStates.Value, "may hold an error")
                : (ResultStates.Error, "may hold a value");

            if (Problem(StateOf(invocation.Instance, state), expected, wrongContent) is { } problem)
                _report(Diagnostic.Create(UncheckedUnwrapAnalyzer.Rule, invocation.Syntax.GetLocation(), name, problem));
        }

        /// <summary>
        /// Why unwrapping is not proven safe, or <c>null</c> if it is. A result that may be uninitialized is accepted
        /// as long as it may also hold <paramref name="expected"/>: the check that excluded the opposite state is
        /// taken as the author's intent, and an uninitialized result is reported only where nothing else is possible.
        /// </summary>
        private static string? Problem(ResultStates states, ResultStates expected, string wrongContent)
        {
            // Unreachable path.
            if (states == ResultStates.None)
                return null;

            var opposite = expected == ResultStates.Value ? ResultStates.Error : ResultStates.Value;

            if ((states & opposite) != 0)
                return wrongContent;

            if ((states & expected) == 0)
                return "may be uninitialized (default)";

            return null;
        }
    }
}
