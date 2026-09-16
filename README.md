<p align="center"><img src="assets/logo.svg" alt="Assured" width="128"></p>

# Assured

Typed results for C#: `Result<TValue, TError>` with explicit state.

General-purpose .NET, designed so that it also works in Unity: the code stays within the C# dialect Unity compiles, and the hot path does not allocate.

## Packages

Each package lives in a folder of the same name, and that folder's `README.md` is the package's documentation.

- [`Assured`](Assured/README.md) — the library.

## Building

Standard .NET SDK workflow from the repository root:

```
dotnet build
dotnet pack
```

Package metadata, target frameworks and language versions are declared in each project file.

## Contributing

Warnings are errors, and every public member needs XML documentation; the build enforces both.

## License

MIT, see [`LICENSE`](LICENSE).
