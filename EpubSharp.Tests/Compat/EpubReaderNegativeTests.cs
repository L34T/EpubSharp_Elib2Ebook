#nullable enable
using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat;

public class EpubReaderNegativeTests
{
    [Fact]
    public void Read_throws_when_container_xml_is_missing()
    {
        var epubBytes = BuildZip(archive =>
        {
            // Intentionally omit META-INF/container.xml
            AddTextEntry(archive, "mimetype", "application/epub+zip", stored: true);
        });

        using var stream = new MemoryStream(epubBytes, writable: false);
        Action act = () => EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);
        act.Should().Throw<EpubParseException>();
    }

    [Fact]
    public void Read_throws_when_container_points_to_missing_opf()
    {
        var epubBytes = BuildZip(archive =>
        {
            AddTextEntry(archive, "mimetype", "application/epub+zip", stored: true);

            AddTextEntry(archive, "META-INF/container.xml", ContainerXml("EPUB/package.opf"), stored: false);

            // Intentionally omit EPUB/package.opf
        });

        using var stream = new MemoryStream(epubBytes, writable: false);
        Action act = () => EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);
        act.Should().Throw<EpubParseException>();
    }

    [Fact]
    public void Read_throws_when_opf_manifest_references_missing_resource()
    {
        var epubBytes = BuildZip(archive =>
        {
            AddTextEntry(archive, "mimetype", "application/epub+zip", stored: true);

            AddTextEntry(archive, "META-INF/container.xml", ContainerXml("EPUB/package.opf"), stored: false);

            // OPF references missing.xhtml, but we do not add it to zip.
            AddTextEntry(archive, "EPUB/package.opf", MinimalOpfWithMissingXhtml(), stored: false);

            // nav.xhtml exists (required baseline for EPUB3), but points to missing.xhtml too.
            AddTextEntry(archive, "EPUB/nav.xhtml", MinimalNav("missing.xhtml", "Missing"), stored: false);
        });

        using var stream = new MemoryStream(epubBytes, writable: false);
        Action act = () => EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);
        act.Should().Throw<EpubParseException>();
    }

    private static string ContainerXml(string opfFullPath)
    {
        return
            "<?xml version=\"1.0\"?>\n" +
            "<container xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\" version=\"1.0\">\n" +
            "  <rootfiles>\n" +
            $"    <rootfile full-path=\"{opfFullPath}\" media-type=\"application/oebps-package+xml\"/>\n" +
            "  </rootfiles>\n" +
            "</container>\n";
    }

    private static string MinimalOpfWithMissingXhtml()
    {
        // Minimal EPUB3 OPF: 1 identifier, 1 title, 1 language, NAV item, and one missing spine doc.
        return
            "<?xml version=\"1.0\"?>\n" +
            "<package xmlns=\"http://www.idpf.org/2007/opf\" unique-identifier=\"pub-id\" version=\"3.2\">\n" +
            "  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n" +
            "    <dc:identifier id=\"pub-id\">urn:uuid:00000000-0000-0000-0000-000000000000</dc:identifier>\n" +
            "    <dc:title>Test</dc:title>\n" +
            "    <dc:language>en</dc:language>\n" +
            "    <meta property=\"dcterms:modified\">2025-10-18T12:00:00Z</meta>\n" +
            "  </metadata>\n" +
            "  <manifest>\n" +
            "    <item id=\"nav\" href=\"nav.xhtml\" media-type=\"application/xhtml+xml\" properties=\"nav\"/>\n" +
            "    <item id=\"c1\" href=\"missing.xhtml\" media-type=\"application/xhtml+xml\"/>\n" +
            "  </manifest>\n" +
            "  <spine>\n" +
            "    <itemref idref=\"c1\"/>\n" +
            "  </spine>\n" +
            "</package>\n";
    }

    private static string MinimalNav(string href, string title)
    {
        return
            "<?xml version=\"1.0\"?>\n" +
            "<!DOCTYPE html>\n" +
            "<html xmlns=\"http://www.w3.org/1999/xhtml\" xmlns:epub=\"http://www.idpf.org/2007/ops\">\n" +
            "  <head>\n" +
            "    <meta charset=\"utf-8\"/>\n" +
            "    <title>TOC</title>\n" +
            "  </head>\n" +
            "  <body>\n" +
            "    <nav epub:type=\"toc\">\n" +
            "      <ol>\n" +
            $"        <li><a href=\"{href}\">{title}</a></li>\n" +
            "      </ol>\n" +
            "    </nav>\n" +
            "  </body>\n" +
            "</html>\n";
    }
}
