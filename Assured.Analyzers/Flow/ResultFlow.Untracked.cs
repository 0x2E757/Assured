using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers.Flow
{
    internal sealed partial class ResultFlow
    {
        /// <summary>
        /// The variables of an operation block that the analysis must not trust, because code outside the analyzed
        /// path can change them: a variable assigned inside a lambda or a local function changes whenever that
        /// function runs, and a variable aliased by a <c>ref</c> local changes through the alias.
        /// </summary>
        private static HashSet<ISymbol> UntrackedIn(IOperation block)
        {
            var untracked = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

            foreach (var operation in block.DescendantsAndSelf())
            {
                switch (operation)
                {
                    case IAnonymousFunctionOperation lambda:
                        AddOuterVariablesWrittenIn(lambda.Symbol, lambda.Body, untracked);
                        break;

                    case ILocalFunctionOperation { Body: { } body } localFunction:
                        AddOuterVariablesWrittenIn(localFunction.Symbol, body, untracked);
                        break;

                    case ISimpleAssignmentOperation { IsRef: true } aliasing:
                        AddVariablesReferencedIn(aliasing.Value, untracked);
                        break;

                    case IVariableDeclaratorOperation { Symbol: { RefKind: not RefKind.None } } declarator when declarator.GetVariableInitializer() is { } initializer:
                        AddVariablesReferencedIn(initializer.Value, untracked);
                        break;
                }
            }

            return untracked;
        }

        /// <summary>Adds every variable a reference expression may point to; a conditional reference points to several.</summary>
        private static void AddVariablesReferencedIn(IOperation reference, HashSet<ISymbol> untracked)
        {
            foreach (var operation in reference.DescendantsAndSelf())
                if (VariableOf(operation) is { } variable)
                    untracked.Add(variable);
        }

        /// <summary>Adds the locals and parameters that <paramref name="function"/> assigns but does not own.</summary>
        private static void AddOuterVariablesWrittenIn(IMethodSymbol function, IOperation body, HashSet<ISymbol> untracked)
        {
            var own = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { function };

            foreach (var operation in body.Descendants())
            {
                if (operation is IAnonymousFunctionOperation nestedLambda)
                    own.Add(nestedLambda.Symbol);
                else if (operation is ILocalFunctionOperation nestedFunction)
                    own.Add(nestedFunction.Symbol);
            }

            foreach (var written in WrittenSymbols(body, captureTargets: null))
                if (written is ILocalSymbol or IParameterSymbol && !own.Contains(written.ContainingSymbol))
                    untracked.Add(written);
        }
    }
}
