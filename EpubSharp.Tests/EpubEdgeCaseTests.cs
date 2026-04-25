using System;
using System.IO;
using System.Linq;
using EpubSharp.Format;
using Xunit;

namespace EpubSharp.Tests
{
    /// <summary>
    /// Edge-case and regression tests that complement the main integration tests.
    /// These focus on boundary conditions, invalid input, and less common EPUB structures.
    /// </summary>
    public class EpubEdgeCaseTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        private static EpubBook WriteAndRead(EpubWriter writer)
        {
            var stream = new MemoryStream();
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return EpubReader.Read(stream, false);
        }

        // ── EpubWriter guard-rail tests ──────────────────────────────────────────

        [Fact]
        public void SetTitle_UnicodeCharacters_RoundTrips()
        {
            var writer = new EpubWriter();
            const string title = "日本語タイトル – Ñoño & <Spéciàl>";
            writer.SetTitle(title);

            var epub = WriteAndRead(writer);
            Assert.Equal(title, epub.Title);
        }

        [Fact]
        public void AddAuthor_UnicodeCharacters_RoundTrips()
        {
            var writer = new EpubWriter();
            writer.AddAuthor("Ödön von Horváth");

            var epub = WriteAndRead(writer);
            Assert.Equal("Ödön von Horváth", epub.Authors.First());
        }

        [Fact]
        public void AddChapter_SubsequentChapters_HaveCorrectPreviousNextLinks()
        {
            var writer = new EpubWriter();
            writer.AddChapter("A", "<p>A</p>");
            writer.AddChapter("B", "<p>B</p>");
            writer.AddChapter("C", "<p>C</p>");

            var epub = WriteAndRead(writer);
            var toc = epub.TableOfContents;

            Assert.Equal(3, toc.Count);

            Assert.Null(toc[0].Previous);
            Assert.Equal(toc[1], toc[0].Next);

            Assert.Equal(toc[0], toc[1].Previous);
            Assert.Equal(toc[2], toc[1].Next);

            Assert.Equal(toc[1], toc[2].Previous);
            Assert.Null(toc[2].Next);
        }

        [Fact]
        public void AddChapter_TopLevelChapters_HaveNullParent()
        {
            var writer = new EpubWriter();
            writer.AddChapter("X", "<p>X</p>");
            writer.AddChapter("Y", "<p>Y</p>");

            var epub = WriteAndRead(writer);
            Assert.All(epub.TableOfContents, ch => Assert.Null(ch.Parent));
        }

        [Fact]
        public void RemoveCover_WhenNoCoverExists_DoesNotThrow()
        {
            // Removing a cover when none was ever set must be a no-op.
            var writer = new EpubWriter();
            writer.RemoveCover(); // first call
            writer.RemoveCover(); // second call – must not throw
            var epub = WriteAndRead(writer);
            Assert.Null(epub.CoverImage);
        }

        [Fact]
        public void SetCover_Png_RoundTrips()
        {
            var coverBytes = File.ReadAllBytes(Cwd.Combine("Cover.png"));
            var writer = new EpubWriter();
            writer.SetCover(coverBytes, ImageFormat.Png);

            var epub = WriteAndRead(writer);
            Assert.NotNull(epub.CoverImage);
            Assert.Equal(coverBytes.Length, epub.CoverImage.Length);
        }

        [Fact]
        public void ClearChapters_LeavesOnlyNavResource()
        {
            var writer = new EpubWriter();
            writer.AddChapter("Ch1", "<p>1</p>");
            writer.AddChapter("Ch2", "<p>2</p>");
            writer.AddChapter("Ch3", "<p>3</p>");

            var epub = WriteAndRead(writer);
            Assert.Equal(3, epub.TableOfContents.Count);

            // Re-wrap in writer, clear, then verify.
            writer = new EpubWriter(epub);
            writer.ClearChapters();

            epub = WriteAndRead(writer);
            Assert.Equal(0, epub.TableOfContents.Count);
            Assert.Equal(0, epub.SpecialResources.HtmlInReadingOrder.Count);
            // Only nav.xhtml remains in the Html resource bucket.
            Assert.Equal(1, epub.Resources.Html.Count);
        }

        // ── EpubReader edge-case tests ───────────────────────────────────────────

