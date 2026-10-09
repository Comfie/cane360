"""Build offline manual artifacts from one reviewed Markdown source.

Run using the Documents skill's bundled Python (python-docx and Pillow).
No application dependencies, database access or external services are required.
"""

from pathlib import Path
from html import escape
import argparse
import csv
import re

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
SOURCE = HERE / "Cane360-User-Manual.md"
PUBLIC = ROOT / "src/Web/ClientApp/public/help"


def blocks(text):
    lines = text.splitlines()
    i = 0
    while i < len(lines):
        line = lines[i]
        if not line.strip():
            i += 1
            continue
        heading = re.match(r"^(#{1,3}) (.+)$", line)
        if heading:
            yield ("heading", len(heading[1]), heading[2])
            i += 1
        elif line.startswith("| "):
            rows = []
            while i < len(lines) and lines[i].startswith("| "):
                cells = [cell.strip() for cell in lines[i].strip("|").split("|")]
                if not all(re.fullmatch(r":?-+:?", cell) for cell in cells):
                    rows.append(cells)
                i += 1
            yield ("table", rows)
        elif re.match(r"^(- |\d+\. )", line):
            ordered = not line.startswith("- ")
            items = []
            while i < len(lines) and re.match(r"^(- |\d+\. )", lines[i]):
                items.append(re.sub(r"^(- |\d+\. )", "", lines[i]))
                i += 1
            yield ("list", ordered, items)
        else:
            paragraph = [line]
            i += 1
            while i < len(lines) and lines[i].strip() and not re.match(r"^(#|\||- |\d+\. )", lines[i]):
                paragraph.append(lines[i])
                i += 1
            yield ("paragraph", " ".join(paragraph))


def inline_html(text):
    return re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", escape(text))


def anchor(text):
    return "section-" + re.sub(r"[^a-z0-9]+", "-", text.lower()).strip("-")


