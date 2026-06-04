#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // 1. ZIP Integrity & Mimetype
        AssertMimetypeIsFirstAndStored(epubBytes);

        using var zip = OpenZip(epubBytes);
        
        // 2. OPF Resolution
        var opfPath = zip.GetOpfPath();
        var opfXml = ReadEntryText(zip, opfPath);
        
        // 3. OPF Low-level Regression Checks
        var totalVersions = CountOccurrences(opfXml, "version=\"");
        totalVersions.Should().Be(2, $"OPF XML should have exactly 2 version attributes (XML decl + package): {opfXml}");
        CountOccurrences(opfXml, " unique-identifier=\"").Should().Be(1);

        var opf = XDocument.Parse(opfXml);
        var opfNs = OpfNs;
        var dcNs = DcNs;

        opf.Root!.Name.Should().Be(opfNs + "package");
        opf.Root.Attribute("version")!.Value.Should().Be(packageVersion);

        var uniqueId = opf.Root.Attribute("unique-identifier")?.Value;
        uniqueId.Should().NotBeNullOrWhiteSpace();

        // 4. Metadata Compliance
        var metadata = opf.Root.Element(opfNs + "metadata");
        metadata.Should().NotBeNull();

        metadata!.Elements(dcNs + "title").Select(e => e.Value).Should().Contain("Book Title");
        metadata.Elements(dcNs + "language").Select(e => e.Value).Should().NotBeEmpty();
        metadata.Elements(dcNs + "identifier").Should().Contain(e => (string?)e.Attribute("id") == uniqueId);

        // EPUB3-specific invariants:
        metadata.Elements(dcNs + "identifier").Should().AllSatisfy(id => id.Attribute("scheme").Should().BeNull());
        opf.Root.Element(opfNs + "spine")!.Attribute("toc").Should().BeNull();
        metadata.Elements(opfNs + "meta")
            .Count(m => (string?)m.Attribute("property") == "dcterms:modified" && string.IsNullOrWhiteSpace((string?)m.Attribute("refines")))
            .Should().Be(1);

        // 5. NAV Compliance
        var navItem = opf.Root.Element(opfNs + "manifest")!.Elements(opfNs + "item")
            .SingleOrDefault(i => ((string?)i.Attribute("properties"))?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Contains("nav") == true);
        navItem.Should().NotBeNull();
        ((string?)navItem!.Attribute("media-type")).Should().Be("application/xhtml+xml");

        var navHref = (string?)navItem!.Attribute("href");
        var navAbsolute = CombineOpfDirAndHref(opfPath, navHref!);
        var navXml = ReadEntryText(zip, navAbsolute);
        AssertNavHasSingleToc(navXml, expectedLinks: 2);

        // 6. Navigation Consistency
        var tocHrefs = GetNavTocHrefs(navXml);
        tocHrefs.Should().HaveCount(2);
        
        var spineHrefs = GetSpineHrefs(opf);
        foreach(var href in tocHrefs) spineHrefs.Should().Contain(href);
    }
}
