using System;
using System.Collections.Generic;

namespace Assured
{
    public readonly partial struct Result<TValue, TError> : IEquatable<Result<TValue, TError>>
    {
        /// <summary>Returns <c>true</c> if both results are in the same state and hold equal contents.</summary>
        public bool Equals(Result<TValue, TError> other)
        {
            if (_state != other._state)
                return false;

            return _state switch
            {
                State.HasValue => EqualityComparer<TValue?>.Default.Equals(_value, other._value),
                State.HasError => EqualityComparer<TError?>.Default.Equals(_error, other._error),
                _ => true,
            };
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Result<TValue, TError> other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return _state switch
            {
                State.HasValue => HashCode.Combine(_state, _value),
                State.HasError => HashCode.Combine(_state, _error),
                _ => 0,
            };
        }
    }
}
