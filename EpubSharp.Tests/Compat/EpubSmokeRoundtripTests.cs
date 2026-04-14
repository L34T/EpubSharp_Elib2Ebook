using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpubSharp.Format;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class EpubSmokeRoundtripTests
{
    [Fact]
    public async Task Writer_then_reader_roundtrip_is_working_for_basic_book()
    {
        var writer = new EpubWriter();

        writer.AddAuthor("Author 1");
        writer.SetTitle("Book Title");
        writer.AddDescription("Description");

        writer.AddFile("style.css", "body { }", EpubContentType.Css);
        writer.AddFile("font.ttf", new byte[] { 0x01, 0x02 }, EpubContentType.FontTruetype);
        writer.AddFile("img.jpeg", new byte[] { 0x42 }, EpubContentType.ImageJpeg);

        writer.AddChapter("Chapter 1", "<html><body><p>One</p></body></html>");
        writer.AddChapter("Chapter 2", "<html><body><p>Two</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        epub.Title.Should().Be("Book Title");
        epub.Authors.Should().Contain("Author 1");

        epub.Resources.Css.Should().ContainSingle(f => f.Href == "style.css");
        epub.Resources.Fonts.Should().ContainSingle(f => f.Href == "font.ttf");
        epub.Resources.Images.Should().ContainSingle(f => f.Href == "img.jpeg");

        epub.TableOfContents.Should().HaveCount(2);
        epub.SpecialResources.HtmlInReadingOrder.Should().HaveCount(2);

        // Ensure we did not accidentally place nav.xhtml into the spine reading order.
        epub.SpecialResources.HtmlInReadingOrder.Select(h => h.Href).Should().NotContain("nav.xhtml");
    }

    [Fact]
    public async Task Writer_produces_an_epub_that_can_be_opened_as_zip()
    {
        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.ToArray().Length.Should().BeGreaterThan(0);
        stream.Position = 0;

        using var zip = new System.IO.Compression.ZipArchive(stream, System.IO.Compression.ZipArchiveMode.Read,
            leaveOpen: true, Encoding.UTF8);

        zip.Entries.Should().NotBeEmpty();
        zip.GetEntry("mimetype").Should().NotBeNull();
        zip.GetEntry("META-INF/container.xml").Should().NotBeNull();
    }
}

