#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests.Compat;

public class OpfWriterRegressionTests
{
    [Fact]
    public async Task Opf_writer_does_not_emit_duplicate_attributes_and_is_parseable()
    {
        var writer = new EpubWriter();
        writer.SetTitle("Book Title");
        writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        stream.Position = 0;
        var epub = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        var opfXml = epub.SpecialResources.Opf.TextContent;

        // If OPF contains invalid XML (e.g., duplicate attributes like unique-identifier),
        // XDocument.Parse will throw. We also explicitly sanity-check the key attributes exist once.
        var doc = XDocument.Parse(opfXml);

        XNamespace opfNs = "http://www.idpf.org/2007/opf";
        doc.Root!.Name.Should().Be(opfNs + "package");

        var uniqueIdentifier = doc.Root.Attribute("unique-identifier")?.Value;
        uniqueIdentifier.Should().NotBeNullOrWhiteSpace();

        // NOTE: the root element includes both the attribute itself and the attribute-definition reference in the
        // human-readable spec prose isn't present here; however xmlns declarations can include the substring too.
        // We rely on XDocument.Parse + attribute presence checks to catch actual duplicate attribute emission.
        CountOccurrences(opfXml, " unique-identifier=\"").Should().BeGreaterThanOrEqualTo(1);
        CountOccurrences(opfXml, " version=\"").Should().BeGreaterThanOrEqualTo(1);
    }

    private static int CountOccurrences(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;

        var count = 0;
        var idx = 0;
        while (true)
        {
            idx = text.IndexOf(needle, idx, StringComparison.Ordinal);
            if (idx < 0) break;
            count++;
            idx += needle.Length;
        }

        return count;
    }
}
