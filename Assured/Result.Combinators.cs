using System;

namespace Assured
{
    public readonly partial struct Result<TValue, TError>
    {
        /// <summary>Calls <paramref name="onValue"/> or <paramref name="onError"/> depending on the state and returns its result.</summary>
        /// <exception cref="InvalidOperationException">The result is undefined.</exception>
        public TOut Match<TOut>(Func<TValue, TOut> onValue, Func<TError, TOut> onError)
        {
            return _state switch
            {
                State.HasValue => onValue(_value!),
                State.HasError => onError(_error!),
                _ => throw new InvalidOperationException("Result is undefined!"),
            };
        }

        /// <summary>Transforms the value with <paramref name="map"/>; an error or undefined state is passed through.</summary>
        public Result<TOut, TError> Map<TOut>(Func<TValue, TOut> map)
        {
            return _state switch
            {
                State.HasValue => Result<TOut, TError>.Value(map(_value!)),
                State.HasError => Result<TOut, TError>.Error(_error!),
                _ => default,
            };
        }

        /// <summary>Transforms the error with <paramref name="map"/>; a value or undefined state is passed through.</summary>
        public Result<TValue, TOut> MapError<TOut>(Func<TError, TOut> map)
        {
            return _state switch
            {
                State.HasValue => Result<TValue, TOut>.Value(_value!),
                State.HasError => Result<TValue, TOut>.Error(map(_error!)),
                _ => default,
            };
        }

        /// <summary>Passes the value to <paramref name="next"/> and returns its result; an error or undefined state is passed through.</summary>
        public Result<TOut, TError> Bind<TOut>(Func<TValue, Result<TOut, TError>> next)
        {
            return _state switch
            {
                State.HasValue => next(_value!),
                State.HasError => Result<TOut, TError>.Error(_error!),
                _ => default,
            };
        }

        /// <summary>Passes the error to <paramref name="next"/> and returns its result; a value or undefined state is passed through.</summary>
        public Result<TValue, TOut> BindError<TOut>(Func<TError, Result<TValue, TOut>> next)
        {
            return _state switch
            {
                State.HasValue => Result<TValue, TOut>.Value(_value!),
                State.HasError => next(_error!),
                _ => default,
            };
        }
    }
}
