#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace EpubSharp.Tests.TestHelpers;

public static class EpubTestHelpers
{
    public static async Task<byte[]> WriteEpubAsync(Action<EpubWriter> configure, params EpubSharp.Format.FileMeta[] files)
    {
        var writer = new EpubWriter();
        configure(writer);

        await using var stream = new MemoryStream();
        await writer.Write(stream, files ?? Array.Empty<EpubSharp.Format.FileMeta>());
        return stream.ToArray();
    }

    public static ZipArchive OpenZip(byte[] epubBytes)
    {
        var ms = new MemoryStream(epubBytes, writable: false);
        return new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false, Encoding.UTF8);
    }

    public static string ReadEntryText(ZipArchive zip, string fullName)
    {
        var entry = zip.GetEntry(fullName) ?? throw new Exception($"Missing zip entry: {fullName}");
        using var s = entry.Open();
        using var r = new StreamReader(s, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false);
        return r.ReadToEnd();
    }

    public static string GetOpfFullPathFromContainerXml(string containerXml)
    {
        var doc = XDocument.Parse(containerXml);
        XNamespace ocf = "urn:oasis:names:tc:opendocument:xmlns:container";
        var rootfile = doc.Root!
            .Element(ocf + "rootfiles")!
            .Elements(ocf + "rootfile")
            .FirstOrDefault();
        var fullPath = (string?)rootfile?.Attribute("full-path");
        if (string.IsNullOrWhiteSpace(fullPath))
        {
            throw new Exception("container.xml has no rootfile full-path");
        }
        return fullPath;
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
        XNamespace opfNs = "http://www.idpf.org/2007/opf";
        var manifest = opf.Root!.Element(opfNs + "manifest")!;

        return manifest.Elements(opfNs + "item")
            .Select(i =>
            {
                var props = ((string?)i.Attribute("properties"))?
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                return new ManifestItem(
                    Id: (string?)i.Attribute("id") ?? string.Empty,
                    Href: (string?)i.Attribute("href") ?? string.Empty,
                    MediaType: (string?)i.Attribute("media-type") ?? string.Empty,
                    Properties: props);
            })
            .ToList();
    }

    public static HashSet<string> GetSpineHrefs(XDocument opf)
    {
        XNamespace opfNs = "http://www.idpf.org/2007/opf";
        var manifest = opf.Root!.Element(opfNs + "manifest")!;
        var manifestById = manifest.Elements(opfNs + "item")
            .Where(i => i.Attribute("id") != null && i.Attribute("href") != null)
            .ToDictionary(i => i.Attribute("id")!.Value, i => i.Attribute("href")!.Value, StringComparer.Ordinal);

        var spine = opf.Root!.Element(opfNs + "spine")!;
        var hrefs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var itemref in spine.Elements(opfNs + "itemref"))
        {
            var idref = (string?)itemref.Attribute("idref");
            if (idref == null) continue;
            if (!manifestById.TryGetValue(idref, out var href)) continue;
            hrefs.Add(href);
        }
        return hrefs;
    }

    public static List<string> GetNavTocHrefs(string navXml)
    {
        XNamespace xhtml = "http://www.w3.org/1999/xhtml";
        XNamespace epub = "http://www.idpf.org/2007/ops";
        var doc = XDocument.Parse(navXml);
        var toc = doc.Descendants(xhtml + "nav")
            .First(n => (string?)n.Attribute(epub + "type") == "toc");
        return toc.Descendants(xhtml + "a")
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
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true, Encoding.UTF8))
        {
            build(archive);
        }
        return ms.ToArray();
    }
}

