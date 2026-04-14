using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpubSharp.Format;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class Elib2EbookWriterCompatTests
{
    [Fact]
    public async Task Write_like_Elib2Ebook_builds_readable_epub_and_replaces_placeholder_images()
    {
        var cancellationToken = TestContext.Current.CancellationToken;

        var writer = new EpubWriter();

        writer.AddAuthor("Author 1");
        writer.SetTitle("Book Title");
        writer.AddDescription("Plain annotation text");

        // placeholder as Elib2Ebook does
        writer.AddFile("img.jpeg", Array.Empty<byte>(), EpubContentType.ImageJpeg);

        var chapter1 = writer.AddChapter("Chapter 1", "<html><body><p>One</p><img src=\"img.jpeg\"/></body></html>");
        var chapter2 = writer.AddChapter("Chapter 2", "<html><body><p>Two</p></body></html>");

        var imageBytes = new byte[] { 0x42, 0x43, 0x44 };
        var tempDir = Directory.CreateTempSubdirectory("epubsharp-tests-");
        try
        {
            var imgPath = Path.Combine(tempDir.FullName, "img.jpeg");
            await File.WriteAllBytesAsync(imgPath, imageBytes, cancellationToken);

            await using var epubStream = new MemoryStream();
            await writer.Write(epubStream, [new FileMeta("img.jpeg", imgPath)]);

            epubStream.Position = 0;
            var epub = EpubReader.Read(epubStream, leaveOpen: true, Encoding.UTF8);

            epub.Title.Should().Be("Book Title");
            epub.Authors.Should().Contain("Author 1");

            // Reader prefers NAV for TOC; should match added chapters.
            epub.TableOfContents.Select(c => c.Title).Should().ContainInOrder("Chapter 1", "Chapter 2");
            epub.TableOfContents[0].RelativePath.Should().Be(chapter1.RelativePath);
            epub.TableOfContents[1].RelativePath.Should().Be(chapter2.RelativePath);

            // Placeholder should be replaced by FileMeta content.
            epub.Resources.Images.Should().ContainSingle(i => i.Href == "img.jpeg");
            epub.Resources.Images.Single(i => i.Href == "img.jpeg").Content.Should().Equal(imageBytes);

            // Ensure the ZIP does not contain duplicated entries for the same path.
            epubStream.Position = 0;
            using var zip = new ZipArchive(epubStream, ZipArchiveMode.Read, leaveOpen: true, Encoding.UTF8);
            zip.Entries.Count(e =>
                    e.FullName.EndsWith("/img.jpeg", StringComparison.Ordinal) ||
                    e.FullName == "EPUB/img.jpeg")
                .Should().Be(1);
        }
        finally
        {
            tempDir.Delete(recursive: true);
        }
    }
}
