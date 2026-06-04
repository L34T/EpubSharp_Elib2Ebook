using System.Collections.Generic;
using System.Xml.Linq;

namespace EpubSharp.Format
{
    internal static class NavElements
    {
        public static readonly string Html = "html";

        public static readonly string Head = "head";
        public static readonly string Title = "title";
        public static readonly string Link = "link";
        public static readonly string Meta = "meta";

        public static readonly string Body = "body";
        public static readonly string Nav = "nav";
        public static readonly string Ol = "ol";
        public static readonly string Li = "li";
        public static readonly string A = "a";
    }

    /// <summary>
    /// Represents the EPUB 3 Navigation Document (<c>nav.xhtml</c>).
    /// <para>
    /// In EPUB 3, navigation is expressed as an XHTML5 file that contains one or more
    /// <c>&lt;nav&gt;</c> elements with an <c>epub:type</c> attribute identifying the
    /// navigation purpose (e.g. <c>"toc"</c>, <c>"landmarks"</c>, <c>"page-list"</c>).
    /// This supersedes the EPUB 2 NCX format; see <see cref="NcxDocument"/> for the legacy format.
    /// </para>
    /// </summary>
    public class NavDocument
    {
        /// <summary>Parsed contents of the XHTML <c>&lt;head&gt;</c> element.</summary>
        public NavHead Head { get; internal set; } = new NavHead();

        /// <summary>Parsed contents of the XHTML <c>&lt;body&gt;</c> element.</summary>
        public NavBody Body { get; internal set; } = new NavBody();
    }

    /// <summary>
    /// Parsed representation of the <c>&lt;head&gt;</c> element in a Navigation Document.
    /// </summary>
    public class NavHead
    {
        /// <summary>
        /// The raw <c>&lt;head&gt;</c> XElement.
        /// Populated when the EPUB is read from disk; may be a minimal placeholder
        /// when the document is created programmatically via <see cref="EpubWriter"/>.
        /// </summary>
        internal XElement Dom { get; set; }

        /// <summary>The value of the <c>&lt;title&gt;</c> element, or <c>null</c> if absent.</summary>
        public string Title { get; internal set; }

        /// <summary>All <c>&lt;link&gt;</c> elements (e.g. stylesheet references).</summary>
        public IList<NavHeadLink> Links { get; internal set; } = new List<NavHeadLink>();

        /// <summary>All <c>&lt;meta&gt;</c> elements in the head.</summary>
        public IList<NavMeta> Metas { get; internal set; } = new List<NavMeta>();
    }

    /// <summary>A <c>&lt;link&gt;</c> element inside the Navigation Document head.</summary>
    public class NavHeadLink
    {
        internal static class Attributes
        {
            public static readonly XName Href = "href";
            public static readonly XName Rel = "rel";
            public static readonly XName Type = "type";
            public static readonly XName Class = "class";
            public static readonly XName Title = "title";
            public static readonly XName Media = "media";
        }

        /// <summary>Value of the <c>href</c> attribute.</summary>
        public string Href { get; internal set; }
        /// <summary>Value of the <c>rel</c> attribute (e.g. <c>"stylesheet"</c>).</summary>
        public string Rel { get; internal set; }
        /// <summary>Value of the <c>type</c> attribute (e.g. <c>"text/css"</c>).</summary>
        public string Type { get; internal set; }
        /// <summary>Value of the <c>class</c> attribute.</summary>
        public string Class { get; internal set; }
        /// <summary>Value of the <c>title</c> attribute.</summary>
        public string Title { get; internal set; }
        /// <summary>Value of the <c>media</c> attribute (CSS media query).</summary>
        public string Media { get; internal set; }
    }

    /// <summary>A <c>&lt;meta&gt;</c> element inside the Navigation Document head.</summary>
    public class NavMeta
    {
        internal static class Attributes
        {
            public static readonly XName Name = "name";
            public static readonly XName Content = "content";
            public static readonly XName Charset = "charset";
        }

        /// <summary>Value of the <c>name</c> attribute.</summary>
        public string Name { get; internal set; }
        /// <summary>Value of the <c>content</c> attribute.</summary>
        public string Content { get; internal set; }
        /// <summary>Value of the <c>charset</c> attribute.</summary>
        public string Charset { get; internal set; }
    }

    /// <summary>
    /// Parsed representation of the <c>&lt;body&gt;</c> element in a Navigation Document.
    /// Contains one or more <see cref="NavNav"/> instances for different navigation purposes.
    /// </summary>
    public class NavBody
    {
        /// <summary>
        /// The raw <c>&lt;body&gt;</c> XElement from the parsed XHTML or created
        /// programmatically by <see cref="EpubWriter"/>.
        /// Used by <see cref="EpubSharp.Format.Writers.NavWriter"/> to serialise the document and by
        /// <see cref="EpubWriter"/> to manipulate the TOC structure.
        /// </summary>
        internal XElement Dom { get; set; }

        /// <summary>All <c>&lt;nav&gt;</c> elements found inside the body.</summary>
        public IList<NavNav> Navs { get; internal set; } = new List<NavNav>();
    }

    /// <summary>
    /// A single <c>&lt;nav&gt;</c> element from the Navigation Document body.
    /// The <c>epub:type</c> attribute identifies the navigation purpose
    /// (<c>"toc"</c>, <c>"landmarks"</c>, <c>"page-list"</c>, …).
    /// </summary>
    public class NavNav
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Class = "class";
            /// <summary>The <c>epub:type</c> attribute in the OPS namespace.</summary>
            public static readonly XName Type = Constants.OpsNamespace + "type";
            public static readonly XName Hidden = Constants.OpsNamespace + "hidden";

            internal static class TypeValues
            {
                public const string Toc = "toc";
                public const string Landmarks = "landmarks";
                public const string PageList = "page-list";
            }
        }

        /// <summary>
        /// The raw <c>&lt;nav&gt;</c> XElement.
        /// Populated when the EPUB is read; contains the full sub-tree including
        /// the <c>&lt;ol&gt;</c> list with chapter links.
        /// </summary>
        internal XElement Dom { get; set; }

        /// <summary>Value of the <c>epub:type</c> attribute (e.g. <c>"toc"</c>).</summary>
        public string Type { get; internal set; }
        /// <summary>Value of the <c>id</c> attribute.</summary>
        public string Id { get; internal set; }
        /// <summary>Value of the <c>class</c> attribute.</summary>
        public string Class { get; internal set; }
        /// <summary>Value of the <c>epub:hidden</c> attribute, or <c>null</c> if the nav is visible.</summary>
        public string Hidden { get; internal set; }
    }
}
