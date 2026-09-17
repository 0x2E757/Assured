using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>
        /// Bookkeeping of one fixed-point computation: the state known at the entry of every block, the blocks
        /// whose entry state changed and must be visited again, and the handlers an exception can reach from
        /// each block.
        /// </summary>
        private sealed class Worklist
        {
            private readonly ControlFlowGraph _graph;
            private readonly Dictionary<CaptureId, List<ISymbol?>> _captureTargets;
            private readonly FlowState?[] _entryStates;
            private readonly List<int>?[] _handlers;
            private readonly Queue<int> _pending = new();
            private readonly bool[] _isPending;
            private readonly Dictionary<ControlFlowRegion, List<ISymbol>> _writtenInFinally = new();

            public Worklist(ControlFlowGraph graph, Dictionary<CaptureId, List<ISymbol?>> captureTargets)
            {
                _graph = graph;
                _captureTargets = captureTargets;
                _entryStates = new FlowState?[graph.Blocks.Length];
                _isPending = new bool[graph.Blocks.Length];
                _handlers = HandlersOf(graph);
            }

            /// <summary>State at the entry of <paramref name="block"/>, or <c>null</c> if no path reaches it.</summary>
            public FlowState? EntryStateOf(BasicBlock block)
            {
                return _entryStates[block.Ordinal];
            }

            /// <summary>Takes the next block to visit, if any is left.</summary>
            public bool TryDequeue(out BasicBlock block)
            {
                if (_pending.Count == 0)
                {
                    block = null!;
                    return false;
                }

                block = _graph.Blocks[_pending.Dequeue()];
                _isPending[block.Ordinal] = false;
                return true;
            }

            /// <summary>
            /// Propagates <paramref name="state"/> along <paramref name="branch"/>. The graph leads the branch past
            /// the finally regions it leaves, so what those regions assign is forgotten on the way.
            /// </summary>
            public void Propagate(ControlFlowBranch? branch, FlowState? state)
            {
                if (branch?.Destination is null || state is null)
                    return;

                foreach (var region in branch.FinallyRegions)
                {
                    foreach (var written in WrittenInFinally(region))
                    {
                        state.Assign(written, ResultStates.Any);
                        state.ForgetBool(written);
                    }
                }

                Propagate(branch.Destination, state);
            }

            /// <summary>Merges <paramref name="state"/> into the entry of <paramref name="target"/> and schedules it if anything changed.</summary>
            public void Propagate(BasicBlock target, FlowState state)
            {
                var existing = _entryStates[target.Ordinal];
                var merged = existing is null ? state : FlowState.Merge(existing, state);

                if (existing is not null && existing.SameAs(merged))
                    return;

                _entryStates[target.Ordinal] = merged;

                if (_isPending[target.Ordinal])
                    return;

                _isPending[target.Ordinal] = true;
                _pending.Enqueue(target.Ordinal);
            }

            /// <summary>
            /// Propagates <paramref name="state"/> to every handler an exception thrown in <paramref name="block"/>
            /// can reach. The graph has no edges for exceptions, so this stands in for them.
            /// </summary>
            public void PropagateToHandlers(BasicBlock block, FlowState state)
            {
                if (_handlers[block.Ordinal] is not { } handlers)
                    return;

                foreach (var handler in handlers)
                    Propagate(_graph.Blocks[handler], state.Clone());
            }

            private List<ISymbol> WrittenInFinally(ControlFlowRegion region)
            {
                if (_writtenInFinally.TryGetValue(region, out var written))
                    return written;

                written = new List<ISymbol>();

                for (var ordinal = region.FirstBlockOrdinal; ordinal <= region.LastBlockOrdinal; ordinal++)
                {
                    var block = _graph.Blocks[ordinal];

                    foreach (var operation in block.Operations)
                        written.AddRange(WrittenSymbols(operation, _captureTargets));

                    if (block.BranchValue is not null)
                        written.AddRange(WrittenSymbols(block.BranchValue, _captureTargets));
                }

                _writtenInFinally[region] = written;
                return written;
            }

            /// <summary>
            /// For every block, the entry blocks of the catch, filter and finally regions that an exception thrown in
            /// it can reach, through every region that encloses the block.
            /// </summary>
            private static List<int>?[] HandlersOf(ControlFlowGraph graph)
            {
                var handlers = new List<int>?[graph.Blocks.Length];

                foreach (var block in graph.Blocks)
                {
                    for (var region = block.EnclosingRegion; region is not null; region = region.EnclosingRegion)
                    {
                        foreach (var handler in HandlersReachedFrom(region))
                        {
                            handlers[block.Ordinal] ??= new List<int>();
                            handlers[block.Ordinal]!.Add(handler.FirstBlockOrdinal);
                        }
                    }
                }

                return handlers;
            }

            /// <summary>
            /// The handler regions an exception leaving <paramref name="region"/> goes to next: from the try part, all
            /// of its handlers; from a filter, which rejects the exception by throwing or returning false, the
            /// handlers that follow it.
            /// </summary>
            private static IEnumerable<ControlFlowRegion> HandlersReachedFrom(ControlFlowRegion region)
            {
                if (region.Kind == ControlFlowRegionKind.Try)
                {
                    foreach (var sibling in region.EnclosingRegion!.NestedRegions)
                        if (sibling != region)
                            yield return sibling;
                }
                else if (region.Kind == ControlFlowRegionKind.Filter)
                {
                    var filterAndHandler = region.EnclosingRegion!;
                    var isAfter = false;

                    foreach (var sibling in filterAndHandler.EnclosingRegion!.NestedRegions)
                    {
                        if (isAfter)
                            yield return sibling;
                        else if (sibling == filterAndHandler)
                            isAfter = true;
                    }
                }
            }
        }
    }
}
