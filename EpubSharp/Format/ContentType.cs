using System.Collections.Generic;

namespace EpubSharp.Format
{
    public enum EpubContentType
    {
        Xhtml11 = 1,
        Dtbook,
        DtbookNcx,
        Oeb1Document,
        Xml,
        Css,
        Oeb1Css,
        ImageGif,
        ImageJpeg,
        ImagePng,
        ImageSvg,
        FontTruetype,
        FontOpentype,
        Other,
        ImageWebp,
        ImageAvif,
        ImageJxl,
        FontWoff,
        FontWoff2,
        FontSfnt,
        AudioMpeg,
        AudioMp4,
        AudioOggOpus
    }

    internal class ContentType
    {
        private static readonly (EpubContentType Type, string Mime, bool IsPrimary)[] Mappings = new[]
        {
            (EpubContentType.Xhtml11, "application/xhtml+xml", true),
            (EpubContentType.Dtbook, "application/x-dtbook+xml", true),
            (EpubContentType.DtbookNcx, "application/x-dtbncx+xml", true),
            (EpubContentType.Oeb1Document, "text/x-oeb1-document", true),
            (EpubContentType.Xml, "application/xml", true),
            (EpubContentType.Css, "text/css", true),
            (EpubContentType.Oeb1Css, "text/x-oeb1-css", true),
            
            // Images
            (EpubContentType.ImageGif, "image/gif", true),
            (EpubContentType.ImageJpeg, "image/jpeg", true),
            (EpubContentType.ImagePng, "image/png", true),
            (EpubContentType.ImageSvg, "image/svg+xml", true),
            (EpubContentType.ImageWebp, "image/webp", true),
            (EpubContentType.ImageAvif, "image/avif", true),
            (EpubContentType.ImageJxl, "image/jxl", true),
            
            // Fonts
            (EpubContentType.FontTruetype, "font/ttf", true),
            (EpubContentType.FontTruetype, "font/truetype", false),
            (EpubContentType.FontTruetype, "application/x-font-ttf", false),
            (EpubContentType.FontOpentype, "font/opentype", true),
            (EpubContentType.FontOpentype, "application/vnd.ms-opentype", false),
            (EpubContentType.FontWoff, "font/woff", true),
            (EpubContentType.FontWoff, "application/font-woff", false),
            (EpubContentType.FontWoff2, "font/woff2", true),
            (EpubContentType.FontSfnt, "application/font-sfnt", true),
            
            // Audio
            (EpubContentType.AudioMpeg, "audio/mpeg", true),
            (EpubContentType.AudioMp4, "audio/mp4", true),
            (EpubContentType.AudioMp4, "audio/mp4; codecs=aac", false),
            (EpubContentType.AudioMp4, "audio/mp4; codecs=opus", false),
            (EpubContentType.AudioOggOpus, "audio/ogg; codecs=opus", true),
            
            (EpubContentType.Other, "application/octet-stream", true)
        };

        public static readonly IReadOnlyDictionary<string, EpubContentType> MimeTypeToContentType;
        public static readonly IReadOnlyDictionary<EpubContentType, string> ContentTypeToMimeType;

        static ContentType()
        {
            var mimeToType = new Dictionary<string, EpubContentType>(System.StringComparer.OrdinalIgnoreCase);
            var typeToMime = new Dictionary<EpubContentType, string>();

            foreach (var m in Mappings)
            {
                mimeToType[m.Mime] = m.Type;
                if (m.IsPrimary)
                {
                    typeToMime[m.Type] = m.Mime;
                }
            }

            MimeTypeToContentType = mimeToType;
            ContentTypeToMimeType = typeToMime;
        }
    }
}
