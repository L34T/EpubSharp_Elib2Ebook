using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EpubSharp.Tests
{
    public class EpubBookTests
    {
        [Fact]
        public void EpubAsPlainTextTest1()
        {
            var book = EpubReader.Read(Cwd.Combine(@"Samples/Bogtyven.epub"));

            var actual = book.ToPlainText();
            var expectedPath = Cwd.Combine(@"Samples/epub-assorted/boothbyg3249432494-8epub.txt");
            if (!File.Exists(expectedPath))
            {
                File.WriteAllText(expectedPath, actual);
            }

            Func<string, string> normalize = text => text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var expected = File.ReadAllText(expectedPath);
            Assert.Equal(normalize(expected), normalize(actual));

            // Bogtyven (The Book Thief, Danish edition) is structured in 10 "Del" (Parts)
            var lines = actual.Split('\n').Select(str => str.Trim()).ToList();
            Assert.NotNull(lines.SingleOrDefault(e => e == "FØRSTE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "ANDEN DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "TREDJE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "FJERDE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "FEMTE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "SJETTE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "SYVENDE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "OTTENDE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "NIENDE DEL"));
            Assert.NotNull(lines.SingleOrDefault(e => e == "TIENDE DEL"));
        }

        [Fact]
        public void EpubAsPlainTextTest2()
        {
            var book = EpubReader.Read(Cwd.Combine(@"Samples/epub-assorted/iOS Hackers Handbook.epub"));

            var actual = book.ToPlainText();
            var expectedPath = Cwd.Combine(@"Samples/epub-assorted/iOS Hackers Handbook.txt");
            if (!File.Exists(expectedPath))
            {
                File.WriteAllText(expectedPath, actual);
            }

            Func<string, string> normalize = text => text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var expected = File.ReadAllText(expectedPath);
            Assert.Equal(normalize(expected), normalize(actual));
            
            var trimmed = string.Join("\n", actual.Split('\n').Select(str => str.Trim()));
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 1\niOS Security Basics").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 2\niOS in the Enterprise").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 3\nEncryption").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 4\nCode Signing and Memory Protections").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 5\nSandboxing").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 6\nFuzzing iOS Applications").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 7\nExploitation").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 8\nReturn-Oriented Programming").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 9\nKernel Debugging and Exploitation").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 10\nJailbreaking").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "Chapter 11\nBaseband Attacks").Count);
            Assert.Equal(1, Regex.Matches(trimmed, "How This Book Is Organized").Count);
            Assert.Equal(2, Regex.Matches(trimmed, "Appendix: Resources").Count);
            Assert.Equal(2, Regex.Matches(trimmed, "Case Study: Pwn2Own 2010").Count);
        }             
    }
}
