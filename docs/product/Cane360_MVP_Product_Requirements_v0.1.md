# Cane360 MVP Product Requirements Specification

> **Implementation reference.** Generated from the approved `Cane360_MVP_Product_Requirements_v0.1.docx` baseline. The DOCX remains the approved publication artifact; this Markdown version preserves its requirements, rules, acceptance criteria, and decision registers in a searchable format.

A field-record, input-control, and operational-payroll system for Zimbabwean sugarcane growers

**Version:** 0.1 — Baseline specification

**Market:** Zimbabwe; pilot across Hippo Valley, Mkwasine, and Triangle

**Pilot:** 20 individual growers; one farm per grower

**Primary user:** Farm manager

**Scope:** Blueprint Modules 1–6, deliberately narrowed for MVP

**Prepared:** 11 August 2026

**Status:** Ready for stakeholder review and pilot-manager validation

> **MVP promise.** Cane360 gives a grower reliable field, input, and payroll records—showing what happened, who performed the work, what inputs were consumed, what remains unaccounted for, and what each crop cycle is costing.

# Document purpose

This document converts the Cane360 blueprint and completed discovery decisions into a buildable, testable MVP baseline. It defines scope, roles, workflows, business rules, functional requirements, screens, data concepts, reporting, non-functional requirements, pilot measures, and acceptance gates.

It is intentionally not a full ERP specification. Items that do not directly support better crop records, reduced input leakage, or reliable operational payroll are deferred unless they are essential to connect those outcomes.

## How to use this specification

- Product and pilot stakeholders use Sections 1–6 to confirm the operating model and scope.

- Design uses Sections 7–10 to define navigation, screens, states, and error handling.

- Engineering uses Sections 8–14 as the functional and non-functional baseline.

- Quality assurance uses the requirement IDs and acceptance criteria in Section 9.

- Pilot interviews validate the assumptions and open items in Section 16 without reopening already confirmed decisions.

# 1. Product definition

## 1.1 Problem statement

Zimbabwean sugarcane growers in the target segment manage field activities, input movements, worker records, payroll, and mill documents through manual and disconnected processes. The result is incomplete crop history, weak accountability between store issue and field application, payroll records that are difficult to verify, and limited visibility of crop-cycle costs.

## 1.2 Product position

Cane360 is a responsive, online web application designed specifically for individual Zimbabwean sugarcane growers. The MVP is operated primarily by the farm manager and gives the grower oversight and approval authority. It connects farm and crop records to activities, input application, labour, payroll, basic operational expenses, and limited mill records.

## 1.3 MVP outcomes

| **Outcome**              | **What changes for the grower**                                                                     | **MVP evidence**                                                                          |
|--------------------------|-----------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------|
| Better crop records      | Each field has a current crop cycle and a dated, attributable activity history.                     | Active field coverage; timely activity capture; complete field diary.                     |
| Reduced input leakage    | Every controlled input is traced from request through field application, return, loss, and closure. | Unaccounted quantity; late confirmations; stock-count variance; exception trends.         |
| Reliable payroll records | Monthly pay is supported by attendance, verified work, advances, deductions, and recorded payment.  | Approved work support; payroll exception rate; preparation time; payment acknowledgement. |

## 1.4 Design principles

- Sugarcane-specific before generic: hectares, fields/blocks, plant cane, ratoons, line-based work, irrigation, and crop-cycle costing are first-class concepts.

- Evidence before totals: users must be able to trace a dashboard number back to its operational records.

- No silent edits: approved or posted operational records are corrected through an auditable correction, reversal, or authorised reopening.

- Simple enough for daily use: the farm manager should complete common entries with minimal accounting or technical knowledge.

- Responsive by default: field-facing forms must work well on issued smartphones, even though offline support and direct field-user accounts are deferred.

- Configuration where practice may vary: thresholds, tolerances, activity types, input units, rates, and approval assignment must not be hard-coded unnecessarily.

## 1.5 Non-goals

| **Deferred capability**                                                                  | **Reason for deferral**                                                                                              |
|------------------------------------------------------------------------------------------|----------------------------------------------------------------------------------------------------------------------|
| Native mobile application and offline synchronization                                    | Stable office connectivity is assumed for the pilot; the responsive web application is the fallback for field entry. |
| AI, IoT, satellite data, and predictive agronomy                                         | Insufficient structured history and no direct contribution to the three MVP outcomes.                                |
| Harvest planning, transport, logistics, and mill API integration                         | Blueprint Modules 7–9 are outside MVP; only manual weighbridge and statement records are retained.                   |
| Full double-entry accounting, tax filing, VAT, fiscalisation, AP/AR, bank reconciliation | The MVP requires operational costs and basic cash records, not an accounting package.                                |
| PAYE/NSSA calculation or statutory submission                                            | Operational payroll is the confirmed scope; statutory payroll requires separate policy and legal validation.         |
| Multi-farm growers, cooperatives, mills, financial institutions, or agronomist portals   | The pilot validates one grower–one farm tenancy and primary farm-manager use.                                        |
| Direct mobile-money or banking integrations                                              | The MVP records payments and references only.                                                                        |
| Fuel management, barcodes, complex procurement, equipment maintenance                    | Not critical to current pilot outcomes.                                                                              |

# 2. Confirmed operating model

| **Decision area**          | **Confirmed MVP rule**                                                                                                       |
|----------------------------|------------------------------------------------------------------------------------------------------------------------------|
| Customer and tenant        | One individual grower is the customer and owns one farm in the MVP.                                                          |
| Pilot                      | 20 identified growers across Hippo Valley, Mkwasine, and Triangle.                                                           |
| Primary operator           | One farm manager per farm.                                                                                                   |
| Operational representation | The manager captures records on behalf of named supervisors and storekeepers; the system must not imply that they logged in. |
| Store                      | One store per farm.                                                                                                          |
| Workers                    | A worker belongs to one farm and is identified by national ID.                                                               |
| Attendance                 | Present or absent; no clock-in/out in MVP.                                                                                   |
| Daily allocation           | A worker has one primary field allocation per day but may perform several activities in that field.                          |
| Piece work                 | Weeding is commonly paid by standardised line or by area. Supervisor verifies; manager confirms.                             |
| Payroll                    | Monthly operational payroll; grower approval is mandatory.                                                                   |
| Advances                   | Recovered over three months by default, with authorised adjustment support.                                                  |
| Payments                   | Cash and mobile money; payment execution remains outside Cane360.                                                            |
| Currency                   | USD only.                                                                                                                    |
| Input approval             | Grower and farm manager may authorise; exception thresholds define when grower approval is required.                         |
| Leakage control point      | Field-application confirmation is the primary control.                                                                       |
| Data entry timing          | Both field-time and retrospective office entry; actual event time and record-entry time are stored separately.               |
| Connectivity               | Online-first; stable office connectivity assumed; no offline guarantee.                                                      |
| Mill evidence              | Manual capture of weighbridge records and grower statements; consistent ticket reference expected.                           |

