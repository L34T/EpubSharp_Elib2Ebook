using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace EpubSharp.Tests
{
    public class EpubBookTests
    {
        [Fact(Skip = "Временно отключен: WIP")]
        public void EpubAsPlainTextTest1()
        {
            var book = EpubReader.Read(Cwd.Combine(TestFiles.SampleEpubPath));
            //File.WriteAllText(Cwd.Join("Samples/epub-assorted/boothbyg3249432494-8epub.txt", book.ToPlainText()));

            Func<string, string> normalize = text => text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var expected = File.ReadAllText(Cwd.Combine(@"Samples/epub-assorted/boothbyg3249432494-8epub.txt"));
            var actual = book.ToPlainText();
            Assert.Equal(normalize(expected), normalize(actual));

            var lines = actual.Split('\n').Select(str => str.Trim()).ToList();
            Assert.NotNull(lines.SingleOrDefault(e => e == "I. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "II. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "III. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "IV. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "V. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "VI. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "VII. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "VIII. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "IX. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "X. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XI. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XII. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XIII. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XIV. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XV. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XVI. KAPITEL."));
            Assert.NotNull(lines.SingleOrDefault(e => e == "XVII. KAPITEL."));
        }

        [Fact(Skip = "Временно отключен: WIP")]
        public void EpubAsPlainTextTest2()
        {
            var book = EpubReader.Read(Cwd.Combine(@"Samples/epub-assorted/iOS Hackers Handbook.epub"));
            //File.WriteAllText(Cwd.Join("Samples/epub-assorted/iOS Hackers Handbook.txt", book.ToPlainText()));

            Func<string, string> normalize = text => text.Replace("\r", "").Replace("\n", "").Replace(" ", "");
            var expected = File.ReadAllText(Cwd.Combine(@"Samples/epub-assorted/iOS Hackers Handbook.txt"));
            var actual = book.ToPlainText();
            Assert.Equal(normalize(expected), normalize(actual));

            var trimmed = string.Join("\n", actual.Split('\n').Select(str => str.Trim()));
            Assert.Single(Regex.Matches(trimmed, "Chapter 1\niOS Security Basics"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 2\niOS in the Enterprise"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 3\nEncryption"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 4\nCode Signing and Memory Protections"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 5\nSandboxing"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 6\nFuzzing iOS Applications"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 7\nExploitation"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 8\nReturn-Oriented Programming"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 9\nKernel Debugging and Exploitation"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 10\nJailbreaking"));
            Assert.Single(Regex.Matches(trimmed, "Chapter 11\nBaseband Attacks"));
            Assert.Single(Regex.Matches(trimmed, "How This Book Is Organized"));
            Assert.Equal(2, Regex.Matches(trimmed, "Appendix: Resources").Count);
            Assert.Equal(2, Regex.Matches(trimmed, "Case Study: Pwn2Own 2010").Count);
        }
    }
}
