using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EpubSharp.Format;
using FluentAssertions;
using Xunit;

namespace EpubSharp.Tests
{
    public class EpubWriterTests
    {
        [Fact]
        public async Task CanWriteTest()  // async Task вместо void
        {
            var book = EpubReader.Read(Cwd.Combine(TestFiles.SampleEpubPath));
            var writer = new EpubWriter(book);
    
            using var stream = new MemoryStream();
            await writer.Write(stream, Enumerable.Empty<FileMeta>());  // новый async overload
    
            // Проверяем, что записалось (не пустой)
            stream.Position.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task CanCreateEmptyEpubTest()
        {
            var epub = await WriteAndReadAsync(new EpubWriter());

            Assert.Null(epub.Title);
            Assert.Empty(epub.Authors);
            Assert.Null(epub.CoverImage);

            Assert.Empty(epub.Resources.Html);
            Assert.Empty(epub.Resources.Css);
            Assert.Empty(epub.Resources.Images);
            Assert.Empty(epub.Resources.Fonts);
            Assert.Single(epub.Resources.Other); // ncx
            
            Assert.Empty(epub.SpecialResources.HtmlInReadingOrder);
            Assert.NotNull(epub.SpecialResources.Ocf);
            Assert.NotNull(epub.SpecialResources.Opf);

            Assert.Empty(epub.TableOfContents);

            Assert.NotNull(epub.Format.Ocf);
            Assert.NotNull(epub.Format.Opf);
            Assert.NotNull(epub.Format.Ncx);
            Assert.Null(epub.Format.Nav);
        }

        [Fact]
        public async Task AddRemoveAuthorTest()
        {
            var writer = new EpubWriter();

            writer.AddAuthor("Foo Bar");
            var epub = await WriteAndReadAsync(writer);
            Assert.Single(epub.Authors);

            writer.AddAuthor("Zoo Gar");
            epub = await WriteAndReadAsync(writer);
            Assert.Equal(2, epub.Authors.Count());

            writer.RemoveAuthor("Foo Bar");
            epub = await WriteAndReadAsync(writer);
            Assert.Single(epub.Authors);
            Assert.Equal("Zoo Gar", epub.Authors.First());

            writer.RemoveAuthor("Unexisting");
            epub = await WriteAndReadAsync(writer);
            Assert.Single(epub.Authors);

            writer.ClearAuthors();
            epub = await WriteAndReadAsync(writer);
            Assert.Empty(epub.Authors);

            writer.RemoveAuthor("Unexisting");
            writer.ClearAuthors();
        }

        [Fact]
        public async Task AddRemoveTitleTest()
        {
            var writer = new EpubWriter();

            writer.SetTitle("Title1");
            var epub = await WriteAndReadAsync(writer);
            Assert.Equal("Title1", epub.Title);

            writer.SetTitle("Title2");
            epub = await WriteAndReadAsync(writer);
            Assert.Equal("Title2", epub.Title);

            writer.RemoveTitle();
            epub = await WriteAndReadAsync(writer);
            Assert.Null(epub.Title);

            writer.RemoveTitle();
        }

        [Fact(Skip = "Временно отключен: WIP")]
        public async Task SetCoverTest()
        {
            var writer = new EpubWriter();
            writer.SetCover(File.ReadAllBytes(Cwd.Combine("c://Cover.png")), ImageFormat.Png);

            var epub = await WriteAndReadAsync(writer);

            Assert.Single(epub.Resources.Images);
            Assert.NotNull(epub.CoverImage);
        }

        [Fact(Skip = "Временно отключен: WIP")]
        public async Task RemoveCoverTest()
        {
            var epub1 = EpubReader.Read(Cwd.Combine(TestFiles.SampleEpubPath));

            var writer = new EpubWriter(EpubWriter.MakeCopy(epub1));
            writer.RemoveCover();

            var epub2 = await WriteAndReadAsync(writer);

            Assert.NotNull(epub1.CoverImage);
            Assert.Null(epub2.CoverImage);
            Assert.Equal(epub1.Resources.Images.Count - 1, epub2.Resources.Images.Count);
        }

        [Fact]
        public void RemoveCoverWhenThereIsNoCoverTest()
        {
            var writer = new EpubWriter();
            writer.RemoveCover();
            writer.RemoveCover();
        }

        [Fact]
        public async Task CanAddChapterTest()
        {
            var writer = new EpubWriter();
            var chapters = new[]
            {
                writer.AddChapter("Chapter 1", "bla bla bla"),
                writer.AddChapter("Chapter 2", "foo bar")
            };
            var epub = await WriteAndReadAsync(writer);

            Assert.Equal("Chapter 1", chapters[0].Title);
            Assert.Equal("Chapter 2", chapters[1].Title);

            Assert.Equal(2, epub.TableOfContents.Count);
            for (var i = 0; i < chapters.Length; ++i)
            {
                Assert.Equal(chapters[i].Title, epub.TableOfContents[i].Title);
                Assert.Equal(chapters[i].RelativePath, epub.TableOfContents[i].RelativePath);
                Assert.Equal(chapters[i].HashLocation, epub.TableOfContents[i].HashLocation);
                Assert.Empty(chapters[i].SubChapters);
                Assert.Empty(epub.TableOfContents[i].SubChapters);
            }
        }

        [Fact]
        public async Task ClearChaptersTest()
        {
            var writer = new EpubWriter();
            writer.AddChapter("Chapter 1", "bla bla bla");
            writer.AddChapter("Chapter 2", "foo bar");
            writer.AddChapter("Chapter 3", "fooz barz");

            var epub = await WriteAndReadAsync(writer);
            Assert.Equal(3, epub.TableOfContents.Count);

            writer = new EpubWriter(epub);
            writer.ClearChapters();
            
            epub = await WriteAndReadAsync(writer);
            Assert.Empty(epub.TableOfContents);
        }

        [Fact]
        public async Task ClearBogtyvenChaptersTest()
        {
            var writer = new EpubWriter(EpubReader.Read(Cwd.Combine(TestFiles.SampleEpubPath)));
            writer.ClearChapters();

            var epub = await WriteAndReadAsync(writer);
            Assert.Empty(epub.TableOfContents);
        }

        [Fact]
        public async Task AddFileTest()
        {
            var writer = new EpubWriter();
            writer.AddFile("style.css", "body {}", EpubContentType.Css);
            writer.AddFile("img.jpeg", new byte[] { 0x42 }, EpubContentType.ImageJpeg);
            writer.AddFile("font.ttf", new byte[] { 0x24 }, EpubContentType.FontTruetype);

            var epub = await WriteAndReadAsync(writer);

            Assert.Single(epub.Resources.Css);
            Assert.Equal("style.css", epub.Resources.Css.First().Href);
            Assert.Equal("body {}", epub.Resources.Css.First().TextContent);

            Assert.Single(epub.Resources.Images);
            Assert.Equal("img.jpeg", epub.Resources.Images.First().Href);
            Assert.Single(epub.Resources.Images.First().Content);
            Assert.Equal(0x42, epub.Resources.Images.First().Content.First());

            Assert.Single(epub.Resources.Fonts);
            Assert.Equal("font.ttf", epub.Resources.Fonts.First().Href);
            Assert.Single(epub.Resources.Fonts.First().Content);
            Assert.Equal(0x24, epub.Resources.Fonts.First().Content.First());
        }

        private async Task<EpubBook> WriteAndReadAsync(EpubWriter writer)  // async Task
        {
            using var stream = new MemoryStream();
            await writer.Write(stream, Enumerable.Empty<FileMeta>());

            stream.Seek(0, SeekOrigin.Begin);
            var epub = EpubReader.Read(stream, false);
            return epub;
        }
    }
}
