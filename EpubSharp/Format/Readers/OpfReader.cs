#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EpubSharp.Extensions;

namespace EpubSharp.Format.Readers
{
    internal static class OpfReader
    {
        public static OpfDocument Read(XDocument xml)
        {
            Guard.NotNull(xml);
            Guard.IsTrue(xml.Root != null, "XML document has no root element.");

            var versionAttribute = (string)xml.Root.Attribute(OpfDocument.Attributes.Version);
            var epubVersion = GetAndValidateVersion(versionAttribute);

            var package = new OpfDocument
            {
                Prefixes = ParsePrefixes((string)xml.Root.Attribute(OpfDocument.Attributes.Prefix)),
                UniqueIdentifier = (string)xml.Root.Attribute(OpfDocument.Attributes.UniqueIdentifier),
                EpubVersion = epubVersion,
                PackageVersion = versionAttribute,
                Metadata = ReadMetadata(xml.Root.Element(OpfElements.Metadata), epubVersion),
                Guide = ReadGuide(xml.Root.Element(OpfElements.Guide)),
                Manifest = ReadManifest(xml.Root.Element(OpfElements.Manifest)),
                Spine = ReadSpine(xml.Root.Element(OpfElements.Spine))
            };

            return package;
        }

        private static OpfMetadata ReadMetadata(XElement metadataElement, EpubVersion epubVersion)
        {
            if (metadataElement == null) return new OpfMetadata();

            Func<XElement, OpfMetadataCreator> readCreator = elem => new OpfMetadataCreator
            {
                Role = (string)elem.Attribute(OpfMetadataCreator.Attributes.Role),
                FileAs = (string)elem.Attribute(OpfMetadataCreator.Attributes.FileAs),
                AlternateScript = (string)elem.Attribute(OpfMetadataCreator.Attributes.AlternateScript),
                Text = elem.Value
            };

            return new OpfMetadata
            {
                Creators = metadataElement.Elements(OpfElements.Creator).AsObjectList(readCreator),
                Contributors = metadataElement.Elements(OpfElements.Contributor).AsObjectList(readCreator),
                Coverages = metadataElement.Elements(OpfElements.Coverages).AsStringList(),
                Dates = metadataElement.Elements(OpfElements.Date).AsObjectList(elem => new OpfMetadataDate
                {
                    Text = elem.Value,
                    Event = (string)elem.Attribute(OpfMetadataDate.Attributes.Event)
                }),
                Descriptions = metadataElement.Elements(OpfElements.Description).AsStringList(),
                Formats = metadataElement.Elements(OpfElements.Format).AsStringList(),
                Identifiers = metadataElement.Elements(OpfElements.Identifier).AsObjectList(elem =>
                    new OpfMetadataIdentifier
                    {
                        Id = (string)elem.Attribute(OpfMetadataIdentifier.Attributes.Id),
                        Scheme = (string)elem.Attribute(OpfMetadataIdentifier.Attributes.Scheme),
                        Text = elem.Value
                    }),
                Languages = metadataElement.Elements(OpfElements.Language).AsStringList(),
                Metas = metadataElement.Elements(OpfElements.Meta).AsObjectList(elem => new OpfMetadataMeta
                {
                    Id = (string)elem.Attribute(OpfMetadataMeta.Attributes.Id),
                    Name = (string)elem.Attribute(OpfMetadataMeta.Attributes.Name),
                    Refines = (string)elem.Attribute(OpfMetadataMeta.Attributes.Refines),
                    Scheme = (string)elem.Attribute(OpfMetadataMeta.Attributes.Scheme),
                    Property = (string)elem.Attribute(OpfMetadataMeta.Attributes.Property),
                    Text = epubVersion == EpubVersion.Epub2
                        ? (string)elem.Attribute(OpfMetadataMeta.Attributes.Content)
                        : elem.Value
                }),
                Links = metadataElement.Elements(OpfElements.Link).AsObjectList(elem => new OpfMetadataLink
                {
                    Href = (string)elem.Attribute(OpfMetadataLink.Attributes.Href),
                    HrefLang = (string)elem.Attribute(OpfMetadataLink.Attributes.HrefLang),
                    Id = (string)elem.Attribute(OpfMetadataLink.Attributes.Id),
                    MediaType = (string)elem.Attribute(OpfMetadataLink.Attributes.MediaType),
                    Properties = ((string)elem.Attribute(OpfMetadataLink.Attributes.Properties))?
                        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
                    Refines = (string)elem.Attribute(OpfMetadataLink.Attributes.Refines),
                    Rel = (string)elem.Attribute(OpfMetadataLink.Attributes.Rel)
                }) ?? new List<OpfMetadataLink>(),
                Publishers = metadataElement.Elements(OpfElements.Publisher).AsStringList(),
                Relations = metadataElement.Elements(OpfElements.Relation).AsStringList(),
                Rights = metadataElement.Elements(OpfElements.Rights).AsStringList(),
                Sources = metadataElement.Elements(OpfElements.Source).AsStringList(),
                Subjects = metadataElement.Elements(OpfElements.Subject).AsStringList(),
                Titles = metadataElement.Elements(OpfElements.Title).AsStringList(),
                Types = metadataElement.Elements(OpfElements.Type).AsStringList()
            };
        }

