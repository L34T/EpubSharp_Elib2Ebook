using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class ReaderSpineOrderTests
{
    [Fact]
    public async Task HtmlInReadingOrder_contains_only_spine_documents_excluding_nav()
    {
        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>One</p></body></html>");
        writer.AddChapter("Chapter 2", "<html><body><p>Two</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        epub.SpecialResources.HtmlInReadingOrder.Should().NotBeEmpty();
        epub.SpecialResources.HtmlInReadingOrder.Select(h => h.Href).Should().NotContain("nav.xhtml");

        // TableOfContents comes from NAV; ensure both sources agree on chapter count.
        epub.TableOfContents.Should().HaveCount(2);
        epub.SpecialResources.HtmlInReadingOrder.Should().HaveCount(2);
    }
}

