#nullable enable
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EpubSharp.Format;
using FluentAssertions;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat;

public class EpubManifestIntegrityTests
{
    [Fact]
    public async Task All_manifest_items_exist_in_zip()
    {
        var epubBytes = await WriteEpubAsync(writer =>
        {
            writer.SetTitle("Book Title");
            writer.AddAuthor("Author 1");
            writer.AddDescription("Desc");

            writer.AddFile("style.css", "body { }", EpubContentType.Css);
            writer.AddFile("font.ttf", new byte[] { 0x01 }, EpubContentType.FontTruetype);
            writer.AddFile("img.jpeg", new byte[] { 0x42 }, EpubContentType.ImageJpeg);

            writer.AddChapter("Chapter 1", "<html><body><p>One</p></body></html>");
        });

        using var zip = OpenZip(epubBytes);

        // Resolve OPF path via container.xml (keeps this test valid if we ever change default paths).
        var containerXml = ReadEntryText(zip, "META-INF/container.xml");
        var opfPath = GetOpfFullPathFromContainerXml(containerXml);

        var opf = ReadXml(zip, opfPath);
        XNamespace opfNs = "http://www.idpf.org/2007/opf";

        var opfDir = GetDir(opfPath);
        var items = opf.Root!.Element(opfNs + "manifest")!.Elements(opfNs + "item").ToList();
        items.Should().NotBeEmpty();

        foreach (var item in items)
        {
            var href = (string?)item.Attribute("href");
            if (string.IsNullOrWhiteSpace(href)) continue;

            var entryName = opfDir + href;
            zip.GetEntry(entryName).Should().NotBeNull($"manifest item should exist in zip: {entryName}");
        }
    }
}
