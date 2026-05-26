#nullable enable
using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat;

public class Epub201CompatibilityTests
{
    [Fact]
    public void Epub2_minimal_fixture_is_readable_and_toc_is_loaded_from_ncx()
    {
        var epubBytes = BuildZip(archive =>
        {
            // EPUB requires mimetype to be the first entry and uncompressed.
            AddTextEntry(archive, "mimetype", "application/epub+zip", stored: true);

            AddTextEntry(archive, "META-INF/container.xml", ContainerXml("OPS/package.opf"), stored: false);

            AddTextEntry(archive, "OPS/package.opf", MinimalOpf20(), stored: false);
            AddTextEntry(archive, "OPS/toc.ncx", MinimalNcx(), stored: false);
            AddTextEntry(archive, "OPS/c1.xhtml", MinimalXhtml(), stored: false);
        });

        using var stream = new MemoryStream(epubBytes, writable: false);
        var book = EpubReader.Read(stream, leaveOpen: true, Encoding.UTF8);

        book.Format.Opf.EpubVersion.Should().Be(EpubSharp.Format.EpubVersion.Epub2);
        book.Format.Nav.Should().BeNull("EPUB2 has no nav.xhtml by default");

        book.TableOfContents.Should().HaveCount(1);
        book.TableOfContents[0].Title.Should().Be("Chapter 1");
        book.TableOfContents[0].RelativePath.Should().Be("c1.xhtml");

        book.SpecialResources.HtmlInReadingOrder.Should().HaveCount(1);
        book.SpecialResources.HtmlInReadingOrder[0].Href.Should().Be("c1.xhtml");

        book.Resources.Html.Should().ContainSingle(h => h.Href == "c1.xhtml");
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

    private static string MinimalOpf20()
    {
        // OPF 2.0 minimal package with NCX.
        // - spine@toc is required for EPUB2.
        // - nav.xhtml is not used.
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<package xmlns=\"http://www.idpf.org/2007/opf\" unique-identifier=\"BookId\" version=\"2.0\">\n" +
            "  <metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n" +
            "    <dc:title>Test EPUB2</dc:title>\n" +
            "    <dc:language>en</dc:language>\n" +
            "    <dc:identifier id=\"BookId\">urn:uuid:00000000-0000-0000-0000-000000000000</dc:identifier>\n" +
            "  </metadata>\n" +
            "  <manifest>\n" +
            "    <item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\"/>\n" +
            "    <item id=\"c1\" href=\"c1.xhtml\" media-type=\"application/xhtml+xml\"/>\n" +
            "  </manifest>\n" +
            "  <spine toc=\"ncx\">\n" +
            "    <itemref idref=\"c1\"/>\n" +
            "  </spine>\n" +
            "</package>\n";
    }

    private static string MinimalNcx()
    {
        // Minimal NCX for EPUB2.
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<ncx xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" version=\"2005-1\">\n" +
            "  <head>\n" +
            "    <meta name=\"dtb:uid\" content=\"urn:uuid:00000000-0000-0000-0000-000000000000\"/>\n" +
            "    <meta name=\"dtb:depth\" content=\"1\"/>\n" +
            "    <meta name=\"dtb:totalPageCount\" content=\"0\"/>\n" +
            "    <meta name=\"dtb:maxPageNumber\" content=\"0\"/>\n" +
            "  </head>\n" +
            "  <docTitle><text>Test EPUB2</text></docTitle>\n" +
            "  <navMap>\n" +
            "    <navPoint id=\"navPoint-1\" playOrder=\"1\">\n" +
            "      <navLabel><text>Chapter 1</text></navLabel>\n" +
            "      <content src=\"c1.xhtml\"/>\n" +
            "    </navPoint>\n" +
            "  </navMap>\n" +
            "</ncx>\n";
    }

    private static string MinimalXhtml()
    {
        return
            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n" +
            "<!DOCTYPE html>\n" +
            "<html xmlns=\"http://www.w3.org/1999/xhtml\">\n" +
            "  <head><title>Chapter 1</title></head>\n" +
            "  <body><p>Hello EPUB2</p></body>\n" +
            "</html>\n";
    }
}

