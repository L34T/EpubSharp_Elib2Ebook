using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EpubSharp.Extensions;
using EpubSharp.Format;
using EpubSharp.Format.Readers;
using EpubSharp.Misc;

namespace EpubSharp
{
    /// <summary>
    /// Reads and parses EPUB publications from files or streams.
    /// </summary>
    public static class EpubReader
    {
        /// <summary>
        /// Reads an EPUB publication from the specified file path.
        /// </summary>
        public static EpubBook Read(string filePath)
        {
            return Read(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Reads an EPUB publication from the specified file path using a specific encoding.
        /// </summary>
        public static EpubBook Read(string filePath, Encoding encoding)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using (var stream = File.OpenRead(filePath))
            {
                return Read(stream, false, encoding);
            }
        }

        /// <summary>
        /// Reads an EPUB publication from the provided stream.
        /// </summary>
        public static EpubBook Read(Stream stream, bool leaveOpen)
        {
            return Read(stream, leaveOpen, Encoding.UTF8);
        }

        /// <summary>
        /// Reads an EPUB publication from the provided stream using a specific encoding.
        /// </summary>
        public static EpubBook Read(Stream stream, bool leaveOpen, Encoding encoding)
        {
            Guard.NotNull(stream);

            var book = new EpubBook();

            using (var epubArchive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen, encoding))
            {
                var ocfEntry = epubArchive.GetEntryImproved(Constants.OcfPath);
                if (ocfEntry == null) throw new EpubParseException("container.xml not found.");

                book.Format = new EpubFormat
                {
                    Ocf = OcfReader.Read(XDocument.Load(ocfEntry.Open())),
                };

                var opfEntry = epubArchive.GetEntryImproved(book.Format.Ocf.RootFilePath);
                if (opfEntry == null)
                    throw new EpubParseException($"Root file {book.Format.Ocf.RootFilePath} not found in archive.");

                book.Format.Opf = OpfReader.Read(XDocument.Load(opfEntry.Open()));
                book.Format.Paths.OpfAbsolutePath = book.Format.Ocf.RootFilePath;

                var ncxPath = book.Format.Opf.FindNcxPath();
                if (ncxPath != null)
                {
                    var ncxEntry = epubArchive.GetEntryImproved(ncxPath.ToAbsolutePath(book.Format.Paths.OpfAbsolutePath));
                    if (ncxEntry != null)
                    {
                        book.Format.Ncx = NcxReader.Read(XDocument.Load(ncxEntry.Open()));
                    }
                }

                var navPath = book.Format.Opf.FindNavPath();
                if (navPath != null)
                {
                    var navEntry = epubArchive.GetEntryImproved(navPath.ToAbsolutePath(book.Format.Paths.OpfAbsolutePath));
                    if (navEntry != null)
                    {
                        book.Format.Nav = NavReader.Read(XDocument.Load(navEntry.Open()));
                    }
                }

                book.Resources = LoadResources(epubArchive, book);
                book.SpecialResources = LoadSpecialResources(epubArchive, book);
                book.CoverImage = LoadCoverImage(epubArchive, book);
                book.TableOfContents = LoadChapters(book);
            }

            return book;
        }

        private static byte[] LoadCoverImage(ZipArchive epubArchive, EpubBook book)
        {
            var coverPath = book.Format.Opf.FindCoverPath();
            if (string.IsNullOrWhiteSpace(coverPath)) return null;

            var path = coverPath.ToAbsolutePath(book.Format.Paths.OpfAbsolutePath);
            var entry = epubArchive.GetEntryImproved(path);

            if (entry == null) return null;

            using (var stream = entry.Open())
            {
                return stream.ReadToEnd();
            }
        }

        private static IList<EpubChapter> LoadChapters(EpubBook book)
        {
            if (book.Format.Nav != null)
            {
                return LoadChaptersFromNav(book.Format.Nav, book.Resources.Html, book.Format.Paths.OpfAbsolutePath);
            }

            if (book.Format.Ncx != null)
            {
                return LoadChaptersFromNcx(book.Format.Ncx, book.Resources.Html, book.Format.Paths.OpfAbsolutePath);
            }

            return new List<EpubChapter>();
        }

        private static IList<EpubChapter> LoadChaptersFromNav(NavDocument nav, IList<EpubTextFile> htmlResources,
            string opfPath)
        {
            var chapters = new List<EpubChapter>();

            var navXml = nav.Body.Dom;
            var ns = navXml.Name.Namespace;
            var tocNav = navXml.Descendants(ns + NavElements.Nav)
                .FirstOrDefault(e => (string)e.Attribute(NavNav.Attributes.Type) == NavNav.Attributes.TypeValues.Toc);

            if (tocNav == null) return chapters;

            var ol = tocNav.Element(ns + NavElements.Ol);
            if (ol == null) return chapters;

            EpubChapter lastChapter = null;

            foreach (var li in ol.Elements(ns + NavElements.Li))
            {
                var a = li.Element(ns + NavElements.A);
                if (a == null) continue;

                var title = a.Value;
                var hrefString = (string)a.Attribute("href");
                var href = new Href(hrefString);

                var chapter = new EpubChapter
                {
                    Title = title,
                    RelativePath = href.Path,
                    AbsolutePath = href.Path.ToAbsolutePath(opfPath),
                    HashLocation = href.HashLocation,
                    Previous = lastChapter
                };

                if (lastChapter != null)
                {
                    lastChapter.Next = chapter;
                }

                chapters.Add(chapter);
                lastChapter = chapter;
            }

            return chapters;
        }

