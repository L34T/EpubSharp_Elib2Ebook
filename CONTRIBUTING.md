# Contributing

Guidelines for contributing to this downstream fork of `asido/EpubSharp`.

## Core Principles

* **7-Year Compatibility**: Modern EPUB standards with guaranteed rendering on systems under 7 years old.
* **Automation**: OPF metadata and ZIP packaging optimized for machine-readability.
* **Legacy Targets**: High code quality and support for older build frameworks.
* **Open-Source Readers**: Welcoming features that improve compatibility with open-source reading systems.
* **Ecosystem Needs**: Prioritizing requirements of the Elib2Ebook ecosystem ([OnlyFart/Elib2Ebook](https://github.com/OnlyFart/Elib2Ebook) and [RedBuld/Elib2Ebook](https://github.com/RedBuld/Elib2Ebook)).
* **Upstream Synchronization**: Low code complexity and localized changes to facilitate upstream merges.

## Workflow

* **Active Development**: Local testing can run on a single framework.
* **Release Verification**: Full compatibility verification is required before final commit/PR:
  ```bash
  ./scripts/build-matrix-extended.sh
  ```

## Quality Constraints

* **Heart & Craft**: Write code that brings you joy to read and is a pleasure to maintain.
* **API Stability**: Changes to public API signatures are rejected.
* **Multi-targeting**: Use internal polyfills (`Guard`, `IndexRange`) instead of compiler directives (`#if`) to support older runtimes (`netstandard2.0`, `net48`).
* **Code Duplication**: Minimize duplication across all target frameworks.

## Testing Standards

* **Scope**: Tests are required only for structural refactorings or new logic. Minor fixes, single-line edits, and formatting do not require coverage.
* **Complexity Thresholds**: Stricter limits are enforced on new/modified code (75% of standard thresholds):
  - **CCN**: 9 (standard: 12)
  - **NLOC**: 75 lines (standard: 100)
* **Coverage Rules**: If methods exceed thresholds (CCN > 9 or NLOC > 75):
  - Minimum 50% test coverage.
  - Prioritize a small number of deep, comprehensive tests over many shallow tests.
