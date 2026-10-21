#!/usr/bin/env python3
"""Generates docs/PdfBooklet-guide.pdf: an 8-page A5 user guide that doubles as a sample PDF.

Eight pages fill exactly two booklet sheets, so importing the guide into PdfBooklet shows a
complete imposition with no blank pages. Requires: pip install reportlab
"""
import io
from pathlib import Path

from PIL import Image
from reportlab.lib import colors
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A5
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (BaseDocTemplate, Flowable, Frame, PageBreak, PageTemplate,
                                Paragraph, Spacer, Table, TableStyle)

DOCS = Path(__file__).resolve().parent
ROOT = DOCS.parent
OUTPUT = DOCS / "PdfBooklet-guide.pdf"
VERSION = (ROOT / "version.txt").read_text().strip()
PAGE_COUNT = 8

FONT_DIR = Path("/usr/share/fonts/truetype/dejavu")
for name, file in [("Body", "DejaVuSansCondensed.ttf"), ("Body-Bold", "DejaVuSansCondensed-Bold.ttf"),
                   ("Body-Italic", "DejaVuSansCondensed-Oblique.ttf"),
                   ("Body-BoldItalic", "DejaVuSansCondensed-BoldOblique.ttf"), ("Mono", "DejaVuSansMono.ttf")]:
    pdfmetrics.registerFont(TTFont(name, str(FONT_DIR / file)))
pdfmetrics.registerFontFamily("Body", normal="Body", bold="Body-Bold", italic="Body-Italic", boldItalic="Body-BoldItalic")

ACCENT = colors.HexColor("#1D4ED8")
ACCENT_LIGHT = colors.HexColor("#D6E4FA")
INK = colors.HexColor("#1B1F27")
MUTED = colors.HexColor("#5B6472")
RED = colors.HexColor("#D92D20")
BUTTON_GRAY = colors.HexColor("#CCCCCC")
BUTTON_BLUE = colors.HexColor("#3BAEF0")

W, H = A5
CONTENT_W = W - 36 * mm

body = ParagraphStyle("body", fontName="Body", fontSize=9.4, leading=13.2, textColor=INK, alignment=TA_LEFT, spaceAfter=5)
small = ParagraphStyle("small", parent=body, fontSize=8, leading=11, textColor=MUTED)
h1 = ParagraphStyle("h1", parent=body, fontName="Body-Bold", fontSize=20, leading=24, textColor=ACCENT, spaceAfter=8)
h2 = ParagraphStyle("h2", parent=body, fontName="Body-Bold", fontSize=11.5, leading=15, textColor=INK, spaceBefore=6, spaceAfter=3)
check = ParagraphStyle("check", parent=body, leftIndent=12, firstLineIndent=-12, spaceAfter=3)
bullet = ParagraphStyle("bullet", parent=body, leftIndent=11, bulletIndent=0, spaceAfter=3)
title = ParagraphStyle("title", parent=body, fontName="Body-Bold", fontSize=34, leading=38, textColor=ACCENT, alignment=1, spaceAfter=4)
subtitle = ParagraphStyle("subtitle", parent=body, fontSize=13, leading=17, textColor=MUTED, alignment=1, spaceAfter=14)
cell = ParagraphStyle("cell", parent=body, fontSize=8.6, leading=11.5, spaceAfter=0)
cell_bold = ParagraphStyle("cell_bold", parent=cell, fontName="Body-Bold")


def P(text, style=body):
    return Paragraph(text, style)


def bullets(*items):
    return [Paragraph(text, bullet, bulletText="•") for text in items]