## 2.1 Data-entry accountability

Operational records distinguish the system user from the person who performed, issued, received, supervised, or verbally confirmed the underlying work. Where the manager captures a supervisor confirmation, the audit wording must read “Entered by \[manager\]; confirmation provided by \[supervisor\].”

## 2.2 Retrospective entry controls

| **Entry timing**                     | **Default treatment**                                                                 |
|--------------------------------------|---------------------------------------------------------------------------------------|
| Same day                             | Normal entry.                                                                         |
| 1–2 calendar days late               | Allowed; visibly flagged as retrospective.                                            |
| More than 2 days late                | Reason required.                                                                      |
| After payroll submission or approval | Underlying labour entry locked; authorised reopen or next-period correction required. |
| Closed crop cycle                    | New operational entry prohibited; authorised correction process only.                 |

## 2.3 Sensitive data treatment

- National ID is encrypted at rest and excluded from ordinary list views and exports unless explicitly authorised.

- Ordinary screens and payslips display a masked identifier; full value access is restricted and audited.

- Mobile-money phone numbers are treated as personal data and masked where full display is unnecessary.

- Uploaded documents inherit the farm tenant’s access restrictions and must not be exposed through guessable public URLs.

# 3. Users and permissions

## 3.1 MVP personas

| **Persona**            | **Primary goal**                                                                             | **MVP access**                                                                                             |
|------------------------|----------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------|
| Grower / Owner         | Trust the records, see exceptions and costs, approve exceptional inputs and monthly payroll. | Full farm visibility; approvals; reports; user administration; configuration oversight.                    |
| Farm Manager           | Capture and control daily field, labour, inventory, and payroll work.                        | Operational setup and entry; normal approvals; activity closure; payroll preparation and submission.       |
| Platform Administrator | Support the service without becoming a farm operator.                                        | Tenant setup, access support, reference configuration, operational monitoring, and audited support access. |

## 3.2 Named operational actors

Supervisors, storekeepers, payroll clerks, issuers, recipients, and workers exist as named farm personnel even when they do not have active logins. Their names and roles provide operational attribution. The system data model and responsive screens should permit restricted user accounts to be activated later without redesigning records.

## 3.3 Permission rules

| **Capability**                                         | **Grower** | **Farm manager**         | **Platform admin**    |
|--------------------------------------------------------|------------|--------------------------|-----------------------|
| View all farm data and reports                         | Yes        | Yes                      | Support-only, audited |
| Configure farm reference data                          | Yes        | Yes                      | No routine edit       |
| Approve normal planned input issue                     | Yes        | Yes                      | No                    |
| Approve issue above plan/tolerance                     | Yes        | Submit only              | No                    |
| Approve stock adjustment/write-off                     | Yes        | Submit only              | No                    |
| Capture field, activity, labour, and inventory records | Optional   | Yes                      | No                    |
| Prepare and submit payroll                             | View       | Yes                      | No                    |
| Approve payroll                                        | Yes        | No                       | No                    |
| Record payroll payment                                 | Yes        | Yes after approval       | No                    |
| Manage farm users                                      | Yes        | Limited invitation/reset | Support-only          |

## 3.4 Approval integrity

- A user cannot satisfy two required approval stages by changing role labels; the recorded approver identities and timestamps remain distinct.

- Because the manager is the sole operational system user in the pilot, supervisor verification is an attestation captured by the manager, not a digital approval by the supervisor.

- Grower approval actions require an authenticated grower session and cannot be delegated to the farm manager in MVP.

# 4. MVP module scope

## 4.1 Module 1 — Grower, farm, and fields

- Grower profile and contact details.

- One farm profile with address, location, tenure, declared area, irrigation context, and documents.

- Fields/blocks with unique code, status, declared hectares, mapped hectares, reporting-area choice, irrigation method, soil notes, and geometry when available.

- Map boundary drawing and KML/KMZ/GeoJSON import; mapping is optional and must not block registration.

- One farm manager assignment and named farm personnel register.

## 4.2 Module 2 — Crop lifecycle

- One active crop cycle per field at a time.

- Plant cane or ratoon, ratoon number, variety, start date, expected harvest window, expected yield, actual harvest date, and actual tonnes.

- Crop states: Draft, Active, Ready for harvest, Harvested, Closed, Cancelled.

- Historical cycle view and cycle-based cost accumulation.

- Harvest closure sufficient to calculate cost per tonne; detailed harvest scheduling remains deferred.

## 4.3 Module 3 — Activities and agronomy

- Configurable activity types including land preparation, planting, irrigation, fertilisation, weeding, chemical application, pest/disease control, inspection, fire incident, ratoon management, harvest preparation, and harvesting record.

- Planned and unplanned activities tied to a field and current crop cycle.

- Actual date, area/lines covered, supervisor, workers, input requirements, evidence, status, and cost roll-up.

- Digital field diary and chronological field history.

## 4.4 Module 4 — Labour and operational payroll

- Worker register: permanent, seasonal, casual, contract, and task-based categories.

- Present/absent attendance, one-field-per-worker-per-day allocation, daily-rated, monthly-rated, and piece-rated work.

- Weeding rates by hectare or standardised line; verified quantity drives pay.

- Overtime as a configurable addition where used, worker advances, deductions, payroll calculation, grower approval, payslips, cash registers, and mobile-money payment references.

- Operational payroll only; no PAYE/NSSA or statutory submission.

## 4.5 Module 5 — Inventory and leakage control

- One farm store; items, units, suppliers, purchases, receipts, issues, field receipt, application, returns, approved loss, adjustments, and stock counts.

- End-to-end traceability from request and approval to field application and closure.

- No negative stock; immutable transaction history; reversals/corrections rather than silent edits.

- Application-rate tolerance and unaccounted-quantity exception reports.

- Fuel excluded from the MVP reference catalogue by default.

## 4.6 Module 6 — Basic finance and mill records

- USD-only operational expenses and income records, with attachments and crop-cycle allocation where relevant.

- Automatic labour cost from approved payroll and input cost from confirmed field application.

- Budget versus actual, cost per hectare, cost per tonne, and crop-cycle cost composition.

- Manual weighbridge ticket capture and grower-statement upload/basic reconciliation.

- No general ledger, journal entry, bank reconciliation, accounts payable/receivable, tax filing, or statutory reports.

# 5. End-to-end workflows

## 5.1 Farm setup and crop-cycle activation

1.  Grower or manager completes the grower and farm profile.

2.  Manager creates fields with unique codes and declared areas; mapping may be completed immediately or later.

3.  Manager creates a crop cycle for a field, selects plant cane or ratoon, variety, dates, expected harvest window, and yield.

4.  The system validates that the field has no other active cycle.

