using Xunit;

namespace EpubSharp.Tests
{
    public class EpubArchiveTests
    {
        [Fact]
        public void FindEntryTest()
        {
            var archive = new EpubArchive(Cwd.Combine("C:/Bogtyven.epub"));
            Assert.NotNull(archive.FindEntry("META-INF/container.xml"));
            Assert.Null(archive.FindEntry("UNEXISTING_ENTRY"));
        }
    }
}
