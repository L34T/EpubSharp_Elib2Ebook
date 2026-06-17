# EpubSharp

A C# library for reading and writing EPUB files.

[![.NET](https://img.shields.io/badge/.NET-net10%20%7C%20net9%20%7C%20netstandard2.0%20%7C%20net4.8-512BD4)](docs/BUILD_AND_RELEASE.md)
[![EPUB](https://img.shields.io/badge/EPUB-2.0%20%7C%203.0%20%7C%203.4-2F6FED)](https://github.com/w3c/epub-specs)
[![License](https://img.shields.io/badge/License-MPL_2.0-brightgreen.svg)](LICENSE)  
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/6f9edbbb2baf43ddba415ca8d14ce75c)](https://app.codacy.com/gh/L34T/EpubSharp_Elib2Ebook/dashboard)
[![CodeFactor](https://www.codefactor.io/repository/github/l34t/epubsharp_elib2ebook/badge)](https://www.codefactor.io/repository/github/l34t/epubsharp_elib2ebook)

This repository is a downstream fork of [asido/EpubSharp](https://github.com/asido/EpubSharp) optimized for:
* [OnlyFart/Elib2Ebook](https://github.com/OnlyFart/Elib2Ebook)
* [RedBuld/Elib2Ebook](https://github.com/RedBuld/Elib2Ebook)

## Specifications & Targets

* **EPUB 3.0 (2011) to EPUB 3.3 (2023) & EPUB 3.4 (Draft)**: Full read support and compliant write support (NAV/OPF packaging, collection metadata, async write APIs).
* **EPUB 2.0.1 (2010)**: Maintained for legacy compatibility.
* **Target Frameworks**: `.net10.0` (recommended), `.net9.0`, `.netstandard2.0`, `.net48`.

## Installation

Add a direct assembly reference to the compiled DLL:

```bash
dotnet add reference /path/to/EpubSharp-net10.dll
```

## Documentation

* **[Why This Fork](docs/WHY_FORK.md)**: Project requirements, 7-year device compatibility policy, supported media formats, and packaging rules.
* **[Migration Guide](docs/MIGRATION.md)**: Transitioning from the original library with code examples.
* **[Building & Releasing](docs/BUILD_AND_RELEASE.md)**: Script suites, CI/CD workflows, and test dependency pinning details.
* **[Contributing Guide](CONTRIBUTING.md)**: Development guidelines and code style.

## License

Licensed under the **Mozilla Public License 2.0 (MPL-2.0)**. See [LICENSE](LICENSE) for details.
