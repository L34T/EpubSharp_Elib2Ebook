using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using EpubSharp.Extensions;
using EpubSharp.Format;
using EpubSharp.Format.Writers;
using EpubSharp.Misc;

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

namespace EpubSharp
{
    /// <summary>Identifies the image format of a cover image.</summary>
    public enum ImageFormat
    {
        /// <summary>Graphics Interchange Format (GIF).</summary>
        Gif,
        /// <summary>Portable Network Graphics (PNG).</summary>
        Png,
        /// <summary>JPEG / JFIF compressed image.</summary>
        Jpeg,
        /// <summary>Scalable Vector Graphics (SVG).</summary>
        Svg,
        /// <summary>WebP image format (supported since EPUB 3.3).</summary>
        Webp,
        /// <summary>AV1 Image File Format (AVIF) (supported since EPUB 3.4).</summary>
        Avif,
        /// <summary>JPEG XL image format (supported since EPUB 3.4).</summary>
        Jxl
    }

    /// <summary>
    /// Creates or modifies EPUB publications.
    /// Use <see cref="EpubWriter()"/> to create a new empty EPUB 3 package, or
    /// <see cref="EpubWriter(EpubBook)"/> to modify an existing one.
    /// Call one of the <c>Write</c> overloads when finished.
    /// </summary>
    public class EpubWriter
    {
        private readonly string opfPath = "EPUB/package.opf";
        private readonly string ncxPath = "EPUB/toc.ncx";
        private readonly string navPath;
        private readonly EpubFormat format;
        private readonly EpubResources resources;

        public EpubWriter()
        {
            var opf = new OpfDocument
            {
                UniqueIdentifier = Guid.NewGuid().ToString("D"),
                EpubVersion = EpubVersion.Epub3,
                PackageVersion = "3.2"
            };

            opf.UniqueIdentifier = Constants.DefaultOpfUniqueIdentifier;
            opf.Metadata.Identifiers.Add(new OpfMetadataIdentifier
            {
                Id = Constants.DefaultOpfUniqueIdentifier,
                Scheme = "uuid",
                Text = Guid.NewGuid().ToString("D")
            });
            opf.Metadata.Dates.Add(new OpfMetadataDate { Text = DateTimeOffset.UtcNow.ToString("o") });

            // EPUB 3.x baseline requires at least one dc:language (handled by EnsureDefaultLanguage on Write).
            opf.Metadata.Metas.Add(new OpfMetadataMeta
            {
                Property = "dcterms:modified",
                Text = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });

            // EPUB 2 legacy NCX
            opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = "ncx",
                Href = "toc.ncx",
                MediaType = ContentType.ContentTypeToMimeType[EpubContentType.DtbookNcx]
            });