def build_html(content):
    chapters = [b for b in content if b[0] == "heading" and b[1] == 2]
    toc = "".join(f'<li><a href="#{anchor(b[2])}">{escape(b[2])}</a></li>' for b in chapters)
    body = []
    for b in content:
        if b[0] == "heading":
            body.append(f'<h{b[1]} id="{anchor(b[2])}">{escape(b[2])}</h{b[1]}>')
        elif b[0] == "paragraph":
            body.append(f"<p>{inline_html(b[1])}</p>")
        elif b[0] == "list":
            tag = "ol" if b[1] else "ul"
            body.append(f"<{tag}>" + "".join(f"<li>{inline_html(x)}</li>" for x in b[2]) + f"</{tag}>")
        else:
            head = "".join(f'<th scope="col">{inline_html(x)}</th>' for x in b[1][0])
            rows = "".join("<tr>" + "".join(f"<td>{inline_html(x)}</td>" for x in row) + "</tr>" for row in b[1][1:])
            body.append(f'<div class="table-wrap"><table><thead><tr>{head}</tr></thead><tbody>{rows}</tbody></table></div>')
    # Search finds whole procedures, preserving context and revealing matching chapters.
    html = '''<!doctype html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Cane360 User Manual</title><style>
:root{color-scheme:light;--ink:#182b24;--muted:#56645e;--line:#d7e0d9;--accent:#24593f}
*{box-sizing:border-box}html{scroll-behavior:smooth;scroll-padding-top:90px}body{margin:0;color:var(--ink);background:#fafbf8;font:17px/1.65 system-ui,sans-serif}
a{color:var(--accent);text-underline-offset:3px}a:focus-visible,button:focus-visible,input:focus-visible{outline:3px solid #ca9a39;outline-offset:3px}
.toolbar{position:sticky;top:0;background:#fff;border-bottom:1px solid var(--line);padding:12px 24px;display:flex;gap:14px;align-items:center;z-index:2;flex-wrap:wrap}
.toolbar label{font-size:14px;font-weight:600}.toolbar input{margin-left:8px;padding:9px;border:1px solid #aab8ae;border-radius:4px;font:inherit;width:240px}
button{background:var(--accent);color:white;border:0;padding:11px 16px;border-radius:4px;font:600 14px system-ui;cursor:pointer}.toolbar a{font-size:14px}
.shell{display:grid;grid-template-columns:270px minmax(0,900px);gap:40px;max-width:1260px;margin:auto;padding:36px 24px}
aside{align-self:start;position:sticky;top:100px;max-height:calc(100vh - 120px);overflow:auto}aside h2{font-size:16px;margin:0 0 12px}aside ol{list-style:none;padding:0;margin:0}aside li{font-size:14px;line-height:1.45;margin:0 0 13px}
main{min-width:0;background:white;border:1px solid var(--line);padding:38px 44px}h1{font-size:38px;line-height:1.15;margin:0 0 16px;letter-spacing:-1px}h2{font-size:27px;line-height:1.25;margin:54px 0 20px;border-top:2px solid var(--line);padding-top:24px}h3{font-size:20px;line-height:1.35;margin:30px 0 12px}p{margin:0 0 14px}li{margin-bottom:8px}.table-wrap{overflow:auto}table{border-collapse:collapse;width:100%;font-size:14px;line-height:1.5;margin:18px 0 24px}th,td{text-align:left;vertical-align:top;border:1px solid var(--line);padding:10px 12px}th{background:#edf3ee}strong{font-weight:650}.status{font-size:14px;color:var(--muted)}[hidden]{display:none!important}
@media(max-width:900px){.shell{display:block;padding:18px}aside{position:static;max-height:none;margin-bottom:24px}main{padding:26px}h1{font-size:32px}.toolbar{padding:10px 18px}.toolbar input{width:180px}}
@media print{@page{size:A4;margin:18mm}body{font:10.5pt/1.45 Calibri,Arial,sans-serif;background:white;color:black}.toolbar,.status{display:none}aside{position:static;max-height:none;overflow:visible;break-after:page}aside h2{font-size:18pt}aside li{font-size:11pt;margin-bottom:9px}.shell{display:block;padding:0}main{border:0;padding:0}h1{font-size:26pt}h2{font-size:17pt;break-before:page;border:0;padding-top:0;margin-top:0}h3{font-size:12pt;break-after:avoid}p,li{orphans:3;widows:3}table{font-size:9pt}tr{break-inside:avoid}thead{display:table-header-group}a{color:black;text-decoration:none}.table-wrap{overflow:visible}[hidden]{display:revert!important}}
@media(prefers-reduced-motion:reduce){html{scroll-behavior:auto}}
</style></head><body>
<div class="toolbar"><label for="manual-search">Find a procedure<input id="manual-search" type="search" placeholder="e.g. attendance" aria-controls="manual-content"></label><button id="print-manual" type="button">Print manual</button><a href="#contents">Contents</a><span id="search-status" class="status" role="status" aria-live="polite"></span></div>
<div class="shell"><aside id="contents" aria-label="Manual contents"><h2>Contents</h2><ol>''' + toc + '''</ol></aside><main id="manual-content">''' + "\n".join(body) + '''</main></div>
<script>
const main=document.getElementById('manual-content');
const chapterHeads=[...main.querySelectorAll('h2')];
const chapters=[];
for(const heading of chapterHeads){const section=document.createElement('section');heading.before(section);section.append(heading);while(section.nextElementSibling&&!['H2'].includes(section.nextElementSibling.tagName)){section.append(section.nextElementSibling)}chapters.push(section)}
const search=document.getElementById('manual-search');const status=document.getElementById('search-status');
function filter(){const term=search.value.trim().toLocaleLowerCase();let matches=0;for(const chapter of chapters){const hit=!term||chapter.textContent.toLocaleLowerCase().includes(term);chapter.hidden=!hit;if(hit)matches++}status.textContent=term?matches+' matching chapters. Use your browser Find for a specific occurrence.':''}
search.addEventListener('input',filter);
for(const link of document.querySelectorAll('aside a'))link.addEventListener('click',()=>{search.value='';filter()});
document.getElementById('print-manual').addEventListener('click',()=>{search.value='';filter();window.print()});
window.addEventListener('beforeprint',()=>{for(const chapter of chapters)chapter.hidden=false});
window.addEventListener('afterprint',filter);
</script></body></html>'''
    HERE.joinpath("Cane360-User-Manual.html").write_text(html)
    PUBLIC.mkdir(parents=True, exist_ok=True)
    PUBLIC.joinpath("index.html").write_text(html)


