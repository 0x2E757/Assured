using System;

namespace Assured
{
    public readonly partial struct Success : IEquatable<Success>
    {
        /// <inheritdoc/>
        public bool Equals(Success other)
        {
            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Success;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
