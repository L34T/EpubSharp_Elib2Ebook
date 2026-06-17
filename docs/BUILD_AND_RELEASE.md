# Building & Releasing

## Local Build Environment

* **Recommended SDK**: .NET 10 SDK (2026).
* **Build Scripts (`scripts/`)**:
  - `build-net10.sh` / `.ps1` ([build-net10.sh](../scripts/build-net10.sh)): Targets .NET 10.0 (2026).
  - `build-net9.sh` / `.ps1` ([build-net9.sh](../scripts/build-net9.sh)): Targets .NET 9.0 (2024).
  - `build-net2.sh` / `.ps1` ([build-net2.sh](../scripts/build-net2.sh)): Targets .NET Standard 2.0 (2017).
  - `build-net48.sh` / `.ps1` ([build-net48.sh](../scripts/build-net48.sh)): Targets .NET Framework 4.8 (2019).
  - `build-matrix.sh` ([build-matrix.sh](../scripts/build-matrix.sh)): Compiles and tests .NET 9 and .NET 10.
  - `build-matrix-extended.sh` ([build-matrix-extended.sh](../scripts/build-matrix-extended.sh)): Compiles and tests all targets.

## Release Process

1. **Update Versions**: Update metadata in [EpubSharp.csproj](../EpubSharp/EpubSharp.csproj):
   - `<Version>`: Semantic version (e.g. `1.1.6-alpha.4`).
   - `<AssemblyVersion>` / `<FileVersion>`: Standard .NET assembly versioning (e.g. `1.1.6.0`).
2. **Verify Local Build**:
   ```bash
   ./scripts/build-matrix.sh
   ```
3. **Tag & Push**:
   ```bash
   git tag v1.1.6-alpha.4
   git push origin v1.1.6-alpha.4
   ```

GitHub Actions automatically compiles, tests, and publishes release binaries on tag:
* `EpubSharp-net10.dll` / `.pdb`
* `EpubSharp-net9.dll` / `.pdb`
* `EpubSharp-netstandard20.dll` / `.pdb`
* `EpubSharp-net48.dll` / `.pdb`

## CI/CD Workflows

Defined in [.github/workflows/build-release.yml](../.github/workflows/build-release.yml):
* **Pull Request Matrix**: Builds and runs tests against `.net9.0` and `.net10.0` on Ubuntu runners.
* **Auto-Publish**: Releases assets on push of `v*` tags.

## Dependency Pinning: FluentAssertions

`FluentAssertions` in [EpubSharp.Tests.csproj](../EpubSharp.Tests/EpubSharp.Tests.csproj) is pinned to `8.8.0`:

```xml
<PackageReference Include="FluentAssertions" Version="8.8.0" />
```

> **Warning: Roslyn Overload Resolution Bug**
> 
> Pinned to `8.8.0` to prevent compile errors on .NET SDK versions prior to `9.0.200` when targeting .NET 9. Earlier SDK versions (`9.0.1xx`) trigger overload resolution compile errors due to a bug fixed in Roslyn PR [#75878](https://github.com/dotnet/roslyn/pull/75878) (merged only in `9.0.200+` and .NET 10).
