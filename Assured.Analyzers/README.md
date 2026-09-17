![Assured](https://raw.githubusercontent.com/0x2E757/Assured/main/assets/logo-128.png)

# Assured.Analyzers

Roslyn analyzers for code that uses [Assured](https://www.nuget.org/packages/Assured). The compiler reports the two mistakes the library's conventions exist to prevent, at the place where they are made.

```csharp
Result<int, string> parsed = Parse(input);
var value = parsed.UnwrapValue(); // ASR001: 'UnwrapValue' is called on a result that may hold an error

if (parsed.HasValue())
    value = parsed.UnwrapValue();  // proven safe, nothing reported
```

## What is reported

- **Unwrapping an unchecked result.** `UnwrapValue()` and `UnwrapError()` throw when the result is not in the state they expect. The analyzer follows every result through branches, assignments and loops, and reports a call whose safety is not established by a preceding `HasValue()`, `HasError()` or `TryUnwrap*` check, or by the way the result was created. A result obtained from `default` or `new()` is reported as uninitialized.
- **Delegates allocated on every call.** Combinators such as `Map`, `Bind` and `Match` take delegates. A lambda that captures anything, whether `this`, a local or a parameter, and a method group the compiler does not cache, is a new allocation each time the call runs, which matters in hot paths and under Unity's garbage collector. A lambda that captures nothing is cached by the compiler, and a delegate created once and stored is reused; neither is reported. The diagnostic names what is captured.

Rule ids, categories and default severities are declared in the analyzers' source, and each diagnostic carries its own description in the IDE. Severities can be changed per rule or per category in `.editorconfig`. The correctness rule is in the `Correctness` category; the allocation rules are in `Performance`, and a project that does not care about allocations turns them off in one line:

```ini
dotnet_diagnostic.ASR001.severity = error
dotnet_analyzer_diagnostic.category-Performance.severity = none
```

## Design

- **No false sense of safety.** The unwrap analysis is conservative: when the state of a result cannot be proven on every path that reaches the call, the call is reported. Knowledge is discarded where it can no longer be trusted, such as after a reassignment or an `out` argument, and a `catch` or `finally` block sees every state the `try` body could be in when it threw. One shortcut is deliberate: a result that passed a `HasError()` check is accepted by `UnwrapValue()`, and the other way round, although an uninitialized result would pass that check too. An uninitialized result is reported where nothing else is possible.
- **Knowledge follows the code, not the syntax.** A check survives being stored in a boolean local, combined with `&&`, negated, compared with `false`, or used as the condition of a loop or a conditional expression. A lambda sees the checks made before it was created, and is not examined again if the variable is reassigned later; a local function sees none, because it may run from anywhere. A variable that a lambda or a local function assigns, or that a `ref` local aliases, is not tracked at all.
- **Fields are trusted between a check and the unwrap.** A field of type `Result` is tracked like a local; a call made in between could reassign it, but treating every call as a reassignment would make the rule useless for fields. This is the same trade-off C# nullable analysis makes.
- **Zero cost without the library.** Both analyzers register nothing when the compilation does not reference `Assured`.

## Installation

The package is a development dependency: it takes part in compilation and adds nothing to the build output. In Unity, add the analyzer assembly to the project and label it `RoslynAnalyzer`, as described in the Unity documentation. The compiler version the analyzers require follows from the Roslyn version declared in the project file; an older compiler skips them with a warning.

## Where to look

- The analyzers live in the `Assured.Analyzers` project. The unwrap rule is a data-flow analysis in the `Flow` folder: the `ResultFlow` files, one concern per file, over the state types `ResultStates`, `FlowState` and `FlowBranches`. `ResultMembers` is the only file that names the library's API.
- The tests in `Assured.Analyzers.Tests` compile consumer snippets against the real `Assured` assembly and list, case by case, what is and is not reported.

## License

MIT, see [LICENSE](https://github.com/0x2E757/Assured/blob/main/LICENSE).
