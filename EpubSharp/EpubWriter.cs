using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using EpubSharp.Format;
using EpubSharp.Format.Writers;

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
        Svg
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

        // ncxPath is null for new EPUB 3 books (no NCX); set in EpubWriter(EpubBook) for existing books.
        private readonly string ncxPath;

        // navPath is set in the no-arg constructor so Write() generates nav.xhtml via NavWriter.
        // It is null for EpubWriter(EpubBook) because the nav file already lives in resources.
        private readonly string navPath;

        private readonly EpubFormat format;
        private readonly EpubResources resources;

        /// <summary>
        /// Initialises a new, empty EPUB 3 publication.
        /// The package uses a Navigation Document (<c>nav.xhtml</c>) as required by EPUB 3;
        /// no legacy NCX file is produced.
        /// </summary>
        public EpubWriter()
        {
            var opf = new OpfDocument
            {
                EpubVersion = EpubVersion.Epub3
            };

            opf.UniqueIdentifier = Constants.DefaultOpfUniqueIdentifier;
            opf.Metadata.Identifiers.Add(new OpfMetadataIdentifier
            {
                Id = Constants.DefaultOpfUniqueIdentifier,
                Scheme = "uuid",
                Text = Guid.NewGuid().ToString("D")
            });
            opf.Metadata.Dates.Add(new OpfMetadataDate { Text = DateTimeOffset.UtcNow.ToString("o") });

            // EPUB 3 requires dc:language and the dcterms:modified meta.
            opf.Metadata.Languages.Add("en");
            opf.Metadata.Metas.Add(new OpfMetadataMeta
            {
                Property = "dcterms:modified",
                Text = DateTimeOffset.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
            });

            // EPUB 3 uses a Navigation Document instead of the NCX.
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
                Nav = new NavDocument()
                // Ncx intentionally left null for pure EPUB 3.
            };

            // Initialise the Nav body DOM with an empty TOC structure.
            format.Nav.Head.Dom = new XElement(NavElements.Head);
            format.Nav.Body.Dom =
                new XElement(
                    NavElements.Body,
                    new XElement(NavElements.Nav,
                        new XAttribute(NavNav.Attributes.Type, NavNav.Attributes.TypeValues.Toc),
                        new XElement(NavElements.Ol)));

            resources = new EpubResources();

            // This path is used by Write() to serialise nav.xhtml via NavWriter.
            navPath = "EPUB/nav.xhtml";
        }

        /// <summary>
        /// Initialises an <see cref="EpubWriter"/> from an existing <see cref="EpubBook"/>
        /// so that it can be modified and saved again.
        /// The nav.xhtml of the source book is preserved as-is (written from the resource bytes).
        /// If the source book contains an NCX, it is regenerated from the in-memory model.
        /// </summary>
        /// <param name="book">The source EPUB book to modify.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="book"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when the book's OPF document is <c>null</c>.</exception>
        public EpubWriter(EpubBook book)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
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

            // If the book has an EPUB 3 Navigation Document, Write() will regenerate it from
            // the in-memory DOM via NavWriter (so that DOM mutations like ClearChapters/AddChapter
            // are properly persisted).  We record the absolute archive path here; the nav file
            // is intentionally left in resources.Html so that ClearChapters() can still find and
            // remove it when the nav is listed in the spine (which is EPUB 3 best practice).
            var navRelPath = format.Opf.FindNavPath();
            if (navRelPath != null && format.Nav != null)
            {
                navPath = navRelPath.ToAbsolutePath(opfPath);
            }
        }

        /// <summary>
        /// Writes <paramref name="book"/> to <paramref name="filename"/> on disk.
        /// This is a convenience wrapper around <see cref="EpubWriter(EpubBook)"/> and <see cref="Write(string)"/>.
        /// </summary>
        /// <param name="book">The EPUB book to write.</param>
        /// <param name="filename">Destination file path.</param>
        public static void Write(EpubBook book, string filename)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));

            var writer = new EpubWriter(book);
            writer.Write(filename);
        }

        /// <summary>
        /// Writes <paramref name="book"/> to the given <paramref name="stream"/>.
        /// This is a convenience wrapper around <see cref="EpubWriter(EpubBook)"/> and <see cref="Write(Stream)"/>.
        /// </summary>
        /// <param name="book">The EPUB book to write.</param>
        /// <param name="stream">Destination stream.</param>
        public static void Write(EpubBook book, Stream stream)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var writer = new EpubWriter(book);
            writer.Write(stream);
        }

        /// <summary>
        /// Clones the book instance by writing and reading it from memory.
        /// </summary>
        /// <param name="book"></param>
        /// <returns></returns>
        public static EpubBook MakeCopy(EpubBook book)
        {
            var stream = new MemoryStream();
            var writer = new EpubWriter(book);
            writer.Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            var epub = EpubReader.Read(stream, false);
            return epub;
        }
        
        /// <summary>
        /// Adds an arbitrary resource file to the publication.
        /// The file is added to the manifest and to the appropriate resource collection.
        /// </summary>
        /// <param name="filename">File name (used as the manifest href).</param>
        /// <param name="content">File content as raw bytes.</param>
        /// <param name="type">EPUB content type that determines the manifest media-type and resource bucket.</param>
        public void AddFile(string filename, byte[] content, EpubContentType type)
        {
            if (string.IsNullOrWhiteSpace(filename)) throw new ArgumentNullException(nameof(filename));
            if (content == null) throw new ArgumentNullException(nameof(content));

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

        public void SetTitle(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            RemoveTitle();
            format.Opf.Metadata.Titles.Add(title);
        }

        /// <summary>
        /// Adds a new chapter to the publication.
        /// The chapter HTML is stored as a resource file, a manifest and spine entry are created,
        /// and both the EPUB 3 Navigation Document TOC and (if present) the NCX are updated.
        /// </summary>
        /// <param name="title">Display title shown in the table of contents.</param>
        /// <param name="html">Full XHTML content of the chapter.</param>
        /// <param name="fileId">
        /// Optional unique identifier used as the manifest item ID and file name base.
        /// A new GUID is generated when omitted.
        /// </param>
        /// <returns>An <see cref="EpubChapter"/> describing the newly added chapter.</returns>
        public EpubChapter AddChapter(string title, string html, string fileId = null)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentNullException(nameof(title));
            if (html == null) throw new ArgumentNullException(nameof(html));

            fileId = fileId ?? Guid.NewGuid().ToString("N");
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

            FindNavTocOl()?.Add(new XElement(NavElements.Li, new XElement(NavElements.A, new XAttribute("href", file.Href), title)));

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

        public void ClearChapters()
        {
            var spineItems = format.Opf.Spine.ItemRefs.Select(spine => format.Opf.Manifest.Items.Single(e => e.Id == spine.IdRef));
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
            foreach (var item in format.Opf.Manifest.Items.Where(e => e.MediaType.StartsWith("image/") && e.Href != coverPath).ToList())
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

        /// <summary>
        /// Sets or replaces the cover image of the publication.
        /// Any existing cover is removed first (see <see cref="RemoveCover"/>).
        /// </summary>
        /// <param name="data">Raw image bytes.</param>
        /// <param name="imageFormat">Format of the image data.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is <c>null</c>.</exception>
        public void SetCover(byte[] data, ImageFormat imageFormat)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

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
                default:
                    throw new ArgumentException($"Unsupported cover format: {format}", nameof(format));
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
            format.Opf.Metadata.Metas.Add(new OpfMetadataMeta() { 
                Name= "cover",                
                Content = "cover-image"
            });
        }

        public byte[] Write()
        {
            var stream = new MemoryStream();
            Write(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ReadToEnd();
        }
        
        public void Write(string filename)
        {
            using (var file = File.Create(filename))
            {
                Write(file);
            }
        }

        /// <summary>
        /// Serialises the publication to the provided <paramref name="stream"/> as a ZIP-based EPUB archive.
        /// </summary>
        /// <param name="stream">The writable stream to write the EPUB data into.</param>
        public void Write(Stream stream)
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                archive.CreateEntry("mimetype", MimeTypeWriter.Format());
                archive.CreateEntry(Constants.OcfPath, OcfWriter.Format(opfPath));
                archive.CreateEntry(opfPath, OpfWriter.Format(format.Opf));

                if (format.Ncx != null && ncxPath != null)
                {
                    archive.CreateEntry(ncxPath, NcxWriter.Format(format.Ncx));
                }

                // Write the EPUB 3 Navigation Document from the in-memory NavDocument DOM
                // (via NavWriter) so that any DOM mutations are persisted.
                // The nav.xhtml entry in resources.Html (if present) is intentionally skipped
                // below to avoid overwriting the freshly generated content.
                if (format.Nav != null && navPath != null)
                {
                    archive.CreateEntry(navPath, NavWriter.Format(format.Nav));
                }

                // Determine the nav href (relative to OPF) so we can skip it from resources.
                var navRelHref = navPath != null ? format.Opf.FindNavPath() : null;

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
                    // Skip the nav.xhtml resource — it was already written by NavWriter above.
                    if (navRelHref != null && file.Href == navRelHref)
                        continue;

                    var absolutePath = PathExt.Combine(relativePath, file.Href);
                    archive.CreateEntry(absolutePath, file.Content);
                }
            }
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

            if (element == null)
            {
                throw new EpubWriteException(@"Missing ol: <nav type=""toc""><ol/></nav>");
            }

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