5.  Manager activates the cycle. Operational activities, labour, inventory application, and costs may now be posted to it.

## 5.2 Planned field activity with inputs

1.  Manager creates an activity for the active crop cycle and records the planned date, area/lines, supervisor, and required inputs.

2.  The input request is checked against plan, application-rate guidance, available stock, and approval thresholds.

3.  Farm manager approves a normal request; grower approves an exception above the configured threshold or tolerance.

4.  Manager records store issue on behalf of the storekeeper, naming the issuer and field recipient.

5.  Manager records quantity received at field and any immediate discrepancy.

6.  After work, manager records applied quantity, returned quantity, approved loss, area covered, workers, and supervisor verification.

7.  Manager confirms the application. The system calculates unaccounted quantity and application-rate variance.

8.  The activity closes only when labour is confirmed and all input quantities are accounted for or an exception is authorised.

## 5.3 Input reconciliation control

> **Control formula.** Unaccounted quantity = issued quantity − applied quantity − returned quantity − approved loss. A non-zero balance keeps the transaction open and visible on the leakage-exception dashboard.

- Quantity applied cannot exceed quantity received at field.

- Quantity received cannot exceed issued quantity without an authorised correction.

- Applied cost posts to the crop cycle only after manager confirmation.

- Returned quantity increases usable store stock only when the return is recorded as received by the store.

- Approved loss posts to a separate loss/variance cost category and retains reason and approver.

## 5.4 Attendance, work verification, and payroll

1.  Manager records each worker as present or absent for the work date and, if present, selects one field allocation.

2.  For daily or monthly pay, the manager records activity participation; for piece work, the manager records lines or hectares completed and the agreed rate.

3.  Manager captures the named supervisor’s verification, then completes manager confirmation.

4.  At month end, Cane360 creates a draft payroll from approved attendance and work records, rate rules, advances, additions, and deductions.

5.  Manager resolves exceptions, reviews totals, and submits payroll to the grower.

6.  Grower approves or rejects with a reason. Any change after submission returns the payroll to Draft/Needs review and invalidates the prior approval.

7.  After approval, manager records cash or mobile-money payments and acknowledgements.

8.  When all required payments and exceptions are handled, the period is closed and underlying records are locked.

## 5.5 Retrospective office entry

1.  Manager selects the actual activity or transaction date, not the current date by default.

2.  Cane360 records the separate system timestamp automatically and flags the record as retrospective.

3.  For entries more than two calendar days late, the manager selects or enters a reason and captures the source-sheet reference.

4.  Optional source-sheet photo is attached.

5.  Normal validation and approval rules still apply; late entry never bypasses closed-period or stock controls.

# 6. Business rules

| **ID** | **Rule**                                                                                                                                                           |
|--------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| BR-001 | One active farm per grower tenant in MVP.                                                                                                                          |
| BR-002 | Field code is unique within the farm; weighbridge ticket reference is unique within the mill context.                                                              |
| BR-003 | A field may have many historical crop cycles but only one Active or Ready-for-harvest cycle at a time.                                                             |
| BR-004 | Operational labour, input application, and directly allocated expenses must reference an active crop cycle.                                                        |
| BR-005 | Declared area and mapped area are stored independently; the manager selects the reporting area and the system displays variance.                                   |
| BR-006 | A worker belongs to one farm and may have only one primary field allocation per work date.                                                                         |
| BR-007 | A worker may participate in multiple activities on that same field and date.                                                                                       |
| BR-008 | Duplicate attendance for the same worker and work date is blocked.                                                                                                 |
| BR-009 | Piece-work pay uses manager-confirmed quantity that has recorded supervisor verification.                                                                          |
| BR-010 | For standard line work: pay = verified completed lines × rate per line.                                                                                            |
| BR-011 | The same field line range or work section cannot be claimed twice for the same activity scope.                                                                     |
| BR-012 | An issue does not equal consumption; input cost posts to the crop cycle after application confirmation.                                                            |
| BR-013 | Stock cannot become negative. Transactions that would cause negative stock are blocked.                                                                            |
| BR-014 | Approved, issued, applied, paid, or closed records cannot be silently edited.                                                                                      |
| BR-015 | An activity cannot close while input quantities remain unaccounted for, unless the remaining quantity is handled through an approved loss or authorised exception. |
| BR-016 | Application confirmation more than 48 hours after the work date requires a reason and is flagged.                                                                  |
| BR-017 | An application without an approved issue is blocked, except for an authorised opening-balance or correction workflow.                                              |
| BR-018 | Normal issues within plan may be approved by the manager; above-plan/tolerance issues require grower approval.                                                     |
| BR-019 | Stock adjustments and damaged/expired write-offs require grower approval.                                                                                          |
| BR-020 | Payroll is monthly and requires grower approval before any payment is recorded.                                                                                    |
| BR-021 | Unapproved work, unresolved duplicate allocation, and unverified piece quantity cannot enter an approvable payroll.                                                |
| BR-022 | An advance defaults to three recovery instalments; approved payroll posting reduces the outstanding balance.                                                       |
| BR-023 | Cash payment records require acknowledgement status; mobile-money records require provider, recipient number, transaction reference, date, amount, and status.     |
| BR-024 | Closing payroll locks source attendance/work entries; corrections require reopening or next-period adjustment.                                                     |
| BR-025 | All monetary values in the MVP are recorded and reported in USD.                                                                                                   |
| BR-026 | National ID and mobile-money phone details are masked by default and full-value access is audited.                                                                 |
| BR-027 | System user, operational actor, event timestamp, entry timestamp, and approval timestamps are recorded separately.                                                 |
| BR-028 | Entries more than two days late require a reason; entries into a closed crop cycle are prohibited.                                                                 |
| BR-029 | Farm manager cannot approve payroll on behalf of the grower.                                                                                                       |
| BR-030 | Audit records are append-only from the application perspective and retained for the pilot duration plus the agreed retention period.                               |

# 7. Information architecture and screen inventory

## 7.1 Primary navigation

| **Navigation area** | **Primary screens**                                                                                                              |
|---------------------|----------------------------------------------------------------------------------------------------------------------------------|
| Dashboard           | Farm overview; action queue; input exceptions; payroll status; crop-cycle alerts; recent activity.                               |
| Farm & Fields       | Grower profile; farm profile; field list; field detail; map; personnel.                                                          |
| Crop Cycles         | Cycle list; create/edit; cycle overview; timeline; costs; harvest close.                                                         |
| Activities          | Calendar/list; create activity; activity detail; labour; input request/application; field diary.                                 |
| Labour & Payroll    | Workers; attendance; work records; rates; advances; payroll runs; payments; payslips.                                            |
| Inventory           | Stock overview; items; suppliers; receipts; requests; approvals; issues; applications; returns; counts; adjustments; exceptions. |
| Finance             | Operational transactions; budgets; crop-cycle costs; weighbridge records; grower statements.                                     |
| Reports             | Crop records; leakage; stock; labour; payroll; costs; weighbridge reconciliation; audit.                                         |
| Administration      | Users; roles; activity types; units; rate/tolerance rules; document categories; audit access.                                    |

