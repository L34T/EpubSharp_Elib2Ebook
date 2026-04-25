using System.IO;
using System.Reflection;

namespace EpubSharp.Tests
{
    public static class Cwd
    {
        private static readonly string ProjectDir = Path.GetFullPath(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..", ".."));

        public static string Combine(string relativePath)
        {
            return Path.Combine(ProjectDir, relativePath);
        }
    }
}