        private static IList<EpubChapter> LoadChaptersFromNcx(NcxDocument ncx, IList<EpubTextFile> htmlResources,
            string opfPath)
        {
            var chapters = new List<EpubChapter>();
            EpubChapter lastChapter = null;

            foreach (var navPoint in ncx.NavMap.NavPoints)
            {
                var href = new Href(navPoint.ContentSrc);
                var chapter = new EpubChapter
                {
                    Title = navPoint.NavLabelText,
                    RelativePath = href.Path,
                    AbsolutePath = href.Path.ToAbsolutePath(opfPath),
                    HashLocation = href.HashLocation,
                    Previous = lastChapter
                };

                if (lastChapter != null)
                {
                    lastChapter.Next = chapter;
                }

                chapters.Add(chapter);
                lastChapter = chapter;
            }

            return chapters;
        }

        private static EpubResources LoadResources(ZipArchive epubArchive, EpubBook book)
        {
            var resources = new EpubResources();

            foreach (var item in book.Format.Opf.Manifest.Items)
            {
                var path = item.Href.ToAbsolutePath(book.Format.Paths.OpfAbsolutePath);
                var entry = epubArchive.GetEntryImproved(path);

                if (entry == null) throw new EpubParseException($"file {path} not found in archive.");

                if (entry.Length > int.MaxValue) throw new EpubParseException($"file {path} is bigger than 2 Gb.");

                var href = item.Href;
                var mimeType = item.MediaType;

                EpubContentType contentType;
                contentType = ContentType.MimeTypeToContentType.TryGetValue(mimeType, out contentType)
                    ? contentType
                    : EpubContentType.Other;

                switch (contentType)
                {
                    case EpubContentType.Xhtml11:
                    case EpubContentType.Css:
                    case EpubContentType.Oeb1Document:
                    case EpubContentType.Oeb1Css:
                    case EpubContentType.Xml:
                    case EpubContentType.Dtbook:
                    case EpubContentType.DtbookNcx:
                    {
                        var file = new EpubTextFile
                        {
                            AbsolutePath = path,
                            Href = href,
                            MimeType = mimeType,
                            ContentType = contentType
                        };

                        resources.All.Add(file);

                        using (var stream = entry.Open())
                        {
                            file.Content = stream.ReadToEnd();
                        }

                        switch (contentType)
                        {
                            case EpubContentType.Xhtml11:
                                resources.Html.Add(file);
                                break;
                            case EpubContentType.Css:
                                resources.Css.Add(file);
                                break;
                            default:
                                resources.Other.Add(file);
                                break;
                        }

                        break;
                    }
                    default:
                    {
                        var file = new EpubByteFile
                        {
                            AbsolutePath = path,
                            Href = href,
                            MimeType = mimeType,
                            ContentType = contentType
                        };

                        resources.All.Add(file);

                        using (var stream = entry.Open())
                        {
                            if (stream == null)
                            {
                                throw new EpubException(
                                    $"Incorrect EPUB file: content file \"{href}\" specified in manifest is not found");
                            }

                            using (var memoryStream = new MemoryStream((int)entry.Length))
                            {
                                stream.CopyTo(memoryStream);
                                file.Content = memoryStream.ToArray();
                            }
                        }

                        switch (contentType)
                        {
                            case EpubContentType.ImageGif:
                            case EpubContentType.ImageJpeg:
                            case EpubContentType.ImagePng:
                            case EpubContentType.ImageSvg:
                            case EpubContentType.ImageWebp:
                            case EpubContentType.ImageAvif:
                            case EpubContentType.ImageJxl:
                                resources.Images.Add(file);
                                break;
                            case EpubContentType.FontTruetype:
                            case EpubContentType.FontOpentype:
                                resources.Fonts.Add(file);
                                break;
                            default:
                                resources.Other.Add(file);
                                break;
                        }

                        break;
                    }
                }
            }

            return resources;
        }

        private static EpubSpecialResources LoadSpecialResources(ZipArchive epubArchive, EpubBook book)
        {
            var result = new EpubSpecialResources();

            var ocfEntry = epubArchive.GetEntryImproved(Constants.OcfPath);
            result.Ocf = new EpubTextFile
            {
                AbsolutePath = Constants.OcfPath,
                Href = Constants.OcfPath,
                MimeType = Constants.OcfMediaType,
                ContentType = EpubContentType.Xml
            };
            using (var stream = ocfEntry.Open())
            {
                result.Ocf.Content = stream.ReadToEnd();
            }

            var opfEntry = epubArchive.GetEntryImproved(book.Format.Paths.OpfAbsolutePath);
            result.Opf = new EpubTextFile
            {
                AbsolutePath = book.Format.Paths.OpfAbsolutePath,
                Href = book.Format.Paths.OpfAbsolutePath,
                MimeType = "application/oebps-package+xml",
                ContentType = EpubContentType.Xml
            };
            using (var stream = opfEntry.Open())
            {
                result.Opf.Content = stream.ReadToEnd();
            }

            var htmlIndex = book.Resources.Html.ToDictionary(h => h.Href, h => h, StringComparer.Ordinal);

            foreach (var itemref in book.Format.Opf.Spine.ItemRefs)
            {
                var manifestItem = book.Format.Opf.Manifest.Items.FirstOrDefault(i => i.Id == itemref.IdRef);
                var href = manifestItem?.Href;

                if (href != null && htmlIndex.TryGetValue(href, out var html) && html != null)
                {
                    result.HtmlInReadingOrder.Add(html);
                }
            }

            return result;
        }
    }
}
