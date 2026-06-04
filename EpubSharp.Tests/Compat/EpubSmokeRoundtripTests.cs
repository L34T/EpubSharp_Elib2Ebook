#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EpubSharp.Format;
using FluentAssertions;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat;

public class EpubSmokeRoundtripTests
{
    [Fact]
    public async Task Full_roundtrip_preserves_all_resource_types_and_reading_order()
    {
        // 1. Create a complex book with various resources
        var epubBytes = await WriteEpubAsync(writer =>
        {
            writer.AddAuthor("Author 1");
            writer.SetTitle("Book Title");
            writer.AddDescription("Description");

            writer.AddFile("style.css", "body { }", EpubContentType.Css);
            writer.AddFile("font.ttf", new byte[] { 0x01, 0x02 }, EpubContentType.FontTruetype);
            writer.AddFile("img.jpeg", new byte[] { 0x42 }, EpubContentType.ImageJpeg);

            writer.AddChapter("Chapter 1", "<html><body><p>One</p></body></html>");
            writer.AddChapter("Chapter 2", "<html><body><p>Two</p></body></html>");
        });

        // 2. Read it back
        var epub = EpubReader.Read(new MemoryStream(epubBytes), leaveOpen: false, Encoding.UTF8);

        // 3. Verify Metadata
        epub.Title.Should().Be("Book Title");
        epub.Authors.Should().Contain("Author 1");

        // 4. Verify Resources
        epub.Resources.Css.Should().ContainSingle(f => f.Href == "style.css");
        epub.Resources.Fonts.Should().ContainSingle(f => f.Href == "font.ttf");
        epub.Resources.Images.Should().ContainSingle(f => f.Href == "img.jpeg");

        // 5. Verify Reading Order (Spine)
        epub.SpecialResources.HtmlInReadingOrder.Should().HaveCount(2);
        epub.TableOfContents.Should().HaveCount(2);

        var spineHrefs = epub.SpecialResources.HtmlInReadingOrder.Select(h => h.Href).ToList();
        
        // Ensure nav.xhtml is NOT part of the reading flow (EPUB3 requirement)
        spineHrefs.Should().NotContain("nav.xhtml");
        
        // 6. Basic ZIP check (migrated from redundant Fact)
        using var zip = OpenZip(epubBytes);
        zip.GetEntry("mimetype").Should().NotBeNull();
        zip.GetEntry("META-INF/container.xml").Should().NotBeNull();
    }
}
