using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>One block being pushed through in one phase of the analysis.</summary>
        private readonly struct Step
        {
            public ControlFlowGraph Graph { get; }

            public BasicBlock Block { get; }

            /// <summary>The variables the graph refers to through temporaries.</summary>
            public Dictionary<CaptureId, List<ISymbol?>> CaptureTargets { get; }

            /// <summary>Set in the reporting phase; while the fixed point is computed nothing is reported.</summary>
            public bool Report { get; }

            private readonly Worklist? _worklist;

            public Step(ControlFlowGraph graph, BasicBlock block, Dictionary<CaptureId, List<ISymbol?>> captureTargets, bool report, Worklist? worklist)
            {
                Graph = graph;
                Block = block;
                CaptureTargets = captureTargets;
                Report = report;
                _worklist = worklist;
            }

            /// <summary>
            /// Called whenever the knowledge changes. An exception may leave the block at any point, so while the
            /// fixed point is computed every state the block passes through reaches the exception handlers.
            /// </summary>
            public void Changed(FlowState state)
            {
                _worklist?.PropagateToHandlers(Block, state);
            }
        }
    }
}
