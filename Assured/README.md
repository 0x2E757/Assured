# Assured

`Result<TValue, TError>` for C#: a value or an error, never both, never neither by accident.

```csharp
using Assured;

Result<int, string> Parse(string text) {
    return int.TryParse(text, out var number) ? number : $"not a number: {text}";
}

if (Parse(input).TryUnwrapValue(out var value))
    Console.WriteLine(value);
```

## API at a glance

Grouped by what you want to do; see the XML documentation for exact signatures.

| Need | Members |
|---|---|
| Create | `Result<V, E>.Value(v)`, `Result<V, E>.Error(e)`, or assign a `V` or `E` directly (implicit conversion) |
| Inspect | `HasValue()`, `HasError()` |
| Take the contents, or throw | `UnwrapValue()`, `UnwrapError()` |
| Take the contents, checked | `TryUnwrapValue(out v)`, `TryUnwrapError(out e)` |
| Take the contents, or a substitute | `ValueOr(fallback)`, `ValueOrGenerate(() => fallback)` |
| Handle both cases in one expression | `Match(onValue, onError)` |
| Transform without unwrapping | `Map(f)` for the value, `MapError(f)` for the error, `Bind(f)` to chain a call that returns another `Result` |
| Compare | `==`, `!=`, `Equals`, `GetHashCode`; usable as a dictionary key |
| Success without data | `Success.Value` as the value of `Result<Success, E>` |

## Design

- **State is explicit.** A result holds a value or an error because it was created through the `Value` or `Error` factory, or through the implicit conversion from `TValue` or `TError`. State is never inferred from `null` or `default`, so value types and nullable references work the same way.
- **`default` is not a result.** A struct can always be obtained uninitialized: `default`, `new()`, an array element, an unassigned field. Such a result reports neither a value nor an error. Accessors that must produce something throw; combinators that transform the contents pass it through unchanged. Do not construct this state on purpose; for a success that carries no data use `Result<Success, TError>`.
- **Readonly struct, no hidden allocations.** Creating, copying, comparing and inspecting a result does not allocate. The only allocations the library can cause are the delegates the caller passes to combinators; how much a delegate allocates depends on what it captures and on the C# compiler version, not on this library.
- **Equality is by state and contents.** Two results are equal when they are in the same state and their active field compares equal with the default comparer for its type.

## Conventions for code that uses Assured

- Return `Result` from any operation that can fail in a way the caller is expected to handle. Exceptions are for bugs and unrecoverable failures.
- Check the state before unwrapping, or use the `Try*` and `*Or*` accessors. Unwrapping an unchecked result is the mistake this library exists to prevent.
- Prefer non-capturing lambdas or pre-created delegates in hot paths when calling combinators.
- Keep the return type as `Result` even while a method cannot fail yet; adding the first error later then changes only the method, not its callers.

## Where to look

- The public API is small and lives in the `Assured` project; each concern is a separate partial file of `Result`.
- XML documentation is generated and shipped with the package, so IDE tooltips describe every public member.
- Compatibility targets and the language version are declared in the project file.

## License

MIT, see [LICENSE](https://github.com/0x2E757/Assured/blob/main/LICENSE).
