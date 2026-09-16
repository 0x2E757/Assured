namespace Assured
{
    public readonly partial struct Result<TValue, TError>
    {
        /// <summary>Wraps <paramref name="value"/> into a result holding a value.</summary>
        public static implicit operator Result<TValue, TError>(TValue value)
        {
            return Value(value);
        }

        /// <summary>Wraps <paramref name="error"/> into a result holding an error.</summary>
        public static implicit operator Result<TValue, TError>(TError error)
        {
            return Error(error);
        }

        /// <summary>Returns <c>true</c> if both results are equal; see <see cref="Equals(Result{TValue, TError})"/>.</summary>
        public static bool operator ==(Result<TValue, TError> left, Result<TValue, TError> right)
        {
            return left.Equals(right) == true;
        }

        /// <summary>Returns <c>true</c> if the results are not equal; see <see cref="Equals(Result{TValue, TError})"/>.</summary>
        public static bool operator !=(Result<TValue, TError> left, Result<TValue, TError> right)
        {
            return left.Equals(right) == false;
        }
    }
}
