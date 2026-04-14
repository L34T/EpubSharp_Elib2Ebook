# Migration guide (Asido/EpubSharp → this fork)

This repository is a downstream fork used by **Elib2Ebook** and is not a drop-in replacement for every consumer of
`https://github.com/asido/EpubSharp`.

The main goals of the fork are:

- Keep **Elib2Ebook** building/running without changing its usage contract.
- Move writer output towards **EPUB 3.4** recommendations while staying compatible with **EPUB 3.3 / 3.2**.
- Keep **EPUB 2.0.1** working for a transition period (NCX is optional/legacy).

## Distribution / build environment

- Primary target framework: `net9.0` (requires .NET SDK 9.0.x).
- Optional local target: `net10.0` (opt-in via `-p:EpubSharpEnableNet10=true`).
- Distribution is currently **GitHub Releases assets** (not NuGet):
  - `EpubSharp.dll`
  - `EpubSharp.pdb`
  - `EpubSharp.deps.json`

## Public API compatibility

### Preserved (required by Elib2Ebook)

These are relied upon by Elib2Ebook and should stay stable (names/signatures/behavior):

- `new EpubWriter()`
- `EpubWriter.AddAuthor(string)`
- `EpubWriter.SetTitle(string)`
- `EpubWriter.SetCover(byte[], ImageFormat)`
- `EpubWriter.AddDescription(string)`
- `EpubWriter.AddCollection(string name, string number)`
- `EpubWriter.AddFile(string filename, byte[] content, EpubContentType type)`
- `EpubWriter.AddChapter(string title, string html, string fileId = null)`
- `Task EpubWriter.Write(string filename, IEnumerable<FileMeta> files)`
- `Task EpubWriter.Write(Stream stream, IEnumerable<FileMeta> files)`
- `static EpubBook EpubReader.Read(Stream, bool leaveOpen, Encoding encoding)`

### Additive (optional, feature-detect friendly)

New methods intended to be used via feature-detect (reflection is OK for older consumers):

- `bool EpubWriter.TrySetSeriesUrl(string seriesUrl)`
  - Does nothing and returns `false` when:
    - URL is null/empty/whitespace
    - URL does not start with `http`
    - collection/series was not created via `AddCollection(...)`
  - When valid: overwrites existing series URL if already present.
- `bool EpubWriter.TryAddNcxWarningPage(string title, string xhtml)`
  - Writes/overwrites `warning-ncx.xhtml`, adds two NCX `navPoint` entries (first and last), **does not** add to spine/NAV.
  - Intended for legacy NCX-only readers; EPUB 3 readers should not surface this page.

### Potentially breaking (vs upstream consumers)

This fork intentionally makes some older APIs **compile-time errors** to prevent accidental usage:

- Some legacy writer methods are marked `[Obsolete(..., true)]` (error on use), e.g. sync `Write(Stream)` and `Write()` byte[].
  - If your project previously used these upstream methods, migrate to the async `Write(..., IEnumerable<FileMeta>)` overloads.
  - Even if you currently use sync APIs successfully upstream, the recommended direction is to switch to the async write path.

## Behavioral changes (writer output)

These changes can affect downstream expectations (even if API is unchanged):

- **EPUB ZIP packaging**
  - `mimetype` is written **first** and **stored** (no compression), per EPUB spec.
  - ZIP entry names are normalized to not start with `/` (improves compatibility with real readers).
- **EPUB 3 navigation**
  - Ensures `nav.xhtml` exists in the manifest with `properties="nav"`.
  - NAV is generated from the book’s TOC; NCX (if present) is treated as legacy/transitional.
- **Metadata defaults**
  - If `dc:language` is missing, writer adds a default (`"en"`) to keep EPUB 3 output valid.
- **OPF serialization changes (EPUB 3.x)**
  - Does not emit `dc:identifier@scheme` for EPUB 3 (OPF 3 uses different mechanisms).
  - Does not emit EPUB2-only `spine@toc` for EPUB 3.

## Behavioral changes (reader)

- Some reader indexing was optimized; as part of this, **duplicate HTML hrefs** now result in an `EpubParseException`
  instead of silently picking one (prevents ambiguous resolution).

## Recommended migration steps for a consumer

1. Confirm you are not using obsolete legacy writer methods (sync `Write(...)` / `Write()` byte[]).
2. Switch to `Write(string/Stream, IEnumerable<FileMeta>)`.
3. If you need series URL support:
   - Call `AddCollection(name, number)` first.
   - Then call `TrySetSeriesUrl(url)` and handle `false` as “not supported / invalid / no collection”.
4. If you rely on EPUB 2 NCX behavior, treat it as transitional and plan to validate with EPUB 3 NAV-based readers.

## Notes for Elib2Ebook

Elib2Ebook already treats new features as optional via reflection (feature-detect), so the expected integration is:

- Keep the existing writer lifecycle.
- Call `TrySetSeriesUrl(...)` only if present.
- Avoid assuming legacy `Write(...)` overloads exist.
