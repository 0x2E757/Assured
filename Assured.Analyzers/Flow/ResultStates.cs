using System;

namespace Assured.Analyzers.Flow
{
    /// <summary>
    /// The states a <c>Result</c> may be in at a program point. The analysis never knows the exact state, only which states remain possible.
    /// </summary>
    [Flags]
    internal enum ResultStates : byte
    {
        /// <summary>No state is possible: the program point is unreachable.</summary>
        None = 0,

        /// <summary>The result may hold a value.</summary>
        Value = 1,

        /// <summary>The result may hold an error.</summary>
        Error = 2,

        /// <summary>The result may be uninitialized, that is <c>default</c>.</summary>
        Undefined = 4,

        /// <summary>Any state is possible: nothing is known.</summary>
        Any = Value | Error | Undefined,
    }
}
