#nullable enable
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class EpubOptionalFeaturesTests
{
    [Fact]
    public async Task TrySetSeriesUrl_is_safe_and_persists_url_in_opf_metadata()
    {
        const string url = "https://example.com/work/series/12345";

        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");

        // Safe failures
        writer.TrySetSeriesUrl("").Should().BeFalse();
        writer.TrySetSeriesUrl("ftp://example.com/nope").Should().BeFalse();
        writer.TrySetSeriesUrl(url).Should().BeFalse("series url should not be set without a collection");

        writer.AddCollection("Series Name", "1");
        writer.TrySetSeriesUrl(url).Should().BeTrue();

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        epub.Format.Opf.Metadata.Links.Should().Contain(l =>
            l.Refines == "#collection" &&
            l.Href == url &&
            l.Rel != null &&
            l.Rel.Contains("series-url"));

        epub.Format.Opf.Metadata.Metas.Should().Contain(m =>
            m.Refines == "#collection" &&
            m.Property == "dcterms:identifier" &&
            m.Text == url);
    }

    [Fact]
    public async Task TryAddNcxWarningPage_does_not_affect_spine_or_nav()
    {
        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");

        writer.TryAddNcxWarningPage("Warning", "<html><body><p>Warn</p></body></html>").Should().BeTrue();

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        // Must not appear in spine reading order
        epub.SpecialResources.HtmlInReadingOrder.Select(h => h.Href).Should().NotContain("warning-ncx.xhtml");

        // Must not appear in nav toc
        epub.TableOfContents.Select(c => c.RelativePath).Should().NotContain("warning-ncx.xhtml");

        // But should exist as an EPUB XHTML resource
        epub.Resources.Html.Select(h => h.Href).Should().Contain("warning-ncx.xhtml");
        epub.Format.Opf.Manifest.Items.Should().Contain(i => i.Href == "warning-ncx.xhtml");

        // And NCX should contain two warning navPoints (first+last) pointing to warning-ncx.xhtml
        epub.Format.Ncx.Should().NotBeNull();
        var points = epub.Format.Ncx!.NavMap.NavPoints;
        points.Should().NotBeEmpty();
        points.Count(p => p.ContentSrc == "warning-ncx.xhtml").Should().Be(2);
        points.First().ContentSrc.Should().Be("warning-ncx.xhtml");
        points.Last().ContentSrc.Should().Be("warning-ncx.xhtml");
    }
}