        private static OpfGuide ReadGuide(XElement guideElement)
        {
            if (guideElement == null) return null;

            return new OpfGuide
            {
                References = guideElement.Elements(OpfElements.Reference).AsObjectList(elem => new OpfGuideReference
                {
                    Title = (string)elem.Attribute(OpfGuideReference.Attributes.Title),
                    Type = (string)elem.Attribute(OpfGuideReference.Attributes.Type),
                    Href = (string)elem.Attribute(OpfGuideReference.Attributes.Href)
                })
            };
        }

        private static OpfManifest ReadManifest(XElement manifestElement)
        {
            if (manifestElement == null) return new OpfManifest();

            return new OpfManifest
            {
                Items = manifestElement.Elements(OpfElements.Item).AsObjectList(elem =>
                    new OpfManifestItem
                    {
                        Fallback = (string)elem.Attribute(OpfManifestItem.Attributes.Fallback),
                        FallbackStyle = (string)elem.Attribute(OpfManifestItem.Attributes.FallbackStyle),
                        Href = (string)elem.Attribute(OpfManifestItem.Attributes.Href),
                        Id = (string)elem.Attribute(OpfManifestItem.Attributes.Id),
                        MediaType = (string)elem.Attribute(OpfManifestItem.Attributes.MediaType),
                        Properties = ((string)elem.Attribute(OpfManifestItem.Attributes.Properties))?
                            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>(),
                        RequiredModules = (string)elem.Attribute(OpfManifestItem.Attributes.RequiredModules),
                        RequiredNamespace = (string)elem.Attribute(OpfManifestItem.Attributes.RequiredNamespace)
                    })
            };
        }

        private static OpfSpine ReadSpine(XElement spineElement)
        {
            if (spineElement == null) return new OpfSpine();

            return new OpfSpine
            {
                ItemRefs = spineElement.Elements(OpfElements.ItemRef).AsObjectList(elem => new OpfSpineItemRef
                {
                    IdRef = (string)elem.Attribute(OpfSpineItemRef.Attributes.IdRef),
                    Linear = (string)elem.Attribute(OpfSpineItemRef.Attributes.Linear) != "no",
                    Id = (string)elem.Attribute(OpfSpineItemRef.Attributes.Id),
                    Properties = ((string)elem.Attribute(OpfSpineItemRef.Attributes.Properties))?
                        .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList() ?? new List<string>()
                }),
                Toc = spineElement.Attribute(OpfSpine.Attributes.Toc)?.Value
            };
        }

        private static IDictionary<string, string> ParsePrefixes(string prefixValue)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(prefixValue)) return result;

            var parts = prefixValue
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToArray();

            for (var i = 0; i + 1 < parts.Length; i += 2)
            {
                var prefix = parts[i];
                if (prefix.EndsWith(":"))
                {
                    prefix = prefix.Substring(0, prefix.Length - 1);
                }

                var iri = parts[i + 1];
                if (string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(iri)) continue;
                result[prefix] = iri;
            }

            return result;
        }

        private static EpubVersion GetAndValidateVersion(string version)
        {
            Guard.NotNullOrWhiteSpace(version);

            if (version == "2.0")
            {
                return EpubVersion.Epub2;
            }

            if (version == "3.0" || version == "3.0.1" || version == "3.1")
            {
                return EpubVersion.Epub3;
            }

            // EPUB 3.2, 3.3, and 3.4 still use version="3.0" in the package element per spec,
            // but some tools may write the actual release number.
            if (version == "3.2" || version == "3.3" || version == "3.4")
            {
                return EpubVersion.Epub34;
            }

            throw new NotSupportedException($"Unsupported EPUB version: {version}.");
        }
    }
}
