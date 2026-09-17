namespace Assured.Analyzers
{
    /// <summary>
    /// Ids of every diagnostic the package reports. An id is a contract: once published it is referenced by
    /// suppressions and <c>.editorconfig</c> files, so it is never renamed or reused.
    /// </summary>
    public static class DiagnosticIds
    {
        /// <summary>Where every diagnostic points the reader for an explanation.</summary>
        internal const string HelpLink = "https://github.com/0x2E757/Assured/blob/main/Assured.Analyzers/README.md";

        /// <summary><c>UnwrapValue</c> or <c>UnwrapError</c> on a result whose state is not proven by a check.</summary>
        public const string UncheckedUnwrap = "ASR001";

        /// <summary>Lambda passed to a combinator captures state and allocates on every call.</summary>
        public const string LambdaCaptures = "ASR002";

        /// <summary>Method group passed to a combinator allocates a delegate on every call.</summary>
        public const string MethodGroupAllocates = "ASR003";
    }
}
