# Release notes

The notes of every release, the newest first. A section belongs to a version without its BUILD number (`## 1.1.0`): the releases of the same code (1.1.0.0, 1.1.0.1, …) share it. The release workflow puts the section of the tag into the GitHub release, with the downloads, and refuses a tag that has no section; CI fails when the version in `version.txt` has none. A paragraph or an item is one line: GitHub shows every line break of a release.

## 1.2.1

- Maintenance release: PdfBooklet uses Promptuarium.Serializer 8.4.0 to read and write its settings. Nothing changes for you: the settings are kept, and `settings.prmt` stays a `PRMT:CFG1-LE` container, so earlier versions of PdfBooklet (and of `pedit` and PStudio) can still read the files this version saves.

## 1.2.0

- **Make a booklet from part of the PDF.** Type the pages into the new *Pages* field above the preview: single pages and ranges separated by commas, in the order they should appear in the booklet, e.g. `5-12` or `1-4, 7, 10-` (`10-` runs to the last page). Leave it empty for all pages, as before. The preview follows while you type, the summary shows how many of the pages are used, and a selection that cannot work (a page the PDF does not have, a range that ends before it starts) shows the reason and disables Export. The selection is cleared when another PDF is imported; it is not stored with the settings.
- The top of the main window has more room, and long file names wrap instead of running off the window.

## 1.1.1

- The settings files explain themselves: every section and every setting of `settings.prmt` carries a comment with its meaning, its unit and its allowed values or range (e.g. "The width of a Custom sheet, in mm (50 to 2000)"). Promptuarium's `pedit` and PStudio show the comments; the per-user file gets them the next time the settings are saved. The settings themselves are unchanged and kept.

## 1.1.0

The settings are stored in a Promptuarium container instead of a JSON file, and the installers are reworked.

- **The settings start from the defaults after upgrading.** They move from the per-user `appsettings.json` to `settings.prmt`, a [Promptuarium](https://github.com/fuszenecker/Promptuarium) container in the `PRMT:CFG1-LE` format, written by [Promptuarium.Serializer](https://www.nuget.org/packages/Promptuarium.Serializer/) ([SERIALIZER_FORMAT.md](https://github.com/fuszenecker/Promptuarium/blob/master/SERIALIZER_FORMAT.md)). The old file is not read. Promptuarium's `pedit` and PStudio can show and edit the new one.
- The bundled defaults are a `settings.prmt` too, in the application directory; the per-user file is applied on top of them.
- **Linux:** the packages install PdfBooklet to `/usr/lib/pdfbooklet` (instead of `/opt/pdfbooklet`), still started with `pdfbooklet`. PdfBooklet is offered in "Open with" for PDF files. The packages carry the copyright and a changelog, and no debug symbols.
- **Windows:** the installer removes the previous installation first, so no file of an older version stays behind. It can offer PdfBooklet in "Open with" for PDF files (the default PDF application is not changed). The desktop shortcut is optional and off by default; the Start Menu has a single PdfBooklet entry (uninstall it in Apps). The setup program carries its version.

## 1.0.1

- **Fix: macOS remembers the settings.** There was no per-user settings location on macOS, so every start began with the defaults; the settings are now saved to `~/Library/Application Support/PdfBooklet`.
- A user guide, [`PdfBooklet-guide.pdf`](https://github.com/fuszenecker/PdfBooklet/blob/1.0.1.0/docs/PdfBooklet-guide.pdf): eight pages, so it is also a sample to try PdfBooklet and the printer with (two sheets, no blank pages), with a checklist on its last page. The README describes installing, using and printing.
- Releases are stable releases from now on, and the newest one is marked as the latest.

## 1.0.0

- **Manual duplex** for single-sided printers (Settings → Output PDF): the exported PDF has all the fronts first, then all the backs in reverse order. Print the first half, turn the printed stack over, feed it back in, and print the second half.

## 0.1.0

The first release of PdfBooklet: it turns a PDF into a print-ready 2-up booklet.

- Three buttons: import a PDF, settings, export the booklet. The live preview shows the front and the back of every sheet, exactly as they are exported; the blank pages that fill up the last sheet are dashed.
- Sheets A3, A4, A5, Letter, Legal, Tabloid or a custom size, always landscape with two pages side by side.
- Binding on the left or on the right (right-to-left books), and signatures: a long document folded in separate groups of a multiple of 4 pages.
- Top, bottom, inner and outer margins; fit to the area, actual size or a custom scale; horizontal and vertical alignment.
- Output PDF version 1.4–1.7 and stream compression. The original file is never modified.
- The settings are remembered between runs (on Windows and Linux), and `pdfbooklet file.pdf` opens a file at startup.
- 0.1.0.0 came as self-contained archives; 0.1.0.1 adds the installers (Debian/Ubuntu `.deb`, Fedora/RHEL/Rocky `.rpm`, Windows setup, macOS disk image), the application icon, and the AGPL license in every installer.
