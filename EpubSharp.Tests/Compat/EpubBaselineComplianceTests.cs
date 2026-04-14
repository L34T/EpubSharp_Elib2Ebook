#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using FluentAssertions;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat;

public class EpubBaselineComplianceTests
{
    public static IEnumerable<object[]> Epub3Versions()
    {
        yield return ["3.2"];
        yield return ["3.3"];
        yield return ["3.4"];
    }

    [Theory]
    [MemberData(nameof(Epub3Versions))]
    public async Task Epub3_baseline_compliance_smoke(string packageVersion)
    {
        var writer = new EpubWriter();
        writer.TrySetPackageVersion(packageVersion).Should().BeTrue();
        writer.SetTitle("Book Title");
        writer.AddAuthor("Author 1");
        writer.AddChapter("Chapter 1", "<html><body><p>One</p></body></html>");
        writer.AddChapter("Chapter 2", "<html><body><p>Two</p></body></html>");

        await using var stream = new MemoryStream();
        await writer.Write(stream, []);

        var epubBytes = stream.ToArray();

        AssertMimetypeIsFirstAndStored(epubBytes);

        using var zip = OpenZip(epubBytes);

        zip.GetEntry("META-INF/container.xml").Should().NotBeNull();
        zip.GetEntry("mimetype").Should().NotBeNull();

        var containerXml = ReadEntryText(zip, "META-INF/container.xml");
        var opfPath = GetOpfFullPathFromContainerXml(containerXml);
        zip.GetEntry(opfPath).Should().NotBeNull($"container.xml rootfile should exist: {opfPath}");

        var opfXml = ReadEntryText(zip, opfPath);
        var opf = XDocument.Parse(opfXml);

        XNamespace opfNs = "http://www.idpf.org/2007/opf";
        XNamespace dcNs = "http://purl.org/dc/elements/1.1/";

        opf.Root!.Name.Should().Be(opfNs + "package");
        opf.Root.Attribute("version")!.Value.Should().Be(packageVersion);

        var uniqueId = opf.Root.Attribute("unique-identifier")?.Value;
        uniqueId.Should().NotBeNullOrWhiteSpace();

        var metadata = opf.Root.Element(opfNs + "metadata");
        metadata.Should().NotBeNull();

        metadata!.Elements(dcNs + "title").Select(e => e.Value).Should().Contain("Book Title");

        // Required for baseline compatibility: language + identifier referenced by unique-identifier.
        metadata.Elements(dcNs + "language").Select(e => e.Value).Should().NotBeEmpty();
        metadata.Elements(dcNs + "identifier")
            .Should()
            .Contain(e => (string?)e.Attribute("id") == uniqueId);

        // EPUB3.4-aligned invariants we rely on:
        // - no dc:identifier@scheme
        metadata.Elements(dcNs + "identifier").Should().AllSatisfy(id =>
            id.Attribute("scheme").Should().BeNull());

        // - no spine@toc for EPUB3
        opf.Root.Element(opfNs + "spine")!.Attribute("toc").Should().BeNull();

        // - one primary dcterms:modified (no refines)
        metadata.Elements(opfNs + "meta")
            .Count(m => (string?)m.Attribute("property") == "dcterms:modified" &&
                        string.IsNullOrWhiteSpace((string?)m.Attribute("refines")))
            .Should()
            .Be(1);

        // NAV manifest item exists and file exists
        var navItem = opf.Root.Element(opfNs + "manifest")!.Elements(opfNs + "item")
            .SingleOrDefault(i => ((string?)i.Attribute("properties"))?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Contains("nav") == true);
        navItem.Should().NotBeNull();
        ((string?)navItem!.Attribute("media-type")).Should().Be("application/xhtml+xml", "nav must be a core media type XHTML resource");

        var navHref = (string?)navItem!.Attribute("href");
        navHref.Should().NotBeNullOrWhiteSpace();

        var navAbsolute = CombineOpfDirAndHref(opfPath, navHref!);
        zip.GetEntry(navAbsolute).Should().NotBeNull($"nav.xhtml should exist: {navAbsolute}");

        var navXml = ReadEntryText(zip, navAbsolute);
        AssertNavHasSingleToc(navXml, expectedLinks: 2);

        // Ensure TOC targets are in spine (baseline sanity).
        var spineHrefs = GetSpineHrefs(opf);
        var manifestItems = GetManifestByHref(opf);
        var tocHrefs = GetNavTocHrefs(navXml);
        tocHrefs.Should().NotBeEmpty();
        tocHrefs.Should().OnlyContain(h => spineHrefs.Contains(h), "TOC should point to spine items");
        tocHrefs.Should().OnlyContain(h => manifestItems.ContainsKey(h), "TOC targets must exist in the OPF manifest");
        tocHrefs.Should().AllSatisfy(h =>
            manifestItems[h].MediaType.Should().Be("application/xhtml+xml", "TOC targets should be XHTML content documents"));

        // And those spine items should physically exist in the ZIP.
        foreach (var href in spineHrefs)
        {
            manifestItems.Should().ContainKey(href, "spine items must exist in the OPF manifest");
            manifestItems[href].MediaType.Should().Be("application/xhtml+xml", "spine items should be XHTML content documents");

            var absolute = CombineOpfDirAndHref(opfPath, href);
            zip.GetEntry(absolute).Should().NotBeNull($"spine item should exist: {absolute}");
        }
    }