## 7.2 Dashboard priorities

- Action queue: approvals, overdue field confirmations, unaccounted quantities, late records, and payroll exceptions.

- Crop status: active cycles, upcoming work, fields without current cycles, and late activities.

- Inventory control: items below threshold, open issues, unreturned quantities, recent adjustments, and stock-count variance.

- Payroll status: current period, missing attendance/work evidence, unverified piece work, submission/approval/payment status.

- Cost visibility: labour and confirmed input cost by crop cycle, cost per hectare, and cost per tonne when actual tonnes exist.

## 7.3 Responsive form behaviour

- Common field forms use one-column layouts on smartphones and preserve large touch targets.

- Draft saving is supported online; a visible connection failure must prevent users from assuming an unsaved record was submitted.

- Long multi-stage workflows are split into clear steps with a persistent summary of field, crop cycle, activity, and quantities.

- Approvals show the source request, plan variance, stock availability, cost, and prior approvals without requiring navigation away.

- Errors appear beside the affected field and in a short summary; entered data is preserved after validation failure.

# 8. Core data concepts

This section is a conceptual model for product and API design. A separate logical data model should define keys, constraints, history strategy, and physical schema before implementation.

| **Entity**              | **Purpose**                                                                                     | **Important relationships**                                             |
|-------------------------|-------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------|
| Tenant / Grower         | Commercial and security boundary for one grower in MVP.                                         | Farm; users; subscriptions/configuration.                               |
| Farm                    | Single operating farm owned/managed by the grower.                                              | Fields; store; personnel; workers; suppliers.                           |
| Field                   | Named sugarcane block with area and optional geometry.                                          | Crop cycles; standard line data; irrigation; documents.                 |
| Crop Cycle              | Plant cane or ratoon production period; central cost object.                                    | Activities; labour allocations; applications; expenses; harvest result. |
| Activity                | Planned and completed agronomic/operational work.                                               | Field/cycle; supervisor; workers; input request/application; evidence.  |
| Person / Farm Personnel | Named actor such as manager, supervisor, storekeeper, issuer, or recipient.                     | Role assignments; operational attestations; optional future user.       |
| Worker                  | Paid farm worker with masked national ID and employment type.                                   | Attendance; work records; rates; advances; payroll lines; payments.     |
| Attendance              | Present/absent record for worker and work date.                                                 | One field allocation; work records.                                     |
| Work Record             | Activity participation and quantity/rate evidence.                                              | Supervisor verification; manager confirmation; payroll line.            |
| Inventory Item          | Stock-managed input with unit, category, cost method, and tolerance data.                       | Receipts; lots/batches; issues; applications; returns; counts.          |
| Input Request           | Planned need tied to activity and crop cycle.                                                   | Approval; store issue.                                                  |
| Stock Movement          | Immutable receipt, issue, return, adjustment, write-off, or count correction.                   | Store; item; quantity; actor; reason; source document.                  |
| Field Application       | Reconciles issued, field-received, applied, returned, lost, and unaccounted quantities.         | Activity; application rate; verifications; crop-cycle cost.             |
| Payroll Period / Run    | Monthly operational payroll state and totals.                                                   | Payroll lines; approval; payments; payslips.                            |
| Advance                 | Worker loan/advance and planned recovery schedule.                                              | Instalments; payroll deductions; balance.                               |
| Expense / Income        | Simple USD cash/operational transaction.                                                        | Category; vendor/payee; crop-cycle allocation; attachment.              |
| Weighbridge Record      | Manual mill ticket record with unique reference and tonnes.                                     | Mill; date; field/cycle if known; statement match.                      |
| Grower Statement        | Uploaded statement and basic reconciliation metadata.                                           | Period; mill; ticket references; total tonnes/amount; variance.         |
| Audit Event             | Append-only record of create, change, state transition, approval, export, and sensitive access. | User; actor; tenant; timestamp; before/after reference; reason.         |

## 8.1 Required record metadata

- Tenant/farm identifier; record identifier; status; created by/at; last changed by/at.

- Actual event date/time where applicable, separate from system entry timestamp.

- Operational actor and role when the logged-in user records on another person’s behalf.

- Source-sheet or document reference and attachments where applicable.

- Approval identity, decision, timestamp, reason, and superseded approval reference.

- Correction/reversal link to the original record; records must remain traceable through the correction chain.

# 9. Functional requirements and acceptance criteria

Priority uses Must, Should, and Could. All Must requirements form the MVP release gate unless explicitly moved through change control.

