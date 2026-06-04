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
        public static readonly XName Link = Constants.OpfNamespace + "link";
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
            public static readonly XName Prefix = "prefix";
            public static readonly XName UniqueIdentifier = "unique-identifier";
            public static readonly XName Version = "version";
        }

        /// <summary>Gets the value of the <c>unique-identifier</c> attribute, which references the
        /// <c>dc:identifier</c> element that uniquely identifies this publication.</summary>
        public string UniqueIdentifier { get; internal set; }

        /// <summary>Gets the EPUB specification version declared in the package document.</summary>
        public EpubVersion EpubVersion { get; internal set; }

        /// <summary>Gets or sets the full package version string (e.g., "3.2").</summary>
        public string PackageVersion { get; internal set; }

        /// <summary>Gets the namespace prefixes declared in the package element.</summary>
        public IDictionary<string, string> Prefixes { get; internal set; } = new Dictionary<string, string>();

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
        /// <summary>Gets the publication subjects (<c>dc:subject</c> elements).</summary>
        public IList<string> Subjects { get; internal set; } = new List<string>();
        /// <summary>Gets the publication descriptions (<c>dc:description</c> elements).</summary>
        public IList<string> Descriptions { get; internal set; } = new List<string>();
        /// <summary>Gets the publication publishers (<c>dc:publisher</c> elements).</summary>
        public IList<string> Publishers { get; internal set; } = new List<string>();
        /// <summary>Gets the publication creators (authors, illustrators, etc.).</summary>
        public IList<OpfMetadataCreator> Creators { get; internal set; } = new List<OpfMetadataCreator>();
        /// <summary>Gets the publication contributors.</summary>
        public IList<OpfMetadataCreator> Contributors { get; internal set; } = new List<OpfMetadataCreator>();
        /// <summary>Gets the publication dates.</summary>
        public IList<OpfMetadataDate> Dates { get; internal set; } = new List<OpfMetadataDate>();
        /// <summary>Gets the publication types.</summary>
        public IList<string> Types { get; internal set; } = new List<string>();
        /// <summary>Gets the publication formats.</summary>
        public IList<string> Formats { get; internal set; } = new List<string>();
        /// <summary>Gets the publication identifiers (<c>dc:identifier</c> elements).</summary>
        public IList<OpfMetadataIdentifier> Identifiers { get; internal set; } = new List<OpfMetadataIdentifier>();
        /// <summary>Gets the publication sources.</summary>
        public IList<string> Sources { get; internal set; } = new List<string>();
        /// <summary>Gets the publication languages.</summary>
        public IList<string> Languages { get; internal set; } = new List<string>();
        /// <summary>Gets the publication relations.</summary>
        public IList<string> Relations { get; internal set; } = new List<string>();
        /// <summary>Gets the publication coverages.</summary>
        public IList<string> Coverages { get; internal set; } = new List<string>();
        /// <summary>Gets the publication rights.</summary>
        public IList<string> Rights { get; internal set; } = new List<string>();
        /// <summary>Gets the publication meta elements.</summary>
        public IList<OpfMetadataMeta> Metas { get; internal set; } = new List<OpfMetadataMeta>();
        /// <summary>Gets the publication link elements (EPUB 3).</summary>
        public IList<OpfMetadataLink> Links { get; internal set; } = new List<OpfMetadataLink>();

        internal OpfMetadataMeta FindCoverMeta() => Metas.FirstOrDefault(metaItem => metaItem.Name == "cover");

        internal OpfMetadataMeta FindAndDeleteCoverMeta()
        {
            var meta = FindCoverMeta();
            if (meta == null) return null;
            Metas.Remove(meta);
            return meta;
        }
    }

    /// <summary>Represents a <c>dc:date</c> element in the OPF metadata.</summary>
    public class OpfMetadataDate
    {
        internal static class Attributes
        {
            public static readonly XName Event = Constants.OpfNamespace + "event";
        }

        /// <summary>The date value string.</summary>
        public string Text { get; internal set; }

        /// <summary>
        /// The event associated with the date (e.g. "modification", "publication").
        /// </summary>
        public string Event { get; internal set; }
    }

    /// <summary>Represents a <c>dc:creator</c> or <c>dc:contributor</c> element in the OPF metadata.</summary>
    public class OpfMetadataCreator
    {
        internal static class Attributes
        {
            public static readonly XName Role = Constants.OpfNamespace + "role";
            public static readonly XName FileAs = Constants.OpfNamespace + "file-as";
            public static readonly XName AlternateScript = Constants.OpfNamespace + "alternate-script";
        }

        /// <summary>The name of the creator.</summary>
        public string Text { get; internal set; }
        /// <summary>The role of the creator (e.g. "aut", "ill").</summary>
        public string Role { get; internal set; }
        /// <summary>The "file-as" version of the name for sorting.</summary>
        public string FileAs { get; internal set; }
        /// <summary>An alternate script version of the name.</summary>
        public string AlternateScript { get; internal set; }
    }

    /// <summary>Represents a <c>dc:identifier</c> element in the OPF metadata.</summary>
    public class OpfMetadataIdentifier
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Scheme = "scheme";
        }

        /// <summary>The XML ID of the identifier.</summary>
        public string Id { get; internal set; }
        /// <summary>The scheme of the identifier (EPUB 2 legacy).</summary>
        public string Scheme { get; internal set; }
        /// <summary>The identifier value string.</summary>
        public string Text { get; internal set; }
    }

    /// <summary>Represents a <c>meta</c> element in the OPF metadata.</summary>
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

        /// <summary>The name of the meta element (EPUB 2 style).</summary>
        public string Name { get; internal set; }
        /// <summary>The XML ID of the meta element.</summary>
        public string Id { get; internal set; }
        /// <summary>The ID of the element this meta refines (EPUB 3 style).</summary>
        public string Refines { get; internal set; }
        /// <summary>The property being defined (EPUB 3 style).</summary>
        public string Property { get; internal set; }
        /// <summary>The scheme used for the property value.</summary>
        public string Scheme { get; internal set; }
        /// <summary>The text content of the meta element.</summary>
        public string Text { get; internal set; }
        /// <summary>The content attribute value (EPUB 2 style).</summary>
        public string Content { get; internal set; }
    }

    /// <summary>Represents a <c>link</c> element in the OPF metadata (EPUB 3).</summary>
    public class OpfMetadataLink
    {
        internal static class Attributes
        {
            public static readonly XName Href = "href";
            public static readonly XName HrefLang = "hreflang";
            public static readonly XName Id = "id";
            public static readonly XName MediaType = "media-type";
            public static readonly XName Properties = "properties";
            public static readonly XName Refines = "refines";
            public static readonly XName Rel = "rel";
        }

        /// <summary>The link destination.</summary>
        public string Href { get; internal set; }
        /// <summary>The language of the linked resource.</summary>
        public string HrefLang { get; internal set; }
        /// <summary>The XML ID of the link element.</summary>
        public string Id { get; internal set; }
        /// <summary>The media-type of the linked resource.</summary>
        public string MediaType { get; internal set; }
        /// <summary>The properties associated with the link.</summary>
        public IList<string> Properties { get; internal set; } = new List<string>();
        /// <summary>The ID of the element this link refines.</summary>
        public string Refines { get; internal set; }
        /// <summary>The relationship of the linked resource.</summary>
        public string Rel { get; internal set; }
    }

    /// <summary>Represents the <c>manifest</c> section of the OPF package document.</summary>
    public class OpfManifest
    {
        internal const string ManifestItemCoverImageProperty = "cover-image";

        /// <summary>All manifest items (resources) in the publication.</summary>
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

    /// <summary>Represents a single <c>item</c> in the OPF manifest.</summary>
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

        /// <summary>The XML ID of the item.</summary>
        public string Id { get; internal set; }
        /// <summary>The href of the resource relative to the OPF file.</summary>
        public string Href { get; internal set; }
        /// <summary>Properties of the manifest item (e.g. "nav", "cover-image").</summary>
        public IList<string> Properties { get; internal set; } = new List<string>();
        /// <summary>The media-type (MIME type) of the resource.</summary>
        public string MediaType { get; internal set; }
        /// <summary>The required namespace for this item.</summary>
        public string RequiredNamespace { get; internal set; }
        /// <summary>The required modules for this item.</summary>
        public string RequiredModules { get; internal set; }
        /// <summary>The fallback item ID if this item is not supported.</summary>
        public string Fallback { get; internal set; }
        /// <summary>The fallback style ID.</summary>
        public string FallbackStyle { get; internal set; }

        /// <inheritdoc/>
        public override string ToString() => $"Id: {Id}, Href = {Href}, MediaType = {MediaType}";
    }

    /// <summary>Represents the <c>spine</c> section of the OPF package document.</summary>
    public class OpfSpine
    {
        internal static class Attributes
        {
            public static readonly XName Toc = "toc";
        }

        /// <summary>The ID of the NCX manifest item used for navigation (EPUB 2).</summary>
        public string Toc { get; internal set; }
        /// <summary>The ordered list of manifest items that define the reading order.</summary>
        public IList<OpfSpineItemRef> ItemRefs { get; internal set; } = new List<OpfSpineItemRef>();
    }

    /// <summary>Represents a single <c>itemref</c> in the OPF spine.</summary>
    public class OpfSpineItemRef
    {
        internal static class Attributes
        {
            public static readonly XName IdRef = "idref";
            public static readonly XName Linear = "linear";
            public static readonly XName Id = "id";
            public static readonly XName Properties = "properties";
        }

        /// <summary>The ID of the manifest item being referenced.</summary>
        public string IdRef { get; internal set; }
        /// <summary>Whether the item is part of the linear reading order.</summary>
        public bool Linear { get; internal set; }
        /// <summary>The XML ID of the spine reference.</summary>
        public string Id { get; internal set; }
        /// <summary>Properties of the spine reference (e.g. "page-spread-left").</summary>
        public IList<string> Properties { get; internal set; } = new List<string>();

        /// <inheritdoc/>
        public override string ToString() => "IdRef: " + IdRef;
    }

    /// <summary>Represents the legacy EPUB 2 <c>guide</c> section.</summary>
    public class OpfGuide
    {
        /// <summary>References to key publication components (cover, TOC, etc.).</summary>
        public IList<OpfGuideReference> References { get; internal set; } = new List<OpfGuideReference>();
    }

    /// <summary>Represents a single <c>reference</c> in the OPF guide.</summary>
    public class OpfGuideReference
    {
        internal static class Attributes
        {
            public static readonly XName Title = "title";
            public static readonly XName Type = "type";
            public static readonly XName Href = "href";
        }

        /// <summary>The type of the reference (e.g. "cover", "toc").</summary>
        public string Type { get; internal set; }
        /// <summary>The title of the reference.</summary>
        public string Title { get; internal set; }
        /// <summary>The href of the resource.</summary>
        public string Href { get; internal set; }

        /// <inheritdoc/>
        public override string ToString() => $"Type: {Type}, Href: {Href}";
    }
}
