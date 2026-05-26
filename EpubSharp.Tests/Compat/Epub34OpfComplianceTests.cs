using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class Epub34OpfComplianceTests
{
    [Fact]
    public async Task Opf_for_epub3_does_not_write_dc_identifier_scheme_and_spine_toc()
    {
        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        var opfXml = epub.SpecialResources.Opf.TextContent;
        var doc = XDocument.Parse(opfXml);

        XNamespace opfNs = "http://www.idpf.org/2007/opf";
        XNamespace dcNs = "http://purl.org/dc/elements/1.1/";

        var spine = doc.Root!.Element(opfNs + "spine");
        spine.Should().NotBeNull();
        spine!.Attribute("toc").Should().BeNull(); // EPUB2 legacy attribute; should not be written for EPUB3.

        var identifiers = doc.Root!.Element(opfNs + "metadata")!.Elements(dcNs + "identifier").ToList();
        identifiers.Should().NotBeEmpty();
        identifiers.Should().AllSatisfy(id => id.Attribute("scheme").Should().BeNull());
    }
}
