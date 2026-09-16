using System;

namespace Assured
{
    /// <summary>
    /// Unit type for results whose success carries no data, as in <c>Result&lt;Success, TError&gt;</c>.
    /// All instances are equal.
    /// </summary>
    public readonly partial struct Success : IEquatable<Success>
    {
        /// <summary>The single <see cref="Success"/> instance.</summary>
        public static readonly Success Value = default;

        /// <inheritdoc/>
        public override string ToString()
        {
            return "Success";
        }
    }
}
