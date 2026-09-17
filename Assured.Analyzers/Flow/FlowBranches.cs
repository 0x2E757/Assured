using Microsoft.CodeAnalysis;

namespace Assured.Analyzers.Flow
{
    /// <summary>
    /// Program states after a boolean expression turned out true or false. A <c>null</c> side means that outcome
    /// is impossible given the current knowledge. Instances are never mutated once created.
    /// </summary>
    internal sealed class FlowBranches
    {
        /// <summary>State on the path where the expression is true, or <c>null</c> if it cannot be true.</summary>
        public FlowState? WhenTrue { get; }

        /// <summary>State on the path where the expression is false, or <c>null</c> if it cannot be false.</summary>
        public FlowState? WhenFalse { get; }

        public FlowBranches(FlowState? whenTrue, FlowState? whenFalse)
        {
            WhenTrue = whenTrue;
            WhenFalse = whenFalse;
        }

        /// <summary>Outcomes of an expression that tells nothing about results: both paths keep the state.</summary>
        public static FlowBranches Unknown(FlowState state)
        {
            return new(state, state);
        }

        /// <summary>Outcomes of a constant expression: only one path is possible.</summary>
        public static FlowBranches Constant(bool value, FlowState state)
        {
            return value ? new(state, null) : new(null, state);
        }

        /// <summary>Outcomes of the negated expression.</summary>
        public FlowBranches Swap()
        {
            return new(WhenFalse, WhenTrue);
        }

        /// <summary>The same outcomes with only the knowledge about results, the form in which they are stored.</summary>
        public FlowBranches ResultsOnly()
        {
            return new(WhenTrue?.ResultsOnly(), WhenFalse?.ResultsOnly());
        }

        /// <summary>The same outcomes without what they claim about <paramref name="key"/>.</summary>
        public FlowBranches Without(ISymbol key)
        {
            return new(WhenTrue?.Without(key), WhenFalse?.Without(key));
        }

        /// <summary>Returns <c>true</c> if either outcome claims something about <paramref name="key"/>.</summary>
        public bool Mentions(ISymbol key)
        {
            return WhenTrue?.Mentions(key) == true || WhenFalse?.Mentions(key) == true;
        }

        /// <summary>Joins the outcomes of two paths side by side.</summary>
        public static FlowBranches Merge(FlowBranches first, FlowBranches second)
        {
            return new(Merge(first.WhenTrue, second.WhenTrue), Merge(first.WhenFalse, second.WhenFalse));
        }

        /// <summary>Returns <c>true</c> if both outcomes carry exactly the same knowledge.</summary>
        public static bool Same(FlowBranches first, FlowBranches second)
        {
            return Same(first.WhenTrue, second.WhenTrue) && Same(first.WhenFalse, second.WhenFalse);
        }

        private static FlowState? Merge(FlowState? first, FlowState? second)
        {
            if (first is null)
                return second;

            if (second is null)
                return first;

            return FlowState.Merge(first, second);
        }

        private static bool Same(FlowState? first, FlowState? second)
        {
            if (first is null)
                return second is null;

            return second is not null && first.SameAs(second);
        }
    }
}
