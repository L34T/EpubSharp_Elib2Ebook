using System.Collections.Generic;
using System.Linq;
using System.Text;
using EpubSharp.Format;
using EpubSharp.Misc;

namespace EpubSharp
{
    /// <summary>
    /// Represents a fully parsed EPUB publication, including its metadata, resources,
    /// table of contents and cover image.
    /// Obtained via <see cref="EpubReader.Read(string,System.Text.Encoding)"/>.
    /// </summary>
    public class EpubBook
    {
        internal const string AuthorsSeparator = ", ";

        /// <summary>
        /// Raw EPUB format structures (OCF container, OPF package, NCX, Navigation Document).
        /// Intended for advanced scenarios where direct access to the package internals is needed.
        /// </summary>
        public EpubFormat Format { get; internal set; }

        /// <summary>Gets the first <c>dc:title</c> from the OPF metadata, or <c>null</c> if absent.</summary>
        public string Title => Format.Opf.Metadata.Titles.FirstOrDefault();

        /// <summary>Gets all <c>dc:creator</c> values from the OPF metadata as a sequence of strings.</summary>
        public IEnumerable<string> Authors => Format.Opf.Metadata.Creators.Select(creator => creator.Text);

        /// <summary>
        /// All resource files declared in the OPF manifest, grouped by media type.
        /// </summary>
        public EpubResources Resources { get; internal set; }

        /// <summary>
        /// EPUB-format-specific resources: the raw OCF and OPF text files, plus the ordered
        /// list of HTML documents that make up the reading flow.
        /// </summary>
        public EpubSpecialResources SpecialResources { get; internal set; }

        /// <summary>
        /// Raw bytes of the cover image, or <c>null</c> if no cover is declared.
        /// The format is determined by the image media-type in the manifest.
        /// </summary>
        public byte[] CoverImage { get; internal set; }

        /// <summary>Top-level chapters of the publication's table of contents.</summary>
        public IList<EpubChapter> TableOfContents { get; internal set; }

        /// <summary>
        /// Returns the plain-text content of all HTML documents in reading order,
        /// with HTML tags stripped.
        /// </summary>
        public string ToPlainText()
        {
            var builder = new StringBuilder();
            foreach (var html in SpecialResources.HtmlInReadingOrder)
            {
                builder.Append(Html.GetContentAsPlainText(html.TextContent));
                builder.Append('\n');
            }
            return builder.ToString().Trim();
        }
    }

    /// <summary>
    /// Represents a single chapter (or sub-chapter) entry in the publication's table of contents.
    /// Chapters form a tree: each chapter may have <see cref="SubChapters"/>, and the
    /// <see cref="Previous"/> / <see cref="Next"/> links provide a flat, ordered traversal.
    /// </summary>
    public class EpubChapter
    {
        /// <summary>
        /// The manifest item ID for this chapter, derived from the navigation source.
        /// May be <c>null</c> when the chapter is loaded from an NCX file.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The absolute path of the chapter file within the EPUB ZIP archive
        /// (e.g. <c>/EPUB/xhtml/chapter01.xhtml</c>).
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// The path of the chapter file relative to the navigation document
        /// (e.g. <c>xhtml/chapter01.xhtml</c>).
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// The fragment identifier (without the leading <c>#</c>) that points to a specific
        /// anchor inside the chapter file, or <c>null</c> if the link targets the whole file.
        /// </summary>
        public string HashLocation { get; set; }

        /// <summary>The display title of the chapter as it appears in the navigation document.</summary>
        public string Title { get; set; }

        /// <summary>
        /// The parent chapter in the TOC hierarchy, or <c>null</c> for top-level chapters.
        /// </summary>
        public EpubChapter Parent { get; set; }

        /// <summary>
        /// The preceding chapter in the flat reading order (across the entire TOC tree),
        /// or <c>null</c> for the very first chapter.
        /// </summary>
        public EpubChapter Previous { get; set; }

        /// <summary>
        /// The following chapter in the flat reading order (across the entire TOC tree),
        /// or <c>null</c> for the very last chapter.
        /// </summary>
        public EpubChapter Next { get; set; }

        /// <summary>Nested child chapters of this chapter.</summary>
        public IList<EpubChapter> SubChapters { get; set; } = new List<EpubChapter>();

        /// <inheritdoc/>
        public override string ToString() => $"Title: {Title}, Subchapter count: {SubChapters.Count}";
    }

