using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace Assured.Analyzers.Flow
{
    /// <summary>
    /// Knowledge at a program point: the possible states of every tracked result, and the outcomes of boolean
    /// temporaries and locals that were computed from such states. A missing key means nothing is known.
    /// </summary>
    internal sealed class FlowState
    {
        private readonly Dictionary<ISymbol, ResultStates> _results = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<CaptureId, ResultStates> _resultCaptures = new();

        private readonly Dictionary<ISymbol, FlowBranches> _boolLocals = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<CaptureId, FlowBranches> _boolCaptures = new();

        /// <summary>Possible states of the result held by a local, parameter or field.</summary>
        public ResultStates Get(ISymbol key)
        {
            return _results.TryGetValue(key, out var states) ? states : ResultStates.Any;
        }

        /// <summary>Possible states of the result held by a temporary of the control flow graph.</summary>
        public ResultStates Get(CaptureId id)
        {
            return _resultCaptures.TryGetValue(id, out var states) ? states : ResultStates.Any;
        }

        /// <summary>Outcomes of a boolean local, or <c>null</c> if it was not computed from a result.</summary>
        public FlowBranches? GetBool(ISymbol local)
        {
            return _boolLocals.TryGetValue(local, out var branches) ? branches : null;
        }

        /// <summary>Outcomes of a boolean temporary, or <c>null</c> if it was not computed from a result.</summary>
        public FlowBranches? GetBool(CaptureId id)
        {
            return _boolCaptures.TryGetValue(id, out var branches) ? branches : null;
        }

        /// <summary>
        /// Records a new value of a local, parameter or field. Outcomes that were computed from the old value no
        /// longer hold and are forgotten.
        /// </summary>
        public void Assign(ISymbol key, ResultStates states)
        {
            SetResult(key, states);
            Forget(_boolLocals, key);
            Forget(_boolCaptures, key);
        }

        /// <summary>Narrows the possible states of a variable on one path; outcomes computed earlier stay valid.</summary>
        public void Narrow(ISymbol key, ResultStates states)
        {
            SetResult(key, states);
        }

        /// <summary>Records the possible states of a temporary.</summary>
        public void Set(CaptureId id, ResultStates states)
        {
            if (states == ResultStates.Any)
                _resultCaptures.Remove(id);
            else
                _resultCaptures[id] = states;
        }

        /// <summary>Records the outcomes of a boolean local.</summary>
        public void SetBool(ISymbol local, FlowBranches outcomes)
        {
            _boolLocals[local] = outcomes.ResultsOnly();
        }

        /// <summary>Drops the outcomes of a boolean local whose value changed in a way the analysis does not follow.</summary>
        public void ForgetBool(ISymbol local)
        {
            _boolLocals.Remove(local);
        }

        /// <summary>Records the outcomes of a boolean temporary.</summary>
        public void SetBool(CaptureId id, FlowBranches outcomes)
        {
            _boolCaptures[id] = outcomes.ResultsOnly();
        }

        /// <summary>Returns an independent copy.</summary>
        public FlowState Clone()
        {
            var clone = ResultsOnly();

            foreach (var pair in _boolLocals)
                clone._boolLocals[pair.Key] = pair.Value;

            foreach (var pair in _boolCaptures)
                clone._boolCaptures[pair.Key] = pair.Value;

            return clone;
        }

        /// <summary>
        /// A copy that keeps only the knowledge about results. Outcomes are stored in this form, so they never nest
        /// and the analysis stays bounded.
        /// </summary>
        public FlowState ResultsOnly()
        {
            var copy = new FlowState();

            foreach (var pair in _results)
                copy._results[pair.Key] = pair.Value;

            foreach (var pair in _resultCaptures)
                copy._resultCaptures[pair.Key] = pair.Value;

            return copy;
        }

        /// <summary>
        /// Combines the current knowledge with a snapshot taken earlier on the same path: the possible states of
        /// every result intersect. Returns <c>null</c> if some result is left with no possible state, which means
        /// the path is unreachable.
        /// </summary>
        public FlowState? Intersect(FlowState snapshot)
        {
            var combined = Clone();

            foreach (var pair in snapshot._results)
            {
                var states = combined.Get(pair.Key) & pair.Value;

                if (states == ResultStates.None)
                    return null;

                combined.SetResult(pair.Key, states);
            }

            foreach (var pair in snapshot._resultCaptures)
            {
                var states = combined.Get(pair.Key) & pair.Value;

                if (states == ResultStates.None)
                    return null;

                combined.Set(pair.Key, states);
            }

            return combined;
        }

        /// <summary>
        /// Joins two paths: the possible states add up, and knowledge survives only where both paths have it.
        /// </summary>
        public static FlowState Merge(FlowState first, FlowState second)
        {
            var merged = new FlowState();

            foreach (var pair in first._results)
                if (second._results.TryGetValue(pair.Key, out var other))
                    merged.SetResult(pair.Key, pair.Value | other);

            foreach (var pair in first._resultCaptures)
                if (second._resultCaptures.TryGetValue(pair.Key, out var other))
                    merged.Set(pair.Key, pair.Value | other);

            foreach (var pair in first._boolLocals)
                if (second._boolLocals.TryGetValue(pair.Key, out var other))
                    merged._boolLocals[pair.Key] = FlowBranches.Merge(pair.Value, other);

            foreach (var pair in first._boolCaptures)
                if (second._boolCaptures.TryGetValue(pair.Key, out var other))
                    merged._boolCaptures[pair.Key] = FlowBranches.Merge(pair.Value, other);

            return merged;
        }

        /// <summary>Returns <c>true</c> if both states carry exactly the same knowledge.</summary>
        public bool SameAs(FlowState other)
        {
            return Same(_results, other._results, (x, y) => x == y)
                && Same(_resultCaptures, other._resultCaptures, (x, y) => x == y)
                && Same(_boolLocals, other._boolLocals, FlowBranches.Same)
                && Same(_boolCaptures, other._boolCaptures, FlowBranches.Same);
        }

        private void SetResult(ISymbol key, ResultStates states)
        {
            if (states == ResultStates.Any)
                _results.Remove(key);
            else
                _results[key] = states;
        }

        /// <summary>
        /// Removes what the outcomes say about <paramref name="key"/>. The rest of an outcome stays: a guard computed
        /// from one result is still valid after another result has been reassigned.
        /// </summary>
        private static void Forget<TKey>(Dictionary<TKey, FlowBranches> outcomes, ISymbol key)
        {
            var stale = new List<TKey>();

            foreach (var pair in outcomes)
                if (pair.Value.Mentions(key))
                    stale.Add(pair.Key);

            foreach (var staleKey in stale)
                outcomes[staleKey] = outcomes[staleKey].Without(key);
        }

        /// <summary>A copy of the knowledge about results that claims nothing about <paramref name="key"/>.</summary>
        internal FlowState Without(ISymbol key)
        {
            var copy = ResultsOnly();
            copy._results.Remove(key);
            return copy;
        }

        /// <summary>Returns <c>true</c> if this state claims something about <paramref name="key"/>.</summary>
        internal bool Mentions(ISymbol key)
        {
            return _results.ContainsKey(key);
        }

        private static bool Same<TKey, TValue>(Dictionary<TKey, TValue> first, Dictionary<TKey, TValue> second, Func<TValue, TValue, bool> equals)
        {
            if (first.Count != second.Count)
                return false;

            foreach (var pair in first)
                if (!second.TryGetValue(pair.Key, out var other) || !equals(pair.Value, other))
                    return false;

            return true;
        }
    }
}