class Icon(Flowable):
    """The three toolbar buttons, drawn as vectors in the same style as the application."""

    def __init__(self, kind, size=14 * mm):
        super().__init__()
        self.kind, self.size = kind, size
        self.width = self.height = size

    def wrap(self, *_):
        return self.size, self.size

    def draw(self):
        c, size = self.canv, self.size
        c.setFillColor(BUTTON_BLUE if self.kind == "export" else BUTTON_GRAY)
        c.setStrokeColor(colors.black)
        c.rect(0, 0, size, size, stroke=0, fill=1)
        box = size * 0.5
        s = box / 24
        ox = oy = (size - box) / 2

        def pt(x, y):
            return ox + x * s, oy + (24 - y) * s

        c.setLineWidth(1.8 * s)
        c.setLineCap(1)
        c.setLineJoin(1)
        c.setFillColor(colors.black)
        if self.kind == "settings":
            cx, cy = pt(12, 12)
            for k in range(8):
                c.saveState()
                c.translate(cx, cy)
                c.rotate(k * 45)
                c.rect(-1.9 * s, 6.5 * s, 3.8 * s, 3.9 * s, stroke=0, fill=1)
                c.restoreState()
            c.circle(cx, cy, 8 * s, stroke=0, fill=1)
            c.setFillColor(BUTTON_GRAY)
            c.circle(cx, cy, 3.6 * s, stroke=0, fill=1)
            return

        def poly(points):
            p = c.beginPath()
            p.moveTo(*pt(*points[0]))
            for point in points[1:]:
                p.lineTo(*pt(*point))
            c.drawPath(p, stroke=1, fill=0)

        poly([(4, 15), (4, 19), (5, 20), (19, 20), (20, 19), (20, 15)])
        if self.kind == "import":
            poly([(12, 4), (12, 15)])
            poly([(7.5, 10.5), (12, 15), (16.5, 10.5)])
        else:
            poly([(12, 15), (12, 4)])
            poly([(7.5, 8.5), (12, 4), (16.5, 8.5)])


class Badge(Flowable):
    def __init__(self, number, radius=3.2 * mm):
        super().__init__()
        self.number, self.radius = number, radius

    def wrap(self, *_):
        return self.radius * 2, self.radius * 2

    def draw(self):
        c = self.canv
        c.setFillColor(RED)
        c.circle(self.radius, self.radius, self.radius, stroke=0, fill=1)
        c.setFillColor(colors.white)
        c.setFont("Body-Bold", 8.5)
        c.drawCentredString(self.radius, self.radius - 3, str(self.number))


class Shot(Flowable):
    """A screenshot with numbered red badges at pixel positions of the original image."""

    def __init__(self, image, width, badges=()):
        super().__init__()
        self.reader = ImageReader(image)
        iw, ih = self.reader.getSize()
        self.scale = width / iw
        self.width, self.height = width, ih * self.scale
        self.badges = badges

    def wrap(self, *_):
        return self.width, self.height

    def draw(self):
        c = self.canv
        c.drawImage(self.reader, 0, 0, self.width, self.height)
        c.setStrokeColor(colors.HexColor("#9AA3B2"))
        c.setLineWidth(0.6)
        c.rect(0, 0, self.width, self.height, stroke=1, fill=0)
        for number, px, py in self.badges:
            x, y = px * self.scale, self.height - py * self.scale
            c.setFillColor(RED)
            c.circle(x, y, 3.2 * mm, stroke=0, fill=1)
            c.setFillColor(colors.white)
            c.setFont("Body-Bold", 8.5)
            c.drawCentredString(x, y - 3, str(number))


def cropped(path, box):
    buffer = io.BytesIO()
    Image.open(path).crop(box).save(buffer, "PNG")
    buffer.seek(0)
    return buffer


