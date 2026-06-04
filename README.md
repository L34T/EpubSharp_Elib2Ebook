# EpubSharp

A C# library for reading and writing EPUB files.

[![.NET](https://img.shields.io/badge/.NET-net9%20%7C%20net10-512BD4)](#requirements)
[![EPUB](https://img.shields.io/badge/EPUB-2.0%20%7C%203.0%20%7C%203.4-2F6FED)](#overview)
[![License](https://img.shields.io/badge/License-MPL_2.0-brightgreen.svg)](LICENSE)  
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/edc05e6cae004c15bbd060042080e465)](https://app.codacy.com/gh/L34T/EpubSharp_Elib2Ebook/dashboard)
[![CodeFactor](https://www.codefactor.io/repository/github/l34t/epubsharp_elib2ebook/badge)](https://www.codefactor.io/repository/github/l34t/epubsharp_elib2ebook)

## Overview

EpubSharp provides a simple and efficient way to interact with EPUB files in .NET. It supports reading metadata, table of contents, and internal resources (HTML, CSS, images, fonts) from EPUB 2.0 and 3.x documents.

## EPUB Support

- **EPUB 3.x (3.2/3.3/3.4 focus):** full read support and significantly improved write support (OPF + NAV; optional NCX during transition).
- **EPUB 2.0.1:** read + write (NCX/OPF2) — supported for compatibility.

## Requirements

- **SDK:** .NET 10.0 or higher recommended.
- **Runtime:** .NET 8.0, .NET 9.0, .NET 10.0, or compatible environments.

## Installation

### NuGet

Install the package via the NuGet Package Manager:

```bash
dotnet add package EpubSharp.dll
```

### From Source

1. Clone the repository.
2. Build the project:
   ```bash
   dotnet build
   ```

## Usage

### Reading an EPUB

```csharp
using EpubSharp;

// Read an EPUB file
EpubBook book = EpubReader.Read("my.epub");

// Read metadata
string title = book.Title;
string[] authors = book.Authors.ToArray();
byte[] coverImage = book.CoverImage; // Note: Returns byte array in recent versions

// Get table of contents
ICollection<EpubChapter> chapters = book.TableOfContents;

// Get contained files
ICollection<EpubTextFile> html = book.Resources.Html;
ICollection<EpubTextFile> css = book.Resources.Css;
ICollection<EpubByteFile> images = book.Resources.Images;
ICollection<EpubByteFile> fonts = book.Resources.Fonts;

// Convert to plain text
string text = book.ToPlainText();

// Access internal EPUB format specific data structures
EpubFormat format = book.Format;
OcfDocument ocf = format.Ocf;
OpfDocument opf = format.Opf;
NcxDocument ncx = format.Ncx;
NavDocument nav = format.Nav;
```

### Writing an EPUB (EPUB 3.x)

```csharp
using EpubSharp;

var writer = new EpubWriter();
writer.SetTitle("My Book");
writer.AddAuthor("Foo Bar");

writer.AddChapter("Chapter 1", "<html xmlns=\"http://www.w3.org/1999/xhtml\"><body>Hi</body></html>");

// Async Write is recommended in l34t version
await writer.Write("new.epub", files: Array.Empty<FileMeta>());
```

### Special Features (l34t)

- **Series collection** (OPF `belongs-to-collection`): `EpubWriter.AddCollection(name, number)`
- **Series URL** (adds OPF metadata `<link .../>` + `dcterms:identifier`): `EpubWriter.TrySetSeriesUrl(url)`
- **NCX-only warning page** for legacy readers: `EpubWriter.TryAddNcxWarningPage(title, xhtml)`

## Scripts & Commands

The project uses the standard .NET CLI:

- **Build:** `dotnet build`
- **Test:** `dotnet test`
- **Pack:** `dotnet pack` (generates NuGet package in `bin/Debug` or `bin/Release`)

### Build Scripts

Linux/macOS scripts are available in `scripts/`:
- `scripts/build-net9.sh`
- `scripts/build-net10.sh`

## Project Structure

- `EpubSharp/`: Core library containing EPUB parsing and writing logic.
  - `Format/`: Readers and writers for OPF, NCX, NAV, and OCF documents.
  - `Extensions/`: Helper methods for zip archives, streams, and XML.
- `EpubSharp.Tests/`: xUnit test suite with sample EPUB files.

## License

This project is licensed under the **Mozilla Public License 2.0 (MPL-2.0)**. See the [LICENSE](LICENSE) file for details.
