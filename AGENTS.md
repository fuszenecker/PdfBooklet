# PdfBooklet — Agent Guidelines

Cross-platform desktop app (.NET 10, Avalonia) that turns a PDF into a print-ready 2-up booklet.
Flow: open a PDF → adjust sheet/layout/output settings (live preview) → export.

## Architecture

Same layering as PentaGrammata:

```
src/
  Views/           # Avalonia AXAML + code-behind (minimal logic); Views/Controls = custom controls
  ViewModels/      # CommunityToolkit.Mvvm partial classes
  Converters/      # IValueConverter implementations
  Presentation/    # Dialog/file-picker services and the dialog view-model factory
  Services/        # Business logic implementing Interfaces/
  Stores/          # Persistence (JSON user config)
  Interfaces/      # Abstractions injected via Microsoft.Extensions.DependencyInjection
  Exceptions/      # Exceptions thrown across layer boundaries
  Models/          # Plain data records (PdfSourceInfo, BookletPlan, ...)
  Configuration/   # Strongly-typed settings read from settings.prmt
  Composition/     # DI registration (ServiceCollectionExtensions)
tests/
  PdfBooklet.Tests/   # MSTest + NSubstitute; folders mirror src/
```

- `BookletLayoutPlanner` is a pure function from (source page sizes, sheet, layout) to a `BookletPlan`:
  which source page lands where on every sheet side, with scale and offset. The preview control and
  `PdfBookletExporter` both consume the plan, so the preview shows exactly what is exported.
- The output PDF alternates front/back of each sheet (duplex printers). `OutputSettings.ManualDuplex` instead
  writes all fronts first, then all backs in reverse sheet order (`BookletPlan.SidesInPrintOrder`), for
  single-sided printers where the printed stack is turned over and fed back in.
- Sheets are always landscape (two pages side by side, fold across the short axis). Page counts are
  padded with blanks at the end to a multiple of 4 per signature.