def build_docx(content, toc_pdf=None):
    from docx import Document
    from docx.shared import Inches, Pt, RGBColor
    from docx.oxml import OxmlElement
    from docx.oxml.ns import qn
    from docx.enum.text import WD_ALIGN_PARAGRAPH
    from docx.enum.text import WD_TAB_ALIGNMENT
    from docx.enum.style import WD_STYLE_TYPE

    doc = Document()
    sec = doc.sections[0]
    sec.page_width, sec.page_height = Inches(8.5), Inches(11)
    sec.top_margin = sec.bottom_margin = sec.left_margin = sec.right_margin = Inches(1)
    sec.header_distance = sec.footer_distance = Inches(.492)
    # compact_reference_guide preset; editorial_cover header pattern.
    for name, size, color, before, after in [
        ("Normal", 11, "182B24", 0, 6), ("Title", 30, "182B24", 0, 14),
        ("Subtitle", 14, "56645E", 0, 12), ("Heading 1", 16, "2E74B5", 18, 10),
        ("Heading 2", 13, "2E74B5", 14, 7), ("Heading 3", 12, "1F4D78", 10, 5),
        ("List Bullet", 11, "182B24", 0, 4), ("List Number", 11, "182B24", 0, 4)]:
        s = doc.styles[name]
        s.font.name, s.font.size, s.font.color.rgb = "Calibri", Pt(size), RGBColor.from_string(color)
        f = s.paragraph_format
        f.space_before, f.space_after, f.line_spacing = Pt(before), Pt(after), 1.25
        f.widow_control = True
        if name.startswith("Heading"):
            f.keep_with_next = True
        if name.startswith("List"):
            f.left_indent, f.first_line_indent = Inches(.375), Inches(-.188)
    # Named overrides: compact table text and setup checklist; keep the trainer
    # sign-off together. Remove decorative residue inherited from Word Title.
    title_props = doc.styles["Title"].element.find(qn("w:pPr"))
    if title_props is not None:
        for border in list(title_props.findall(qn("w:pBdr"))): title_props.remove(border)
    contents_style = doc.styles.add_style("Manual Contents", WD_STYLE_TYPE.PARAGRAPH)
    contents_style.base_style = doc.styles["Normal"]
    contents_style.font.name = "Calibri"
    contents_style.font.size = Pt(10)
    # Real Word numbering, reset for every procedure.
    numbering = doc.part.numbering_part.element
    abstract_id = 70
    num_id = 100
    for fmt, marker in [("decimal", "%1."), ("bullet", "•")]:
        abstract = OxmlElement("w:abstractNum"); abstract.set(qn("w:abstractNumId"), str(abstract_id))
        level = OxmlElement("w:lvl"); level.set(qn("w:ilvl"), "0")
        for tag, val in [("start", "1"), ("numFmt", fmt), ("lvlText", marker), ("lvlJc", "left")]:
            el = OxmlElement("w:" + tag); el.set(qn("w:val"), val); level.append(el)
        props = OxmlElement("w:pPr")
        tabs = OxmlElement("w:tabs"); tab = OxmlElement("w:tab"); tab.set(qn("w:val"), "num"); tab.set(qn("w:pos"), "540"); tabs.append(tab); props.append(tabs)
        ind = OxmlElement("w:ind"); ind.set(qn("w:left"), "540"); ind.set(qn("w:hanging"), "271"); props.append(ind)
        level.append(props); abstract.append(level); numbering.append(abstract); abstract_id += 1

    def add_runs(p, text):
        for index, part in enumerate(re.split(r"\*\*(.+?)\*\*", text)):
            p.add_run(part).bold = bool(index % 2)

    def bookmark(p, name, identifier):
        start = OxmlElement("w:bookmarkStart"); start.set(qn("w:id"), str(identifier)); start.set(qn("w:name"), name)
        end = OxmlElement("w:bookmarkEnd"); end.set(qn("w:id"), str(identifier))
        p._p.insert(0, start); p._p.append(end)

    # Quiet running header and footer with page numbers.
    p = sec.header.paragraphs[0]; p.text = "CANE360  |  USER MANUAL  |  EDITION 1.0"
    p.style = doc.styles["Normal"]
    p.runs[0].font.size = Pt(8); p.runs[0].font.color.rgb = RGBColor.from_string("56645E")
    p = sec.footer.paragraphs[0]; p.alignment = WD_ALIGN_PARAGRAPH.RIGHT
    p.add_run("Cane360  •  Page ").font.size = Pt(8)
    field = OxmlElement("w:fldSimple"); field.set(qn("w:instr"), "PAGE"); p._p.append(field)

    doc.add_paragraph("GROWER OPERATIONS", style="Subtitle")
    doc.add_paragraph("Cane360\nUser Manual", style="Title")
    doc.add_paragraph("Module-by-module onboarding and operating guide", style="Subtitle")
    doc.add_paragraph("Edition 1.0 • 9 October 2026")
    doc.add_paragraph("From your first login to crop closure, payroll settlement and audit review.")
    doc.add_paragraph(content[3][1])
    doc.add_paragraph("Keep this guide beside your workstation. Read chapters 1–3 first, then follow the procedures for your assigned role.")
    doc.add_page_break()
    doc.add_paragraph("Contents", style="Heading 1")
    page_map = {}
    if toc_pdf:
        from pypdf import PdfReader
        for i, page in enumerate(PdfReader(toc_pdf).pages):
            if i < 2: continue
            text = " ".join(page.extract_text().split())
            for b in content:
                if b[0] == "heading" and b[1] == 2 and b[2] in text:
                    page_map.setdefault(b[2], i + 1)
    for b in content:
        if b[0] == "heading" and b[1] == 2:
            p = doc.add_paragraph(style="Manual Contents")
            link = OxmlElement("w:hyperlink"); link.set(qn("w:anchor"), anchor(b[2]).replace("-", "_"))
            run = OxmlElement("w:r"); text = OxmlElement("w:t"); text.text = b[2]; run.append(text); link.append(run); p._p.append(link)
            if b[2] in page_map:
                p.paragraph_format.tab_stops.add_tab_stop(Inches(6.5), WD_TAB_ALIGNMENT.RIGHT)
                p.add_run("\t" + str(page_map[b[2]]))
    doc.add_paragraph("Contents links work in Word and the exported PDF. Each chapter starts on a new page.")
    identifier = 1
    started = False
    subsection = ""
    chapter_title = ""
    for b in content:
        if not started:
            if b[0] == "heading" and b[1] == 2:
                started = True
            else:
                continue
        if b[0] == "heading":
            subsection = b[2]
            p = doc.add_paragraph(b[2], style="Heading " + str(b[1] - 1))
            if b[1] == 2:
                chapter_title = b[2]
                p.paragraph_format.page_break_before = True
            bookmark(p, anchor(b[2]).replace("-", "_"), identifier); identifier += 1
        elif b[0] == "paragraph":
            p = doc.add_paragraph(); add_runs(p, b[1])
            if chapter_title.startswith("6."):
                p.paragraph_format.line_spacing = 1.15
                p.paragraph_format.space_after = Pt(5)
            if subsection.startswith("16.4") and not b[1].startswith("Record the permitted"):
                p.paragraph_format.keep_with_next = True
        elif b[0] == "list":
            num_id += 1
            num = OxmlElement("w:num"); num.set(qn("w:numId"), str(num_id))
            abstract = OxmlElement("w:abstractNumId"); abstract.set(qn("w:val"), "70" if b[1] else "71"); num.append(abstract)
            override = OxmlElement("w:lvlOverride"); override.set(qn("w:ilvl"), "0")
            start = OxmlElement("w:startOverride"); start.set(qn("w:val"), "1"); override.append(start); num.append(override); numbering.append(num)
            for item in b[2]:
                p = doc.add_paragraph(style="List Number" if b[1] else "List Bullet")
                props = p._p.get_or_add_pPr(); n = OxmlElement("w:numPr")
                level = OxmlElement("w:ilvl"); level.set(qn("w:val"), "0"); n.append(level)
                identity = OxmlElement("w:numId"); identity.set(qn("w:val"), str(num_id)); n.append(identity); props.append(n)
                add_runs(p, item)
                if subsection.startswith("1.5"):
                    p.paragraph_format.line_spacing = 1.15
                    p.paragraph_format.space_after = Pt(3)
                if chapter_title.startswith("6."):
                    p.paragraph_format.line_spacing = 1.15
                    p.paragraph_format.space_after = Pt(3)
        else:
            rows = b[1]; count = len(rows[0])
            table = doc.add_table(rows=1, cols=count); table.autofit = False; table.style = "Table Grid"
            widths = ([1900, 3100, 4360] if count == 3 else [2850, 6510])
            if len(widths) != count:
                widths = [9360 // count] * count; widths[-1] += 9360 - sum(widths)
            for _ in rows[1:]: table.add_row()
            props = table._tbl.tblPr
            w = props.find(qn("w:tblW")); w.set(qn("w:w"), "9360"); w.set(qn("w:type"), "dxa")
            ind = OxmlElement("w:tblInd"); ind.set(qn("w:w"), "120"); ind.set(qn("w:type"), "dxa"); props.append(ind)
            margins = OxmlElement("w:tblCellMar")
            for edge, value in [("top",80),("bottom",80),("start",120),("end",120)]:
                el = OxmlElement("w:"+edge); el.set(qn("w:w"), str(value)); el.set(qn("w:type"), "dxa"); margins.append(el)
            props.append(margins)
            for index, grid in enumerate(table._tbl.tblGrid): grid.set(qn("w:w"), str(widths[index]))
            for i, row in enumerate(table.rows):
                trprops = row._tr.get_or_add_trPr(); avoid = OxmlElement("w:cantSplit"); trprops.append(avoid)
                if i == 0: trprops.append(OxmlElement("w:tblHeader"))
                for j, cell in enumerate(row.cells):
                    cell.width = Inches(widths[j]/1440)
                    cell._tc.get_or_add_tcPr().find(qn("w:tcW")).set(qn("w:w"), str(widths[j]))
                    p = cell.paragraphs[0]; p.paragraph_format.space_after = Pt(3)
                    p.paragraph_format.line_spacing = 1.10
                    add_runs(p, rows[i][j])
                    for run in p.runs: run.font.size = Pt(9)
                    if i == 0:
                        fill = OxmlElement("w:shd"); fill.set(qn("w:fill"), "E8EEF5"); cell._tc.get_or_add_tcPr().append(fill)
                        for run in p.runs: run.bold = True
            doc.add_paragraph().paragraph_format.space_after = Pt(0)
    doc.core_properties.title = "Cane360 User Manual"
    doc.core_properties.subject = "Complete user onboarding and module procedures"
    doc.core_properties.author = "Cane360"
    doc.core_properties.keywords = "Cane360, user manual, onboarding, farm operations"
    doc.save(HERE / "Cane360-User-Manual.docx")


def build_inventory():
    chapter = {
        "Users": "2", "Session": "2", "MyProfile": "12.2", "Health": "Support diagnostics",
        "FarmSetup": "4–5", "FarmPersonnel": "4.5–4.7", "FieldLineProfiles": "5.3",
        "CropCycles": "5.4–5.7", "CropVarieties": "5.4", "Activities": "6",
        "ActivityTypes": "6.1 / 12.6", "Workers": "7.2–7.5", "WorkerRates": "7.6",
        "Attendance": "7.7", "WorkRecords": "7.8–7.10", "Payroll": "8",
        "Inventory": "9", "InventoryCategories": "9.2", "InputControls": "9 / 12.3",
        "Finance": "10", "MillRecords": "11", "Administration": "12",
    }
    rows = []
    for path in sorted((ROOT / "src/Web/Controllers").glob("*Controller.cs")):
        module = path.stem.removesuffix("Controller")
        if module not in chapter:
            raise ValueError(f"New undocumented controller: {module}")
        source = path.read_text()
        base = re.search(r'\[Route\("([^"]+)"\)\]', source)[1].replace("[controller]", module)
        matches = list(re.finditer(r'\[Http(Get|Post|Put|Delete|Patch)(?:\((.*?)\))?\]', source, re.S))
        for i, match in enumerate(matches):
            args = match[2] or ""
            route_match = re.match(r'"([^"]*)"', args)
            route = route_match[1] if route_match else ""
            tail = source[match.end():matches[i+1].start() if i+1 < len(matches) else len(source)]
            summary = re.search(r'\[EndpointSummary\("([^"]+)"\)\]', tail)
            method = re.search(r'public\s+(?:async\s+)?[^\n]+?\s+(\w+)\s*\(', tail)
            if not method:
                raise ValueError(f"Unparsed endpoint in {module}: {args}")
            rows.append([module, match[1].upper(), base + ("/" + route if route else ""), summary[1] if summary else method[1], chapter[module]])
    with (HERE / "functionality-inventory.csv").open("w", newline="") as file:
        writer = csv.writer(file, lineterminator="\n"); writer.writerow(["Module", "Method", "Endpoint", "Capability", "Manual chapter"]); writer.writerows(rows)
    with (HERE / "Coverage-and-maintenance.md").open("w") as file:
        file.write("# Cane360 manual coverage and maintenance\n\n")
        file.write(f"Edition 1.0 maps {len(rows)} controller endpoints across {len(chapter)} controllers to user-manual chapters. The inventory is generated from the checked-in controller attributes, rather than a planned product model.\n\n")
        file.write("Read `functionality-inventory.csv` for the full endpoint list. Server operations with incomplete screen controls are documented in the corresponding module and section 16.2. Health live/ready checks are support diagnostics, not ordinary farm actions. Account/session endpoints support the login and membership checks described in chapter 2.\n\n")
        file.write("## Source of truth\n\nEdit `Cane360-User-Manual.md`; then run `build_manual.py` with the Documents skill's bundled Python. The builder regenerates the DOCX, standalone HTML, in-app HTML and endpoint coverage inventory. Render the DOCX with `render_docx.py --emit_pdf`. Run the builder again with `--toc-pdf /path/to/rendered/Cane360-User-Manual.pdf` to populate the contents page numbers, and render again. Confirm those page numbers still match and inspect every rendered page. Copy the verified PDF to both `docs/user-manual/Cane360-User-Manual.pdf` and `src/Web/ClientApp/public/help/Cane360-User-Manual.pdf`, then rebuild the client.\n\nThe integrated Help route is `/help`; the standalone public guide is `/help/index.html`. Keep both HTML copies identical by using the builder.\n\n")
        file.write("## Screen coverage\n\n")
        for name, section in [("Login / Registration / Activation", "2"),("Dashboard / navigation / common controls", "3"),("Farm / owner / Farm Models / Personnel", "4"),("Fields / line profiles / crop logbook / plan and yield editors", "5"),("Activities / types / actual work / transitions / references / calendar / diary", "6"),("Labour / employee profile / protected IDs / rates / attendance / work evidence", "7"),("Payroll / preflight / advances / schedules / calculations / approval / settlement / print", "8"),("Inventory / stock / receipt / catalogue / input workflow / counts / adjustments", "9"),("Finance / transactions / allocation / costs / budgets / variance", "10"),("Mill records / mills / tickets / statements / uploads / matching / corrections / export", "11"),("Administration / profile / users / roles / references / settings / audit", "12"),("Reports placeholder and available outputs", "13"),("Operating checklists / troubleshooting / glossary / screen gaps", "14–16")]:
            file.write(f"- {name}: chapter {section}.\n")
        file.write("\n## Updating after a release\n\nCompare routes, navigation, forms, allowed transitions, permissions, validators and source calculations. Update procedures and screen limitations; do not invent buttons for service-only capabilities. Add any new controller to the builder mapping before regeneration. Recheck every changed label and lifecycle boundary. Keep personal data and credentials out of examples.\n\n")
        file.write("## Verification scope\n\nThe manual is based on local source review. Help-page checks verify reading, search, links and print layout. No operational records, approvals, migrations or shared-database mutations are needed to build or verify this guide. Deployment-specific roles, settings and account provisioning still need confirmation by the trainer.\n")
    print(f"Coverage: {len(rows)} endpoints; {len(chapter)} controllers; 16 user chapters")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(); parser.add_argument("--html-only", action="store_true")
    parser.add_argument("--toc-pdf", type=Path)
    args = parser.parse_args()
    content = list(blocks(SOURCE.read_text()))
    assert sum(b[0] == "heading" and b[1] == 2 for b in content) == 16
    build_html(content)
    build_inventory()
    if not args.html_only: build_docx(content, args.toc_pdf)
    print("Built manual artifacts from the reviewed Markdown source")
