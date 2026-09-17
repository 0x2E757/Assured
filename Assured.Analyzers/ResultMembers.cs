using Microsoft.CodeAnalysis;

namespace Assured.Analyzers
{
    /// <summary>
    /// Names of the <c>Result</c> members the analyzers reason about. This is the only place that knows the library's API by name.
    /// </summary>
    internal static class ResultMembers
    {
        /// <summary>Metadata name of the open generic <c>Result</c> struct.</summary>
        public const string TypeName = "Assured.Result`2";

        public const string Value = "Value";
        public const string Error = "Error";
        public const string HasValue = "HasValue";
        public const string HasError = "HasError";
        public const string UnwrapValue = "UnwrapValue";
        public const string UnwrapError = "UnwrapError";
        public const string TryUnwrapValue = "TryUnwrapValue";
        public const string TryUnwrapError = "TryUnwrapError";
        public const string Map = "Map";
        public const string MapError = "MapError";
        public const string Bind = "Bind";

        /// <summary>Returns <c>true</c> if <paramref name="type"/> is a constructed <c>Result</c>.</summary>
        public static bool IsResult(ITypeSymbol? type, INamedTypeSymbol resultType)
        {
            return type is INamedTypeSymbol named && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, resultType);
        }

        /// <summary>Returns <c>true</c> if <paramref name="method"/> is declared by <c>Result</c>.</summary>
        public static bool IsResultMember(IMethodSymbol method, INamedTypeSymbol resultType)
        {
            return SymbolEqualityComparer.Default.Equals(method.ContainingType.OriginalDefinition, resultType);
        }
    }
}