| **Requirement** | **Priority** | **Capability**                                                                                               | **Acceptance criterion**                                                                                                                            |
|-----------------|--------------|--------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| FR-SET-001      | Must         | Create one grower tenant, one farm, and one farm-manager assignment.                                         | A newly provisioned tenant cannot create a second active farm; authorised support can correct setup errors with an audit trail.                     |
| FR-SET-002      | Must         | Create farm personnel records without user accounts.                                                         | Manager can select named supervisors/storekeepers on operational records; UI states that the manager entered the record.                            |
| FR-FLD-001      | Must         | Create, edit, activate, archive, and view fields with unique farm-level codes.                               | Duplicate active field code is rejected; archived field remains visible in history.                                                                 |
| FR-FLD-002      | Must         | Store declared area and mapped area separately and select reporting area.                                    | Variance is displayed; reports consistently use selected reporting area and identify its source.                                                    |
| FR-FLD-003      | Should       | Draw a field boundary and import KML, KMZ, or GeoJSON.                                                       | Valid geometry is previewed before save; invalid import returns actionable errors; field creation remains possible without geometry.                |
| FR-CYC-001      | Must         | Create and activate plant-cane and ratoon crop cycles.                                                       | Activation is blocked if the field already has an active/ready cycle or required data is missing.                                                   |
| FR-CYC-002      | Must         | Record variety, start date, expected harvest window/yield, ratoon number, status, and actual harvest result. | Ratoon number is required for ratoon cycles; actual tonnes are required to calculate cost per tonne.                                                |
| FR-CYC-003      | Must         | Show a chronological field and crop-cycle history.                                                           | Activities, confirmed applications, labour, costs, harvest closure, and corrections are traceable from the cycle.                                   |
| FR-ACT-001      | Must         | Create planned or unplanned activities against an active crop cycle.                                         | Field/cycle, activity type, actual/planned date, and responsible supervisor are captured; closed cycles cannot accept new activities.               |
| FR-ACT-002      | Must         | Record actual area or standardised lines covered and completion status.                                      | Quantity basis matches the activity/rate configuration; invalid or excessive values trigger validation or exception.                                |
| FR-ACT-003      | Must         | Attach source-sheet references, documents, or photographs.                                                   | Attachment is linked to the farm tenant and visible only to authorised users; upload failure does not falsely mark the record complete.             |
| FR-LAB-001      | Must         | Register workers with masked national ID and employment type.                                                | Duplicate national ID within the farm is detected; list screens show masked value; full display requires authorised action and audit event.         |
| FR-LAB-002      | Must         | Capture present/absent attendance by work date.                                                              | Duplicate worker/date entry is blocked; present worker requires one field allocation; absent worker cannot have paid work.                          |
| FR-LAB-003      | Must         | Record daily/monthly and piece-rated work.                                                                   | Work record identifies activity, payment basis, rate, quantity if applicable, calculated amount, supervisor verification, and manager confirmation. |
| FR-LAB-004      | Must         | Prevent multiple field allocations for a worker on the same day.                                             | Second distinct field allocation is blocked; multiple activities in the original field remain allowed.                                              |
| FR-LAB-005      | Must         | Prevent duplicate line/section claims for the same activity scope.                                           | Overlapping recorded line ranges or repeated work-section reference triggers a blocking validation or manager-resolved exception.                   |
| FR-ADV-001      | Must         | Create worker advances with a default three-instalment schedule.                                             | Schedule totals equal advance amount; approved payroll deductions reduce balance; deferred instalment requires reason.                              |
| FR-PAY-001      | Must         | Generate monthly draft payroll from approved evidence.                                                       | Only valid attendance/work, rates, additions, and deductions are included; exception list identifies excluded or blocking records.                  |
| FR-PAY-002      | Must         | Submit payroll to the grower for approval.                                                                   | Manager cannot approve; post-submission changes invalidate approval and return payroll to review.                                                   |
| FR-PAY-003      | Must         | Record cash and mobile-money payments after approval.                                                        | Cash includes acknowledgement status; mobile money includes required provider, recipient, reference, date, amount, and status.                      |
| FR-PAY-004      | Must         | Produce employee payslips and a printable cash payment register.                                             | Totals reconcile to approved payroll; sensitive identifiers are masked; generation is repeatable from the approved version.                         |
| FR-PAY-005      | Must         | Close payroll and lock source records.                                                                       | Close is blocked by unresolved required payment/exception state; corrections require authorised reopening or later adjustment.                      |
| FR-INV-001      | Must         | Maintain one store, inventory items, units, suppliers, and opening balances.                                 | Stock on hand is reproducible from immutable movements; opening balance requires date, reason/source, and authorisation.                            |
| FR-INV-002      | Must         | Record purchase receipt with quantity, unit cost, supplier, date, reference, and optional batch/expiry.      | Receipt increases on-hand stock; duplicate source reference warning is shown; correction uses reversal/adjustment.                                  |
| FR-INV-003      | Must         | Create input requests tied to activity and crop cycle.                                                       | Request shows planned rate/quantity, stock availability, estimated cost, and approval level.                                                        |
| FR-INV-004      | Must         | Approve requests using configurable plan/tolerance thresholds.                                               | Manager may approve normal request; grower approval is required when threshold is exceeded; decision and reason are audited.                        |
| FR-INV-005      | Must         | Issue approved stock and name issuer and recipient.                                                          | Issue cannot exceed approved quantity or available stock; stock becomes lower only after issue is posted.                                           |
| FR-INV-006      | Must         | Record field receipt, application, return, and approved loss.                                                | System enforces quantity relationships and calculates unaccounted quantity and rate variance.                                                       |
| FR-INV-007      | Must         | Require supervisor verification and manager confirmation for field application.                              | The system records named supervisor attestation and authenticated manager confirmation separately.                                                  |
| FR-INV-008      | Must         | Prevent closing activity with unaccounted input quantity.                                                    | Close is blocked until applied + returned + approved loss equals issued or an authorised exception is recorded.                                     |
| FR-INV-009      | Must         | Perform stock count and authorised adjustment.                                                               | Variance is calculated; reason is required; grower approves adjustment/write-off; audit shows before and after.                                     |
| FR-INV-010      | Must         | Report leakage exceptions.                                                                                   | Report filters by farm date, field, activity, item, issuer/recipient, supervisor, status, and exception type; result traces to source record.       |
| FR-FIN-001      | Must         | Record USD income and operational expenses with category and attachment.                                     | Transaction preserves original amount/date/payee/source; optional field/crop-cycle allocation appears in cost report.                               |
| FR-FIN-002      | Must         | Accumulate confirmed labour, input, and direct expense costs by crop cycle.                                  | Cost totals reconcile to approved payroll, confirmed applications, and posted expenses without duplication.                                         |
| FR-FIN-003      | Must         | Calculate cost per hectare and, after harvest, cost per tonne.                                               | Formula displays the reporting area and actual tonnes used; missing denominator produces “Not available,” not zero.                                 |
| FR-MILL-001     | Should       | Capture weighbridge records with consistent ticket reference.                                                | Duplicate ticket within mill is blocked; tonnes and date are required; optional field/cycle association is supported.                               |
| FR-MILL-002     | Could        | Upload grower statement and record basic reconciliation totals.                                              | Statement period, mill, total tonnes/amount, and matching status are stored; detailed DOP logic remains out of scope.                               |
| FR-AUD-001      | Must         | Audit create, edit, transition, approval, reversal, export, login, and sensitive-data access.                | Authorised user can filter audit events; original and correction remain traceable; ordinary users cannot alter audit records.                       |
| FR-REP-001      | Must         | Provide operational dashboards and exportable reports.                                                       | Totals reconcile to source data; filters are visible on exported output; authorised CSV/PDF export is audited.                                      |

# 10. States and exception handling

## 10.1 Activity states

| **State**             | **Meaning**                                                           | **Allowed next states**                           |
|-----------------------|-----------------------------------------------------------------------|---------------------------------------------------|
| Draft                 | Incomplete activity being prepared.                                   | Planned; Cancelled                                |
| Planned               | Scheduled with required field/cycle and responsible person.           | In progress; Cancelled                            |
| In progress           | Work started or labour/input records being captured.                  | Awaiting verification; Cancelled by authorisation |
| Awaiting verification | Work recorded; supervisor/application checks incomplete.              | Manager confirmation; In progress                 |
| Manager confirmation  | Supervisor attestation present; manager reviews work and quantities.  | Completed; In progress                            |
| Completed             | Operational work confirmed; may still have close-blocking exceptions. | Closed; Correction required                       |
| Closed                | All quantities/costs accounted for and record locked.                 | Authorised correction only                        |
| Cancelled             | Work did not proceed; reason required.                                | No normal transition                              |

## 10.2 Inventory request/application states

