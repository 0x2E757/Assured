# Working in this repository

Assured is a .NET library, `Result<TValue, TError>` with explicit state, and Roslyn analyzers for code that uses it; both are published to nuget.org. Each shippable package lives in a folder of the same name and carries its own `README.md`; its tests live in a sibling folder with the `.Tests` suffix. The root `README.md` only describes the repository.

## Building

Run from the repository root: `dotnet build`, `dotnet test`, `dotnet pack`. Warnings are errors and every public member needs an XML documentation comment; both are enforced by the build, so a change that introduces either is not done until the build is clean. A change in library behavior comes with tests, and is not done until `dotnet test` passes.

Target frameworks, language versions and package metadata are declared in the project files. Read them there instead of assuming.

## Conventions

- **Documentation does not carry data that drifts.** READMEs state design invariants and conventions, not version numbers, framework names, rule lists or allocation figures. Point the reader to where such data lives (project file, package page, XML documentation) instead of copying it.
- **Documentation describes what exists.** Do not mention packages, projects, workflows or files that are not in the repository yet.
- **The library is general-purpose .NET, designed so that it also works in Unity, whose compiler lags behind current C#.** Check `LangVersion` in the project file before using newer syntax; if a construct is not available there, do not use it. The same applies to the analyzers: the Roslyn version they reference is the oldest compiler they support, so it is not raised without a reason that outweighs losing older Unity editors.
- **`Version` in the project file is a local placeholder and is never edited.** The release version is entered when the publish workflow is run; it also creates the tag and the GitHub release. Never push packages or create tags by hand.
- **API compatibility is checked at publish time** against the latest version on nuget.org. Breaking changes are allowed only when the new version permits them under SemVer.
- **Files use CRLF line endings**, enforced by `.gitattributes`, and no trailing whitespace.

## Git

- **Commit only when the user explicitly asks for a commit.** Staging, editing or reviewing files is not a request to commit. A request to "save", "finish" or "wrap up" is not a request to commit either; ask if unsure.
- **At most one commit per request.** Do not split the requested work into several commits unless the user asks for that split.
- Never push, tag, rebase, reset or amend unless the user asks for that specific action.

### Commit messages

Conventional commits in the form `<type>(<scope>): <slug>`. The scope is omitted when the change is repository-wide. The slug is lowercase, imperative, without a trailing period.

Types: `feat`, `fix`, `refactor`, `docs`, `test`, `ci`, `chore`

Scopes:

- `lib` — the library under `Assured/` and its tests
- `analyzers` — the analyzers under `Assured.Analyzers/` and their tests
- `meta` — repository rules and conventions, such as this file
- `deps` — dependencies, .NET SDK version, toolchain
- `repo` — top-level layout, solution, `.gitignore`, file moves

```
feat(lib): add Result and Success types
fix(lib): keep undefined state through Map and Bind
docs(lib): add api overview to package readme
test(lib): cover undefined state in combinators
feat(analyzers): report unwrap inside catch blocks
ci: publish on manual dispatch
meta: define commit convention
repo: add solution and gitignore
chore(deps): require .net 10 sdk
```
