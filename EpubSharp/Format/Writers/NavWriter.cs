using System;
using System.Linq;
using System.Xml.Linq;

namespace EpubSharp.Format.Writers
{
    /// <summary>
    /// Serializes an EPUB 3 <see cref="NavDocument"/> as a valid XHTML5 navigation document.
    /// The output is a self-contained XHTML file with the EPUB OPS namespace declared,
    /// suitable for the <c>nav.xhtml</c> entry in an EPUB 3 package.
    /// </summary>
    internal static class NavWriter
    {
        /// <summary>
        /// Formats the given <paramref name="nav"/> document as an XHTML5 string.
        /// </summary>
        /// <param name="nav">The navigation document to serialize.</param>
        /// <returns>A string containing the full XHTML5 navigation document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="nav"/> is <c>null</c>.</exception>
        public static string Format(NavDocument nav)
        {
            Guard.NotNull(nav);

            var xhtmlNs = Constants.XhtmlNamespace;
            var opsNs = Constants.OpsNamespace;

            // Build <head> – apply namespace if created programmatically without one
            XElement headEl;
            if (nav.Head.Dom != null)
            {
                headEl = ApplyNamespace(nav.Head.Dom, xhtmlNs);
            }
            else
            {
                headEl = new XElement(xhtmlNs + NavElements.Head);
                headEl.Add(new XElement(xhtmlNs + NavElements.Meta, new XAttribute("charset", "utf-8")));
                if (!string.IsNullOrWhiteSpace(nav.Head.Title))
                {
                    headEl.Add(new XElement(xhtmlNs + NavElements.Title, nav.Head.Title));
                }
            }

            // Build <body> – apply the XHTML namespace to any elements that were created
            // with plain string names (as done in EpubWriter() constructor).
            XElement bodyEl;
            if (nav.Body.Dom != null)
            {
                bodyEl = ApplyNamespace(nav.Body.Dom, xhtmlNs);
            }
            else
            {
                bodyEl = new XElement(xhtmlNs + NavElements.Body);
            }

            // Root <html> element with XHTML + EPUB OPS namespace declarations.
            var htmlEl = new XElement(
                xhtmlNs + NavElements.Html,
                new XAttribute(XNamespace.Xmlns + "epub", opsNs.NamespaceName),
                headEl,
                bodyEl);

            return Constants.Html5Doctype + "\n" + htmlEl;
        }

        /// <summary>
        /// Recursively applies <paramref name="ns"/> to every element whose local name has
        /// no namespace (i.e. was created with a plain string name).  Namespaced attributes
        /// (e.g. <c>epub:type</c>) are preserved as-is.
        /// </summary>
        private static XElement ApplyNamespace(XElement el, XNamespace ns)
        {
            var newName = el.Name.Namespace == XNamespace.None
                ? ns + el.Name.LocalName
                : el.Name;

            return new XElement(
                newName,
                el.Attributes(),
                el.Nodes().Select(n => n is XElement child ? (object)ApplyNamespace(child, ns) : n));
        }
    }
}