def booklet_order(pages):
    """Imposition for left binding: [(front_left, front_right, back_left, back_right), ...], 1-based."""
    slots = (pages + 3) // 4 * 4
    sheets = []
    for i in range(0, slots // 2, 2):
        sheets.append((slots - i, i + 1, i + 2, slots - 1 - i))
    return sheets


class Imposition(Flowable):
    """The four sides of the two sheets of an 8-page booklet."""

    SHEET_W = 46 * mm
    SHEET_H = SHEET_W * 210 / 297
    LABEL = 6 * mm
    GAP_X = 12 * mm
    GAP_Y = 3 * mm

    def __init__(self, pages=PAGE_COUNT):
        super().__init__()
        self.sheets = booklet_order(pages)
        self.width = 2 * self.SHEET_W + self.GAP_X
        self.height = 2 * (self.SHEET_H + self.LABEL) + self.GAP_Y

    def wrap(self, *_):
        return self.width, self.height

    def draw(self):
        c = self.canv
        for index, (fl, fr, bl, br) in enumerate(self.sheets):
            for side, (left, right) in enumerate([(fl, fr), (bl, br)]):
                x = side * (self.SHEET_W + self.GAP_X)
                y = self.height - (index + 1) * (self.SHEET_H + self.LABEL) - index * self.GAP_Y
                c.setFillColor(INK)
                c.setFont("Body-Bold", 8.5)
                c.drawString(x, y + self.SHEET_H + 1.8 * mm, f"Sheet {index + 1} – {'front' if side == 0 else 'back'}")
                c.setFillColor(colors.white)
                c.setStrokeColor(colors.HexColor("#8A93A3"))
                c.setLineWidth(0.8)
                c.rect(x, y, self.SHEET_W, self.SHEET_H, stroke=1, fill=1)
                c.setDash(2, 2)
                c.setStrokeColor(colors.HexColor("#B5BCC8"))
                c.line(x + self.SHEET_W / 2, y, x + self.SHEET_W / 2, y + self.SHEET_H)
                c.setDash()
                for half, number in enumerate((left, right)):
                    px = x + half * self.SHEET_W / 2 + 3 * mm
                    pw = self.SHEET_W / 2 - 6 * mm
                    py, ph = y + 2.5 * mm, self.SHEET_H - 5 * mm
                    c.setFillColor(ACCENT_LIGHT)
                    c.setStrokeColor(ACCENT)
                    c.setLineWidth(0.7)
                    c.rect(px, py, pw, ph, stroke=1, fill=1)
                    c.setFillColor(colors.HexColor("#1F3C73"))
                    c.setFont("Body-Bold", 17)
                    c.drawCentredString(px + pw / 2, py + ph / 2 - 6, str(number))


def decorate(canvas, doc):
    page = canvas.getPageNumber()
    canvas.saveState()
    canvas.setStrokeColor(colors.HexColor("#B4BBC8"))
    canvas.setLineWidth(0.6)
    canvas.rect(7 * mm, 7 * mm, W - 14 * mm, H - 14 * mm, stroke=1, fill=0)
    p = canvas.beginPath()
    top = H - 7 * mm
    p.moveTo(W / 2 - 2.2 * mm, top - 1.2 * mm)
    p.lineTo(W / 2 + 2.2 * mm, top - 1.2 * mm)
    p.lineTo(W / 2, top - 4.4 * mm)
    p.close()
    canvas.setFillColor(colors.HexColor("#B4BBC8"))
    canvas.drawPath(p, stroke=0, fill=1)
    canvas.setFillColor(ACCENT)
    canvas.circle(W - 17 * mm, H - 17 * mm, 6 * mm, stroke=0, fill=1)
    canvas.setFillColor(colors.white)
    canvas.setFont("Body-Bold", 14)
    canvas.drawCentredString(W - 17 * mm, H - 17 * mm - 5, str(page))
    canvas.setFillColor(MUTED)
    canvas.setFont("Body", 7.6)
    canvas.drawString(18 * mm, 11 * mm, f"PdfBooklet guide · version {VERSION}")
    canvas.drawRightString(W - 18 * mm, 11 * mm, f"{page} / {PAGE_COUNT}")
    canvas.restoreState()


def steps_table(rows):
    data = [[Icon(kind), [P(f"<b>{head}</b>", h2), P(text)]] for kind, head, text in rows]
    table = Table(data, colWidths=[18 * mm, CONTENT_W - 18 * mm])
    table.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP"), ("TOPPADDING", (0, 0), (-1, -1), 4),
                               ("BOTTOMPADDING", (0, 0), (-1, -1), 4), ("LEFTPADDING", (0, 0), (-1, -1), 0)]))
    return table