            var navItem = new OpfManifestItem
            {
                Id = "nav",
                Href = "nav.xhtml",
                MediaType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11]
            };
            navItem.Properties.Add("nav");
            opf.Manifest.Items.Add(navItem);

            format = new EpubFormat
            {
                Opf = opf,
                Nav = new NavDocument(),
                Ncx = new NcxDocument()
            };

            format.Nav.Head.Dom = new XElement(Constants.XhtmlNamespace + NavElements.Head);
            format.Nav.Body.Dom =
                new XElement(
                    Constants.XhtmlNamespace + NavElements.Body,
                    new XElement(Constants.XhtmlNamespace + NavElements.Nav,
                        new XAttribute(NavNav.Attributes.Type, NavNav.Attributes.TypeValues.Toc),
                        new XElement(Constants.XhtmlNamespace + NavElements.Ol)));

            resources = new EpubResources();
            navPath = "EPUB/nav.xhtml";

            resources.Html.Add(new EpubTextFile
            {
                AbsolutePath = "nav.xhtml",
                Href = "nav.xhtml",
                ContentType = EpubContentType.Xhtml11,
                MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11],
                TextContent = string.Empty
            });
        }

        public EpubWriter(EpubBook book)
        {
            Guard.NotNull(book);
            Guard.IsTrue(book.Format?.Opf != null, "book opf instance == null");
            format = book.Format;
            resources = book.Resources;

            opfPath = format.Ocf.RootFilePath;
            ncxPath = format.Opf.FindNcxPath();

            if (ncxPath != null)
            {
                resources.Other = resources.Other.Where(e => e.Href != ncxPath).ToList();
                ncxPath = ncxPath.ToAbsolutePath(opfPath);
            }

            var navRelPath = format.Opf.FindNavPath();
            if (navRelPath != null && format.Nav != null)
            {
                navPath = navRelPath.ToAbsolutePath(opfPath);
            }
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static void Write(EpubBook book, string filename)
        {
            Guard.NotNull(book);
            Guard.NotNullOrWhiteSpace(filename);
            var writer = new EpubWriter(book);
            writer.Write(filename);
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static void Write(EpubBook book, Stream stream)
        {
            Guard.NotNull(book);
            Guard.NotNull(stream);
            var writer = new EpubWriter(book);
            writer.Write(stream);
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static EpubBook MakeCopy(EpubBook book)
        {
            var stream = new MemoryStream();
            var writer = new EpubWriter(book);
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return EpubReader.Read(stream, false);
        }

        public void AddFile(string filename, byte[] content, EpubContentType type)
        {
            Guard.NotNullOrWhiteSpace(filename);
            Guard.NotNull(content);

            var file = new EpubByteFile
            {
                AbsolutePath = filename,
                Href = filename,
                ContentType = type,
                Content = content,
                MimeType = ContentType.ContentTypeToMimeType[type]
            };

            CategorizeFile(file, type);

            format.Opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Href = filename,
                MediaType = file.MimeType
            });
        }

        private void CategorizeFile(EpubByteFile file, EpubContentType type)
        {
            switch (type)
            {
                case EpubContentType.Css:
                    resources.Css.Add(file.ToTextFile());
                    break;
                case EpubContentType.FontOpentype:
                case EpubContentType.FontTruetype:
                case EpubContentType.FontWoff:
                case EpubContentType.FontWoff2:
                case EpubContentType.FontSfnt:
                    resources.Fonts.Add(file);
                    break;
                case EpubContentType.ImageGif:
                case EpubContentType.ImageJpeg:
                case EpubContentType.ImagePng:
                case EpubContentType.ImageSvg:
                case EpubContentType.ImageWebp:
                case EpubContentType.ImageAvif:
                case EpubContentType.ImageJxl:
                    resources.Images.Add(file);
                    break;
                case EpubContentType.Xml:
                case EpubContentType.Xhtml11:
                case EpubContentType.Other:
                case EpubContentType.AudioMpeg:
                case EpubContentType.AudioMp4:
                case EpubContentType.AudioOggOpus:
                    resources.Other.Add(file);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported file type: {type}");
            }
        }

        public void AddFile(string filename, string content, EpubContentType type) => AddFile(filename, Constants.DefaultEncoding.GetBytes(content), type);

        public void AddAuthor(string author)
        {
            Guard.NotNullOrWhiteSpace(author);
            format.Opf.Metadata.Creators.Add(new OpfMetadataCreator { Text = author });
        }

        public void AddDescription(string description)
        {
            Guard.NotNullOrWhiteSpace(description);
            format.Opf.Metadata.Descriptions.Add(description);
        }

        public void AddLanguage(string lang)
        {
            Guard.NotNullOrWhiteSpace(lang);
            format.Opf.Metadata.Languages.Add(lang);
        }

        public void ClearAuthors() => format.Opf.Metadata.Creators.Clear();

        public void RemoveAuthor(string author)
        {
            Guard.NotNullOrWhiteSpace(author);
            foreach (var entity in format.Opf.Metadata.Creators.Where(e => e.Text == author).ToList())
            {
                format.Opf.Metadata.Creators.Remove(entity);
            }
        }

        public void RemoveTitle() => format.Opf.Metadata.Titles.Clear();

        public void AddCollection(string name, string number)
        {
            Guard.NotNullOrWhiteSpace(name);
            format.Opf.Metadata.Metas.Add(new OpfMetadataMeta { Property = "belongs-to-collection", Id = "collection", Text = name });
            format.Opf.Metadata.Metas.Add(new OpfMetadataMeta { Refines = "#collection", Property = "collection-type", Text = "set" });
            if (!string.IsNullOrWhiteSpace(number))
            {
                format.Opf.Metadata.Metas.Add(new OpfMetadataMeta { Refines = "#collection", Property = "group-position", Text = number });
            }
        }

        public bool TrySetSeriesUrl(string seriesUrl)
        {
            const string prefix = "todo", prefixIri = "https://github.com/todo/todo/tree/stable/epub/", rel = "todo:series-url";
            if (string.IsNullOrWhiteSpace(seriesUrl) || !seriesUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;
            if (!format.Opf.Metadata.Metas.Any(meta => meta.Property == "belongs-to-collection" && meta.Id == "collection")) return false;

            format.Opf.Prefixes ??= new Dictionary<string, string>(StringComparer.Ordinal);
            format.Opf.Prefixes[prefix] = prefixIri;
            UpsertSeriesUrlLink(seriesUrl, rel);
            UpsertSeriesIdentifier(seriesUrl);
            return true;
        }

        public bool TryAddNcxWarningPage(string title, string xhtml)
        {
            const string warningHref = "warning-ncx.xhtml", manifestId = "warning-ncx", firstNavPointId = "ncx-warning-first", lastNavPointId = "ncx-warning-last";
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(xhtml) || format.Ncx?.NavMap?.NavPoints == null) return false;

            UpsertWarningXhtml(warningHref, xhtml);
            UpsertWarningManifestItem(manifestId, warningHref);
            UpsertWarningNcxNavPoints(title, warningHref, firstNavPointId, lastNavPointId);
            return true;
        }

        public bool TrySetPackageVersion(string packageVersion)
        {
            if (!IsSingleDigitVersion(packageVersion)) return false;
            format.Opf.PackageVersion = packageVersion;
            return true;
        }

        private void UpsertWarningXhtml(string warningHref, string xhtml)
        {
            var matching = resources.Html.Where(h => h.Href == warningHref).ToList();
            var existing = matching.FirstOrDefault();
            if (existing != null)
            {
                existing.ContentType = EpubContentType.Xhtml11;
                existing.MimeType = ContentType.ContentTypeToMimeType[existing.ContentType];
                existing.TextContent = xhtml;
                foreach (var extra in matching.Skip(1)) resources.Html.Remove(extra);
                return;
            }

            var file = new EpubTextFile { AbsolutePath = warningHref, Href = warningHref, ContentType = EpubContentType.Xhtml11, TextContent = xhtml };
            file.MimeType = ContentType.ContentTypeToMimeType[file.ContentType];
            resources.Html.Add(file);
        }

        private void UpsertWarningManifestItem(string manifestId, string warningHref)
        {
            var matching = format.Opf.Manifest.Items.Where(i => i.Href == warningHref).ToList();
            var existing = matching.FirstOrDefault();
            if (existing != null)
            {
                existing.MediaType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11];
                if (string.IsNullOrWhiteSpace(existing.Id)) existing.Id = manifestId;
                foreach (var extra in matching.Skip(1)) format.Opf.Manifest.Items.Remove(extra);
                return;
            }

            format.Opf.Manifest.Items.Add(new OpfManifestItem { Id = manifestId, Href = warningHref, MediaType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11] });
        }

        private void UpsertWarningNcxNavPoints(string title, string warningHref, string firstNavPointId, string lastNavPointId)
        {
            var navPoints = format.Ncx.NavMap.NavPoints;
            for (var i = navPoints.Count - 1; i >= 0; i--)
            {
                var np = navPoints[i];
                if (np == null) continue;
                if (string.Equals(np.Id, firstNavPointId, StringComparison.Ordinal) || string.Equals(np.Id, lastNavPointId, StringComparison.Ordinal) ||
                    (string.Equals(np.ContentSrc, warningHref, StringComparison.Ordinal) && np.Id != null && np.Id.StartsWith("ncx-warning", StringComparison.Ordinal)))
                    navPoints.RemoveAt(i);
            }
            navPoints.Insert(0, new NcxNavPoint { Id = firstNavPointId, NavLabelText = title, ContentSrc = warningHref });
            navPoints.Add(new NcxNavPoint { Id = lastNavPointId, NavLabelText = title, ContentSrc = warningHref });
        }

        private void UpsertSeriesUrlLink(string seriesUrl, string rel)
        {
            format.Opf.Metadata.Links ??= new List<OpfMetadataLink>();
            var links = format.Opf.Metadata.Links;
            var matching = links.Where(l => l.Refines == "#collection" && l.Rel == rel).ToList();

            if (matching.Count == 0) { links.Add(new OpfMetadataLink { Refines = "#collection", Rel = rel, Href = seriesUrl }); return; }
            matching[0].Href = seriesUrl;
            foreach (var extra in matching.Skip(1)) links.Remove(extra);
        }

        private void UpsertSeriesIdentifier(string seriesUrl)
        {
            format.Opf.Metadata.Metas ??= new List<OpfMetadataMeta>();
            var metas = format.Opf.Metadata.Metas;
            var matching = metas.Where(m => m.Refines == "#collection" && m.Property == "dcterms:identifier").ToList();

            if (matching.Count == 0) { metas.Add(new OpfMetadataMeta { Refines = "#collection", Property = "dcterms:identifier", Text = seriesUrl }); return; }
            matching[0].Text = seriesUrl;
            foreach (var extra in matching.Skip(1)) metas.Remove(extra);
        }

        public void SetTitle(string title)
        {
            Guard.NotNullOrWhiteSpace(title);
            RemoveTitle();
            format.Opf.Metadata.Titles.Add(title);
        }

        public EpubChapter AddChapter(string title, string html, string fileId = null)
        {
            Guard.NotNullOrWhiteSpace(title);
            Guard.NotNull(html);

            fileId ??= Guid.NewGuid().ToString("N");
            var file = new EpubTextFile { AbsolutePath = fileId + ".html", Href = fileId + ".html", TextContent = html, ContentType = EpubContentType.Xhtml11 };
            file.MimeType = ContentType.ContentTypeToMimeType[file.ContentType];
            resources.Html.Add(file);

            var manifestItem = new OpfManifestItem { Id = fileId, Href = file.Href, MediaType = file.MimeType };
            format.Opf.Manifest.Items.Add(manifestItem);
            format.Opf.Spine.ItemRefs.Add(new OpfSpineItemRef { IdRef = manifestItem.Id, Linear = true });

            FindNavTocOl()?.Add(new XElement(Constants.XhtmlNamespace + NavElements.Li,
                new XElement(Constants.XhtmlNamespace + NavElements.A, new XAttribute("href", file.Href), title)));

            format.Ncx?.NavMap.NavPoints.Add(new NcxNavPoint
            {
                Id = Guid.NewGuid().ToString("N"), NavLabelText = title, ContentSrc = file.Href,
                PlayOrder = format.Ncx.NavMap.NavPoints.Any() ? format.Ncx.NavMap.NavPoints.Max(e => e.PlayOrder) : 1
            });

            return new EpubChapter { Id = fileId, Title = title, RelativePath = file.AbsolutePath };
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public void ClearChapters()
        {
            var spineItems = format.Opf.Spine.ItemRefs.Select(spine => format.Opf.Manifest.Items.Single(e => e.Id == spine.IdRef));
            var otherItems = format.Opf.Manifest.Items.Where(e => !spineItems.Contains(e)).ToList();

            foreach (var item in spineItems)
            {
                var href = new Href(item.Href);
                if (otherItems.All(e => new Href(e.Href).Path != href.Path))
                {
                    var file = resources.Html.Single(e => e.Href == href.Path);
                    resources.Html.Remove(file);
                }
                format.Opf.Manifest.Items.Remove(item);
            }

            format.Opf.Spine.ItemRefs.Clear();
            format.Opf.Guide = null;
            format.Ncx?.NavMap.NavPoints.Clear();
            FindNavTocOl()?.Descendants().Remove();

            var coverPath = format.Opf.FindCoverPath();
            foreach (var item in format.Opf.Manifest.Items.Where(e => e.MediaType.StartsWith("image/") && e.Href != coverPath).ToList())
            {
                format.Opf.Manifest.Items.Remove(item);
                var image = resources.Images.Single(e => e.Href == new Href(item.Href).Path);
                resources.Images.Remove(image);
            }
        }

        public void RemoveCover()
        {
            var path = format.Opf.FindAndRemoveCover();
            if (path == null) return;
            var resource = resources.Images.SingleOrDefault(e => e.Href == path);
            if (resource != null) resources.Images.Remove(resource);
        }

        public void SetCover(byte[] data, ImageFormat imageFormat)
        {
            Guard.NotNull(data);
            RemoveCover();

            string filename;
            EpubContentType type;

            switch (imageFormat)
            {
                case ImageFormat.Gif: filename = "cover.gif"; type = EpubContentType.ImageGif; break;
                case ImageFormat.Jpeg: filename = "cover.jpeg"; type = EpubContentType.ImageJpeg; break;
                case ImageFormat.Png: filename = "cover.png"; type = EpubContentType.ImagePng; break;
                case ImageFormat.Svg: filename = "cover.svg"; type = EpubContentType.ImageSvg; break;
                case ImageFormat.Webp: filename = "cover.webp"; type = EpubContentType.ImageWebp; break;
                case ImageFormat.Avif: filename = "cover.avif"; type = EpubContentType.ImageAvif; break;
                case ImageFormat.Jxl: filename = "cover.jxl"; type = EpubContentType.ImageJxl; break;
                default: throw new ArgumentException($"Unsupported cover format: {imageFormat}", nameof(imageFormat));
            }

            var coverResource = new EpubByteFile { AbsolutePath = filename, Href = filename, ContentType = type, Content = data };
            coverResource.MimeType = ContentType.ContentTypeToMimeType[coverResource.ContentType];
            resources.Images.Add(coverResource);

            var coverItem = new OpfManifestItem { Id = OpfManifest.ManifestItemCoverImageProperty, Href = coverResource.Href, MediaType = coverResource.MimeType };
            coverItem.Properties.Add(OpfManifest.ManifestItemCoverImageProperty);
            format.Opf.Manifest.Items.Add(coverItem);
        }

        [Obsolete("Seems to be not used anymore", true)]
        public byte[] Write()
        {
            var stream = new MemoryStream();
            Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ReadToEnd();
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public void Write(string filename) { using var file = File.Create(filename); Write(file); }

        [Obsolete("Seems to be removed in Elib2Ebook", true)]
        public void Write(Stream stream)
        {
            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true);
            archive.CreateEntry("mimetype", MimeTypeWriter.Format());
            archive.CreateEntry(Constants.OcfPath, OcfWriter.Format(opfPath));
            archive.CreateEntry(opfPath, OpfWriter.Format(format.Opf));
            if (format.Ncx != null && ncxPath != null) archive.CreateEntry(ncxPath, NcxWriter.Format(format.Ncx));
            if (format.Nav != null && navPath != null) archive.CreateEntry(navPath, NavWriter.Format(format.Nav));

            var navRelHref = navPath != null ? format.Opf.FindNavPath() : null;
            var allFiles = new[] { resources.Html.Cast<EpubFile>(), resources.Css, resources.Images, resources.Fonts, resources.Other }.SelectMany(c => c.ToArray());
            var relativePath = PathExt.GetDirectoryPath(opfPath);
            foreach (var file in allFiles) { if (navRelHref != null && file.Href == navRelHref) continue; archive.CreateEntry(PathExt.Combine(relativePath, file.Href).TrimStart('/'), file.Content); }
        }

        public async Task Write(string filename, IEnumerable<FileMeta> files) { using var file = File.Create(filename); await Write(file, files); }

        public async Task Write(Stream stream, IEnumerable<FileMeta> files)
        {
            EnsureDefaultLanguage();
            EnsureDctermsModified();
            EnsureNavXhtmlUpToDate();

            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true, Encoding.UTF8);
            string relativePath = PathExt.GetDirectoryPath(this.opfPath);

            await CreateStoredEntry(archive, "mimetype", MimeTypeWriter.Format());
            await archive.CreateEntry("META-INF/container.xml", OcfWriter.Format(this.opfPath));
            await archive.CreateEntry(this.opfPath, OpfWriter.Format(this.format.Opf));
            if (this.format.Ncx != null) await archive.CreateEntry(this.ncxPath, NcxWriter.Format(this.format.Ncx));

            var fileMetaList = (files ?? Array.Empty<FileMeta>()).ToList();
            var fileMetaNames = new HashSet<string>(fileMetaList.Where(fm => fm?.Name != null).Select(fm => fm.Name), StringComparer.Ordinal);

            var allFiles = new[] { this.resources.Html.Cast<EpubFile>(), this.resources.Css, this.resources.Images, this.resources.Fonts, this.resources.Other }.SelectMany(c => c.ToArray());
            foreach (var file in allFiles) { if (file == null || fileMetaNames.Contains(file.Href)) continue; await archive.CreateEntry(PathExt.Combine(relativePath, file.Href).TrimStart('/'), file.Content); }
            foreach (var fileMeta in fileMetaList) await archive.CreateEntryByPath(PathExt.Combine(relativePath, fileMeta.Name).TrimStart('/'), fileMeta.Path);
        }

        private static async Task CreateStoredEntry(ZipArchive archive, string file, string content)
        {
            var data = Constants.DefaultEncoding.GetBytes(content);
            var entry = archive.CreateEntry(file, CompressionLevel.NoCompression);
#if NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
            await using var stream = entry.Open();
            await stream.WriteAsync(data, 0, data.Length);
#else
            using (var stream = entry.Open())
            {
                await stream.WriteAsync(data, 0, data.Length);
            }
#endif
        }

        private void EnsureNavXhtmlUpToDate()
        {
            if (format.Nav?.Head?.Dom == null || format.Nav?.Body?.Dom == null) return;
            EnsureNavHeadUpToDate();
            var navXhtml = NavWriter.Format(format.Nav);
            var existing = resources.Html.FirstOrDefault(h => h.Href == "nav.xhtml");
            if (existing == null) { resources.Html.Insert(0, new EpubTextFile { AbsolutePath = "nav.xhtml", Href = "nav.xhtml", ContentType = EpubContentType.Xhtml11, MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11], TextContent = navXhtml }); return; }
            existing.ContentType = EpubContentType.Xhtml11;
            existing.MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11];
            existing.TextContent = navXhtml;
        }

        private void EnsureNavHeadUpToDate()
        {
            var head = format.Nav?.Head?.Dom;
            if (head == null) return;
            var ns = Constants.XhtmlNamespace;
            head.Name = ns + NavElements.Head;

            var titleValue = format.Opf?.Metadata?.Titles?.FirstOrDefault() ?? "Table of Contents";

            EnsureCharsetMeta(head, ns);
            EnsureTitleElement(head, ns, titleValue);
            
            format.Nav.Head.Title = titleValue;
        }

        private static void EnsureCharsetMeta(XElement head, XNamespace ns)
        {
            if (!head.Elements(ns + NavElements.Meta).Any(m => m.Attribute(NavMeta.Attributes.Charset) != null))
                head.AddFirst(new XElement(ns + NavElements.Meta, new XAttribute(NavMeta.Attributes.Charset, "utf-8")));
        }

        private static void EnsureTitleElement(XElement head, XNamespace ns, string titleValue)
        {
            var title = head.Elements(ns + NavElements.Title).FirstOrDefault();
            if (title == null)
            {
                var charsetMeta = head.Elements(ns + NavElements.Meta).FirstOrDefault(m => m.Attribute(NavMeta.Attributes.Charset) != null);
                if (charsetMeta != null) charsetMeta.AddAfterSelf(new XElement(ns + NavElements.Title, titleValue));
                else head.AddFirst(new XElement(ns + NavElements.Title, titleValue));
            }
            else title.Value = titleValue;
        }

        private void EnsureDctermsModified()
        {
            const string property = "dcterms:modified";
            format.Opf.Metadata.Metas ??= new List<OpfMetadataMeta>();
            var metas = format.Opf.Metadata.Metas;
            var primary = metas.Where(m => m.Property == property && string.IsNullOrWhiteSpace(m.Refines)).ToList();
            var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (primary.Count == 0) { metas.Add(new OpfMetadataMeta { Property = property, Text = now }); return; }
            primary[0].Text = now;
            foreach (var extra in primary.Skip(1)) metas.Remove(extra);
        }

        private void EnsureDefaultLanguage()
        {
            format.Opf.Metadata.Languages ??= new List<string>();
            if (format.Opf.Metadata.Languages.Count > 0) return;
            format.Opf.Metadata.Languages.Add("en");
        }

        private static bool IsSingleDigitVersion(string value) => value != null && value.Length == 3 && char.IsDigit(value[0]) && value[1] == '.' && char.IsDigit(value[2]);

        private XElement FindNavTocOl()
        {
            if (format.Nav == null) return null;
            var ns = format.Nav.Body.Dom.Name.Namespace;
            var element = format.Nav.Body.Dom.Descendants(ns + NavElements.Nav)
                .SingleOrDefault(e => (string)e.Attribute(NavNav.Attributes.Type) == NavNav.Attributes.TypeValues.Toc)
                ?.Element(ns + NavElements.Ol);
            if (element == null) throw new EpubWriteException(@"Missing ol: <nav type=""toc""><ol/></nav>");
            return element;
        }
    }
}
