namespace EpubSharp.Format
{
    public class FileMeta(string name, string path)
    {
        public string Name { get; set; } = name;
        public string Path { get; set; } = path;
    }
}
