# Migration Guide

Downstream fork optimized for the **Elib2Ebook** ecosystem. May not suit general-use scenarios.

Target repositories:
* [OnlyFart/Elib2Ebook](https://github.com/OnlyFart/Elib2Ebook)
* [RedBuld/Elib2Ebook](https://github.com/RedBuld/Elib2Ebook)

## Migration Steps

1. **Remove NuGet References**: Uninstall `EpubSharp` to avoid namespace collisions.
2. **Add Assembly Reference**:
   ```bash
   dotnet add reference /path/to/EpubSharp-net10.dll
   ```
3. **Adopt Asynchronous APIs**: Replace synchronous `EpubWriter.Write(...)` with async task calls:
   ```csharp
   await writer.Write("book.epub", files: Array.Empty<FileMeta>());
   ```
4. **Handle Parse Exceptions**: Catch `EpubParseException` where duplicate assets/hrefs may occur.

## Simple Example

### Reading
```csharp
using EpubSharp;

EpubBook book = EpubReader.Read("my.epub");
string title = book.Title;
string[] authors = book.Authors.ToArray();
byte[] cover = book.CoverImage;
```

### Writing
```csharp
using EpubSharp;
using System;
using System.Threading.Tasks;

var writer = new EpubWriter();
writer.SetTitle("Simple Book");
writer.AddAuthor("John Doe");
writer.AddChapter("Intro", "<html><body><h1>Hello</h1></body></html>");

await writer.Write("simple.epub", files: Array.Empty<FileMeta>());
```

## Extended Example

Adds multiple resource types (images, fonts, stylesheets, and audio) and metadata:

```csharp
using EpubSharp;
using EpubSharp.Format;
using System;
using System.IO;
using System.Threading.Tasks;

public async Task CreateExtendedEpub()
{
    var writer = new EpubWriter();
    
    writer.SetTitle("Extended Book");
    writer.AddAuthor("Lead Developer");
    writer.AddDescription("Extended features showcase.");
    
    // Metadata (EPUB 3.0 (2011))
    writer.AddCollection("Elib2Ebook Series", "1");
    writer.TrySetSeriesUrl("https://github.com/OnlyFart/Elib2Ebook");
    
    // Stylesheet (CSS) (EPUB 2.0 (2007))
    byte[] cssBytes = File.ReadAllBytes("theme.css");
    writer.AddFile("styles/theme.css", cssBytes, EpubContentType.Css);
    
    // Cover Image (JPEG) (EPUB 2.0 (2007))
    byte[] coverBytes = File.ReadAllBytes("cover.jpg");
    writer.SetCover(coverBytes, ImageFormat.Jpeg);
    
    // Image (WebP) (EPUB 3.3 (2023))
    byte[] webpBytes = File.ReadAllBytes("illustration.webp");
    writer.AddFile("images/illustration.webp", webpBytes, EpubContentType.ImageWebp);
    
    // Font (WOFF2) (EPUB 3.2 (2019))
    byte[] fontBytes = File.ReadAllBytes("OpenSans-Regular.woff2");
    writer.AddFile("fonts/OpenSans-Regular.woff2", fontBytes, EpubContentType.FontWoff2);
    
    // Audio (MP3) (EPUB 3.0 (2011))
    byte[] mp3Bytes = File.ReadAllBytes("narration.mp3");
    writer.AddFile("audio/narration.mp3", mp3Bytes, EpubContentType.AudioMpeg);
    
    string chapterHtml = @"
    <html xmlns=""http://www.w3.org/1999/xhtml"">
    <head>
        <link rel=""stylesheet"" type=""text/css"" href=""styles/theme.css"" />
    </head>
    <body>
        <h1>Chapter 1</h1>
        <p>Uses custom fonts and modern images:</p>
        <img src=""images/illustration.webp"" alt=""Illustration"" />
        <audio controls=""controls"">
            <source src=""audio/narration.mp3"" type=""audio/mpeg"" />
        </audio>
    </body>
    </html>";
    writer.AddChapter("Chapter 1", chapterHtml);
    
    // Legacy NCX-only warning page (devices older than 7 years)
    string warningHtml = @"
    <html xmlns=""http://www.w3.org/1999/xhtml"">
    <body>
        <p>This is a modern EPUB document designed for reading systems supporting modern standards. Devices or reader software older than 7 years may not display all elements correctly; upgrading your reader software is recommended.</p>
    </body>
    </html>";
    writer.TryAddNcxWarningPage("Compatibility Warning", warningHtml);
    
    await writer.Write("extended.epub", files: Array.Empty<FileMeta>());
}
```