Draft → Requested → Approved → Issued → Received at field → Applied → Verified → Closed. Rejected, Cancelled, Partially returned, Exception, and Corrected are explicit side states; the UI must never compress Issue and Application into a single action.

## 10.3 Payroll states

Open period → Draft calculation → Needs review → Submitted → Approved or Rejected → Payment in progress → Paid/Part-paid → Closed. Any material post-submission edit invalidates the approval version.

## 10.4 Blocking versus warning exceptions

| **Severity**        | **Examples**                                                                                                                                         | **System behaviour**                                                                     |
|---------------------|------------------------------------------------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------|
| Block               | Negative stock; duplicate attendance; multiple fields/day; application above received quantity; closed crop cycle; unverified piece work in payroll. | Action cannot complete. Error identifies correction required and preserves entered data. |
| Approval escalation | Issue above planned quantity/tolerance; stock adjustment; damaged/expired write-off; exceptional late correction.                                    | Record is saved pending grower approval; no downstream posting occurs until approved.    |
| Warning             | Late same-week entry; low stock; unusual but within tolerance rate; duplicate supplier reference suspicion.                                          | User may continue with acknowledgement; warning is logged and reportable.                |
| Information         | Mapped/declared area variance; missing optional geometry; cost per tonne unavailable before harvest.                                                 | Clear non-alarming status and explanation.                                               |

## 10.5 Core exception catalogue

- Issued but not received at field by expected date.

- Received quantity lower than issue quantity.

- Applied quantity lower than received, with no return or approved loss.

- Application-rate variance outside configured tolerance.

- Application confirmed more than 48 hours after actual work date.

- Manager confirmation captured without named supervisor verification.

- Frequent adjustments involving the same item or operational actor.

- Physical count variance above approval threshold.

- Payroll line without valid attendance, verified work, rate, or required payment detail.

# 11. Reports and analytics

| **Report**                  | **Purpose**                                                              | **Minimum filters / outputs**                                                                |
|-----------------------------|--------------------------------------------------------------------------|----------------------------------------------------------------------------------------------|
| Farm and field register     | Confirm setup coverage and current crop state.                           | Field status; hectares; mapping status; irrigation; active cycle; variety; ratoon.           |
| Crop-cycle field diary      | Provide complete operational history.                                    | Date; activity; supervisor; workers; inputs; area/lines; costs; evidence; late-entry flag.   |
| Input traceability          | Trace each input from receipt to field application.                      | Item; batch; receipt; request; approval; issue; recipient; application; return/loss; status. |
| Leakage exceptions          | Focus action on unaccounted or unusual input movement.                   | Unaccounted quantity/value; delay; rate variance; actors; field/activity; days open.         |
| Stock on hand and movement  | Manage availability and reconcile ledger to physical stock.              | Opening; receipts; issues; returns; adjustments; closing; unit cost; value.                  |
| Stock-count variance        | Compare system and physical quantity.                                    | Count date; item; expected; counted; variance; reason; approval.                             |
| Attendance and work support | Verify who worked, where, and on what.                                   | Worker; date; attendance; field; activities; quantity; verification; pay basis.              |
| Payroll register            | Review monthly gross, additions, deductions, advances, net, and payment. | Period; worker; pay basis; gross; additions; deductions; net; payment method/status.         |
| Advance balances            | Monitor planned and actual recoveries.                                   | Worker; original amount; instalments; recovered; deferred; outstanding.                      |
| Crop-cycle cost             | Understand labour, inputs, and other cost.                               | Field/cycle; cost category; cost/ha; actual tonnes; cost/tonne; source drill-down.           |
| Weighbridge register        | Retain mill ticket history and identify gaps/duplicates.                 | Mill; ticket; date; tonnes; field/cycle; statement match status.                             |
| Late and corrected records  | Measure manual-process transition risk.                                  | Event date; entry date; delay; reason; source sheet; correction chain; user.                 |
| Audit trail                 | Support investigation and trust.                                         | Record type/id; action; user; operational actor; timestamps; reason; approval/correction.    |

## 11.1 Formula definitions

| **Metric**              | **Definition**                                                                                  |
|-------------------------|-------------------------------------------------------------------------------------------------|
| Unaccounted quantity    | Issued − Applied − Returned − Approved loss.                                                    |
| Stock variance          | Physically counted quantity − system quantity at count cut-off.                                 |
| Application rate        | Applied quantity ÷ verified area or line basis, expressed in the configured unit.               |
| Labour cost per hectare | Approved labour cost allocated to cycle ÷ selected reporting hectares.                          |
| Total crop-cycle cost   | Approved payroll labour + confirmed input application cost + posted direct expenses.            |
| Cost per hectare        | Total crop-cycle cost ÷ selected reporting hectares.                                            |
| Cost per tonne          | Total crop-cycle cost ÷ actual harvested tonnes; unavailable before actual tonnes are recorded. |
| Piece-work pay by line  | Manager-confirmed lines × agreed USD rate per standard line.                                    |
| Activity capture delay  | Entry timestamp/date − actual activity date.                                                    |

# 12. Non-functional requirements

| **ID**  | **Area**            | **Requirement**                                                                                                                                                                   |
|---------|---------------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| NFR-001 | Availability        | Pilot target: 99.5% monthly service availability excluding notified maintenance.                                                                                                  |
| NFR-002 | Performance         | For normal pilot load, 95th-percentile authenticated page/API response under 2 seconds for common lists and under 4 seconds for complex reports, excluding file transfer.         |
| NFR-003 | Responsive UX       | All core capture, approval, and review screens work from 360-pixel smartphone width through desktop without horizontal form scrolling.                                            |
| NFR-004 | Accessibility       | Target WCAG 2.2 AA for keyboard access, focus visibility, labels, error association, contrast, and non-colour status cues.                                                        |
| NFR-005 | Security            | TLS in transit; strong password/managed identity policy; role-based authorisation; tenant isolation enforced server-side; secure secret management.                               |
| NFR-006 | Privacy             | Sensitive identifiers encrypted at rest, masked by default, omitted from logs, and access audited.                                                                                |
| NFR-007 | Auditability        | Operational state changes, approvals, reversals, exports, and sensitive access are append-only and queryable.                                                                     |
| NFR-008 | Data integrity      | Database constraints and transactions enforce tenant ownership, unique keys, valid state transitions, quantity relationships, and non-negative stock.                             |
| NFR-009 | Backup and recovery | Automated daily backups and point-in-time recovery for the production database; recovery procedure tested before pilot go-live.                                                   |
| NFR-010 | Observability       | Structured logs, request tracing, error monitoring, job monitoring, and tenant-safe operational metrics.                                                                          |
| NFR-011 | Exportability       | Authorised users can export core registers in CSV and printable PDF; every export shows filters, generation time, and farm context.                                               |
| NFR-012 | Browser support     | Current and previous major versions of Chrome and Edge; Safari/Firefox smoke-tested for core workflows.                                                                           |
| NFR-013 | Data retention      | Retention and deletion policy is configurable; pilot records are not deleted automatically without grower agreement and support review.                                           |
| NFR-014 | Supportability      | Configuration, validation errors, audit context, and correlation identifiers support pilot troubleshooting without direct database manipulation.                                  |
| NFR-015 | Scalability         | Architecture supports growth beyond 20 farms through tenant-safe indexes, pagination, background report generation, and managed infrastructure without introducing microservices. |

