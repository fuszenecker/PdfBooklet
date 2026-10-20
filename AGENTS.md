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
  Configuration/   # Strongly-typed settings bound from appsettings.json
  Composition/     # DI registration (ServiceCollectionExtensions)
tests/
  PdfBooklet.Tests/   # MSTest + NSubstitute; folders mirror src/
```

- `BookletLayoutPlanner` is a pure function from (source page sizes, sheet, layout) to a `BookletPlan`:
  which source page lands where on every sheet side, with scale and offset. The preview control and
  `PdfBookletExporter` both consume the plan, so the preview shows exactly what is exported.
- Sheets are always landscape (two pages side by side, fold across the short axis). Page counts are
  padded with blanks at the end to a multiple of 4 per signature.
- Config lives in `appsettings.json` (bundled defaults) layered with the per-user file
  (`$XDG_CONFIG_HOME/PdfBooklet` on Linux, `%LocalAppData%\PdfBooklet` on Windows).
  `IConfigurationService` is the only writer.

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

## Conventions

- ViewModels use `[ObservableProperty]` / `[RelayCommand]` source generators — keep classes `partial`.
- Register everything in `Composition/ServiceCollectionExtensions.cs`; the container is built in
  `App.axaml.cs`. Never use `new` for services outside the composition root and tests.
- Nullable reference types are enabled; no `null!` suppressions without justification.
- Target framework `net10.0`; compiled Avalonia bindings are on by default — keep bindings compile-time safe.
- Package versions live in `Directory.Packages.props`; the version number lives in `version.txt`.
