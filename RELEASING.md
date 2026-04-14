# Releasing

This repo publishes GitHub Releases via `.github/workflows/build-release.yml` on `v*` tags.

## Checklist

1. Update the version in `EpubSharp/EpubSharp.csproj`:
   - `<Version>` (assembly/package version; even if you don’t publish to NuGet)
   - `<InformationalVersion>` (matches release label)
   - If needed, `<AssemblyVersion>` / `<FileVersion>` (often stays `X.Y.Z.0` for .NET assembly versioning)
2. Build + run tests locally:

```bash
dotnet build EpubSharp/EpubSharp.csproj -c Release -f net9.0
dotnet test EpubSharp.Tests/EpubSharp.Tests.csproj -c Release
```

3. Push a tag like `v1.2.3` or `v1.2.3-alpha.4` (whatever scheme you use, but keep tag and `EpubSharp/EpubSharp.csproj` versions aligned).

The workflow uploads these artifacts to the GitHub Release:

- `EpubSharp/bin/Release/net9.0/EpubSharp.dll`
- `EpubSharp/bin/Release/net9.0/EpubSharp.pdb`
- `EpubSharp/bin/Release/net9.0/EpubSharp.deps.json`