def callouts(rows):
    data = [[Badge(n), P(text, cell)] for n, text in rows]
    table = Table(data, colWidths=[9 * mm, CONTENT_W - 9 * mm])
    table.setStyle(TableStyle([("VALIGN", (0, 0), (-1, -1), "TOP"), ("TOPPADDING", (0, 0), (-1, -1), 1.5),
                               ("BOTTOMPADDING", (0, 0), (-1, -1), 1.5), ("LEFTPADDING", (0, 0), (-1, -1), 0)]))
    return table


def grid(data, widths, header=True):
    data = [[P(f"<b>{c}</b>" if header and r == 0 else c, cell) for c in row] for r, row in enumerate(data)]
    table = Table(data, colWidths=widths, repeatRows=1 if header else 0)
    style = [("GRID", (0, 0), (-1, -1), 0.5, colors.HexColor("#B4BBC8")), ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
             ("TOPPADDING", (0, 0), (-1, -1), 3), ("BOTTOMPADDING", (0, 0), (-1, -1), 3)]
    if header:
        style.append(("BACKGROUND", (0, 0), (-1, 0), ACCENT_LIGHT))
    table.setStyle(TableStyle(style))
    return table


def build_story():
    icon = ImageReader(str(ROOT / "src" / "Assets" / "pdfbooklet-icon.png"))
    story = []

    # 1 — cover
    class Cover(Flowable):
        def wrap(self, *_):
            return CONTENT_W, 52 * mm

        def draw(self):
            self.canv.drawImage(icon, CONTENT_W / 2 - 22 * mm, 4 * mm, 44 * mm, 44 * mm, mask="auto")

    story += [Spacer(1, 16 * mm), Cover(), Spacer(1, 6 * mm), P("PdfBooklet", title),
              P("Turn any PDF into a print-ready booklet", subtitle),
              P("PdfBooklet puts two pages on each side of a sheet, in the order that makes them read correctly "
                "once the stack is folded in the middle. Open a PDF, adjust the settings, and export."),
              Spacer(1, 4 * mm),
              P("<b>This guide is also the sample file.</b> It has exactly 8 pages, which makes two sheets with "
                "no blank pages. Import it into PdfBooklet, look at the preview, export it and print it. The "
                "checklist on the last page tells you what you should see."),
              Spacer(1, 12 * mm),
              P(f"Version {VERSION} · AGPL-3.0 · github.com/fuszenecker/PdfBooklet", small), PageBreak()]

    # 2 — quick start
    story += [P("Quick start", h1),
              P("The window has three square buttons. Use them from left to right."),
              steps_table([
                  ("import", "1  Import", "Pick the PDF you want to turn into a booklet. PdfBooklet reads the page "
                   "sizes and immediately shows how the pages will be laid out. You can also start the program with "
                   "a file name: <font name='Mono'>pdfbooklet file.pdf</font>."),
                  ("settings", "2  Settings", "Choose the paper size, binding, margins, scale, alignment and output "
                   "options. The preview updates when you press OK, and the choices are remembered for next time."),
                  ("export", "3  Export", "Choose where to save the booklet PDF. Print it double-sided, or tick "
                   "<i>Manual duplex</i> in the settings first if your printer only prints on one side (see page 7)."),
              ]),
              Spacer(1, 4 * mm),
              P("The original file is never modified, and PdfBooklet refuses to overwrite it. The export button "
                "stays disabled until a PDF is loaded and the settings are valid.", small), PageBreak()]

    # 3 — main window
    main = ROOT / "docs" / "images" / "main-window.png"
    story += [P("The main window", h1),
              Shot(str(main), 96 * mm, badges=[(1, 44, 48), (2, 117, 48), (3, 192, 48), (4, 700, 48), (5, 140, 250), (6, 560, 631)]),
              Spacer(1, 3 * mm),
              callouts([
                  (1, "<b>Import</b> a PDF."),
                  (2, "<b>Settings</b> for the sheet, layout and output."),
                  (3, "<b>Export</b> the booklet. It lights up once there is something to export."),
                  (4, "File name, page count and size of the first page, then a summary: sheets, printed sides, "
                      "blank pages added and the scale of page 1. A red message appears here when the settings "
                      "cannot work."),
                  (5, "<b>Preview</b> of one sheet, front above and back below, exactly as it will be exported. "
                      "Numbers are pages of your PDF; dashed boxes are blank pages added to fill the booklet."),
                  (6, "<b>Arrows</b> to step through all the sheets."),
              ]), PageBreak()]

    # 4 — imposition
    story += [P("How a booklet is made", h1),
              P("A booklet sheet carries two pages on each side. The sheets are stacked, folded in the middle "
                "and the pages fall into reading order. This guide has 8 pages, so it needs 2 sheets, "
                "which is 4 printed sides:"),
              Spacer(1, 2 * mm), Imposition(), Spacer(1, 3 * mm)]
    story += bullets(
        "The outermost sheet carries the last page next to the first. Each sheet further in carries the next "
        "pair of pages.",
        "When the page count is not a multiple of 4, PdfBooklet adds blank pages at the end (dashed in the preview).",
        "<b>Pages per signature</b> splits a long document into separate folded groups. With 16, each group "
        "of 4 sheets is folded and stapled on its own; with 0 the whole document is one group.",
        "<b>Binding edge: Right</b> mirrors every side, for books that read from right to left.")
    story += [PageBreak()]

    # 5 — settings 1
    settings = ROOT / "docs" / "images" / "settings-dialog.png"
    story += [P("Sheet, binding, margins", h1),
              Shot(cropped(settings, (0, 0, 460, 368)), 84 * mm), Spacer(1, 3 * mm),
              P("Sheet", h2),
              P("<b>Paper size</b> is the physical sheet: A3, A4, A5, Letter, Legal, Tabloid or Custom (50 to 2000 mm). "
                "Sheets are always landscape, because two pages sit side by side. Pages are scaled to fit, so any "
                "source size works; with zero margins, A5 pages fill A4 sheets at 100 %."),
              P("Binding", h2),
              P("<b>Binding edge</b>: Left for books read left to right, Right for right to left. "
                "<b>Pages per signature</b>: 0 folds everything as one group, otherwise a multiple of 4."),
              P("Margins", h2),
              P("Top, bottom, inner (between a page and the fold) and outer (between a page and the sheet edge), in "
                "millimetres. Most printers cannot print the outermost 4 to 6 mm."),
              PageBreak()]

    # 6 — settings 2
    story += [P("Scale, alignment, output", h1),
              Shot(cropped(settings, (0, 372, 460, 660)), 84 * mm), Spacer(1, 3 * mm),
              P("Scale and alignment", h2),
              P("<b>Fit to area</b> scales every page up or down to fill the space inside the margins. "
                "<b>Actual size</b> keeps 100 % and clips what does not fit. <b>Custom</b> takes a percentage "
                "from 1 to 1000. <b>Horizontal</b> alignment (inner, center, outer) is mirrored on the two pages; "
                "<b>Vertical</b> is top, middle or bottom. Alignment matters when a page is smaller than the area."),
              P("Output PDF", h2),
              P("<b>PDF version</b> 1.4 to 1.7 sets the file header only; PDF/A is not supported. "
                "<b>Compress streams</b> makes smaller files. <b>Manual duplex</b> is for single-sided printers "
                "(next page)."),
              P("Settings that cannot work (negative margins, a signature size that is not a multiple of 4, margins "
                "that leave no room) show the reason and disable OK.", small),
              PageBreak()]

    # 7 — printing
    story += [P("Printing the booklet", h1),
              P("Duplex printer", h2),
              P("Print the exported PDF as it is: double-sided, <b>flip on short edge</b>, scale <b>100 % / actual "
                "size</b> (not “fit to page”), paper size equal to the sheet size you chose."),
              P("Single-sided printer", h2),
              P("Tick <b>Manual duplex</b> in the settings before exporting. The PDF then holds all fronts first, "
                "then all backs in reverse order:")]
    story += [P("1.  Print the first half of the pages (fronts).", bullet),
              P("2.  Turn the whole stack over around its short edge and put it back in the tray.", bullet),
              P("3.  Print the second half (backs).", bullet),
              Spacer(1, 2 * mm),
              P("For this 8-page guide the exported PDF has 4 pages: print pages 1–2, turn the stack over, then "
                "print pages 3–4. Finally fold the stack in half and staple along the fold."),
              P("If it comes out wrong", h2)]
    story += bullets(
        "Backs upside down: turn the stack over around the other edge.",
        "Backs in the wrong order: your printer feeds the other way. Print the second half again with your "
        "viewer’s “reverse order” option.",
        "Always test with this guide first: it takes only two sheets.")
    story += [PageBreak()]

    # 8 — checklist
    sheets = booklet_order(PAGE_COUNT)
    duplex_rows = [["Output page", "Left | right"]]
    manual_rows = [["Output page", "Left | right"]]
    sides = []
    for number, (fl, fr, bl, br) in enumerate(sheets, 1):
        sides.append((f"Sheet {number} front", f"{fl} | {fr}", f"Sheet {number} back", f"{bl} | {br}"))
    duplex_order = [(f, a) for f, a, b, c in [(s[0], s[1], s[2], s[3]) for s in sides]]
    duplex_pages = []
    for s in sides:
        duplex_pages += [(s[0], s[1]), (s[2], s[3])]
    manual_pages = [(s[0], s[1]) for s in sides] + [(s[2], s[3]) for s in reversed(sides)]
    table = [["Page", "Duplex order", "Manual duplex order"]]
    for i in range(len(duplex_pages)):
        table.append([str(i + 1), f"{duplex_pages[i][0]}:  {duplex_pages[i][1]}", f"{manual_pages[i][0]}:  {manual_pages[i][1]}"])
    story += [P("Checklist", h1),
              P("What you should see with this guide (left binding, A4 sheets):"),
              grid(table, [14 * mm, 49 * mm, 49 * mm]),
              Spacer(1, 4 * mm)]
    story += [P(text, check) for text in (
        "☐  The import summary says <b>8 pages → 2 sheets (4 printed sides)</b>, with no blank pages.",
        "☐  The exported PDF has 4 landscape pages in the order above.",
        "☐  Printed double-sided and folded, the pages read 1 to 8 in order.",
        "☐  With binding Right, the two pages of every side swap places.",
        "☐  The thin frame around each page is fully visible, so nothing is clipped.")]
    story += [Spacer(1, 3 * mm),
              P(f"PdfBooklet {VERSION} · © 2026 Róbert Fuszenecker · AGPL-3.0. Built with Avalonia (MIT) and "
                "PDFsharp (AGPL-3.0). Source and releases: github.com/fuszenecker/PdfBooklet", small)]
    return story


def main():
    doc = BaseDocTemplate(str(OUTPUT), pagesize=A5, title="PdfBooklet guide", author="Róbert Fuszenecker",
                          subject="User guide and sample PDF for PdfBooklet", invariant=1,
                          leftMargin=18 * mm, rightMargin=18 * mm, topMargin=24 * mm, bottomMargin=18 * mm)
    frame = Frame(18 * mm, 18 * mm, W - 36 * mm, H - 18 * mm - 25 * mm, leftPadding=0, rightPadding=0,
                  topPadding=0, bottomPadding=0, id="content")
    doc.addPageTemplates([PageTemplate(id="page", frames=[frame], onPage=decorate)])
    doc.build(build_story())
    print(f"wrote {OUTPUT} ({doc.page} pages)")
    if doc.page != PAGE_COUNT:
        raise SystemExit(f"expected {PAGE_COUNT} pages, got {doc.page}: adjust the content")


if __name__ == "__main__":
    main()
