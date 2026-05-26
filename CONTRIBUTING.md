# Contributing

Thanks for considering a contribution.

## Ground rules

- Keep public API changes minimal and intentional. This library is used as a dependency in other projects.
- Prefer small, reviewable PRs (one concern per PR).
- Add tests for bug fixes (prefer invariants over exact XML serialization layout).

## Build & test

Requirements: .NET SDK 9.0.x

```bash
dotnet build EpubSharp/EpubSharp.csproj -c Release -f net9.0
dotnet test EpubSharp.Tests/EpubSharp.Tests.csproj -c Release
```

## Tests

Test suite principles are documented in `EpubSharp.Tests/README.md`.

## Code style

- Match the existing code style in the modified area.
- Avoid large refactors in the same PR as behavioral changes.
