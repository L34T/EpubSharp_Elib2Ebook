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
    public enum ImageFormat
    {
        Gif,
        Png,
        Jpeg,
        Svg,
        Webp, // epub3.3 . 2023
        Avif, // epub3.4, 2026
        Jxl // epub3.4, 2026
    }

    public class EpubWriter
    {
        private readonly string opfPath = "EPUB/package.opf";
        private readonly string ncxPath = "EPUB/toc.ncx";

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
                { Id = Constants.DefaultOpfUniqueIdentifier, Scheme = "uuid", Text = Guid.NewGuid().ToString("D") });
            opf.Metadata.Dates.Add(new OpfMetadataDate { Text = DateTimeOffset.UtcNow.ToString("o") });
            opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = "ncx", Href = "toc.ncx", MediaType = ContentType.ContentTypeToMimeType[EpubContentType.DtbookNcx]
            });

            opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = "nav",
                Href = "nav.xhtml",
                MediaType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11],
                Properties = new List<string> { "nav" }
            });

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

            resources.Html.Add(new EpubTextFile
            {
                AbsolutePath = "nav.xhtml",
                Href = "nav.xhtml",
                ContentType = EpubContentType.Xhtml11,
                MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11],
                TextContent = string.Empty
            });
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public EpubWriter(EpubBook book)
        {
            ArgumentNullException.ThrowIfNull(book);
            if (book.Format?.Opf == null) throw new ArgumentException("book opf instance == null", nameof(book));

            format = book.Format;
            resources = book.Resources;

            opfPath = format.Ocf.RootFilePath;
            ncxPath = format.Opf.FindNcxPath();

            if (ncxPath != null)
            {
                // Remove NCX file from the resources - Write() will format a new one.
                resources.Other = resources.Other.Where(e => e.Href != ncxPath).ToList();

                ncxPath = ncxPath.ToAbsolutePath(opfPath);
            }
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static void Write(EpubBook book, string filename)
        {
            ArgumentNullException.ThrowIfNull(book);
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));

            var writer = new EpubWriter(book);
            writer.Write(filename);
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static void Write(EpubBook book, Stream stream)
        {
            ArgumentNullException.ThrowIfNull(book);
            ArgumentNullException.ThrowIfNull(stream);

            var writer = new EpubWriter(book);
            writer.Write(stream);
        }

        /// <summary>
        /// Clones the book instance by writing and reading it from memory.
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        [Obsolete("Seems to be removed in Elib2Ebook")]
        public static EpubBook MakeCopy(EpubBook book)
        {
            var stream = new MemoryStream();
            var writer = new EpubWriter(book);
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var epub = EpubReader.Read(stream, false);
            return epub;
        }

        public void AddFile(string filename, byte[] content, EpubContentType type)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));
            ArgumentNullException.ThrowIfNull(content);

            var file = new EpubByteFile
            {
                AbsolutePath = filename,
                Href = filename,
                ContentType = type,
                Content = content
            };
            file.MimeType = ContentType.ContentTypeToMimeType[file.ContentType];

            switch (type)
            {
                case EpubContentType.Css:
                    resources.Css.Add(file.ToTextFile());
                    break;

                case EpubContentType.FontOpentype:
                case EpubContentType.FontTruetype:
                    resources.Fonts.Add(file);
                    break;

                case EpubContentType.ImageGif:
                case EpubContentType.ImageJpeg:
                case EpubContentType.ImagePng:
                case EpubContentType.ImageSvg:
                    resources.Images.Add(file);
                    break;

                case EpubContentType.Xml:
                case EpubContentType.Xhtml11:
                case EpubContentType.Other:
                    resources.Other.Add(file);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported file type: {type}");
            }

            format.Opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Href = filename,
                MediaType = file.MimeType
            });
        }

        public void AddFile(string filename, string content, EpubContentType type)
        {
            AddFile(filename, Constants.DefaultEncoding.GetBytes(content), type);
        }

        public void AddAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author)) throw new ArgumentNullException(nameof(author));
            format.Opf.Metadata.Creators.Add(new OpfMetadataCreator { Text = author });
        }

        public void AddDescription(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) throw new ArgumentNullException("description");
            format.Opf.Metadata.Descriptions.Add(description);
        }

        public void AddLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) throw new ArgumentNullException("lang");
            format.Opf.Metadata.Languages.Add(lang);
        }

        public void ClearAuthors()
        {
            format.Opf.Metadata.Creators.Clear();
        }

        public void RemoveAuthor(string author)
        {
            if (string.IsNullOrWhiteSpace(author)) throw new ArgumentNullException(nameof(author));
            foreach (var entity in format.Opf.Metadata.Creators.Where(e => e.Text == author).ToList())
            {
                format.Opf.Metadata.Creators.Remove(entity);
            }
        }

        public void RemoveTitle()
        {
            format.Opf.Metadata.Titles.Clear();
        }

        public void AddCollection(string name, string number)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
            format.Opf.Metadata.Metas.Add(new OpfMetadataMeta
            {
                Property = "belongs-to-collection",
                Id = "collection",
                Text = name
            });
            format.Opf.Metadata.Metas.Add(new OpfMetadataMeta
            {
                Refines = "#collection",
                Property = "collection-type",
                Text = "set"
            });
            if (!string.IsNullOrWhiteSpace(number))
            {
                format.Opf.Metadata.Metas.Add(new OpfMetadataMeta
                {
                    Refines = "#collection",
                    Property = "group-position",
                    Text = number
                });
            }
        }

        public bool TrySetSeriesUrl(string seriesUrl)
        {
            const string prefix = "todo";
            const string prefixIri = "https://github.com/todo/todo/tree/stable/epub/";
            const string rel = "todo:series-url";

            if (string.IsNullOrWhiteSpace(seriesUrl)) return false;
            if (!seriesUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return false;

            var hasCollection = format.Opf.Metadata.Metas.Any(meta =>
                meta.Property == "belongs-to-collection" && meta.Id == "collection");
            if (!hasCollection) return false;

            format.Opf.Prefixes ??= new Dictionary<string, string>(StringComparer.Ordinal);
            format.Opf.Prefixes[prefix] = prefixIri;

            UpsertSeriesUrlLink(seriesUrl, rel);
            UpsertSeriesIdentifier(seriesUrl);

            return true;
        }

        public bool TryAddNcxWarningPage(string title, string xhtml)
        {
            const string warningHref = "warning-ncx.xhtml";
            const string manifestId = "warning-ncx";
            const string firstNavPointId = "ncx-warning-first";
            const string lastNavPointId = "ncx-warning-last";

            if (string.IsNullOrWhiteSpace(title)) return false;
            if (string.IsNullOrWhiteSpace(xhtml)) return false;

            if (format.Ncx?.NavMap?.NavPoints == null) return false;

            UpsertWarningXhtml(warningHref, xhtml);
            UpsertWarningManifestItem(manifestId, warningHref);
            UpsertWarningNcxNavPoints(title, warningHref, firstNavPointId, lastNavPointId);

            return true;
        }

        public bool TrySetPackageVersion(string packageVersion)
        {
            if (!IsSingleDigitVersion(packageVersion))
            {
                return false;
            }

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

                foreach (var extra in matching.Skip(1))
                {
                    resources.Html.Remove(extra);
                }

                return;
            }

            var file = new EpubTextFile
            {
                AbsolutePath = warningHref,
                Href = warningHref,
                ContentType = EpubContentType.Xhtml11,
                TextContent = xhtml
            };
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
                if (string.IsNullOrWhiteSpace(existing.Id))
                {
                    existing.Id = manifestId;
                }

                foreach (var extra in matching.Skip(1))
                {
                    format.Opf.Manifest.Items.Remove(extra);
                }

                return;
            }

            format.Opf.Manifest.Items.Add(new OpfManifestItem
            {
                Id = manifestId,
                Href = warningHref,
                MediaType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11]
            });
        }

        private void UpsertWarningNcxNavPoints(string title, string warningHref, string firstNavPointId,
            string lastNavPointId)
        {
            var navPoints = format.Ncx.NavMap.NavPoints;

            for (var i = navPoints.Count - 1; i >= 0; i--)
            {
                var np = navPoints[i];
                if (np == null) continue;

                if (string.Equals(np.Id, firstNavPointId, StringComparison.Ordinal) ||
                    string.Equals(np.Id, lastNavPointId, StringComparison.Ordinal) ||
                    (string.Equals(np.ContentSrc, warningHref, StringComparison.Ordinal) &&
                     np.Id != null && np.Id.StartsWith("ncx-warning", StringComparison.Ordinal)))
                {
                    navPoints.RemoveAt(i);
                }
            }

            navPoints.Insert(0, new NcxNavPoint
            {
                Id = firstNavPointId,
                NavLabelText = title,
                ContentSrc = warningHref
            });

            navPoints.Add(new NcxNavPoint
            {
                Id = lastNavPointId,
                NavLabelText = title,
                ContentSrc = warningHref
            });
        }

        private void UpsertSeriesUrlLink(string seriesUrl, string rel)
        {
            format.Opf.Metadata.Links ??= new List<OpfMetadataLink>();
            var links = format.Opf.Metadata.Links;
            var matching = links
                .Where(l => l.Refines == "#collection" && l.Rel == rel)
                .ToList();

            if (matching.Count == 0)
            {
                links.Add(new OpfMetadataLink
                {
                    Refines = "#collection",
                    Rel = rel,
                    Href = seriesUrl
                });
                return;
            }

            matching[0].Href = seriesUrl;
            foreach (var extra in matching.Skip(1))
            {
                links.Remove(extra);
            }
        }

        private void UpsertSeriesIdentifier(string seriesUrl)
        {
            format.Opf.Metadata.Metas ??= new List<OpfMetadataMeta>();
            var metas = format.Opf.Metadata.Metas;
            var matching = metas
                .Where(m => m.Refines == "#collection" && m.Property == "dcterms:identifier")
                .ToList();

            if (matching.Count == 0)
            {
                metas.Add(new OpfMetadataMeta
                {
                    Refines = "#collection",
                    Property = "dcterms:identifier",
                    Text = seriesUrl
                });
                return;
            }

            matching[0].Text = seriesUrl;
            foreach (var extra in matching.Skip(1))
            {
                metas.Remove(extra);
            }
        }

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            RemoveTitle();
            format.Opf.Metadata.Titles.Add(title);
        }

        public EpubChapter AddChapter(string title, string html, string fileId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            ArgumentNullException.ThrowIfNull(html);

            fileId ??= Guid.NewGuid().ToString("N");
            var file = new EpubTextFile
            {
                AbsolutePath = fileId + ".html",
                Href = fileId + ".html",
                TextContent = html,
                ContentType = EpubContentType.Xhtml11
            };
            file.MimeType = ContentType.ContentTypeToMimeType[file.ContentType];
            resources.Html.Add(file);

            var manifestItem = new OpfManifestItem
            {
                Id = fileId,
                Href = file.Href,
                MediaType = file.MimeType
            };
            format.Opf.Manifest.Items.Add(manifestItem);

            var spineItem = new OpfSpineItemRef { IdRef = manifestItem.Id, Linear = true };
            format.Opf.Spine.ItemRefs.Add(spineItem);

            FindNavTocOl()?.Add(new XElement(Constants.XhtmlNamespace + NavElements.Li,
                new XElement(Constants.XhtmlNamespace + NavElements.A, new XAttribute("href", file.Href), title)));

            format.Ncx?.NavMap.NavPoints.Add(new NcxNavPoint
            {
                Id = Guid.NewGuid().ToString("N"),
                NavLabelText = title,
                ContentSrc = file.Href,
                PlayOrder = format.Ncx.NavMap.NavPoints.Any() ? format.Ncx.NavMap.NavPoints.Max(e => e.PlayOrder) : 1
            });

            return new EpubChapter
            {
                Id = fileId,
                Title = title,
                RelativePath = file.AbsolutePath
            };
        }

        [Obsolete("Seems to be removed in Elib2Ebook")]
        public void ClearChapters()
        {
            var spineItems =
                format.Opf.Spine.ItemRefs.Select(spine => format.Opf.Manifest.Items.Single(e => e.Id == spine.IdRef));
            var otherItems = format.Opf.Manifest.Items.Where(e => !spineItems.Contains(e)).ToList();

            foreach (var item in spineItems)
            {
                var href = new Href(item.Href);
                if (otherItems.All(e => new Href(e.Href).Path != href.Path))
                {
                    // The HTML file is not referenced by anything outside spine item, thus can be removed from the archive.
                    var file = resources.Html.Single(e => e.Href == href.Path);
                    resources.Html.Remove(file);
                }

                format.Opf.Manifest.Items.Remove(item);
            }

            format.Opf.Spine.ItemRefs.Clear();
            format.Opf.Guide = null;
            format.Ncx?.NavMap.NavPoints.Clear();
            FindNavTocOl()?.Descendants().Remove();

            // Remove all images except the cover.
            // I can't think of a case where this is a bad idea.
            var coverPath = format.Opf.FindCoverPath();
            foreach (var item in format.Opf.Manifest.Items
                         .Where(e => e.MediaType.StartsWith("image/") && e.Href != coverPath).ToList())
            {
                format.Opf.Manifest.Items.Remove(item);

                var image = resources.Images.Single(e => e.Href == new Href(item.Href).Path);
                resources.Images.Remove(image);
            }
        }

        //public void InsertChapter(string title, string html, int index, EpubChapter parent = null)
        //{
        //    throw new NotImplementedException("Implement me!");
        //}

        public void RemoveCover()
        {
            var path = format.Opf.FindAndRemoveCover();
            if (path == null) return;

            var resource = resources.Images.SingleOrDefault(e => e.Href == path);
            if (resource != null)
            {
                resources.Images.Remove(resource);
            }
        }

        public void SetCover(byte[] data, ImageFormat imageFormat)
        {
            ArgumentNullException.ThrowIfNull(data);

            RemoveCover();

            string filename;
            EpubContentType type;

            switch (imageFormat)
            {
                case ImageFormat.Gif:
                    filename = "cover.gif";
                    type = EpubContentType.ImageGif;
                    break;
                case ImageFormat.Jpeg:
                    filename = "cover.jpeg";
                    type = EpubContentType.ImageJpeg;
                    break;
                case ImageFormat.Png:
                    filename = "cover.png";
                    type = EpubContentType.ImagePng;
                    break;
                case ImageFormat.Svg:
                    filename = "cover.svg";
                    type = EpubContentType.ImageSvg;
                    break;
                case ImageFormat.Webp:
                    filename = "cover.webp";
                    type = EpubContentType.ImageWebp;
                    break;
                default:
                    throw new ArgumentException($"Unsupported cover format: {format}", nameof(format));
                // TODO: epub3.4 still in draft for now and image/avif image/jxl not implemented yet in readers
                // case ImageFormat.Avif:
                //     filename = "cover.avif";
                //     type = EpubContentType.ImageAvif;
                //     break;
                // case ImageFormat.Jxl:
                //     filename = "cover.jxl";
                //     type = EpubContentType.ImageJxl;
                //     break;
            }

            var coverResource = new EpubByteFile
            {
                AbsolutePath = filename,
                Href = filename,
                ContentType = type,
                Content = data
            };
            coverResource.MimeType = ContentType.ContentTypeToMimeType[coverResource.ContentType];
            resources.Images.Add(coverResource);

            var coverItem = new OpfManifestItem
            {
                Id = OpfManifest.ManifestItemCoverImageProperty,
                Href = coverResource.Href,
                MediaType = coverResource.MimeType
            };
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
        public void Write(string filename)
        {
            using (var file = File.Create(filename))
            {
                Write(file);
            }
        }

        // TODO: remove `#pragma warning disable CS4014` when this sync `Write(Stream stream)` method removed
        [Obsolete("Seems to be removed in Elib2Ebook", true)]
        public void Write(Stream stream)
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                archive.CreateEntry("mimetype", MimeTypeWriter.Format());
                archive.CreateEntry(Constants.OcfPath, OcfWriter.Format(opfPath));
                archive.CreateEntry(opfPath, OpfWriter.Format(format.Opf));

                if (format.Ncx != null)
                {
                    archive.CreateEntry(ncxPath, NcxWriter.Format(format.Ncx));
                }

                var allFiles = new[]
                {
                    resources.Html.Cast<EpubFile>(),
                    resources.Css,
                    resources.Images,
                    resources.Fonts,
                    resources.Other
                }.SelectMany(collection => collection as EpubFile[] ?? collection.ToArray());
                var relativePath = PathExt.GetDirectoryPath(opfPath);
                foreach (var file in allFiles)
                {
                    var absolutePath = PathExt.Combine(relativePath, file.Href);
                    archive.CreateEntry(absolutePath, file.Content);
                }
            }
        }

        public async Task Write(string filename, IEnumerable<FileMeta> files)
        {
            using var file = File.Create(filename);
            await Write(file, files);
        }

        public async Task Write(Stream stream, IEnumerable<FileMeta> files)
        {
            EnsureDctermsModified();
            EnsureNavXhtmlUpToDate();

            using var archive = new ZipArchive(stream, ZipArchiveMode.Create, true, Encoding.UTF8);

            string relativePath = PathExt.GetDirectoryPath(this.opfPath);

            // mimetype (без сжатия, по стандарту EPUB) — должен быть первым entry
            await CreateStoredEntry(archive, "mimetype", MimeTypeWriter.Format());

            // container.xml
            await archive.CreateEntry("META-INF/container.xml", OcfWriter.Format(this.opfPath));

            // OPF
            await archive.CreateEntry(this.opfPath, OpfWriter.Format(this.format.Opf));

            // NCX (если есть)
            if (this.format.Ncx != null)
            {
                await archive.CreateEntry(this.ncxPath, NcxWriter.Format(this.format.Ncx));
            }

            var fileMetaList = (files ?? Array.Empty<FileMeta>()).ToList();

            // Дополнительные файлы (FileMeta) — используем как фильтр, чтобы не писать placeholder-ресурсы тем же путём
            var fileMetaNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var fileMeta in fileMetaList)
            {
                if (fileMeta?.Name == null) continue;
                fileMetaNames.Add(fileMeta.Name);
            }

            // Внутренние ресурсы (HTML/CSS/etc)
            var allFiles = new[]
            {
                this.resources.Html.Cast<EpubFile>(),
                this.resources.Css,
                this.resources.Images,
                this.resources.Fonts,
                this.resources.Other
            }.SelectMany(collection => (collection as EpubFile[]) ?? collection.ToArray<EpubFile>());

            foreach (var file in allFiles)
            {
                if (file == null) continue;
                if (fileMetaNames.Contains(file.Href))
                {
                    continue;
                }

                await archive.CreateEntry(PathExt.Combine(relativePath, file.Href), file.Content);
            }

            // Дополнительные файлы (FileMeta)
            foreach (var fileMeta in fileMetaList)
            {
                await archive.CreateEntryByPath(PathExt.Combine(relativePath, fileMeta.Name), fileMeta.Path);
            }
        }

        private static async Task CreateStoredEntry(ZipArchive archive, string file, string content)
        {
            var data = Constants.DefaultEncoding.GetBytes(content);
            var entry = archive.CreateEntry(file, CompressionLevel.NoCompression);
            await using var stream = entry.Open();
            await stream.WriteAsync(data, 0, data.Length);
        }

        private void EnsureNavXhtmlUpToDate()
        {
            if (format.Nav?.Head?.Dom == null || format.Nav?.Body?.Dom == null) return;

            EnsureNavHeadUpToDate();

            var navXhtml = NavWriter.Format(format.Nav);
            var existing = resources.Html.FirstOrDefault(h => h.Href == "nav.xhtml");
            if (existing == null)
            {
                resources.Html.Insert(0, new EpubTextFile
                {
                    AbsolutePath = "nav.xhtml",
                    Href = "nav.xhtml",
                    ContentType = EpubContentType.Xhtml11,
                    MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11],
                    TextContent = navXhtml
                });
                return;
            }

            existing.ContentType = EpubContentType.Xhtml11;
            existing.MimeType = ContentType.ContentTypeToMimeType[EpubContentType.Xhtml11];
            existing.TextContent = navXhtml;
        }

        private void EnsureNavHeadUpToDate()
        {
            var head = format.Nav?.Head?.Dom;
            if (head == null) return;

            var ns = Constants.XhtmlNamespace;
            if (head.Name != ns + NavElements.Head)
            {
                head.Name = ns + NavElements.Head;
            }

            var titleValue = format.Opf?.Metadata?.Titles?.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(titleValue))
            {
                titleValue = "Table of Contents";
            }

            // XHTML5-compatible shorthand for declaring UTF-8.
            var hasCharset = head.Elements(ns + NavElements.Meta)
                .Any(m => m.Attribute(NavMeta.Attributes.Charset) != null);
            if (!hasCharset)
            {
                head.AddFirst(new XElement(ns + NavElements.Meta, new XAttribute(NavMeta.Attributes.Charset, "utf-8")));
            }

            var title = head.Elements(ns + NavElements.Title).FirstOrDefault();
            if (title == null)
            {
                var charsetMeta = head.Elements(ns + NavElements.Meta)
                    .FirstOrDefault(m => m.Attribute(NavMeta.Attributes.Charset) != null);
                if (charsetMeta != null)
                {
                    charsetMeta.AddAfterSelf(new XElement(ns + NavElements.Title, titleValue));
                }
                else
                {
                    head.AddFirst(new XElement(ns + NavElements.Title, titleValue));
                }
            }
            else
            {
                title.Value = titleValue;
            }
        }

        private void EnsureDctermsModified()
        {
            const string property = "dcterms:modified";

            format.Opf.Metadata.Metas ??= new List<OpfMetadataMeta>();
            var metas = format.Opf.Metadata.Metas;

            var primary = metas
                .Where(m => m.Property == property && string.IsNullOrWhiteSpace(m.Refines))
                .ToList();

            var now = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if (primary.Count == 0)
            {
                metas.Add(new OpfMetadataMeta
                {
                    Property = property,
                    Text = now
                });
                return;
            }

            primary[0].Text = now;
            foreach (var extra in primary.Skip(1))
            {
                metas.Remove(extra);
            }
        }

        private static bool IsSingleDigitVersion(string value)
        {
            return value != null &&
                   value.Length == 3 &&
                   char.IsDigit(value[0]) &&
                   value[1] == '.' &&
                   char.IsDigit(value[2]);
        }

        private XElement FindNavTocOl()
        {
            if (format.Nav == null)
            {
                return null;
            }

            var ns = format.Nav.Body.Dom.Name.Namespace;
            var element = format.Nav.Body.Dom.Descendants(ns + NavElements.Nav)
                .SingleOrDefault(e => (string)e.Attribute(NavNav.Attributes.Type) == NavNav.Attributes.TypeValues.Toc)
                ?.Element(ns + NavElements.Ol);

            if (element == null) throw new EpubWriteException(@"Missing ol: <nav type=""toc""><ol/></nav>");

            return element;
        }

        // Old code to add toc.ncx
        /*
            if (opf.Spine.Toc != null)
            {
                var ncxPath = opf.FindNcxPath();
                if (ncxPath == null)
                {
                    throw new EpubWriteException("Spine TOC is set, but NCX path is not.");
                }
                manifest.Add(new XElement(OpfElements.Item, new XAttribute(OpfManifestItem.Attributes.Id, "ncx"), new XAttribute(OpfManifestItem.Attributes.MediaType, ContentType.ContentTypeToMimeType[EpubContentType.DtbookNcx]), new XAttribute(OpfManifestItem.Attributes.Href, ncxPath)));
            }
         */
    }
}