## 12.1 Technology baseline

Responsive web frontend: React + TypeScript + Vite. Backend: ASP.NET Core 10 modular monolith with REST/OpenAPI. Database: managed PostgreSQL with PostGIS. File storage: managed object/blob storage. Hosting, identity, telemetry, and secrets should use managed cloud services. Next.js, microservices, Kubernetes, Kafka, GraphQL, and a separate data warehouse are not MVP dependencies.

## 12.2 Tenant isolation

- Every farm-owned record carries tenant/farm ownership enforced in the backend; frontend filtering is never treated as a security boundary.

- Identifiers from another tenant must return no usable data and must not reveal whether the record exists.

- Support access is time-bound or purpose-bound where possible and creates an audit event visible to authorised administration.

# 13. MVP release and pilot acceptance

## 13.1 Release gate

- All Must functional requirements pass end-to-end testing with tenant isolation and audit verification.

- The three golden paths pass on supported desktop and smartphone layouts: crop record, input application/reconciliation, and monthly payroll.

- No Severity 1 or Severity 2 defects remain open; agreed Severity 3 defects have workarounds and owners.

- Backup restore, incident escalation, and production support access are rehearsed before onboarding pilot farms.

- Pilot training material and numbered source-sheet process are available.

- At least three farm managers validate terminology, workflow sequence, and smartphone form usability with realistic examples.

## 13.2 Golden-path acceptance scenarios

| **Scenario**         | **Pass condition**                                                                                                                                                               |
|----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Field diary          | Manager creates field and active cycle, records an activity retrospectively with source sheet, allocates workers, confirms completion, and sees the full trace in cycle history. |
| Input accountability | Requested fertiliser is approved, issued, received, applied, partially returned, verified, and closed with zero unaccounted quantity; stock and crop cost reconcile.             |
| Leakage exception    | A one-unit discrepancy remains open, appears on dashboard/report, blocks closure, and resolves only through return or grower-approved loss/adjustment.                           |
| Piece-rate payroll   | Present worker completes verified standard lines in one field, calculated pay enters the monthly payroll, grower approves, and a cash/mobile-money payment is recorded.          |
| Advance recovery     | Worker advance creates three instalments; approved payroll posts one deduction and reduces outstanding balance without changing future instalments incorrectly.                  |
| Correction integrity | An approved/posted record cannot be overwritten; authorised correction preserves the original, reason, actor, timestamps, and corrected totals.                                  |

## 13.3 Pilot success measures

| **Measure**           | **Pilot interpretation**                                                                                        |
|-----------------------|-----------------------------------------------------------------------------------------------------------------|
| Active-field coverage | Percentage of active fields with a current crop cycle.                                                          |
| Activity timeliness   | Percentage of activities recorded within 48 hours; distribution of retrospective delay.                         |
| Input traceability    | Percentage of stock issues linked to field, crop cycle, activity, issuer, recipient, and confirmed application. |
| Unaccounted input     | Quantity and USD value open beyond 48 hours; age and resolution method.                                         |
| Stock accuracy        | Variance between system and physical stock count.                                                               |
| Adjustment pattern    | Number/value of adjustments and repeated actors/items.                                                          |
| Payroll support       | Percentage of payroll lines supported by valid attendance/work/verification.                                    |
| Payroll effort        | Manager time required to prepare and resolve monthly payroll.                                                   |
| Adoption              | Weekly active managers; farms active after 4, 8, and 12 weeks.                                                  |
| Trust signal          | Grower acceptance of payroll, inventory, and crop records in structured pilot interviews.                       |

## 13.4 Suggested pilot cadence

| **Period**            | **Focus**                                                                                                                                                   |
|-----------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Pre-pilot (2–3 weeks) | Interview 3–5 managers; validate terminology/source sheets; configure farms, fields, workers, items, rates, and opening balances; train grower and manager. |
| Weeks 1–2             | Daily onboarding support; measure capture delay, form errors, missing configuration, and input workflow completion.                                         |
| Weeks 3–4             | First controlled stock count and leakage review; refine reports and exception thresholds.                                                                   |
| Month end             | Run first complete operational payroll; observe preparation, grower approval, and payment-record workflow.                                                  |
| Weeks 5–8             | Stabilise usage, reduce support dependence, compare records to source sheets and physical counts.                                                           |
| Weeks 9–12            | Retention and trust review; decide whether to activate restricted supervisor/storekeeper accounts and define post-MVP priorities.                           |

# 14. Delivery backlog and sequencing

| **Increment**            | **Build focus**                                                                                               | **Exit condition**                                               |
|--------------------------|---------------------------------------------------------------------------------------------------------------|------------------------------------------------------------------|
| Foundation               | Tenant, authentication, permissions, audit, configuration, farm/personnel setup, file storage.                | Secure tenant provisioned; roles and audit verified.             |
| Farm core                | Fields, mapping, crop cycles, activity types, field diary, retrospective entry.                               | Complete crop-record golden path works.                          |
| Labour core              | Workers, attendance, allocation, work records, rates, verification.                                           | Valid labour evidence available for payroll.                     |
| Inventory control        | Items, receipts, requests, approvals, issues, field receipt, application, returns, loss, counts, exceptions.  | Input-accountability golden paths and stock reconciliation pass. |
| Payroll                  | Advances, monthly calculation, exceptions, submission, grower approval, payslips, payment records, closure.   | Full monthly-payroll golden path passes.                         |
| Finance and mill records | Operational expenses/income, crop-cycle costs, weighbridge records, statement upload.                         | Cost reports reconcile; manual mill evidence stored.             |
| Pilot hardening          | Responsive polish, performance, exports, backup/restore, monitoring, training, migration/configuration tools. | Release and pilot gates passed.                                  |

## 14.1 Explicitly excluded backlog

- Mobile/offline application, supervisor/storekeeper logins, and push notifications.

- Statutory payroll, EcoCash/bank integration, and automated tax submissions.

- Fuel management, barcode scanning, purchase orders, complex approval chains, and equipment maintenance.

- Full harvest scheduling, transport, mill APIs, division-of-proceeds calculations, and payment reconciliation.

- General ledger, invoicing, accounts payable/receivable, tax, and financial statements intended for statutory use.

- AI recommendations, yield prediction, satellite imagery, IoT, weather automation, cooperative/mill portals, and financing products.

## 14.2 Change-control test

