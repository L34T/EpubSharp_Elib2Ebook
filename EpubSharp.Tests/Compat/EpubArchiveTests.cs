using System.Threading.Tasks;
using Xunit;
using static EpubSharp.Tests.TestHelpers.EpubTestHelpers;

namespace EpubSharp.Tests.Compat
{
    public class EpubArchiveTests
    {
        [Fact]
        public async Task FindEntryTest()
        {
            // Create a test EPUB in memory to avoid file system dependency and locking issues on Windows
            var epubBytes = await WriteEpubAsync(writer =>
            {
                writer.SetTitle("Test Book");
                writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");
            });

            // Use the byte array constructor which is cross-platform safe
            var archive = new EpubArchive(epubBytes);

            Assert.NotNull(archive.FindEntry("META-INF/container.xml"));
            Assert.Null(archive.FindEntry("UNEXISTING_ENTRY"));
        }
    }
}
