using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Assured.Analyzers.Flow
{
    /// <summary>
    /// Data-flow analysis of one operation block. A <see cref="FlowState"/> is pushed along the edges of the
    /// control flow graph until the states at block entries stop changing; then one more pass over the converged
    /// states reports the unwrap calls that are not proven safe.
    /// </summary>
    /// <remarks>
    /// Branches on <c>HasValue</c>, <c>HasError</c> and <c>TryUnwrap*</c> narrow the possible states of a result,
    /// assignments replace them, join points take the union, and loops iterate to a fixed point. Boolean temporaries
    /// and locals computed from such checks keep both outcomes, so the knowledge survives <c>a &amp;&amp; b</c> inside a
    /// conditional expression and <c>var ok = r.HasValue(); if (ok) ...</c>.
    /// </remarks>
    internal sealed partial class ResultFlow
    {
        private readonly INamedTypeSymbol _resultType;
        private readonly HashSet<ISymbol> _untracked;
        private readonly Action<Diagnostic> _report;
        private readonly CancellationToken _cancellationToken;

        public ResultFlow(INamedTypeSymbol resultType, IOperation block, Action<Diagnostic> report, CancellationToken cancellationToken)
        {
            _resultType = resultType;
            _untracked = UntrackedIn(block);
            _report = report;
            _cancellationToken = cancellationToken;
        }

        /// <summary>Analyzes the control flow graph of the block and reports diagnostics.</summary>
        public void Run(ControlFlowGraph graph)
        {
            Run(graph, new FlowState());
        }

        private void Run(ControlFlowGraph graph, FlowState entryState)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var captureTargets = CaptureTargetsOf(graph);
            var worklist = new Worklist(graph, captureTargets);
            worklist.Propagate(graph.Blocks[0], entryState);

            // Phase 1: fixed point over the states at block entry.
            while (worklist.TryDequeue(out var block))
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var (fallThrough, conditional) = Transfer(new Step(graph, block, captureTargets, report: false, worklist), worklist.EntryStateOf(block)!);
                worklist.Propagate(block.FallThroughSuccessor, fallThrough);
                worklist.Propagate(block.ConditionalSuccessor, conditional);
            }

            // Phase 2: one pass over the converged states, reporting diagnostics.
            foreach (var block in graph.Blocks)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (worklist.EntryStateOf(block) is { } state)
                    Transfer(new Step(graph, block, captureTargets, report: true, worklist: null), state);
            }

            // A local function may be called from anywhere: nothing is known about the variables it captures.
            foreach (var localFunction in graph.LocalFunctions)
                Run(graph.GetLocalFunctionControlFlowGraph(localFunction), new FlowState());
        }

        /// <summary>Pushes the state through a block and returns the states for its two outgoing edges.</summary>
        private (FlowState? FallThrough, FlowState? Conditional) Transfer(Step step, FlowState entryState)
        {
            var block = step.Block;
            var state = entryState.Clone();
            step.Changed(state);

            foreach (var operation in block.Operations)
                Visit(step, operation, state);

            if (block.BranchValue is null)
                return (state, null);

            Visit(step, block.BranchValue, state);

            if (block.ConditionKind == ControlFlowConditionKind.None)
                return (state, null);

            var outcomes = Resolve(block.BranchValue, state);
            var whenTrue = Apply(state, outcomes.WhenTrue);
            var whenFalse = Apply(state, outcomes.WhenFalse);

            // The conditional successor is taken when the condition matches ConditionKind, otherwise fall-through.
            return block.ConditionKind == ControlFlowConditionKind.WhenTrue
                ? (whenFalse, whenTrue)
                : (whenTrue, whenFalse);
        }

        /// <summary>The state on a path where an outcome holds, or <c>null</c> if that path is unreachable.</summary>
        private static FlowState? Apply(FlowState state, FlowState? outcome)
        {
            return outcome is null ? null : state.Intersect(outcome);
        }
    }
}
