namespace EpubSharp.Format
{
    /// <summary>
    /// Holds the absolute archive paths of the key EPUB structural files resolved during reading.
    /// All paths are ZIP-relative and start with the root of the archive (no leading slash after
    /// <see cref="ZipArchiveExt.GetEntryImproved"/> normalisation).
    /// </summary>
    public class EpubFormatPaths
    {
        /// <summary>Absolute path of the OCF container file (<c>META-INF/container.xml</c>).</summary>
        public string OcfAbsolutePath { get; internal set; }

        /// <summary>Absolute path of the OPF package document declared in the OCF container.</summary>
        public string OpfAbsolutePath { get; internal set; }

        /// <summary>
        /// Absolute path of the NCX navigation file, or <c>null</c> if the publication does not
        /// include an NCX (pure EPUB 3 publications omit it).
        /// </summary>
        public string NcxAbsolutePath { get; internal set; }

        /// <summary>
        /// Absolute path of the EPUB 3 Navigation Document (<c>nav.xhtml</c>), or <c>null</c>
        /// for EPUB 2 publications that use only NCX navigation.
        /// </summary>
        public string NavAbsolutePath { get; internal set; }
    }

    /// <summary>
    /// Aggregates all parsed EPUB structural documents for one publication.
    /// <para>
    /// An EPUB 3 publication typically has: <see cref="Ocf"/>, <see cref="Opf"/>, 
    /// <see cref="Nav"/> (and optionally <see cref="Ncx"/> for backwards compatibility).
    /// An EPUB 2 publication has: <see cref="Ocf"/>, <see cref="Opf"/>, <see cref="Ncx"/>.
    /// </para>
    /// </summary>
    public class EpubFormat
    {
        /// <summary>Resolved absolute paths of the key structural files in the archive.</summary>
        public EpubFormatPaths Paths { get; internal set; } = new EpubFormatPaths();

        /// <summary>Parsed OCF container document (<c>META-INF/container.xml</c>).</summary>
        public OcfDocument Ocf { get; internal set; }

        /// <summary>Parsed OPF package document (metadata, manifest, spine, guide).</summary>
        public OpfDocument Opf { get; internal set; }

        /// <summary>
        /// Parsed EPUB 2 NCX navigation document, or <c>null</c> for pure EPUB 3 publications.
        /// </summary>
        public NcxDocument Ncx { get; internal set; }

        /// <summary>
        /// Parsed EPUB 3 Navigation Document (<c>nav.xhtml</c>), or <c>null</c> for
        /// EPUB 2 publications that lack a nav item.
        /// </summary>
        public NavDocument Nav { get; internal set; }
    }
}
