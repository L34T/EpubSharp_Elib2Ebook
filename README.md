# EpubSharp

C# library for reading and writing EPUB files.

[![.NET](https://img.shields.io/badge/.NET-net9%20(primary)%20%7C%20net10%20(opt--in)-512BD4)](#requirements)
[![EPUB](https://img.shields.io/badge/EPUB-3.2%20%7C%203.3%20%7C%203.4%20focus-2F6FED)](#epub-support)
[![Release](https://img.shields.io/github/v/release/L34T/EpubSharp_Elib2Ebook?include_prereleases&sort=semver)](https://github.com/L34T/EpubSharp_Elib2Ebook/releases)
[![License](https://img.shields.io/github/license/L34T/EpubSharp_Elib2Ebook)](LICENSE)

## EPUB support

- **EPUB 3.x (3.2/3.3/3.4 focus):** read + write (OPF + NAV; optional NCX during transition)
- **EPUB 2.0.1:** read + write (NCX/OPF2) — supported for compatibility, may be deprecated in the future

## Requirements

- .NET SDK: **9.0.x** (primary / default)
- Optional: .NET SDK **10.0.x** (for local `net10.0` builds; opt-in)

## Installation (GitHub Releases)

This project is currently distributed as **GitHub Release assets** (not via NuGet).
Consumers typically download these 3 files from a release tag:

- `EpubSharp.dll`
- `EpubSharp.pdb`
- `EpubSharp.deps.json`

### Download examples

Linux / macOS (bash):

```bash
set -euo pipefail

REPO="L34T/EpubSharp_Elib2Ebook"
TAG="" # empty = latest
OUTDIR="Core/External"

mkdir -p "$OUTDIR"

# Option A (recommended): GitHub CLI (`gh`)
if command -v gh >/dev/null 2>&1; then
  if [ -z "${TAG}" ]; then
    gh release download --repo "$REPO" --dir "$OUTDIR" --pattern "EpubSharp.*"
  else
    gh release download "$TAG" --repo "$REPO" --dir "$OUTDIR" --pattern "EpubSharp.*"
  fi
  exit 0
fi

# Option B: curl + jq
if ! command -v jq >/dev/null 2>&1; then
  echo "Need either 'gh' or 'jq' installed." >&2
  exit 2
fi

api() { curl -fsSL -H "Accept: application/vnd.github+json" -H "User-Agent: EpubSharp-fetch" "$1"; }
release_json="$(
  if [ -z "${TAG}" ]; then
    api "https://api.github.com/repos/${REPO}/releases/latest"
  else
    api "https://api.github.com/repos/${REPO}/releases/tags/${TAG}"
  fi
)"

download_url_for() {
  local asset_name="$1"
  jq -r --arg name "$asset_name" '.assets[] | select(.name==$name) | .browser_download_url' <<<"$release_json"
}

for name in EpubSharp.dll EpubSharp.pdb EpubSharp.deps.json; do
  url="$(download_url_for "$name")"
  [ -n "$url" ] || { echo "Missing asset: $name" >&2; exit 1; }
  curl -fsSL -o "$OUTDIR/$name" "$url"
done
```

Windows (PowerShell):

```powershell
$Repo = "L34T/EpubSharp_Elib2Ebook"
$Tag = "" # empty = latest
$OutDir = "Core/External"

$headers = @{ "Accept"="application/vnd.github+json"; "User-Agent"="EpubSharp-fetch" }
$release =
    if ([string]::IsNullOrWhiteSpace($Tag)) {
        Invoke-RestMethod -Headers $headers -Uri "https://api.github.com/repos/$Repo/releases/latest"
    } else {
        Invoke-RestMethod -Headers $headers -Uri "https://api.github.com/repos/$Repo/releases/tags/$Tag"
    }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$names = @("EpubSharp.dll","EpubSharp.pdb","EpubSharp.deps.json")
foreach ($name in $names) {
    $asset = $release.assets | Where-Object { $_.name -eq $name } | Select-Object -First 1
    if ($null -eq $asset) { throw "Missing asset: $name" }
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile (Join-Path $OutDir $name) | Out-Null
}
```

## Usage

### Reading an EPUB

```csharp
using System.Text;
using EpubSharp;

using var stream = File.OpenRead("my.epub");
var book = EpubReader.Read(stream, leaveOpen: true, encoding: Encoding.UTF8);

var title = book.Title;
var authors = book.Authors;
var toc = book.TableOfContents;

var html = book.Resources.Html;
var images = book.Resources.Images;
```

### Writing an EPUB (EPUB 3.x)

```csharp
using EpubSharp;

var writer = new EpubWriter();
writer.SetTitle("My Book");
writer.AddAuthor("Foo Bar");

writer.AddChapter("Chapter 1", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body>Hi</body></html>");

await writer.Write("new.epub", files: Array.Empty<FileMeta>());
```

### Optional features

- Series collection (OPF `belongs-to-collection`): `EpubWriter.AddCollection(name, number)`
- Series URL (adds OPF metadata `<link .../>` + `dcterms:identifier`): `EpubWriter.TrySetSeriesUrl(url)`
- NCX-only warning page for legacy readers: `EpubWriter.TryAddNcxWarningPage(title, xhtml)`

## Build & test (CLI / IDE)

```bash
dotnet build EpubSharp/EpubSharp.csproj -c Release -f net9.0
dotnet test EpubSharp.Tests/EpubSharp.Tests.csproj -c Release
```

### Build under .NET 10 (local)

This repo pins .NET 9 via `global.json`. For local .NET 10 builds, use `dotnet10/global.json` and enable the optional net10 target.

```bash
cd dotnet10
dotnet build ../EpubSharp/EpubSharp.csproj -c Release -f net10.0 -p:EpubSharpEnableNet10=true
```

Scripts (Linux/macOS): `scripts/build-net9.sh`, `scripts/build-net10.sh`  
Scripts (Windows / PowerShell): `scripts/build-net9.ps1`, `scripts/build-net10.ps1`

Build matrix helpers: `scripts/build-matrix.sh`, `scripts/build-matrix.ps1`

## Release artifacts (GitHub Actions)

The workflow `.github/workflows/build-release.yml` publishes a GitHub Release on `v*` tags and uploads:

- `EpubSharp.dll`
- `EpubSharp.pdb`
- `EpubSharp.deps.json`

See `RELEASING.md` for the version/tag checklist.

## Notes for forks / consumers

There is an upstream repository (Asido/EpubSharp) and there are forks used by downstream projects (e.g. Elib2Ebook).
If you consume binaries from GitHub Releases, treat the **Release assets** as the source of truth for what features are
present in that build.

If you are migrating from upstream, see `MIGRATION.md`.
