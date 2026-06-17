# Why This Fork

Downstream fork of `asido/EpubSharp` optimized for:
* [OnlyFart/Elib2Ebook](https://github.com/OnlyFart/Elib2Ebook)
* [RedBuld/Elib2Ebook](https://github.com/RedBuld/Elib2Ebook)

## Extensions Introduced

* **Modern Media**: Native mapping for WebP, AVIF, JPEG XL, WOFF2, and Opus audio.
* **Strict OCF Packaging**: Zero-compression `mimetype` as the first ZIP entry; clean normalized ZIP entry paths.
* **Extended Metadata**: Core EPUB series/collection attributes and reference links.
* **Asynchronous APIs**: Task-based async write operations.

## Compatibility & Standards

* **Standards**: EPUB 3.0 (2011), EPUB 3.2 (2019), EPUB 3.3 (2023), and EPUB 3.4 (Draft). EPUB 2.0.1 (2010) maintained for legacy.
* **7-Year Policy**: Guaranteed validation/rendering on devices and reading software released within the last 7 years.
* **API Stability**: Backwards-compatibility preserved for downstream consumers.

## Standard Deviations

This fork introduces deliberate design deviations from strict EPUB specifications to align with target reading systems:

* **Package Version Attribute**: The EPUB 3 specification mandates `version="3.0"` in the `<package>` element for all 3.x sub-standards. This library writes the actual targeted version (e.g. `version="3.2"`, `version="3.3"`, or `version="3.4"`) to declare targeted feature sets.
* **Collection Element Usage**: The `<collection>` metadata tag is deprecated in EPUB 3.4. The library actively uses it to automate series mapping within the ecosystem.
* **Permissive Media Validation**: The writer does not restrict or validate modern media formats (e.g. AVIF, WebP, Opus) against the target EPUB version. Conformance and validation are delegated to the downstream developer.
* **Draft Font Mapping**: The library registers classic font mappings (`font/ttf`, `font/truetype`) without enforcing the newer EPUB 3.4 draft `application/x-font-ttf` mapping as primary.

## Supported Media Formats

### Images
| Format | MIME Type | Standard Version |
| :--- | :--- | :--- |
| **WebP** | `image/webp` | EPUB 3.3 (2023) |
| **AVIF** | `image/avif` | EPUB 3.4 (Draft) |
| **JPEG XL** | `image/jxl` | EPUB 3.4 (Draft) |
| **JPEG/PNG/GIF/SVG** | Various | EPUB 2.0 (2007) |

### Fonts
| Format | MIME Type | Standard Version |
| :--- | :--- | :--- |
| **WOFF** | `font/woff` | EPUB 3.0 (2011) |
| **WOFF2** | `font/woff2` | EPUB 3.2 (2019) |
| **SFNT** | `application/font-sfnt` | EPUB 3.0 (2011) |
| **TrueType** | `font/ttf` | EPUB 3.0 (2011) |
| **OpenType** | `font/opentype` | EPUB 3.0 (2011) |

### Audio
| Format | MIME Type | Standard Version |
| :--- | :--- | :--- |
| **MPEG (MP3)** | `audio/mpeg` | EPUB 3.0 (2011) |
| **MP4 (AAC)** | `audio/mp4` | EPUB 3.0 (2011) |
| **MP4 (Opus)** | `audio/mp4; codecs=opus` | EPUB 3.4 (Draft) |
| **Ogg (Opus)** | `audio/ogg; codecs=opus` | EPUB 3.2 (2019) / EPUB 3.3 (2023) |

## ZIP Packaging Specifications

* **Mimetype file**: Placed first, stored uncompressed (`Compression Method 0`).
* **Path normalization**: Cleaned of leading slashes (e.g. `/EPUB/` to `EPUB/`) to prevent extract failures.
* **Auto-generated metadata**: Injects `dc:language="en"` if omitted; automatically injects/updates `dcterms:modified` UTC timestamp.
