# Manual verification — edition 1.0

Verified against the repository on 9 October 2026.

- PDF: 46 pages; all pages visually reviewed; contents page numbers checked against all 16 chapter starts.
- Content preservation: all 826 source headings, paragraphs, list items and table cells are present in the Word document.
- Coverage: 236 current controller endpoints across 22 controllers mapped in `functionality-inventory.csv`; UI procedures and service-only operations distinguished in the manual.
- Standalone and public HTML copies match; downloadable and documentation PDF copies match.
- Browser: contents anchors, chapter search, restoration of all 16 chapters before printing, embedded manual, public login Help link and successful PDF response verified. Desktop and 390-pixel mobile layouts reviewed; no mobile page overflow. Authenticated Help was checked with a synthetic mocked session, without accessing farm records.
- Frontend: `npm run lint`, `npm run typecheck`, `npm test` (135 tests) and `npm run build` passed. The build retains the existing Vite chunk-size warning.
- Backend: the focused UsersController, OpenApiCoverage and MyProfileController unit tests passed (14 tests).
- `git diff --check` passed. No dependencies, migrations or operational data changes were required.

`Help-page-preview.png` shows the integrated page with a fictional training account. This verification covers local source and local Help behavior; it does not assert deployment or exercise live payroll, stock posting or approvals.
