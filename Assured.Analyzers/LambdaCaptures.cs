using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace Assured.Analyzers
{
    /// <summary>
    /// Finds what a lambda captures. A local or parameter is captured when it belongs to neither the lambda nor a
    /// function nested in it; the enclosing instance is captured by an explicit <c>this</c>, <c>base</c>, or an
    /// instance member access. A call to a local function declared outside the lambda captures whatever that
    /// function captures.
    /// </summary>
    internal sealed class LambdaCaptures
    {
        private readonly IOperation _root;
        private readonly List<string> _captured = new();
        private readonly HashSet<ISymbol> _followed = new(SymbolEqualityComparer.Default);

        private LambdaCaptures(IOperation root)
        {
            _root = root;
        }

        /// <summary>The names of everything <paramref name="lambda"/> captures, in order of appearance; empty if nothing.</summary>
        public static IReadOnlyList<string> Of(IAnonymousFunctionOperation lambda)
        {
            IOperation root = lambda;

            while (root.Parent is not null)
                root = root.Parent;

            var captures = new LambdaCaptures(root);
            captures.VisitFunction(lambda.Symbol, lambda.Body);
            return captures._captured;
        }

        private void VisitFunction(IMethodSymbol function, IOperation body)
        {
            var own = new HashSet<ISymbol>(SymbolEqualityComparer.Default) { function };

            foreach (var operation in body.Descendants())
            {
                if (operation is IAnonymousFunctionOperation nestedLambda)
                    own.Add(nestedLambda.Symbol);
                else if (operation is ILocalFunctionOperation nestedFunction)
                    own.Add(nestedFunction.Symbol);
            }

            Visit(body, own);
        }

        private void Visit(IOperation operation, HashSet<ISymbol> own)
        {
            switch (operation)
            {
                case INameOfOperation:
                    // The operand is never evaluated, so naming a variable in it captures nothing.
                    return;

                case IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance }:
                    Add("this");
                    break;

                case ILocalReferenceOperation { Local: { IsConst: false } local } when !own.Contains(local.ContainingSymbol):
                    Add(local.Name);
                    break;

                case IParameterReferenceOperation { Parameter: var parameter } when !own.Contains(parameter.ContainingSymbol):
                    Add(parameter.Name);
                    break;

                case IInvocationOperation { TargetMethod: { MethodKind: MethodKind.LocalFunction } called }:
                    Follow(called, own);
                    break;

                case IMethodReferenceOperation { Method: { MethodKind: MethodKind.LocalFunction } referenced }:
                    Follow(referenced, own);
                    break;
            }

            foreach (var child in operation.Children)
                Visit(child, own);
        }

        /// <summary>A local function declared outside brings its own captures along with it.</summary>
        private void Follow(IMethodSymbol localFunction, HashSet<ISymbol> own)
        {
            localFunction = localFunction.OriginalDefinition;

            if (localFunction.IsStatic || own.Contains(localFunction) || !_followed.Add(localFunction))
                return;

            foreach (var operation in _root.DescendantsAndSelf())
            {
                if (operation is ILocalFunctionOperation { Body: { } body } declaration && SymbolEqualityComparer.Default.Equals(declaration.Symbol, localFunction))
                {
                    VisitFunction(localFunction, body);
                    return;
                }
            }
        }

        private void Add(string name)
        {
            var quoted = "'" + name + "'";

            if (!_captured.Contains(quoted))
                _captured.Add(quoted);
        }
    }
}
