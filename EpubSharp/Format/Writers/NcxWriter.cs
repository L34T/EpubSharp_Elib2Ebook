using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace EpubSharp.Format.Writers
{
    internal class NcxWriter
    {
        public static string Format(NcxDocument ncx)
        {
            Guard.NotNull(ncx);

            var root = new XElement(NcxElements.Ncx);
            root.Add(new XAttribute("xmlns", Constants.NcxNamespace));
            root.Add(new XAttribute("version", "2005-1"));

            root.Add(WriteHead(ncx.Meta));

            if (!string.IsNullOrWhiteSpace(ncx.DocTitle))
            {
                root.Add(new XElement(NcxElements.DocTitle, new XElement(NcxElements.Text, ncx.DocTitle)));
            }

            if (!string.IsNullOrWhiteSpace(ncx.DocAuthor))
            {
                root.Add(new XElement(NcxElements.DocAuthor, new XElement(NcxElements.Text, ncx.DocAuthor)));
            }

            var navMap = new XElement(NcxElements.NavMap);
            WriteNavPoints(navMap, ncx.NavMap.NavPoints);
            root.Add(navMap);

            if (ncx.PageList != null)
            {
                root.Add(WritePageList(ncx.PageList));
            }

            var xml = Constants.XmlDeclaration + "\n" + root;
            return xml;
        }

        private static void AddAttributeIfNotNull(XElement element, XName name, object value)
        {
            if (value != null)
            {
                element.Add(new XAttribute(name, value));
            }
        }

        private static XElement WriteHead(IList<NcxMeta> metas)
        {
            var head = new XElement(NcxElements.Head);
            foreach (var meta in metas)
            {
                var element = new XElement(NcxElements.Meta);
                AddAttributeIfNotNull(element, NcxMeta.Attributes.Name, meta.Name);
                AddAttributeIfNotNull(element, NcxMeta.Attributes.Content, meta.Content);
                AddAttributeIfNotNull(element, NcxMeta.Attributes.Scheme, meta.Scheme);
                head.Add(element);
            }
            return head;
        }

        private static XElement WritePageList(NcxPageList pageList)
        {
            var pageListElement = new XElement(NcxElements.PageList);
            if (pageList.NavInfo != null)
            {
                pageListElement.Add(new XElement(NcxElements.NavInfo, new XElement(NcxElements.Text, pageList.NavInfo.Text)));
            }

            foreach (var target in pageList.PageTargets)
            {
                var targetElement = new XElement(NcxElements.PageTarget);
                AddAttributeIfNotNull(targetElement, NcxPageTarget.Attributes.Id, target.Id);
                AddAttributeIfNotNull(targetElement, NcxPageTarget.Attributes.Value, target.Value);
                AddAttributeIfNotNull(targetElement, NcxPageTarget.Attributes.Type, target.Type?.ToString()?.ToLower());
                AddAttributeIfNotNull(targetElement, NcxPageTarget.Attributes.Class, target.Class);

                if (target.NavLabelText != null)
                {
                    targetElement.Add(new XElement(NcxElements.NavLabel, new XElement(NcxElements.Text, target.NavLabelText)));
                }

                if (target.ContentSrc != null)
                {
                    targetElement.Add(new XElement(NcxElements.Content, new XAttribute(NcxPageTarget.Attributes.ContentSrc, target.ContentSrc)));
                }

                pageListElement.Add(targetElement);
            }
            return pageListElement;
        }

        private static void WriteNavPoints(XElement root, IEnumerable<NcxNavPoint> navPoints)
        {
            foreach (var navPoint in navPoints)
            {
                var element = new XElement(NcxElements.NavPoint);

                AddAttributeIfNotNull(element, NcxNavPoint.Attributes.Id, navPoint.Id);
                AddAttributeIfNotNull(element, NcxNavPoint.Attributes.Class, navPoint.Class);
                AddAttributeIfNotNull(element, NcxNavPoint.Attributes.PlayOrder, navPoint.PlayOrder);

                if (navPoint.NavLabelText != null)
                {
                    element.Add(new XElement(NcxElements.NavLabel, new XElement(NcxElements.Text, navPoint.NavLabelText)));
                }

                if (navPoint.ContentSrc != null)
                {
                    element.Add(new XElement(NcxElements.Content, new XAttribute(NcxNavPoint.Attributes.ContentSrc, navPoint.ContentSrc)));
                }

                root.Add(element);

                if (navPoint.NavPoints.Any())
                {
                    WriteNavPoints(element, navPoint.NavPoints);
                }
            }
        }
    }
}
