using System;

namespace EpubSharp.Extensions
{
    internal static class PathExt
    {
        public static string GetDirectoryPath(string filePath)
        {
            var lastSlashIndex = filePath.LastIndexOf('/');
            var dir = lastSlashIndex == -1 ? string.Empty : filePath[..lastSlashIndex];
            if (dir == "/")
            {
                dir = "";
            }

            return dir;
        }

        public static string Combine(string directory, string filename)
        {
            string ensurePrefix(string str, string prefix) =>
                str.StartsWith(prefix) ? str : prefix + str;

            if (string.IsNullOrEmpty(directory) || filename.StartsWith('/'))
            {
                return ensurePrefix(filename, "/");
            }

            if (directory.EndsWith('/'))
            {
                directory = directory[..^1];
            }

            while (true)
            {
                if (filename.StartsWith("../"))
                {
                    var newDir = GetDirectoryPath(directory);
                    if (newDir == directory)
                    {
                        throw new InvalidOperationException(
                            $"There is no room to normalize '../'. Directory={directory}, filename={filename}");
                    }

                    directory = newDir;
                    filename = filename[3..];
                }
                else if (filename.StartsWith("./"))
                {
                    filename = filename[2..];
                }
                else
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(directory))
            {
                return ensurePrefix(filename, "/");
            }
            else
            {
                if (!directory.StartsWith('/')) directory = "/" + directory;
                return string.Concat(directory, "/", filename);
            }
        }
    }
}
