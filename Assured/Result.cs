using System;
using System.Diagnostics.CodeAnalysis;

namespace Assured
{
    /// <summary>
    /// Holds either a value of <typeparamref name="TValue"/> or an error of <typeparamref name="TError"/>.
    /// The state is set explicitly by <see cref="Value"/> and <see cref="Error"/>; a <c>default</c> instance is
    /// undefined and holds neither.
    /// </summary>
    /// <typeparam name="TValue">Type of the successful value.</typeparam>
    /// <typeparam name="TError">Type of the error.</typeparam>
    public readonly partial struct Result<TValue, TError>
    {
        private enum State : byte
        {
            Undefined = 0,
            HasValue = 1,
            HasError = 2,
        }

        private readonly State _state;
        private readonly TValue? _value;
        private readonly TError? _error;

        private Result(State state, TValue? value, TError? error)
        {
            _state = state;
            _value = value;
            _error = error;
        }

        /// <summary>Creates a result holding <paramref name="value"/>.</summary>
        public static Result<TValue, TError> Value(TValue value) => new(State.HasValue, value, default);

        /// <summary>Creates a result holding <paramref name="error"/>.</summary>
        public static Result<TValue, TError> Error(TError error) => new(State.HasError, default, error);

        /// <summary>Returns <c>true</c> if the result holds a value.</summary>
        public bool HasValue() => _state == State.HasValue;

        /// <summary>Returns <c>true</c> if the result holds an error.</summary>
        public bool HasError() => _state == State.HasError;

        /// <summary>Returns the value.</summary>
        /// <exception cref="InvalidOperationException">The result does not hold a value.</exception>
        public TValue UnwrapValue()
        {
            return HasValue() ? _value! : throw new InvalidOperationException("Result does not hold a value!");
        }

        /// <summary>Returns the error.</summary>
        /// <exception cref="InvalidOperationException">The result does not hold an error.</exception>
        public TError UnwrapError()
        {
            return HasError() ? _error! : throw new InvalidOperationException("Result does not hold an error!");
        }

        /// <summary>Returns <c>true</c> and the value if the result holds one; otherwise <c>false</c>.</summary>
        public bool TryUnwrapValue([MaybeNullWhen(false)] out TValue value)
        {
            value = _value;
            return HasValue();
        }

        /// <summary>Returns <c>true</c> and the error if the result holds one; otherwise <c>false</c>.</summary>
        public bool TryUnwrapError([MaybeNullWhen(false)] out TError error)
        {
            error = _error;
            return HasError();
        }

        /// <summary>Returns the value, or <paramref name="fallback"/> if the result holds no value.</summary>
        public TValue ValueOr(TValue fallback)
        {
            return _state == State.HasValue ? _value! : fallback;
        }

        /// <summary>Returns the value, or the result of <paramref name="fallback"/> if the result holds no value.</summary>
        public TValue ValueOrGenerate(Func<TValue> fallback)
        {
            return _state == State.HasValue ? _value! : fallback();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return _state switch
            {
                State.HasValue => $"Value({_value})",
                State.HasError => $"Error({_error})",
                _ => "Result(Undefined)",
            };
        }
    }
}
