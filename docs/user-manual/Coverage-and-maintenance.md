# Cane360 manual coverage and maintenance

Edition 1.0 maps 236 controller endpoints across 22 controllers to user-manual chapters. The inventory is generated from the checked-in controller attributes, rather than a planned product model.

Read `functionality-inventory.csv` for the full endpoint list. Server operations with incomplete screen controls are documented in the corresponding module and section 16.2. Health live/ready checks are support diagnostics, not ordinary farm actions. Account/session endpoints support the login and membership checks described in chapter 2.

## Source of truth

Edit `Cane360-User-Manual.md`; then run `build_manual.py` with the Documents skill's bundled Python. The builder regenerates the DOCX, standalone HTML, in-app HTML and endpoint coverage inventory. Render the DOCX with `render_docx.py --emit_pdf`. Run the builder again with `--toc-pdf /path/to/rendered/Cane360-User-Manual.pdf` to populate the contents page numbers, and render again. Confirm those page numbers still match and inspect every rendered page. Copy the verified PDF to both `docs/user-manual/Cane360-User-Manual.pdf` and `src/Web/ClientApp/public/help/Cane360-User-Manual.pdf`, then rebuild the client.

The integrated Help route is `/help`; the standalone public guide is `/help/index.html`. Keep both HTML copies identical by using the builder.

## Screen coverage

- Login / Registration / Activation: chapter 2.
- Dashboard / navigation / common controls: chapter 3.
- Farm / owner / Farm Models / Personnel: chapter 4.
- Fields / line profiles / crop logbook / plan and yield editors: chapter 5.
- Activities / types / actual work / transitions / references / calendar / diary: chapter 6.
- Labour / employee profile / protected IDs / rates / attendance / work evidence: chapter 7.
- Payroll / preflight / advances / schedules / calculations / approval / settlement / print: chapter 8.
- Inventory / stock / receipt / catalogue / input workflow / counts / adjustments: chapter 9.
- Finance / transactions / allocation / costs / budgets / variance: chapter 10.
- Mill records / mills / tickets / statements / uploads / matching / corrections / export: chapter 11.
- Administration / profile / users / roles / references / settings / audit: chapter 12.
- Reports placeholder and available outputs: chapter 13.
- Operating checklists / troubleshooting / glossary / screen gaps: chapter 14–16.

## Updating after a release

Compare routes, navigation, forms, allowed transitions, permissions, validators and source calculations. Update procedures and screen limitations; do not invent buttons for service-only capabilities. Add any new controller to the builder mapping before regeneration. Recheck every changed label and lifecycle boundary. Keep personal data and credentials out of examples.

## Verification scope

The manual is based on local source review. Help-page checks verify reading, search, links and print layout. No operational records, approvals, migrations or shared-database mutations are needed to build or verify this guide. Deployment-specific roles, settings and account provisioning still need confirmation by the trainer.