> **Before adding an MVP feature, ask:** Does it materially improve crop-record completeness, field-application accountability, payroll reliability, or the evidence required to connect those outcomes? If not, it belongs in the post-MVP backlog unless it is necessary for security, data integrity, or pilot support.

# 15. Traceability to the three MVP outcomes

| **Outcome**              | **Primary modules** | **Key requirements**                           | **Primary reports**                                                                     |
|--------------------------|---------------------|------------------------------------------------|-----------------------------------------------------------------------------------------|
| Better crop records      | 1, 2, 3             | FR-FLD-001–003; FR-CYC-001–003; FR-ACT-001–003 | Farm/field register; crop-cycle diary; late/corrected records.                          |
| Reduced input leakage    | 3, 5, 6             | FR-INV-001–010; FR-AUD-001; FR-FIN-002–003     | Input traceability; leakage exceptions; stock movement/count variance; crop-cycle cost. |
| Reliable payroll records | 3, 4, 6             | FR-LAB-001–005; FR-ADV-001; FR-PAY-001–005     | Attendance/work support; payroll register; advance balances; labour/crop cost.          |

## 15.1 Blueprint alignment

The MVP implements the blueprint’s Modules 1–6 but narrows each to the confirmed pilot outcomes. Module 6 is an operational costing layer rather than a full accounting engine. Manual weighbridge and statement records are included only as supporting evidence; the operational scope of blueprint Modules 7–9 remains deferred.

## 15.2 Architecture alignment

The implementation remains a modular monolith with one PostgreSQL/PostGIS database and clearly separated domain modules. It is API-first and role-based, but it does not introduce a gateway, microservices, or offline synchronization for the pilot. This preserves a path to future mobile, logistics, mill integration, and analytics without making them MVP dependencies.

# 16. Assumptions and validation register

Confirmed decisions are not listed here. These items require validation with pilot managers or real documents, but they do not prevent design and engineering from starting with configurable defaults.

| **ID** | **Assumption / open item**                                                           | **Default for build**                                                                          | **Validation method**                                 |
|--------|--------------------------------------------------------------------------------------|------------------------------------------------------------------------------------------------|-------------------------------------------------------|
| VAL-01 | Exact farm/field terminology differs by estate or outgrower area.                    | Use “Farm” and “Field / Block”; allow labels in configuration later.                           | Interview managers across all three pilot areas.      |
| VAL-02 | Standard line length and line numbering are available per field.                     | Store standard length, estimated line count, and optional line ranges/work sections.           | Inspect field records and test piece-work entry.      |
| VAL-03 | Application-rate tolerances differ by item/activity.                                 | Per item/activity configurable tolerance; no universal hard-coded rate.                        | Review actual fertiliser/chemical practices.          |
| VAL-04 | Numbered paper source sheets are acceptable during manager-only rollout.             | Capture source-sheet reference and optional photo.                                             | Pilot workflow observation.                           |
| VAL-05 | One manager can enter field records often enough to meet the 48-hour target.         | Track entry delay and reasons from day one.                                                    | Weeks 1–4 pilot metrics.                              |
| VAL-06 | Supervisors and storekeepers can be uniquely named without login accounts.           | Personnel register with active/inactive status and operational role.                           | Pilot onboarding.                                     |
| VAL-07 | Worker national IDs are consistently available and suitable as duplicate indicators. | Require national ID with controlled exception for onboarding data correction.                  | Review worker registers and privacy expectations.     |
| VAL-08 | Daily/monthly/piece rate calculation covers pilot payroll.                           | Configure rate type per worker/work record; overtime is optional addition.                     | Observe one historical payroll and first live period. |
| VAL-09 | Three-month advance recovery is the normal rule but exceptions occur.                | Default three instalments; allow grower-approved schedule adjustment.                          | Payroll interviews.                                   |
| VAL-10 | Weighbridge ticket fields are sufficiently consistent across pilot areas.            | Use configurable mill reference plus ticket, date, gross/net tonnes, and optional field/cycle. | Obtain anonymised documents when available.           |
| VAL-11 | Grower statements can initially be stored with manual totals.                        | Upload original plus statement period, total tonnes, amount, and match status.                 | Obtain anonymised statements.                         |
| VAL-12 | USD-only is sufficient through the pilot.                                            | All amounts and reports in USD; no currency/exchange-rate model exposed.                       | Confirm before production onboarding.                 |
| VAL-13 | No statutory payroll output is expected from the pilot.                              | Payslip clearly identifies operational payroll and excludes tax certification claims.          | Confirm with growers and payroll users.               |
| VAL-14 | Stable office connectivity is reliable enough for online-first operation.            | No offline guarantee; preserve unsent form data only where safely possible in browser session. | Connectivity check per pilot farm.                    |

## 16.1 Documents still required

- Two or three anonymised weighbridge tickets from the pilot areas.

- Two or three anonymised grower statements covering realistic deduction and reference patterns.

- A representative monthly payroll register and worker advance record, with personal details removed.

- A representative store issue sheet, field application confirmation sheet, and stock count sheet.

- Representative field/block registers showing line conventions and crop-cycle terminology.

# 17. Definition of done for this specification phase

- Grower and farm-manager stakeholders approve the product promise, outcomes, scope, non-goals, roles, and decision table.

- At least three pilot managers review the three golden paths and identify terminology or sequence differences.

- Product, design, engineering, and QA agree on requirement IDs, state models, blocking rules, and acceptance gates.

- The logical data model and API boundary are derived from Section 8 and business rules without weakening auditability or tenant isolation.

- The screen wireframe plan covers every Must requirement and each error/exception state, not only happy-path dashboards.

- Any proposed scope change is assessed using the three-outcome change-control test and added to the backlog if it does not qualify.

## 17.1 Recommended immediate next artifacts

1.  Logical entity-relationship model with keys, constraints, status history, and audit strategy.

2.  Screen-by-screen UX specification and low-fidelity wireframes for the three golden paths.

3.  API contract outline grouped by the six domain modules.

4.  Pilot interview guide and validation worksheet mapped to VAL-01 through VAL-14.

5.  Prioritised delivery backlog with epics, user stories, estimates, dependencies, and release slices.

## Approval record

| **Role**                          | **Name** | **Decision** | **Date / notes** |
|-----------------------------------|----------|--------------|------------------|
| Product owner / founder           |          | Pending      |                  |
| Pilot grower representative       |          | Pending      |                  |
| Pilot farm-manager representative |          | Pending      |                  |
| Engineering lead                  |          | Pending      |                  |
| Design lead                       |          | Pending      |                  |

## Document notes

Source basis: SugarCane360 Product Blueprint and discovery decisions confirmed for the Zimbabwe MVP. This is a working product specification, not legal, tax, payroll-compliance, agronomic, or accounting advice. Regulatory or statutory functionality must be separately validated before being represented as compliant.