- Settings files are **Promptuarium containers** (`PRMT:CFG1-LE`, layout in Promptuarium's `SERIALIZER_FORMAT.md`),
  read and written by `PromptuariumSerializer` (package `Promptuarium.Serializer`, experimental: the projects opt in
  with `NoWarn` `PRMTSER000`) with `Configuration/ConfigurationFormat.Options`. Layering: the bundled
  `src/settings.prmt` (all defaults) is deserialized, then each per-user `settings.prmt`
  (`$XDG_CONFIG_HOME/PdfBooklet` or `~/.config/PdfBooklet` on Linux, `%LocalAppData%\PdfBooklet` on Windows,
  `~/Library/Application Support/PdfBooklet` on macOS; see `ConfigurationPaths`) is applied onto it with `Populate`.
  There is no `appsettings.json` and no migration from the old JSON files. `IConfigurationService` is the only writer.
- Every settings class and property carries a `[PromptuariumComment]` (unit, allowed values or range, meaning), written
  into the containers as COMMENT nodes for people reading them with `pedit`/PStudio; readers ignore them. A new or
  changed setting gets its comment too.
- `src/settings.prmt` is a committed binary file: the serialized `new AppConfiguration()`. A test checks that it
  matches the code defaults; after changing a setting or its default, regenerate it (serialize `new AppConfiguration()`
  with `ConfigurationFormat.Options`, e.g. from a `dotnet run` file-based script with `#:project src/PdfBooklet.csproj`).

## License

PdfBooklet is **AGPL-3.0** (see `LICENSE`). PDFsharp 6.5+ is dual-licensed AGPL/commercial and throws
`PdfSharpLicenseException` unless a license is loaded; `Program.Main` calls
`PdfSharpLicense.SetAgplLicense()`, and the test assembly does the same in a module initializer.
Do not relicense PdfBooklet under a non-AGPL license without buying a PDFsharp commercial license.

## PDF library

PDFsharp 6.5.0 cannot write PDF/A (no XMP/ICC output intent); `OutputSettings.PdfVersion` only selects
the PDF header version (1.4–1.7).

## Build & Test

```bash
dotnet build src/PdfBooklet.csproj
dotnet test tests/PdfBooklet.Tests/PdfBooklet.Tests.csproj
dotnet run --project src/PdfBooklet.csproj -- path/to/file.pdf   # optional file to open at startup
```

## Packaging and releases

Installer scripts live in `scripts/` (deb, rpm and NSIS modelled on Promptuarium's PStudio packaging) and read the
version from `version.txt` unless `--version` is given; `.csproj` carries no version. The files they package are in
`packaging/` (`linux/pdfbooklet.desktop`, `linux/lintian-overrides`, `windows/PdfBooklet.nsi`). Outputs go to
`installer/` (git-ignored). The Linux packages put the self-contained build in `/usr/lib/pdfbooklet` (no `.pdb`)
with a `/usr/bin/pdfbooklet` launcher; the desktop entry lists `application/pdf` so PdfBooklet shows up in "Open with".
The NSIS installer removes a previous installation first and optionally adds PdfBooklet to "Open with" for `.pdf`
(never the default program).

| Platform | Script | Output |
|---|---|---|
| Linux .deb | `scripts/Build-Deb-Installer.sh` | `installer/deb/pdfbooklet_<ver>_amd64.deb` |
| Linux .rpm | `scripts/Build-Rpm-Installer.sh` | `installer/rpm/x86_64/pdfbooklet-<ver>-1.el10.x86_64.rpm` |
| Linux both, in Podman | `scripts/Build-Installer.Podman.sh` | builds on Ubuntu 26.04 / Rocky 10 images from `container/` |
| Windows .exe | `scripts/Build-Installer.ps1` (PowerShell 7 + NSIS, `packaging/windows/PdfBooklet.nsi`; also runs on Linux) | `installer/nsis/PdfBooklet-<ver>-win-x64-setup.exe` |
| macOS .dmg | `scripts/Build-Dmg-Installer.sh` (macOS only) | `installer/dmg/PdfBooklet-<ver>-osx-arm64.dmg` |

**Every release has proper release notes.** Releasing a version:

1. Bump `version.txt`.
2. Write the notes in `RELEASE_NOTES.md`: a section for the version without its BUILD number (`## 1.1.0`), newest
   first, describing what changed **for the user** (features, fixes, upgrade consequences such as settings being
   reset), one line per paragraph or item (GitHub shows every line break). Releases of the same code share a
   section. Check with `scripts/Release-Notes.sh <version>`. CI fails when `version.txt` has no notes.
3. Commit, tag with the version (`git tag 1.1.0.0`) and push the branch and the tag.

The `Release` workflow runs on every pushed tag: it checks that the tag equals `version.txt` and has notes, runs the
tests, builds the installers in parallel, then creates a GitHub release named after the tag (the latest release)
whose text is the notes of the version, the downloads with install instructions, and the full-changelog link. Do not
create releases by hand. Verify the run succeeded and the release has its notes and all four installers. If the
notes of a published release change, update it too (`gh release edit <tag> --notes-file ...`, same format). The
macOS bundle is ad-hoc signed only.

The application icon is `src/Assets/pdfbooklet-icon.png` / `.ico`; the installers use it too.

## Documentation

- `README.md` describes the product, installation, usage and building. Keep it in step with UI and
  settings changes.
- `docs/PdfBooklet-guide.pdf` is an 8-page user guide that doubles as the sample/test PDF (8 pages = two
  sheets, no blanks). It is generated by `docs/make-guide.py` (Python + `reportlab`, DejaVu fonts) from the
  screenshots in `docs/images/`. Regenerate it, and retake the screenshots, when the UI or settings change.
  When running the app for screenshots or experiments, set `XDG_CONFIG_HOME` to a scratch directory so the
  real per-user config is not touched.

## Conventions

- ViewModels use `[ObservableProperty]` / `[RelayCommand]` source generators — keep classes `partial`.
- Register everything in `Composition/ServiceCollectionExtensions.cs`; the container is built in
  `App.axaml.cs`. Never use `new` for services outside the composition root and tests.
- Nullable reference types are enabled; no `null!` suppressions without justification.
- Target framework `net10.0`; compiled Avalonia bindings are on by default — keep bindings compile-time safe.
- Package versions live in `Directory.Packages.props`; the version number lives in `version.txt`.
