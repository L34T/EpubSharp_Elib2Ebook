using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EpubSharp.Format
{
    internal static class OpfElements
    {
        public static readonly XName Package = Constants.OpfNamespace + "package";

        public static readonly XName Metadata = Constants.OpfNamespace + "metadata";
        public static readonly XName Contributor = Constants.OpfMetadataNamespace + "contributor";
        public static readonly XName Coverages = Constants.OpfMetadataNamespace + "coverages";
        public static readonly XName Creator = Constants.OpfMetadataNamespace + "creator";
        public static readonly XName Date = Constants.OpfMetadataNamespace + "date";
        public static readonly XName Description = Constants.OpfMetadataNamespace + "description";
        public static readonly XName Format = Constants.OpfMetadataNamespace + "format";
        public static readonly XName Identifier = Constants.OpfMetadataNamespace + "identifier";
        public static readonly XName Language = Constants.OpfMetadataNamespace + "language";
        public static readonly XName Meta = Constants.OpfNamespace + "meta";
        public static readonly XName Publisher = Constants.OpfMetadataNamespace + "publisher";
        public static readonly XName Relation = Constants.OpfMetadataNamespace + "relation";
        public static readonly XName Rights = Constants.OpfMetadataNamespace + "rights";
        public static readonly XName Source = Constants.OpfMetadataNamespace + "source";
        public static readonly XName Subject = Constants.OpfMetadataNamespace + "subject";
        public static readonly XName Title = Constants.OpfMetadataNamespace + "title";
        public static readonly XName Type = Constants.OpfMetadataNamespace + "type";

        public static readonly XName Guide = Constants.OpfNamespace + "guide";
        public static readonly XName Reference = Constants.OpfNamespace + "reference";

        public static readonly XName Manifest = Constants.OpfNamespace + "manifest";
        public static readonly XName Item = Constants.OpfNamespace + "item";

        public static readonly XName Spine = Constants.OpfNamespace + "spine";
        public static readonly XName ItemRef = Constants.OpfNamespace + "itemref";
    }

    /// <summary>
    /// Identifies the EPUB specification version used by the package document.
    /// </summary>
    public enum EpubVersion
    {
        /// <summary>EPUB 2.0 / 2.0.1 – uses NCX for navigation and the OPF 2.0 package format.</summary>
        Epub2 = 2,

        /// <summary>
        /// EPUB 3.0 / 3.0.1 / 3.1 – uses an XHTML5 Navigation Document (<c>nav.xhtml</c>) and
        /// the OPF 3.0 package format.  The <c>version</c> attribute in the OPF file is <c>"3.0"</c>.
        /// </summary>
        Epub3 = 3,

        /// <summary>
        /// EPUB 3.2 / 3.3 / 3.4 – functionally identical to <see cref="Epub3"/> at the package level
        /// (the <c>version</c> attribute is still <c>"3.0"</c>), but signals support for the latest
        /// EPUB 3 features such as ARIA attributes and <c>rendition:*</c> metadata.
        /// </summary>
        Epub34 = 34
    }
    
    /// <summary>
    /// Represents the OPF Package Document of an EPUB file.
    /// The OPF document describes the publication: its metadata, list of resources (manifest),
    /// reading order (spine), and optional navigation guide.
    /// </summary>
    public class OpfDocument
    {
        internal static class Attributes
        {
            public static readonly XName UniqueIdentifier = "unique-identifier";
            public static readonly XName Version = "version";
        }

        /// <summary>Gets the value of the <c>unique-identifier</c> attribute, which references the
        /// <c>dc:identifier</c> element that uniquely identifies this publication.</summary>
        public string UniqueIdentifier { get; internal set; }

        /// <summary>Gets the EPUB specification version declared in the package document.</summary>
        public EpubVersion EpubVersion { get; internal set; }

        /// <summary>Gets the publication metadata (title, author, language, etc.).</summary>
        public OpfMetadata Metadata { get; internal set; } = new OpfMetadata();

        /// <summary>Gets the manifest, which lists every resource (HTML, CSS, images, fonts, …) in the publication.</summary>
        public OpfManifest Manifest { get; internal set; } = new OpfManifest();

        /// <summary>Gets the spine, which defines the default reading order of the publication.</summary>
        public OpfSpine Spine { get; internal set; } = new OpfSpine();

        /// <summary>Gets the optional EPUB 2 guide that hints reading-system navigation targets (cover, TOC, …).
        /// May be <c>null</c> for EPUB 3 publications that omit the guide element.</summary>
        public OpfGuide Guide { get; internal set; } = new OpfGuide();

        /// <summary>
        /// Searches the manifest for the cover image path.
        /// First checks for an EPUB 2-style <c>&lt;meta name="cover"/&gt;</c> element that
        /// references a manifest item by ID, then falls back to an EPUB 3-style manifest item
        /// with <c>properties="cover-image"</c>.
        /// </summary>
        /// <returns>The <c>href</c> of the cover image manifest item, or <c>null</c> if not found.</returns>
        internal string FindCoverPath()
        {
            var coverMetaItem = Metadata.FindCoverMeta();
            if (coverMetaItem != null)
            {
                var item = Manifest.Items.FirstOrDefault(e => e.Id == coverMetaItem.Text);
                if (item != null)
                {
                    return item.Href;
                }
            }

            var coverItem = Manifest.FindCoverItem();
            return coverItem?.Href;
        }

        internal string FindAndRemoveCover()
        {
            var path = FindCoverPath();
            var meta = Metadata.FindAndDeleteCoverMeta();
            // For EPUB2: meta.Text holds the cover manifest item ID (read from content="" attribute)
            // For EPUB3 with EPUB2-style meta: meta.Text is empty → fall back to FindCoverItem() via null
            var coverId = string.IsNullOrEmpty(meta?.Text) ? null : meta.Text;
            Manifest.DeleteCoverItem(coverId);
            return path;
        }
        
        /// <summary>
        /// Locates the NCX navigation document path declared in the manifest (EPUB 2 and
        /// some hybrid EPUB 3 publications).  Looks first for a manifest item with
        /// <c>media-type="application/x-dtbncx+xml"</c>, then falls back to the <c>toc</c>
        /// attribute of the <c>&lt;spine&gt;</c> element.
        /// </summary>
        /// <returns>The relative NCX href, or <c>null</c> if no NCX is declared.</returns>
        internal string FindNcxPath()
        {
            string path = null;

            var ncxItem = Manifest.Items.FirstOrDefault(e => e.MediaType == "application/x-dtbncx+xml");
            if (ncxItem != null)
            {
                path = ncxItem.Href;
            }
            else
            {
                // If we can't find the toc by media-type then try to look for id of the item in the spine attributes as
                // according to http://www.idpf.org/epub/20/spec/OPF_2.0.1_draft.htm#Section2.4.1.2,
                // "The item that describes the NCX must be referenced by the spine toc attribute."

                if (!string.IsNullOrWhiteSpace(Spine.Toc))
                {
                    var tocItem = Manifest.Items.FirstOrDefault(e => e.Id == Spine.Toc);
                    if (tocItem != null)
                    {
                        path = tocItem.Href;
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// Locates the EPUB 3 Navigation Document path declared in the manifest.
        /// Looks for the manifest item that carries <c>properties="nav"</c>.
        /// </summary>
        /// <returns>The relative nav href, or <c>null</c> if no nav item is declared.</returns>
        internal string FindNavPath()
        {
            var navItem = Manifest.Items.FirstOrDefault(e => e.Properties.Contains("nav"));
            return navItem?.Href;
        }
    }

    /// <summary>
    /// Holds all Dublin-Core and OPF-specific metadata elements found in the
    /// <c>&lt;metadata&gt;</c> section of the OPF package document.
    /// </summary>
    public class OpfMetadata
    {
        /// <summary>Gets the publication titles (<c>dc:title</c> elements).</summary>
        public IList<string> Titles { get; internal set; } = new List<string>();
        public IList<string> Subjects { get; internal set; } = new List<string>();
        public IList<string> Descriptions { get; internal set; } = new List<string>();
        public IList<string> Publishers { get; internal set; } = new List<string>();
        public IList<OpfMetadataCreator> Creators { get; internal set; } = new List<OpfMetadataCreator>();
        public IList<OpfMetadataCreator> Contributors { get; internal set; } = new List<OpfMetadataCreator>();
        public IList<OpfMetadataDate> Dates { get; internal set; } = new List<OpfMetadataDate>();
        public IList<string> Types { get; internal set; } = new List<string>();
        public IList<string> Formats { get; internal set; } = new List<string>();
        public IList<OpfMetadataIdentifier> Identifiers { get; internal set; } = new List<OpfMetadataIdentifier>();
        public IList<string> Sources { get; internal set; } = new List<string>();
        public IList<string> Languages { get; internal set; } = new List<string>();
        public IList<string> Relations { get; internal set; } = new List<string>();
        public IList<string> Coverages { get; internal set; } = new List<string>();
        public IList<string> Rights { get; internal set; } = new List<string>();
        public IList<OpfMetadataMeta> Metas { get; internal set; } = new List<OpfMetadataMeta>();

        internal OpfMetadataMeta FindCoverMeta()
        {
            return Metas.FirstOrDefault(metaItem => metaItem.Name == "cover");
        }

        internal OpfMetadataMeta FindAndDeleteCoverMeta()
        {
            var meta = FindCoverMeta();
            if (meta == null) return null;
            Metas.Remove(meta);
            return meta;
        }
    }

    public class OpfMetadataDate
    {
        internal static class Attributes
        {
            public static readonly XName Event = Constants.OpfNamespace + "event";
        }

        public string Text { get; internal set; }

        /// <summary>
        /// i.e. "modification"
        /// </summary>
        public string Event { get; internal set; }
    }

    public class OpfMetadataCreator
    {
        internal static class Attributes
        {
            public static readonly XName Role = Constants.OpfNamespace + "role";
            public static readonly XName FileAs = Constants.OpfNamespace + "file-as";
            public static readonly XName AlternateScript = Constants.OpfNamespace + "alternate-script";
        }

        public string Text { get; internal set; }
        public string Role { get; internal set; }
        public string FileAs { get; internal set; }
        public string AlternateScript { get; internal set; }
    }

    public class OpfMetadataIdentifier
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Scheme = "scheme";
        }

        public string Id { get; internal set; }
        public string Scheme { get; internal set; }
        public string Text { get; internal set; }
    }

    public class OpfMetadataMeta
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Name = "name";
            public static readonly XName Refines = "refines";
            public static readonly XName Scheme = "scheme";
            public static readonly XName Property = "property";
            public static readonly XName Content = "content";
        }

        public string Name { get; internal set; }
        public string Id { get; internal set; }
        public string Refines { get; internal set; }
        public string Property { get; internal set; }
        public string Scheme { get; internal set; }
        public string Text { get; internal set; }
        public string Content { get; internal set; }
    }

    public class OpfManifest
    {
        internal const string ManifestItemCoverImageProperty = "cover-image";

        public IList<OpfManifestItem> Items { get; internal set; } = new List<OpfManifestItem>();

        internal OpfManifestItem FindCoverItem()
        {
            return Items.FirstOrDefault(e => e.Properties.Contains(ManifestItemCoverImageProperty));
        }

        internal void DeleteCoverItem(string id = null)
        {
            var item = id != null ? Items.FirstOrDefault(e => e.Id == id) : FindCoverItem();
            if (item != null)
            {
                Items.Remove(item);
            }
        }
    }

    public class OpfManifestItem
    {
        internal static class Attributes
        {
            public static readonly XName Fallback = "fallback";
            public static readonly XName FallbackStyle = "fallback-style";
            public static readonly XName Href = "href";
            public static readonly XName Id = "id";
            public static readonly XName MediaType = "media-type";
            public static readonly XName Properties = "properties";
            public static readonly XName RequiredModules = "required-modules";
            public static readonly XName RequiredNamespace = "required-namespace";
        }

        public string Id { get; internal set; }
        public string Href { get; internal set; }
        public IList<string> Properties { get; internal set; } = new List<string>();
        public string MediaType { get; internal set; }
        public string RequiredNamespace { get; internal set; }
        public string RequiredModules { get; internal set; }
        public string Fallback { get; internal set; }
        public string FallbackStyle { get; internal set; }

        public override string ToString()
        {
            return $"Id: {Id}, Href = {Href}, MediaType = {MediaType}";
        }
    }

    public class OpfSpine
    {
        internal static class Attributes
        {
            public static readonly XName Toc = "toc";
        }

        public string Toc { get; internal set; }
        public IList<OpfSpineItemRef> ItemRefs { get; internal set; } = new List<OpfSpineItemRef>();
    }

    public class OpfSpineItemRef
    {
        internal static class Attributes
        {
            public static readonly XName IdRef = "idref";
            public static readonly XName Linear = "linear";
            public static readonly XName Id = "id";
            public static readonly XName Properties = "properties";
        }

        public string IdRef { get; internal set; }
        public bool Linear { get; internal set; }
        public string Id { get; internal set; }
        public IList<string> Properties { get; internal set; } = new List<string>();

        public override string ToString()
        {
            return "IdRef: " + IdRef;
        }
    }

    public class OpfGuide
    {
        public IList<OpfGuideReference> References { get; internal set; } = new List<OpfGuideReference>();
    }

    public class OpfGuideReference
    {
        internal static class Attributes
        {
            public static readonly XName Title = "title";
            public static readonly XName Type = "type";
            public static readonly XName Href = "href";
        }

        public string Type { get; internal set; }
        public string Title { get; internal set; }
        public string Href { get; internal set; }

        public override string ToString()
        {
            return $"Type: {Type}, Href: {Href}";
        }
    }
}
