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

namespace EpubSharp.Tests.TestHelpers;

public static class EpubTestHelpers
{
    public static readonly XNamespace OpfNs = "http://www.idpf.org/2007/opf";
    public static readonly XNamespace OpsNs = "http://www.idpf.org/2007/ops";
    public static readonly XNamespace DcNs = "http://purl.org/dc/elements/1.1/";
    public static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    public static async Task<byte[]> WriteEpubAsync(Action<EpubWriter> configure, params EpubSharp.Format.FileMeta[] files)
    {
        var writer = new EpubWriter();
        configure(writer);
        await using var stream = new MemoryStream();
        await writer.Write(stream, files ?? Array.Empty<EpubSharp.Format.FileMeta>());
        return stream.ToArray();
    }

    public static EpubBook ReadEpub(byte[] epubBytes) => 
        EpubReader.Read(new MemoryStream(epubBytes, writable: false), leaveOpen: false, Encoding.UTF8);

    public static ZipArchive OpenZip(byte[] epubBytes)
    {
        var ms = new MemoryStream(epubBytes, writable: false);
        return new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
    }

    public static string ReadEntryText(ZipArchive zip, string fullName)
    {
        var entry = zip.GetEntry(fullName);
        if (entry == null) throw new Exception($"Missing zip entry: {fullName}. Available: " + string.Join(", ", zip.Entries.Select(e => e.FullName)));
        using var s = entry.Open();
        using var r = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return r.ReadToEnd();
    }

    public static string GetOpfPath(this ZipArchive zip)
    {
        var containerXml = ReadEntryText(zip, "META-INF/container.xml");
        var doc = XDocument.Parse(containerXml);
        XNamespace ocf = "urn:oasis:names:tc:opendocument:xmlns:container";
        var rootfiles = doc.Root?.Element(ocf + "rootfiles");
        if (rootfiles == null) throw new Exception("container.xml missing <rootfiles>");
        var rootfile = rootfiles.Elements(ocf + "rootfile").FirstOrDefault();
        if (rootfile == null) throw new Exception("container.xml missing <rootfile>");
        var fullPath = (string?)rootfile.Attribute("full-path");
        return fullPath ?? throw new Exception("container.xml <rootfile> has no full-path");
    }

    public static string GetDir(string path)
    {
        var idx = path.LastIndexOf('/');
        return idx < 0 ? string.Empty : path.Substring(0, idx + 1);
    }

    public static XDocument ReadXml(ZipArchive zip, string fullName) => XDocument.Parse(ReadEntryText(zip, fullName));

    public static string CombineOpfDirAndHref(string opfPath, string href) => GetDir(opfPath) + href;

    public sealed record ManifestItem(string Id, string Href, string MediaType, IReadOnlyList<string> Properties);

    public static IReadOnlyList<ManifestItem> GetManifestItems(XDocument opf)
    {
        var manifest = opf.Root?.Element(OpfNs + "manifest");
        if (manifest == null) throw new Exception("OPF missing <manifest>");
        return manifest.Elements(OpfNs + "item")
            .Select(i =>
            {
                var props = ((string?)i.Attribute("properties"))?.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>();
                return new ManifestItem(
                    Id: (string?)i.Attribute("id") ?? string.Empty,
                    Href: (string?)i.Attribute("href") ?? string.Empty,
                    MediaType: (string?)i.Attribute("media-type") ?? string.Empty,
                    Properties: props);
            }).ToList();
    }

    public static HashSet<string> GetSpineHrefs(XDocument opf)
    {
        var manifest = opf.Root?.Element(OpfNs + "manifest");
        if (manifest == null) throw new Exception("OPF missing <manifest>");
        var manifestById = manifest.Elements(OpfNs + "item")
            .Where(i => i.Attribute("id") != null && i.Attribute("href") != null)
            .ToDictionary(i => i.Attribute("id")!.Value, i => i.Attribute("href")!.Value, StringComparer.Ordinal);

        var spine = opf.Root?.Element(OpfNs + "spine");
        if (spine == null) throw new Exception("OPF missing <spine>");
        var hrefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var itemref in spine.Elements(OpfNs + "itemref"))
        {
            var idref = (string?)itemref.Attribute("idref");
            if (idref != null && manifestById.TryGetValue(idref, out var href)) hrefs.Add(href);
        }
        return hrefs;
    }

    public static List<string> GetNavTocHrefs(string navXml)
    {
        var doc = XDocument.Parse(navXml);
        var toc = doc.Descendants(XhtmlNs + "nav").FirstOrDefault(n => (string?)n.Attribute(OpsNs + "type") == "toc");
        if (toc == null) throw new Exception("nav.xhtml missing <nav epub:type=\"toc\">");
        return toc.Descendants(XhtmlNs + "a")
            .Select(a => (string?)a.Attribute("href"))
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h!)
            .ToList();
    }

    public static void AddTextEntry(ZipArchive archive, string name, string content, bool stored)
    {
        var level = stored ? CompressionLevel.NoCompression : CompressionLevel.Optimal;
        var entry = archive.CreateEntry(name, level);
        using var s = entry.Open();
        using var w = new StreamWriter(s, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        w.Write(content);
    }

    public static byte[] BuildZip(Action<ZipArchive> build)
    {
        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8)) build(archive);
        return ms.ToArray();
    }

    public static string ContainerXml(string opfFullPath) =>
        "<?xml version=\"1.0\"?>\n<container xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\" version=\"1.0\">\n" +
        $"<rootfiles>\n<rootfile full-path=\"{opfFullPath}\" media-type=\"application/oebps-package+xml\"/>\n</rootfiles>\n</container>\n";

    public static void AssertMimetypeIsFirstAndStored(byte[] zipBytes)
    {
        zipBytes.Length.Should().BeGreaterThan(30);
        zipBytes[0].Should().Be((byte)'P'); zipBytes[1].Should().Be((byte)'K');
        var compressionMethod = zipBytes[8] | (zipBytes[9] << 8);
        compressionMethod.Should().Be(0, "mimetype must be stored (no compression)");
    }

    public static void AssertNavHasSingleToc(string navXml, int expectedLinks)
    {
        var doc = XDocument.Parse(navXml);
        doc.Root!.Name.Should().Be(XhtmlNs + "html");
        var navs = doc.Descendants(XhtmlNs + "nav").Where(n => (string?)n.Attribute(OpsNs + "type") == "toc").ToList();
        navs.Should().HaveCount(1, "nav.xhtml must have exactly one TOC nav element");
        navs[0].Descendants(XhtmlNs + "a").Count().Should().Be(expectedLinks);
    }

    public static int CountOccurrences(string text, string needle)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(needle)) return 0;
        int count = 0, idx = 0;
        while (true)
        {
            var searchRange = Math.Min(text.Length, 500);
            idx = text.IndexOf(needle, idx, StringComparison.Ordinal);
            if (idx < 0 || idx > searchRange) break;
            count++; idx += needle.Length;
        }
        return count;
    }
}