    /// <summary>
    /// Contains all resource files declared in the OPF manifest, bucketed by media type.
    /// The <see cref="All"/> collection is a flat union of every other collection.
    /// </summary>
    public class EpubResources
    {
        /// <summary>XHTML content documents (spine items and the navigation document).</summary>
        public IList<EpubTextFile> Html { get; internal set; } = new List<EpubTextFile>();

        /// <summary>CSS style sheets.</summary>
        public IList<EpubTextFile> Css { get; internal set; } = new List<EpubTextFile>();

        /// <summary>Raster and vector images (GIF, JPEG, PNG, SVG).</summary>
        public IList<EpubByteFile> Images { get; internal set; } = new List<EpubByteFile>();

        /// <summary>Embedded fonts (TrueType and OpenType).</summary>
        public IList<EpubByteFile> Fonts { get; internal set; } = new List<EpubByteFile>();

        /// <summary>All remaining manifest items not covered by the specific collections above
        /// (e.g. NCX files, XML documents, audio, video).</summary>
        public IList<EpubFile> Other { get; internal set; } = new List<EpubFile>();

        /// <summary>
        /// Concatenation of <see cref="Html"/>, <see cref="Css"/>, <see cref="Images"/>,
        /// <see cref="Fonts"/>, and <see cref="Other"/> — every resource in the publication.
        /// </summary>
        public IList<EpubFile> All { get; internal set; } = new List<EpubFile>();
    }

    /// <summary>
    /// Format-specific resources that are not directly part of the reading content
    /// but are needed for EPUB conformance.
    /// </summary>
    public class EpubSpecialResources
    {
        /// <summary>The raw OCF container XML (<c>META-INF/container.xml</c>).</summary>
        public EpubTextFile Ocf { get; internal set; }

        /// <summary>The raw OPF package document (e.g. <c>EPUB/package.opf</c>).</summary>
        public EpubTextFile Opf { get; internal set; }

        /// <summary>
        /// The HTML resource files in the order they appear in the OPF spine.
        /// Use this list to read the publication text in canonical reading order.
        /// </summary>
        public IList<EpubTextFile> HtmlInReadingOrder { get; internal set; } = new List<EpubTextFile>();
    }

    /// <summary>
    /// Base class for all EPUB resource files.  Provides the archive path, manifest href,
    /// content type, MIME type, and raw byte content.
    /// </summary>
    public abstract class EpubFile
    {
        /// <summary>
        /// The full path of the file within the EPUB ZIP archive
        /// (e.g. <c>/EPUB/xhtml/chapter01.xhtml</c>).
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// The href as declared in the OPF manifest item
        /// (e.g. <c>xhtml/chapter01.xhtml</c> – relative to the OPF document).
        /// </summary>
        public string Href { get; set; }

        /// <summary>The logical content type derived from the manifest media-type.</summary>
        public EpubContentType ContentType { get; set; }

        /// <summary>The raw MIME type string from the manifest (e.g. <c>"application/xhtml+xml"</c>).</summary>
        public string MimeType { get; set; }

        /// <summary>Raw byte content of the file.</summary>
        public byte[] Content { get; set; }
    }

    /// <summary>
    /// An <see cref="EpubFile"/> whose content can be interpreted as a binary blob.
    /// Used for images and fonts.
    /// </summary>
    public class EpubByteFile : EpubFile
    {
        /// <summary>
        /// Creates an <see cref="EpubTextFile"/> with the same metadata and content
        /// as this instance.  Useful when the file needs to be treated as text (e.g. CSS).
        /// </summary>
        internal EpubTextFile ToTextFile()
        {
            return new EpubTextFile
            {
                Content = Content,
                ContentType = ContentType,
                AbsolutePath = AbsolutePath,
                Href = Href,
                MimeType = MimeType
            };
        }
    }

    /// <summary>
    /// An <see cref="EpubFile"/> whose content is UTF-8 encoded text (HTML, CSS, XML, …).
    /// The <see cref="TextContent"/> property decodes and re-encodes the content transparently.
    /// </summary>
    public class EpubTextFile : EpubFile
    {
        /// <summary>
        /// Gets or sets the file content as a UTF-8 decoded string.
        /// Setting this property updates the underlying <see cref="EpubFile.Content"/> byte array.
        /// </summary>
        public string TextContent
        {
            get { return Constants.DefaultEncoding.GetString(Content, 0, Content.Length); }
            set { Content = Constants.DefaultEncoding.GetBytes(value); }
        }
    }
}