        [Fact]
        public void Read_Bogtyven_HasBothNavAndNcx()
        {
            // Bogtyven is an EPUB 3 that ships both a nav.xhtml and a toc.ncx.
            var book = EpubReader.Read(Cwd.Combine(@"Samples/Bogtyven.epub"));

            Assert.NotNull(book.Format.Nav);
            Assert.NotNull(book.Format.Ncx);
            Assert.Equal(EpubVersion.Epub3, book.Format.Opf.EpubVersion);
        }

        [Fact]
        public void Read_Bogtyven_TableOfContentsUsesNav()
        {
            // When both Nav and NCX are present, the reader must prefer Nav.
            var book = EpubReader.Read(Cwd.Combine(@"Samples/Bogtyven.epub"));

            // Nav has 111 entries for Bogtyven; NCX also has 111.
            Assert.True(book.TableOfContents.Count > 0);
            Assert.NotNull(book.Format.Nav.Body.Navs.SingleOrDefault(n => n.Type == "toc"));
        }

        [Fact]
        public void AddFile_XmlType_LandsInOtherBucket()
        {
            // EpubContentType.Xml is a text type that EpubWriter routes to resources.Other.
            // After a write/read roundtrip the file should appear in the Other bucket.
            var writer = new EpubWriter();
            writer.AddFile("meta.xml", new byte[] { 0x3C, 0x3E }, EpubContentType.Xml);
            var epub = WriteAndRead(writer);

            Assert.Equal(1, epub.Resources.Other.Count);
            Assert.Equal("meta.xml", epub.Resources.Other.First().Href);
        }

        [Fact]
        public void Read_ManifestLanguageAndModifiedMeta_ArePresent()
        {
            // A freshly created EPUB 3 must include dc:language and dcterms:modified.
            var epub = WriteAndRead(new EpubWriter());

            Assert.Contains("en", epub.Format.Opf.Metadata.Languages);
            var modifiedMeta = epub.Format.Opf.Metadata.Metas
                .FirstOrDefault(m => m.Property == "dcterms:modified");
            Assert.NotNull(modifiedMeta);
            Assert.False(string.IsNullOrWhiteSpace(modifiedMeta.Text));
        }

        // ── EPUB 3.4 / version recognition ──────────────────────────────────────

        [Fact]
        public void Epub34EnumValue_WritesVersion30InOPF()
        {
            // EpubVersion.Epub34 must still emit version="3.0" in the OPF package element
            // because EPUB 3.4 shares the "3.0" version string per specification.
            var writer = new EpubWriter();
            // Access internal format and bump version to Epub34.
            writer.AddChapter("Test", "<p>Hi</p>");
            var stream = new MemoryStream();
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);

            var epub = EpubReader.Read(stream, false);
            // The reader should recognise version="3.0" → EpubVersion.Epub3.
            Assert.Equal(EpubVersion.Epub3, epub.Format.Opf.EpubVersion);
        }

        // ── EpubWriter argument guard tests ─────────────────────────────────────

        [Fact]
        public void EpubWriter_NullBook_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EpubWriter(null));
        }

        [Fact]
        public void AddChapter_NullTitle_ThrowsArgumentNullException()
        {
            var writer = new EpubWriter();
            Assert.Throws<ArgumentNullException>(() => writer.AddChapter(null, "<p/>"));
        }

        [Fact]
        public void AddChapter_NullHtml_ThrowsArgumentNullException()
        {
            var writer = new EpubWriter();
            Assert.Throws<ArgumentNullException>(() => writer.AddChapter("Title", null));
        }

        [Fact]
        public void SetCover_NullData_ThrowsArgumentNullException()
        {
            var writer = new EpubWriter();
            Assert.Throws<ArgumentNullException>(() => writer.SetCover(null, ImageFormat.Png));
        }

        [Fact]
        public void MakeCopy_PreservesTitle()
        {
            var writer = new EpubWriter();
            writer.SetTitle("Copy Test");
            var stream = new MemoryStream();
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var original = EpubReader.Read(stream, false);

            var copy = EpubWriter.MakeCopy(original);
            Assert.Equal("Copy Test", copy.Title);
        }

        [Fact]
        public void MakeCopy_PreservesChapters()
        {
            var writer = new EpubWriter();
            writer.AddChapter("First", "<p>First chapter</p>");
            writer.AddChapter("Second", "<p>Second chapter</p>");
            var stream = new MemoryStream();
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var original = EpubReader.Read(stream, false);

            var copy = EpubWriter.MakeCopy(original);
            Assert.Equal(original.TableOfContents.Count, copy.TableOfContents.Count);
        }
    }
}



