using System.Collections.Generic;
using System.Xml.Linq;

namespace EpubSharp.Format
{
    internal static class NcxElements
    {
        public static readonly XName Ncx = Constants.NcxNamespace + "ncx";
        public static readonly XName Head = Constants.NcxNamespace + "head";
        public static readonly XName Meta = Constants.NcxNamespace + "meta";
        public static readonly XName DocTitle = Constants.NcxNamespace + "docTitle";
        public static readonly XName DocAuthor = Constants.NcxNamespace + "docAuthor";
        public static readonly XName Text = Constants.NcxNamespace + "text";
        public static readonly XName NavMap = Constants.NcxNamespace + "navMap";
        public static readonly XName NavPoint = Constants.NcxNamespace + "navPoint";
        public static readonly XName NavList = Constants.NcxNamespace + "navList";
        public static readonly XName PageList = Constants.NcxNamespace + "pageList";
        public static readonly XName NavInfo = Constants.NcxNamespace + "navInfo";
        public static readonly XName PageTarget = Constants.NcxNamespace + "pageTarget";
        public static readonly XName NavLabel = Constants.NcxNamespace + "navLabel";
        public static readonly XName NavTarget = Constants.NcxNamespace + "navTarget";
        public static readonly XName Content = Constants.NcxNamespace + "content";
    }

    /// <summary>
    /// Represents the DAISY Navigation Center eXtended (NCX) document used in EPUB 2
    /// publications for navigation (typically stored as <c>toc.ncx</c>).
    /// <para>
    /// The NCX provides a hierarchical table of contents (<see cref="NavMap"/>), an optional
    /// page list, and optional navigation lists.  In EPUB 3 this format is superseded by the
    /// XHTML5 Navigation Document; see <see cref="NavDocument"/>.
    /// </para>
    /// </summary>
    public class NcxDocument
    {
        /// <summary>The <c>&lt;head&gt;</c> meta elements (e.g. <c>dtb:uid</c>, <c>dtb:depth</c>).</summary>
        public IList<NcxMeta> Meta { get; internal set; } = new List<NcxMeta>();
        /// <summary>Publication title from the <c>&lt;docTitle&gt;</c> element.</summary>
        public string DocTitle { get; internal set; }
        /// <summary>Primary author from the <c>&lt;docAuthor&gt;</c> element, or <c>null</c>.</summary>
        public string DocAuthor { get; internal set; }
        /// <summary>The required <c>&lt;navMap&gt;</c> element containing the hierarchical TOC.</summary>
        public NcxNapMap NavMap { get; internal set; } = new NcxNapMap(); // <navMap> is a required element in NCX.
        /// <summary>Optional <c>&lt;pageList&gt;</c> element, or <c>null</c>.</summary>
        public NcxPageList PageList { get; internal set; }
        /// <summary>Optional <c>&lt;navList&gt;</c> element, or <c>null</c>.</summary>
        public NcxNavList NavList { get; internal set; }
    }

    /// <summary>A single <c>&lt;meta&gt;</c> element from the NCX <c>&lt;head&gt;</c>.</summary>
    public class NcxMeta
    {
        internal static class Attributes
        {
            public static readonly XName Name = "name";
            public static readonly XName Content = "content";
            public static readonly XName Scheme = "scheme";
        }

        public string Name { get; internal set; }
        public string Content { get; internal set; }
        public string Scheme { get; internal set; }
    }

    /// <summary>
    /// The NCX <c>&lt;navMap&gt;</c> element, which is the root of the navigation hierarchy.
    /// Contains an ordered list of <see cref="NcxNavPoint"/> entries.
    /// </summary>
    public class NcxNapMap
    {
        /// <summary>
        /// The raw <c>&lt;navMap&gt;</c> XElement, populated when reading an EPUB from disk.
        /// <c>null</c> for navMaps constructed programmatically.
        /// </summary>
        public XElement Dom { get; internal set; }

        /// <summary>Top-level navigation points (chapters).  Each may contain nested points.</summary>
        public IList<NcxNavPoint> NavPoints { get; internal set; } = new List<NcxNavPoint>();
    }

    /// <summary>
    /// A single navigation point (<c>&lt;navPoint&gt;</c>) inside an NCX <c>&lt;navMap&gt;</c>.
    /// Represents one chapter or section and may contain nested <see cref="NavPoints"/>.
    /// </summary>
    public class NcxNavPoint
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Class = "class";
            public static readonly XName PlayOrder = "playOrder";
            public static readonly XName ContentSrc = "src";
        }

        public string Id { get; internal set; }
        public string Class { get; internal set; }

        public int? PlayOrder { get; internal set; }

        // NavLabelText and ContentSrc are flattened elements for convenience.
        // In case <navLabel> or <content/> need to carry more data, then they should have a dedicated model created.
        public string NavLabelText { get; internal set; }
        public string ContentSrc { get; internal set; }
        public IList<NcxNavPoint> NavPoints { get; internal set; } = new List<NcxNavPoint>();

        public override string ToString() => $"Id: {Id}, ContentSource: {ContentSrc}";
    }

    public enum NcxPageTargetType
    {
        Front = 1,
        Normal,
        Special,
        Body
    }

    public class NcxPageList
    {
        public NcxNavInfo NavInfo { get; internal set; }

        public IList<NcxPageTarget> PageTargets { get; internal set; } = new List<NcxPageTarget>();
    }

    public class NcxNavInfo
    {
        public string Text { get; internal set; }
    }

    public class NcxPageTarget
    {
        internal static class Attributes
        {
            public static readonly XName Id = "id";
            public static readonly XName Class = "class";
            public static readonly XName Type = "type";
            public static readonly XName Value = "value";
            public static readonly XName ContentSrc = "src";
        }

        public string Id { get; internal set; }
        public string Value { get; internal set; }
        public string Class { get; internal set; }
        public NcxPageTargetType? Type { get; internal set; }
        public string NavLabelText { get; internal set; }
        public string ContentSrc { get; internal set; }
    }

    public class NcxNavList
    {
        public string Id { get; internal set; }
        public string Class { get; internal set; }
        public string Label { get; internal set; }
        public IList<NcxNavTarget> NavTargets { get; internal set; } = new List<NcxNavTarget>();
    }

    public class NcxNavTarget
    {
        public string Id { get; internal set; }
        public string Class { get; internal set; }
        public int? PlayOrder { get; internal set; }
        public string Label { get; internal set; }
        public string ContentSource { get; internal set; }
    }
}
