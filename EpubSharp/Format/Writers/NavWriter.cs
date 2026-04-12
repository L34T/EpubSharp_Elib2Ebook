using System;
using System.Xml.Linq;

namespace EpubSharp.Format.Writers
{
    internal static class NavWriter
    {
        public static string Format(NavDocument nav)
        {
            ArgumentNullException.ThrowIfNull(nav);
            if (nav.Head?.Dom == null) throw new ArgumentException("Nav.Head.Dom is null", nameof(nav));
            if (nav.Body?.Dom == null) throw new ArgumentException("Nav.Body.Dom is null", nameof(nav));

            var html = new XElement(Constants.XhtmlNamespace + NavElements.Html,
                new XAttribute(XNamespace.Xmlns + "epub", Constants.OpsNamespace),
                nav.Head.Dom,
                nav.Body.Dom);

            var xml = Constants.XmlDeclaration + "\n" + Constants.Html5Doctype + "\n" + html;
            return xml;
        }
    }
}