    private static void AssertMimetypeIsFirstAndStored(byte[] zipBytes)
    {
        // ZIP local file header: PK\003\004
        zipBytes.Length.Should().BeGreaterThan(30);
        zipBytes[0].Should().Be((byte)'P');
        zipBytes[1].Should().Be((byte)'K');
        zipBytes[2].Should().Be(0x03);
        zipBytes[3].Should().Be(0x04);

        // compression method (little endian) at offset 8
        var compressionMethod = zipBytes[8] | (zipBytes[9] << 8);
        compressionMethod.Should().Be(0, "mimetype must be stored (no compression)");

        // file name length at offset 26, extra length at 28
        var nameLen = zipBytes[26] | (zipBytes[27] << 8);
        var extraLen = zipBytes[28] | (zipBytes[29] << 8);
        var nameStart = 30;
        var name = Encoding.ASCII.GetString(zipBytes, nameStart, nameLen);
        name.Should().Be("mimetype", "mimetype must be the first ZIP entry");

        // We don't need extraLen, but sanity-check we don't run past buffer.
        (nameStart + nameLen + extraLen).Should().BeLessThan(zipBytes.Length);
    }

    private static void AssertNavHasSingleToc(string navXml, int expectedLinks)
    {
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";
        XNamespace epub = "http://www.idpf.org/2007/ops";
        var doc = XDocument.Parse(navXml);

        doc.Root!.Name.Should().Be(xhtml + "html");
        doc.Root.Attribute(XNamespace.Xmlns + "epub")?.Value.Should().Be(epub.NamespaceName, "nav.xhtml must declare the epub namespace");

        var navs = doc.Descendants(xhtml + "nav")
            .Where(n => (string?)n.Attribute(epub + "type") == "toc")
            .ToList();

        navs.Should().HaveCount(1);
        navs[0].Descendants(xhtml + "a").Count().Should().Be(expectedLinks);
    }

    private sealed record ManifestItem(string Href, string MediaType);

    private static Dictionary<string, ManifestItem> GetManifestByHref(XDocument opf)
    {
        return GetManifestItems(opf)
            .Where(i => !string.IsNullOrWhiteSpace(i.Href) && !string.IsNullOrWhiteSpace(i.MediaType))
            .ToDictionary(i => i.Href, i => new ManifestItem(i.Href, i.MediaType), StringComparer.Ordinal);
    }

    private static HashSet<string> GetSpineHrefs(XDocument opf)
    {
        return EpubSharp.Tests.TestHelpers.EpubTestHelpers.GetSpineHrefs(opf);
    }

    private static List<string> GetNavTocHrefs(string navXml)
    {
        return EpubSharp.Tests.TestHelpers.EpubTestHelpers.GetNavTocHrefs(navXml);
    }
}
