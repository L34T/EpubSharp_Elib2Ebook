using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace EpubSharp.Tests;

public class EpubArchiveTests
{
    [Fact]
    public async Task FindEntryTest()
    {
        var path = Path.Combine(Path.GetTempPath(), $"epubsharp-test-{Path.GetRandomFileName()}.epub");
        try
        {
            await using (var stream = File.Create(path))
            {
                var writer = new EpubWriter();
                writer.SetTitle("Test Book");
                writer.AddChapter("Chapter 1", "<html><body><p>Hi</p></body></html>");
                await writer.Write(stream, []);
            }

            var archive = new EpubArchive(path);
            Assert.NotNull(archive.FindEntry("META-INF/container.xml"));
            Assert.Null(archive.FindEntry("UNEXISTING_ENTRY"));
        }
        finally
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
        }
    }
}
