# Cane360 User Manual

Module-by-module onboarding and operating guide

Edition 1.0 • 9 October 2026

This guide describes the functionality implemented in the repository at this edition. Screen controls and server-supported functions are distinguished throughout. Your role, the record's status, and the farm's configuration determine which actions you can use. This guide contains fictional examples only.

## 1. Start here

### 1.1 What Cane360 does

Cane360 connects farm and field records to crop cycles, activities, employees, attendance, verified work, payroll, controlled inputs, operational costs, budgets and mill evidence. Records stay linked to the farm workspace. Approval, posting, verification and payment recording are separate steps; saving a draft does not complete any of them.

Use this manual in order for onboarding. For daily work, use the contents and search to jump to your module. Button names appear in bold. A **Result** explains what a successful procedure changes. A **Screen limitation** explains where a supported operation needs an authorized administrator or support operator because the current screen has no complete control for it.

### 1.2 Prepare before onboarding

- Obtain the correct Cane360 website address and your farm invitation or account provisioning instructions.
- Gather farm codes and areas, field details, crop varieties and planting dates, personnel names and roles, employee identity details, approved rates, opening stock evidence and supplier details.
- Agree who acts as Farm Owner, Farm Manager, supervisor and storekeeper. Some roles describe operational responsibility; they do not automatically give website access.
- Use your own login. The person who signs in is recorded separately from the person named as supervisor, issuer, recipient or paying person.
- Use the farm's operational dates. Many screens use Africa/Harare calendar dates; date-and-time displays may follow the browser's timezone. Confirm the intended work date before saving.

### 1.3 Three different people records

| Record | Purpose | Where to manage it |
| --- | --- | --- |
| Login account and membership | Signs in and determines access to a farm | Registration, activation, Administration |
| Operational person | Names the manager, supervisor, storekeeper or other responsible person in evidence | Farm → Personnel register |
| Employee / worker | Adds employment, protected national ID, rates, attendance, work and payroll | Labour → Employees |

A person can have both an operational record and an employee record. Register the existing person as an employee rather than creating a second person. A person record does not create a login. An employee record does not grant payroll approval rights.

### 1.4 Who can do what

| Role | Main use | Key boundaries |
| --- | --- | --- |
| Farm Owner (Grower) | Oversees the farm; reviews approvals, access and audit | Approves payroll and advances; handles owner-only corrections and decisions |
| Farm Manager | Runs operational records, prepares payroll and manages configuration | Cannot perform Grower-only approvals; payroll preparation and Grower approval are separate |
| Supervisor login | Views dashboard and fields; captures assigned activity work | Activities are limited to the linked supervisor; approval transitions and configuration are restricted; Administration shows My profile |
| Storekeeper operational role | Identifies the person issuing or receiving store stock | An operational role, not a self-service login invitation option |
| Platform Administrator | Audited support responsibilities | Does not imply authority for routine tenant operations |

Grower and Farm Manager navigation includes all main modules. Farm Manager Administration omits Users & Access and Audit. Supervisor navigation includes Dashboard, Fields and Crop Cycles, Activities and My profile. The server enforces permissions even if a button is visible. **Roles & Permissions** is a reference matrix, not an editor for custom roles.

### 1.5 Recommended setup order

1. Register or use your provided account; log in and activate membership if required.
2. Confirm the active farm and your role; complete My profile.
3. Review the Farm Owner and farm profile; add named operational personnel and a primary manager.
4. Add fields, choose reporting areas and set standard-line profiles where needed.
5. Add crop varieties, save crop-cycle drafts and activate the current crops.
6. Configure activity types, units, inventory categories, suppliers, items, lots and application-rate rules.
7. Register employees and effective-dated pay rates.
8. Record and post opening stock or purchase receipts with the required approval.
9. Invite the Farm Manager and supervisors to the appropriate operational person records.
10. Practise a full activity, work-evidence and input-accountability chain before the first payroll.

## 2. Registration, login, activation and logout

### 2.1 Create a login account

1. Open the Cane360 website and choose **Create an account** on the login screen.
2. Enter your **Email address**. This becomes your login name.
3. Enter a strong **Password**. The form checks for at least six characters; server password requirements can also reject a password. Use uppercase and lowercase letters, a number and a symbol to satisfy the standard Identity requirements.
4. Choose **Create account**. Successful registration returns you to **Log in**.
5. Log in using the email and password you entered.

**Result:** you have a login account. Registration alone does not create an activated farm membership. Registration deliberately does not disclose whether an email already exists; returning to login is not proof that a duplicate account was created.

### 2.2 Log in

1. Enter **Email address** and **Password** on **Log in**.
2. Choose **Log in** and wait for the workspace to open.
3. If you have farm membership, the system opens the requested protected page or Dashboard. If you do not, it opens **Activate your account**.
4. Check the displayed role and farm before entering operational records.

If login fails, check both fields, Caps Lock and the correct website address. Repeated failed attempts can lock the account temporarily. There is no visible forgot-password, password-change, email-change or two-factor-management workflow in this client; request the approved account-recovery process from your system administrator.

### 2.3 Activate an invited account

1. Ask the Grower for a current invitation token for your named person and intended login role.
2. Register and log in with your own account.
3. On **Activate your account**, paste the token into **Invitation token**.
4. Choose **Activate**.
5. Confirm Dashboard opens and the role is correct.

Tokens are displayed once when created. Expired, revoked or already redeemed invitations cannot be reused. Ask the Grower for a new invitation if activation fails. Do not publish the token or include it in support screenshots.

**First Farm Owner onboarding:** the current protected routes send an account without membership to activation, and the activation screen only redeems invitations. It has no “create my first farm” choice. A new first Grower therefore needs authorized provisioning assistance. Do not try to activate yourself as a manager to work around this. Once your working access is provisioned, use the Farm setup controls described in chapter 4 when they are available.

### 2.4 Session expiry and logout

If the system returns you to login, sign in again before continuing. Reopen the record and verify whether the last submission succeeded before submitting it again.

1. Save any work you intend to retain.
2. Choose the **Log out** icon in the sidebar footer, or the header on a narrow screen.
3. Confirm the login screen appears.

Closing a tab is not a substitute for logging out of a shared computer.

## 3. Navigation, common controls and Dashboard

### 3.1 Move around the system

The desktop sidebar opens Dashboard, Farm, Fields and Crop Cycles, Activities, Labour and Payroll, Inventory, Finance, Reports and Administration according to your role. **Labour and Payroll** opens payroll runs; choose **Manage workers & rates** there to open employees, attendance and work evidence. Labour also has **Open payroll runs** to return.

Use **Collapse sidebar** or **Expand sidebar** to change the sidebar width. On a narrow screen, the first permitted modules appear in the bottom navigation; **More** opens the remaining modules. The account or role link opens **My profile**. The theme control switches the light/dark presentation.

**Help** opens this manual. Its contents links and search help you find a procedure; **Print manual** opens the browser print dialog. Choose a printer or Save as PDF. In the printable document, use the module headings and contents.

### 3.2 Forms, dates, filters and dialogs

- Complete required fields. Select dates using the date picker or the supported input format; use actual event dates rather than today's date for historical work.
- Read the unit beside every quantity: hectares, whole standard lines, tonnes, inventory stock units or USD.
- **Save draft**, **Create**, **Record**, **Submit**, **Approve**, **Post** and **Confirm** have different effects. Check the status afterward.
- **Cancel**, a close icon or Escape can close a form without saving. Save before switching dates, tabs or pages if you want to retain your edits.
- Filters affect what is displayed; an empty filtered list does not prove that the farm has no records. Reset filters and check pagination.
- Expand rows or **View trace** sections to see source details. Previous/Next controls move through records; diary Newer/Older controls move through history.
- When another user changes a record, a version-conflict message may appear. Refresh, review the latest record and retry the intended action.
- A blocked action usually needs prerequisite evidence, permission or a valid lifecycle state. Read the displayed reason before changing anything.

### 3.3 Understand Dashboard

Dashboard shows the persisted active farm, farm code, location, declared hectares, number of fields, reporting hectares, current crops and total expected yield. Expected yield is a planning estimate across current cycles, not mill-delivered tonnage or income. The field list shows each field's current crop context.

For an unconfigured accessible workspace, **Create farm** starts setup. For a configured farm, **Manage fields** or **View fields** opens the field register. The dashboard does not currently provide a complete task, approvals, cashflow or weather dashboard. A sidebar “Farm setup pending” label can remain static; use the Farm record and setup progress as the working setup evidence.

## 4. Farm, Farm Owner and operational personnel

### 4.1 Create the farm when setup is available

1. Open **Farm** and choose the farm setup form if the workspace is not configured.
2. Enter the Farm Owner display name. Optional identity details include title, first name, surname, sex, Grower number / ID and national ID.
3. Enter association and membership number where applicable, primary contact number, email and registered address.
4. Add an existing photograph reference if needed and select owner status. This is a reference field; it does not upload a photograph.
5. Enter **Farm code**, **Farm name**, **Location**, **Tenure**, **Physical address**, **Declared farm area (ha)** and **Irrigation context**.
6. Choose **Create farm**.
7. Review the saved farm, Farm Owner and setup progress, then choose **Add a field**.

Farm codes start with a letter or number and use letters, numbers, underscores or hyphens. Farm and field areas must be positive. Tenure choices include Owned, Leasehold, Outgrower agreement, Communal land and Other.

**Result:** farm creation establishes the Farm Owner workspace, active farm, membership and default store together. The client uses one active farm context; it has no general multi-farm switcher.

### 4.2 Edit the farm and owner profile

1. Open **Farm** and choose the edit action on the farm summary.
2. Update the identity, association, contact, profile and farm details as needed.
3. Select a **Farm Model** category or leave it **Not assigned**.
4. Choose **Save changes** and review the saved summary.

When editing, leave the protected national-ID input blank to retain the existing ID. Owner status is a profile attribute; use Administration access controls when changing a user's permission to sign in to the farm.

### 4.3 Manage Farm Model categories

1. In **Edit farm information**, expand **Manage Farm Model categories**.
2. Enter **New category code** and **New category name**; choose **Add category**.
3. Select the category for the farm.
4. Use **Deactivate** or **Activate** beside an existing category to change availability.
5. Save the farm changes if you changed the selected category.

An inactive category already assigned to the farm remains visible with an inactive marker. These categories classify the farm; they are separate from inventory and document categories.

### 4.4 Reveal the Farm Owner national ID

Only the Grower can use this screen action. In **Farm Owner profile**, choose **Reveal National ID**, inspect it privately, then choose **Hide National ID**. The ordinary profile shows a protected mask. Revealing is an audited action; hide it before taking screenshots or leaving the screen.

### 4.5 Add an operational person

1. Open **Farm → Personnel register → Add person**.
2. Enter **Display name**, optional **Phone** and **Active from**.
3. Choose **Supervisor**, **Farm manager** or **Storekeeper** as the operational role.
4. For a Farm Manager, select **Primary manager** if that person should hold the primary assignment.
5. Choose **Add person**.

**Result:** the named person becomes available for the applicable operational evidence. This does not create a login or employee record. The Farm Owner is not automatically assumed to be the primary manager.

### 4.6 Edit a person's role or contact

1. Choose the pencil action beside an active person.
2. Update the display name, phone, operational role and primary-manager selection.
3. Set **Role effective from**. Current roles end on the preceding day; the form supplies the earliest allowed date.
4. Choose **Save changes** and check the role shown in the register.

Effective dates preserve historical responsibilities. A person must hold the required role on the event date, not merely today. Changing operational responsibility is separate from changing login access.

### 4.7 Link personnel to employment

Use **Register as employee** to add an employee record for an active person, or **View employee** to open an existing employee. **Open Employees** opens the full register. The employee screen provides **View personnel record and roles** to return to the same person.

**Screen limitation:** deactivating a person and ending a specific operational role assignment are supported server operations without dedicated buttons in this register. Give the authorized operator the person's reference, intended effective end date and reason for the change. They must preserve history and confirm the updated register; do not create a duplicate person to replace the old one.

## 5. Fields and Crop Cycles

### 5.1 Add a field

1. Open **Fields and Crop Cycles → Add field**. The farm must exist first.
2. Enter **Field code** and **Field name**. The code must be unique within the farm.
3. Enter **Declared area (ha)** and, if known, **Mapped area (ha)**.
4. Choose **Reporting area source**: Declared area or Mapped area. A mapped area is required when Mapped is selected.
5. Enter **Irrigation method** and optional **Soil notes**.
6. Choose **Add field** and review the field record.

**Result:** a physical field exists. Its crop is set up separately. Reporting hectares come from the selected reporting source and feed operational and cost calculations. Mapped area is a numeric field here; the current screen does not draw or edit GIS boundaries.

### 5.2 Edit field details

Choose **Edit field details**, update the field name, irrigation method or soil notes, then choose **Save field details**. This form does not edit the field code, declared area, mapped area or reporting-area source. Refer area corrections to authorized support rather than changing another field to compensate.

### 5.3 Set or replace the standard-line profile

1. On the field, choose **Set line profile** or **Replace profile**.
2. Enter **Standard line length (m)**, **Estimated whole-line count**, **Numbering scheme** and **Effective from**.
3. Choose **Save line profile**.
4. Confirm the field shows the intended count and standard length.

Example: 120 lines, each 100 metres, numbered north to south. Use whole line numbers for standard-line evidence. Replacement creates new effective context; use the profile applicable on the work date. Without context, line-based activity records can show **context unavailable**.

### 5.4 Create a crop-cycle draft and variety

1. On a field without a current crop, choose **Set up first crop**. With a current crop, choose **Plan next crop**.
2. Select **Plant cane** or **Ratoon**. For ratoon, enter the ratoon number, from 1 to 20.
3. Select an existing variety, or enable **Add a new variety** and enter its code and name.
4. Enter **Planting / cycle start date**.
5. Leave **Use calculated maturity** selected to use the configured maturity calculation shown on screen. Alternatively, enter **Expected harvest from** and **Expected harvest to**.
6. Enter **Expected yield (tonnes)**.
7. Choose **Save draft**. The crop-cycle logbook opens.

**Result:** a draft plan is saved. It is not the current crop and accepts no operational entries until activated. Do not create another variety after a partially successful save without first checking the existing variety list.

### 5.5 Review, edit and activate a draft

1. Choose **Review and activate** or **Review draft** on the field, or open the draft from the crop register.
2. Inspect variety, crop type, dates, maturity, expected harvest window and expected yield.
3. If needed, choose **Edit crop plan**, update dates or expected yield and **Save changes**.
4. Choose **Activate cycle** and confirm.
5. Verify the field now shows this cycle as current.

Only one current crop is permitted for a field. A next draft can be planned while the current crop exists, but activation must meet the displayed lifecycle rules. The draft edit form does not change crop type, ratoon number or variety.

### 5.6 Move a crop from planting through closure

| State | Meaning | Next normal action |
| --- | --- | --- |
| Draft | Saved plan; no operational entries | Activate, edit plan, or cancel draft |
| Active | Current growing crop | Record work; mark ready for harvest |
| Ready for harvest | Current crop ready to harvest | Record harvest result |
| Harvested | Harvest date and actual yield saved | Review or edit actual yield; close cycle |
| Closed | Historical read-only crop | View history and linked sources |
| Cancelled | Cancelled draft retained in history | View read-only history |

1. From Active, choose **Mark ready** and confirm when the crop is ready.
2. Choose **Record harvest**, enter the actual harvest date and actual tonnes, then confirm.
3. Review the result. If the cycle is still Harvested, **Edit actual yield** allows a manual tonnes correction.
4. Finish operational reconciliation and review linked evidence before closing.
5. Choose **Close cycle** and confirm. Resolve any displayed blockers first.
6. Review the historical cycle and activate the next eligible draft when ready.

Actual field yield is entered manually. Mill tickets and matched statement tonnage remain separate evidence; they do not automatically overwrite the crop's actual yield. Closed cycles reject further operational entries and have no reopen screen. **Cancel draft** requires a reason and makes the draft read-only; there is no undo action.

### 5.7 Find crop history and related records

The crop register groups current cycles, drafts and history; use its filter and open the relevant cycle. **View current cycle** opens the current logbook. The overview includes crop age, lifecycle status, expected and actual yields, available actions, blocked-action reasons and chronological history with event and recorded dates.

Cycle-linked cards open **Activities**, **Payroll and labour**, **Inputs** and **Costs**. Activities, Inputs and Costs pass crop context to the destination. Payroll opens the general payroll workspace, so select the correct period and worker sources there.

Supervisors can view fields and crop-register information but cannot manage field setup or crop lifecycles. Some read-only views do not expose every detail link available to managers.

## 6. Activities: the field diary

### 6.1 Configure activity types

1. Open **Activities → Activity types**, or **Administration → Activity Types**.
2. Enter **Code** and **Name**.
3. Choose coverage basis: **No quantity / None**, **Hectares** or **Standard lines**.
4. Enable Planned, Unplanned or both; at least one must be enabled.
5. Choose **Add activity type**.

Use Administration to rename an active activity type or archive it. Archiving preserves historical use and removes it from new active selections. Quantity basis is central to work evidence and application rules; the rename form only changes the name.

### 6.2 Record planned or unplanned activity

1. Open **Activities → Record activity**.
2. Choose **Work kind**, Planned or Unplanned.
3. Select a field with an Active or Ready-for-harvest current crop.
4. Select an active type that supports the chosen work kind.
5. Select the responsible supervisor, whose operational role must be effective for the work.
6. For planned work, enter the planned date.
7. Choose **Record activity**.

The activity is linked to the selected field's current crop. Unplanned work requires actual details before it can leave Draft. Supervisor logins may capture only work assigned to their linked supervisor person; selecting another person does not grant authority.

### 6.3 Capture actual work and late entries

1. Open an activity row or calendar entry.
2. In **Actual work**, enter **When work happened**.
3. Enter actual hectares or whole standard lines if the type uses a quantity. No-quantity activities have no quantity input.
4. If the entry is more than two calendar days late, provide a clear **Late-entry reason**.
5. Choose **Save actual work** and inspect the displayed coverage and retrospective flag.

Actual work can be entered while the activity is Draft, Planned or In progress. Use the real work time. The diary records both when work happened and when it was entered. A late-entry reason should explain the delay, not replace source evidence.

### 6.4 Follow the activity verification chain

The normal chain is Draft → Planned → In progress → Awaiting verification → Manager confirmation → Completed → Closed. Use the available **Next action** buttons in order. The button **Supervisor verified** advances the record into Manager confirmation and records the named operational supervisor fact. The signed-in authorized user remains recorded as the person entering it.

1. Move a valid draft to **Planned**.
2. Move the activity to **In progress** as work begins and capture actual details.
3. Move it to **Awaiting verification** when actual work is ready for review.
4. Record **Supervisor verified** after the responsible supervisor verifies the work.
5. Complete manager confirmation by advancing to **Completed** after review.
6. Confirm labour evidence and resolve input exceptions, then advance to **Closed**.

Only actions allowed by the server are available. Supervisor login capture does not include approval transitions. Closing can be blocked by incomplete labour evidence or inventory accountability exceptions. Resolve the source problem rather than cancelling completed work to hide it.

If actual work needs correction while Awaiting verification or Manager confirmation, an authorized user can return the activity to **In progress** using the available action. Update the actual work, then repeat verification and manager review. Completed activities only advance to Closed; they do not have this return path.

To cancel an eligible activity, choose **Cancelled** and enter the cancellation reason when prompted. Cancellation retains the record and its history; there is no general undo or delete workflow.

### 6.5 Add source references and input requests

For a nonterminal activity, enter **Source-sheet reference** and **Captured date** in **Source reference**, then choose **Add reference**. This stores metadata only; the activity diary does not upload documents or photographs.

For Grower and Farm Manager accounts, the activity's **Inputs** panel lists linked controlled-input requests. Choose **Request input**, select an inventory item and requested quantity, then **Create and submit**. The current panel submits a one-item request directly; chapter 9 explains approval and accountability. A rule must exist for the item/activity combination. Completed, Closed and Cancelled activities do not accept new requests in this panel. Supervisor login activity capture does not grant access to input-control operations.

### 6.6 Use list, calendar, filters and diary history

Filter by field, activity type and status. A link from a crop logbook also filters by crop cycle; **Show all activities** clears that linked context. Choosing another field clears the cycle filter. **List** displays rows; **Calendar** displays a month grid on desktop and an agenda on narrow screens. Use previous/next month arrows and inspect unscheduled drafts separately.

Open an activity to view status, field, supervisor, coverage, retrospective details, input requests and its **Chronological diary**. The diary is newest first and displays six events per page; use **Newer** and **Older**. It distinguishes the authenticated entering user from the named operational actor.

**Screen limitation:** the activity page currently loads the first 100 activities and applies screen filters to that loaded set. There is no activity-register pagination control. An authorized operator can retrieve further pages through the supported service; an empty screen filter may miss older records.

## 7. Labour: employees, attendance and work evidence

### 7.1 Open labour records

Open **Labour and Payroll**, then **Manage workers & rates**, or use **Farm → Open Employees**. Labour has **Employees**, **Attendance** and **Work evidence** tabs. Work date controls appear for Attendance and Work evidence. Save before changing dates.

### 7.2 Create an employee

1. Choose **Create Employee**, or **Register as employee** beside an existing person in Farm.
2. Select the existing operational person when applicable. Otherwise enter the new person's first name and surname.
3. Enter optional phone details where shown and choose employee type: Permanent, Seasonal, Casual, Contract or Task based.
4. Enter **Employment date**. For an existing person it cannot precede that person's active date.
5. Enter **National ID** in the protected input.
6. Choose **Create Employee**.
7. Review the employee detail and add rates before recording payable work.

National IDs are protected and checked for duplicates within the farm. Use the correct existing person to avoid duplicate identities. Employment type and pay basis are different: a permanent employee can still have a specific rate basis.

### 7.3 Maintain the employee profile

1. Open the employee and choose **Edit Employee**.
2. Update employee number, title, first name, surname, display name, sex, date of birth and employee type as applicable.
3. Update contact number and address; complete next-of-kin name, relationship, contact number and address.
4. Enter an existing photograph reference if required.
5. Choose **Save Employee**.

First name and surname supplied together update the display name. Employment start and status are read-only in the ordinary edit form to preserve eligibility history. Photograph upload is not available.

### 7.4 Reveal or correct an employee national ID

Only the Grower sees these actions. **Reveal National ID** opens a reason field; enter the reason and choose **Reveal protected ID**, then **Hide National ID** when finished. **Correct National ID** requires the corrected ID and a reason; choose **Save protected ID**. Ordinary profile editing cannot change the protected ID. These operations are audited; do not include the full ID in screenshots, exported notes or support messages.

### 7.5 Archive an employee

1. Open the active employee and choose **Archive Employee**.
2. Enter the actual employment end date.
3. Read the retention notice and choose **Confirm archive**.
4. Check the archived status and historical evidence.

Archiving ends active employment and retains history. The screen states that this cannot be reversed. Review outstanding work, rates, payroll and advances before archiving; an archived worker can appear as a payroll blocker requiring source review.

### 7.6 Add effective-dated pay rates

1. Open an active employee and find **Pay rates → Add rate period**.
2. Choose Daily, Monthly, Hectare or Standard line.
3. For Hectare or Standard line, select an active activity type with the matching quantity basis. Daily and Monthly rates are worker-wide.
4. Enter **USD rate**, **Effective from** and optional **Effective to**.
5. Choose **Add rate** and check the resulting period.

The rate must be positive. Work uses the rate effective on the work date and retains its snapshot. Do not choose today's rate for historical work. Overlapping or missing periods can prevent valid evidence. **Screen limitation:** ending an existing rate period is supported by the server, but the detail screen only adds rate periods. Ask an authorized operator to end the correct rate with an effective end date before adding an overlapping replacement.

### 7.7 Record attendance and field allocation

1. Choose **Attendance** and select the actual work date.
2. For each worker you are recording, choose **Present** or **Absent**.
3. For every present worker, select the single allocated field.
4. For an absent worker, the field selection clears.
5. Enter a late-entry reason when more than two calendar days late.
6. Choose **Save attendance** and inspect the saved present/absent totals.

Unselected workers are not submitted as attendance choices. Each present worker has one field allocation for the day. Presence alone is not payroll evidence; verified work is also required. Dates in locked approved payroll periods may reject changes.

### 7.8 Record work evidence

1. Select **Work evidence** and the correct work date.
2. Select a **Present worker** with saved attendance and a field allocation.
3. Choose the pay basis that matches the rate and work.
4. Select at least one activity on the allocated field with actual work for that date.
5. For Hectare work, choose a named section, enter section name and actual hectares.
6. For Standard-line work, use a line range with inclusive start/end line numbers, or a named section with whole-line quantity and section name.
7. Enter the late-entry reason where required.
8. Choose **Record evidence** and inspect the saved amount or rate context.

Daily work uses the event-date daily rate. Piece work uses quantity and its applicable activity rate; duplicate or overlapping scopes are blocked. A line range calculates its whole-line quantity from the inclusive endpoints. Monthly work retains a rate snapshot and shows **Deferred to payroll** because the month and eligible dates determine its earnings.

### 7.9 Verify and confirm evidence

1. In the evidence register, locate a Draft work record.
2. Select the **Named supervisor** and choose **Record attestation** after the supervisor has verified it.
3. The record becomes Supervisor verified.
4. The authorized manager reviews the evidence and chooses **Manager confirm**.
5. Confirm the record shows **Payroll-eligible evidence**.

The three facts are Entered, Supervisor and Manager. Recording a named supervisor attestation is separate from that person signing in. Activity verification and work-record verification are also separate; complete both where applicable.

### 7.10 Correct work evidence

**Screen limitation:** the server supports a correction that creates replacement work evidence and preserves the original chain, but this page has no correction editor. Provide the authorized operator with the work reference, corrected quantity/scope/activity facts and reason. The replacement must pass the normal attendance, rate, verification and payroll-lock rules. Recheck payroll readiness afterward; superseded evidence must not be paid twice.

## 8. Payroll, advances, payments and settlement

### 8.1 Understand the full payroll process

The Farm Manager prepares a calculation; the Grower approves the exact submitted version. Approval closes the payroll period and locks consumed evidence and advance recovery. Payment recording happens afterward. Cane360 records cash and mobile-money evidence; it does not transfer funds.

Normal sequence: attendance and confirmed work → open payroll period → readiness check → calculate → submit → Grower approve → record payments → acknowledge cash → close settlement.

### 8.2 Create, open or cancel a period

1. In **Payroll runs → Payroll periods**, enter Year and Month and create the period.
2. Select the period row to work with that month.
3. Use **Open** when it is ready for payroll work.
4. To cancel an eligible period, choose its cancellation action, enter the reason and **Confirm cancellation**.

Periods are calendar months and have Draft, Open, Closed or Cancelled states. Read the available controls and server messages; a cancelled or closed period cannot be used as if it were open. Cancellation is not deletion and is not the same as cancelling a run.

### 8.3 Assess payroll readiness

1. Select the period and review **Labour eligibility preflight**, the payroll-readiness assessment.
2. Refresh the assessment before calculation.
3. Filter by Worker, Eligible/Blocked and Evidence type.
4. Review each blocker and its source; use the relevant Labour or Farm record to resolve it.
5. Use readiness pagination to review all evidence, then refresh again.

Typical blockers include absent attendance, missing or conflicting field allocation, missing supervisor attestation, missing manager confirmation, duplicate evidence or colliding scopes, missing rate snapshots, archived workers, superseded/inactive evidence, evidence outside the period, evidence already consumed, or sources that changed after calculation. A missing monthly-proration policy or a monthly rate too small to allocate daily cents also blocks calculation.

The readiness check is read-only. An eligible row is not paid or consumed by checking it. An empty readiness list may be caused by the selected filters.

### 8.4 Create and calculate a run as Farm Manager

1. Select an Open period.
2. Choose **Create [month] run**. The system permits one noncancelled run for that period.
3. Select the run and choose **Calculate all evidence**.
4. Review worker count, evidence count, gross USD, advance recovery and net USD.
5. Expand every relevant worker row to review earning evidence and advance allocations.
6. Resolve all blockers. After source changes, choose **Calculate new version**.

Each calculation version is retained. The system selects authoritative eligible evidence; the browser does not allow manual replacement of the computed payroll totals. A run can move through Draft, Calculated, Pending Grower approval, Approved, Rejected or Cancelled.

### 8.5 Understand monthly earnings

For a monthly rate, payroll counts distinct eligible work dates with present attendance and supervisor/manager-confirmed work. Each eligible date earns a share using the number of calendar days in that month. Missing or absent days earn no share. Duplicate active monthly evidence for the same worker/date blocks payment of the duplicate records.

Example: a USD 310 monthly rate in a 31-day month earns USD 10 per eligible day; 20 eligible days earn USD 200 before advances. Cumulative rounding assigns cents in date order so a fully eligible month totals the exact monthly rate. This is calendar-day proration, not a fixed 26-day rule. Paid leave and other non-work entitlements are not automatically payable sources in this implementation.

### 8.6 Submit, approve, reject or cancel a run

1. As Farm Manager, ensure a calculated run has evidence and zero blockers.
2. Choose **Send calculation** to submit its exact version to the Grower.
3. As Grower, open the pending run and review totals, worker earning sources and advance deductions.
4. Choose **Approve exact calculation** to approve, or enter a rejection reason and choose **Reject**.
5. If rejected, the Farm Manager reviews the reason, resolves sources, calculates a new version and resubmits.

Approval revalidates source facts. If attendance, rates, verification, employees, advances, balances or period state changed, approval can return a conflict without creating approved payroll facts. Refresh and review rather than repeatedly submitting the old calculation.

**Result of approval:** the period closes, evidence consumption and recovery facts become locked, and settlement becomes available. No money has been paid. The Farm Manager can cancel eligible Draft, Calculated or Rejected runs with a cancellation reason. There is no normal screen to undo approved payroll; reopening settlement does not reopen the calculation or labour evidence.

### 8.7 Create and review a worker advance

1. In **Worker advances**, choose **New advance**.
2. Select the active worker, enter Amount USD, Reason and Requested event date.
3. Select **Recovery starts** and the installment count, from 1 to 60.
4. Choose **Preview authoritative schedule** and inspect each period and installment. Required calendar periods must exist; create missing periods first.
5. Save the advance draft.
6. Open it and review amount, recovery schedule, status and history.

Use worker/status filters to find advances. A Draft or Rejected advance can be revised using **Edit draft**. The worker cannot be changed in an existing draft editor. An eligible draft can be cancelled with a reason.

### 8.8 Submit, approve and issue an advance

1. As Farm Manager, choose **Submit exact version** on the valid Draft.
2. As Grower, inspect the pending advance and **Approve**, or enter a reason and **Reject**.
3. Once Approved, choose **Record issue** only after the actual money has been given or transferred externally.
4. Choose Cash or Mobile Money and record the exact approved amount and issue/transaction date and time.
5. For Cash, select the paying person and record the receiving worker's acknowledgement.
6. For Mobile Money, enter provider, recipient number, external reference and transaction status.
7. Save the issue and verify the advance is Issued and its recovery balance is correct.

Approval alone is not issue. Payroll recovers due **issued** advances according to the approved schedule and authoritative balance. Mobile-money numbers are protected; the returned record shows a mask. Cane360 does not execute the transfer. Do not record a pending or failed transfer as a successful issue.

### 8.9 Record payroll payments after approval

1. Open the Approved run and **Payment and settlement**.
2. Review net payroll, paid, outstanding, workers settled and acknowledgement exceptions.
3. Choose **Record payment** on a worker with an outstanding balance.
4. Select Cash or Mobile money, enter the amount up to the outstanding balance and actual payment date.
5. For Mobile money, enter provider, recipient number, transaction reference and the true external status: Successful, Posted, Pending or Failed.
6. Choose **Record payment evidence** and inspect the worker's refreshed balance and **View trace**.

Partial payments are supported. Only qualifying valid payment evidence contributes to settlement; pending/failed mobile-money records do not prove the worker is paid. The server rechecks the outstanding amount and prevents excess payment. If the save outcome is uncertain, inspect the trace before retrying.

### 8.10 Acknowledge cash, reverse errors and close settlement

1. Expand the worker's payment trace.
2. For active cash payments without acknowledgement, choose **Acknowledge** after receiving the worker's acknowledgement.
3. To correct an active erroneous payment, choose **Reverse**, enter the mandatory reason and **Record reversal**. The screen reverses the active amount; it does not edit the original payment.
4. Record the correct replacement payment evidence if needed and review outstanding balances again.
5. When the server permits closure, choose **Close settlement**. Resolve outstanding payments and acknowledgement exceptions first.
6. If a closed settlement needs payment-side correction, the Grower enters **Reopen reason** and chooses **Reopen settlement**.

Closed settlement makes payment actions read-only. Reopening allows payment corrections only; the approved calculation, evidence consumption and advance recovery remain locked. Payment reversals are records of corrections, not automatic refunds.

### 8.11 Print payslips and the cash register

1. In settlement, choose **Payslip** on a worker or **Cash register** for the run.
2. Inspect the operational document and its approved calculation version.
3. Choose **Print** and select a printer or Save as PDF.
4. Close the document to return to settlement.

Payslips expose approved gross, deductions and net with operational payment context. The cash register supports recording and reviewing cash-payment evidence. Treat printed payroll documents as confidential. The documents are operational outputs; the application does not implement a full statutory tax, benefits or general payroll-compliance engine.

## 9. Inventory and controlled-input accountability

### 9.1 Inventory workspace and units

Inventory tabs are **Stock on hand**, **Receipts**, **Movement ledger**, **Catalogue**, **Inputs**, **Counts** and **Adjustments**. Stock and value come from posted movements; draft receipts and draft issues do not change stock. A store issue moves custody to the field and does not by itself record consumption or applied-input cost.

All quantities use the item's stock unit. Enter kilograms in kilograms, litres in litres and whole-count units at their configured precision. There is no general conversion or alternative purchase-unit editor in these forms.

### 9.2 Set up the catalogue

Use Catalogue to configure these records before receipts and requests:

| Record | Fields and procedure |
| --- | --- |
| Inventory category | Add category: code, name, optional description and display order; edit details, Deactivate or Reactivate when appropriate |
| Stock unit | Add unit: code, name, dimension such as Mass/Volume/Count and decimal precision from 0 to 6 |
| Supplier | Add supplier: code, name and contact; use pencil to edit; Archive or Reactivate changes availability |
| Inventory item | Add item: code, name, active category, active stock unit, optional reorder level, lot policy and expiry policy |
| Lot / batch | Add lot: eligible lot-tracked item, batch code and expiry date as required by policy |

Lot and expiry policy choices are None, Optional and Required. Follow the form's policy dependencies; an expiry requirement needs suitable lot tracking. Historical references remain after archiving. Manage unit names and unit archiving in Administration. Item/category policy choices affect future receipt and issue validation.

Inventory-category and supplier availability actions are available on screen. The current inventory item catalogue provides creation and display, with no general item edit/archive workflow; do not assume the same controls exist for every reference type.

### 9.3 Record a purchase receipt

1. Choose **Record receipt** and **Purchase receipt**.
2. Select an active supplier; enter the receipt date and source reference from the delivery note or invoice.
3. Enter the late-entry reason if the receipt is recorded more than two calendar days late.
4. Add stock-unit lines: active item, required/optional lot, quantity and unit cost USD. Add or remove lines as needed.
5. Review quantities and costs, then choose **Record draft receipt**.
6. In Receipts, review the draft and choose **Post** when the evidence is correct.
7. Check Stock on hand and the new Movement ledger entries.

Source-reference duplicate warnings help identify already recorded deliveries. Review a warning before posting; do not change the reference merely to bypass it. Unit cost is USD per stock unit; the form accepts zero cost where the rules permit it.

### 9.4 Record and approve opening balances

1. Choose **Record receipt → Opening balance**.
2. Enter the date, source reference, opening-balance reason and stock lines with quantities, lots and unit costs.
3. Save the draft, then **Submit** it for approval.
4. The Grower reviews the exact opening-balance facts and **Approve**s or **Reject**s with a reason.
5. Post the Approved opening balance.
6. Review the stock positions and posting sequence.

Opening balances establish initial controlled stock and require approval; they are not ordinary purchase receipts. Rejection does not create stock. The current receipt register does not offer a general draft-line editor.

### 9.5 Reverse a posted receipt

The Grower can choose **Reverse** on a posted receipt and enter the reason. The original remains in history and compensating movements are appended. Reversal is rejected if it would violate stock/value or downstream-use controls, or while the store is frozen for a count. Review the result before creating corrected evidence.

### 9.6 Read stock, reorder alerts and movements

Stock on hand shows item/lot quantities and valuation derived from posted movements. The reorder section identifies items below their configured reorder levels. Lot and expiry information helps select the appropriate batch for an issue; an expired or otherwise ineligible lot can be rejected by posting rules.

Movement ledger shows append-only posting sequence, movement type, quantities, values and source identity. Use source references and lot identity to trace a discrepancy. Do not treat draft receipts, requests or approvals as available stock.

### 9.7 Configure application rules and tolerances

1. Open **Inputs → Application rules**, or **Administration → Rules & Tolerances**.
2. Select the item and activity type.
3. Choose coverage basis: Field hectares or Activity actual quantity.
4. Enter rate per coverage unit and lower/upper tolerance percentages.
5. Enter effective-from and optional effective-to dates.
6. Add the rule and review its valid date range.

Example: a 10-hectare reporting field with a rate of 20 kg/ha plans 200 kg. Tolerances define the permitted range shown with the request. The effective rule and coverage facts are preserved for decision review. End an old rule through Administration before creating an overlapping replacement. Editing a label does not rewrite historical approved quantities.

### 9.8 Request input against an activity

1. Open the operational activity's **Inputs** panel.
2. Choose **Request input**, select the eligible active item and enter the requested quantity in its stock unit.
3. Choose **Create and submit**.
4. Open **Inventory → Inputs → Approval queue**.
5. Review planned/requested quantities, tolerance range, live available stock, estimated value and the approval lane.

Requests outside the normal tolerance lane require Grower decision. Standard requests can be decided by authorized Grower/Farm Manager accounts according to the lane; the screen states when owner approval is required. An estimated value is a planning snapshot and is not final applied cost. Completed, Closed and Cancelled activities disable new input requests in this panel.

### 9.9 Approve or reject an input request

1. Select the pending request in Approval queue.
2. Check field, activity, each item, quantity, effective rule, tolerance range and live stock.
3. Choose **Approve** if permitted, or **Reject** and enter the reason.
4. Confirm the resulting status before issuing stock.

Approval is for an exact version and does not itself deduct store stock. **Screen limitation:** draft request-line edits and request cancellation are supported by the server, but this client panel creates and submits immediately and has no full request editor. Refer a wrong submitted request to the authorized operator; do not issue stock against it to compensate.

### 9.10 Create and post a partial issue

1. In **Inputs → Issues & accountability**, locate the Approved or Partially issued request and choose **Issue remaining**.
2. Enter issue date, named storekeeper issuer, field recipient and any late-entry reason.
3. Inspect approved, already issued and remaining quantities.
4. Select the lot for each line where applicable and enter the quantity actually being issued, within the remaining allowance.
5. Choose **Create draft issue**.
6. Locate the Draft issue and choose **Post**.
7. Check the store movement and updated remaining quantity.

Multiple partial issues can fulfill a request. Posting checks live stock, lots, approvals and store freeze status. The named issuer needs the effective Storekeeper role. A draft issue alone has not deducted stock.

### 9.11 Record field receipt

1. On the posted issue, choose **Field receipt**.
2. Enter received date/time, named field recipient and any late-entry reason.
3. Enter quantities actually received for each issue line; partial receipt is supported.
4. Choose **Record receipt**.
5. Inspect field-received and unacknowledged balances.

Store issue and field receipt are separate facts. A difference between issued and received stock must remain visible until resolved.

### 9.12 Capture, attest and confirm application

1. On the posted issue, choose **Apply**.
2. Select the received-input line; enter actual application date/time, coverage basis, verified coverage and quantity applied.
3. Choose **Capture application**.
4. In **Verify and confirm application**, select the named supervisor and enter an optional attestation note.
5. Choose **Record attestation** after the supervisor verifies it.
6. The authorized Farm Manager reviews and chooses **Confirm application**.
7. Verify confirmed-applied quantities and the cost trace.

Supervisor attestation creates no applied-input cost. Manager confirmation creates the authoritative applied-input fact and source cost. Use only field-received input and eligible coverage; the server checks available outstanding quantities.

**Screen limitation:** the verification dialog opens after capture. The current Issues screen has no complete persistent application register with a reopen-verification button. If a captured application remains unconfirmed after the dialog is closed, request authorized assistance to retrieve and complete that existing application; do not capture it again.

### 9.13 Return unused stock to the store

1. On the posted issue, choose **Return**.
2. Select the issue line, return date, named sender and Storekeeper receiver.
3. Enter the quantity physically returned.
4. Choose **Post Store receipt**.
5. Confirm the return restores the correct item/lot stock and reduces outstanding accountability.

The workflow creates and posts the return; it does not count a draft return as stock. The returned amount must be eligible and unapplied. **Screen limitation:** return reversal is supported by the server without a dedicated return-register control here. Use authorized assistance with the return reference and correction reason.

### 9.14 Record loss, damage, spillage or expiry

1. On the posted issue, choose **Loss**.
2. Select the issue line, loss type (Lost, Damaged, Spilled or Expired), quantity and clear reason.
3. Choose **Submit for Grower approval**.
4. The Grower reviews the submitted loss in Approval queue and **Approve loss** or **Reject**s with a reason.
5. Check the approved loss quantity, cost and remaining accountability.

Submitted loss is not an approved resolution. Approved loss is kept separate from confirmed application. It affects the relevant approved variance cost; it must not be disguised as productive input use.

### 9.15 Read the accountability chain

Use the fixed chain: Requested → Approved → Issued → Field received → Confirmed applied → Returned → Approved loss → Outstanding → Unacknowledged. A posted issue has to be explained by the appropriate later facts; stock remaining in the field is not automatically consumed.

Example: 100 kg issued; 100 kg received; 70 kg confirmed applied; 20 kg returned; 10 kg approved lost. The issue is fully explained, while only 70 kg is productive application. If just 80 kg was received, the unacknowledged balance still needs investigation.

Review warnings and quantities for every activity, item and lot before activity closure. A link from a crop cycle filters Inputs to that cycle; open the full Inventory page when you need the whole farm context.

### 9.16 Correct or reverse issues and accountability facts

On an eligible posted issue, **Correction** records a correction request with a reason. The Grower can **Reverse** when permitted, also with a reason. The original record remains; reversal creates compensating evidence. Downstream use can block reversal.

**Screen limitation:** the server also supports Grower-decided corrections of field receipts, manager-confirmed applications, posted returns and approved losses. This screen has no complete correction queue/editor for those facts. Identify exactly one source record, its current version and the reason to the authorized operator. The server checks dependent records and stock/value before applying a correction. Review the resulting accountability and cost trace before proceeding.

### 9.17 Full-store physical counts

1. Schedule a time when store postings can pause.
2. Open **Counts → New count**; enter named counting persons and count notes; create the Draft.
3. Choose **Start & freeze** only when ready. This captures a fixed ledger cutoff.
4. Count each item/lot physically and have its quantity and notes entered against the count line. Add unexpected item/lot lines when found.
5. Review all lines and variances, then choose **Review & release** when the count is complete.
6. Resolve nonzero variances through separately approved stock adjustments.

Starting a count freezes receipt, issue, return, reversal and adjustment postings until review or cancellation. Count entry does not change stock. **Screen limitation:** the current Counts screen shows progress and start/review controls, but no line-entry, unexpected-line or cancellation form. Arrange authorized service assistance before starting; otherwise the store can remain frozen. Cancellation is supported with a reason and releases the freeze according to the count rules.

### 9.18 Stock adjustments

Adjustments displays item, adjustment type, status, reason, signed quantity and value. The supported process is: create a draft from a count variance or justified discovery/write-off → submit → Grower approves/rejects the exact version → post the approved adjustment. Reversal requires the appropriate permission and reason, and preserves the original.

**Screen limitation:** this tab is a register, not an adjustment editor or approval queue. Authorized operators use the supported service for create, submit, decision, posting and reversal. Supply the count-line reference when applicable, item/lot, positive or negative quantity, event date, reason and explicit unit value where required. Never alter physical counts just to force agreement with the ledger.

### 9.19 Leakage reporting and manager access

**Screen limitation:** the server provides a paged leakage report and CSV export, but the current Inventory screen has no dedicated leakage-report tab. Authorized assistance can filter by dates, field, cycle, activity, item, lot, issuer, recipient, supervisor, status, exception type and severity. Use it to investigate outstanding/unacknowledged inputs and exceptions without relying on global totals.

**Inputs → Manager access** provides a legacy Farm Manager invitation view. The more complete **Administration → Users & Access** workflow supports both Farm Manager and Supervisor invitations; use chapter 12 for new access onboarding.

## 10. Finance: transactions, crop costs and budgets

### 10.1 Finance sections and scope

Finance provides **Transaction register**, **Crop cost trace**, **Budgets & variance** and **Mill records**. Operational money values are USD. This is operational finance rather than a complete accounting ledger, bank-feed system or funds-transfer service.

### 10.2 Create and edit a transaction draft

1. Choose **New transaction**.
2. Select Expense or Income and the appropriate category.
3. Enter event date, amount USD and payee or payer.
4. Enter source reference and notes where available.
5. Choose **Save draft** and inspect it in Transaction register.
6. Use the pencil action to edit a Draft before posting.

Categories are Fuel, Repairs and maintenance, Utilities, Transport, Contract services, Crop inputs, Crop sales, Other expense and Other income. A source reference should let a reviewer find the real invoice, receipt or operating evidence.

### 10.3 Allocate the full amount

1. On the Draft, choose **Allocate**.
2. Add one or more allocation rows.
3. Choose the scope for each: Crop-cycle direct, Field only or Farm overhead.
4. For Crop-cycle direct, select field and cycle. For Field only, select field. Farm overhead has no direct cycle assignment.
5. Enter the amount for each row; add/remove rows as needed.
6. Ensure the total equals the transaction amount exactly and choose **Save exact allocations**.

Example: a USD 100 expense can allocate USD 60 to one crop cycle and USD 40 to farm overhead. Only eligible crop-cycle direct expense contributes directly to that crop's cost; field-only and overhead amounts do not silently become cycle cost. Do not manually duplicate automatic confirmed-input or approved-payroll costs as direct expenses.

### 10.4 Post or reverse a transaction

1. Verify a Draft's direction, category, date, amount, source and allocations.
2. Choose the **Post transaction** action and confirm.
3. Check the Posted status and cost trace where applicable.
4. If a posted transaction is wrong, the Grower chooses **Reverse**, enters the reason and confirms.

Posting makes the money facts and allocations immutable. Reversal appends a correction and preserves the original. There is no general screen to delete posted transactions. A Cancelled status may appear in filters, but the transaction screen does not provide a cancellation action.

### 10.5 Filter and reconcile finance

Filter transactions by From/To date, direction, status, category and payee/source text, then **Apply filters**. Review the filtered totals and use Previous/Next transactions to move through 50-record pages.

Use **Reconcile payroll costs** to bring eligible approved payroll source costs into finance when needed. This reconciles costs; it does not approve payroll, alter earnings or record payments. Review the resulting source trace rather than entering a duplicate labour expense.

### 10.6 Read crop cost trace

1. Choose **Crop cost trace** and select the crop cycle.
2. Inspect total cost, reporting area and available cost-per-hectare or cost-per-tonne measures.
3. Review the category breakdown and **Authoritative source trace**.
4. Follow source identities back to payroll/work evidence, confirmed input applications, approved variance/loss costs or posted direct expenses.

The trace distinguishes Labour, Applied input, Direct expense and Approved variance cost. Issued stock alone is not applied-input cost. Budget is planning data and does not create actual cost. Missing denominators show an unavailable measure rather than an invented value.

### 10.7 Create a crop-cycle budget

1. Choose **Budgets & variance** and select a crop cycle.
2. Enter budget name/reference, optional expected production tonnes and notes; choose **Create draft**.
3. Review or update reporting hectares, expected tonnes and budget details while Draft.
4. Add lines for Labour, Applied input, Direct expense and Approved variance cost.
5. For each line, enter description and amount USD. Optional fields are quantity, unit, unit rate USD and notes.
6. Save the lines and review totals. Use the line edit or remove action to correct Draft lines.

### 10.8 Submit, approve and revise budgets

1. With at least one valid line, submit the Draft budget.
2. The Grower reviews the Submitted budget and chooses **Grower approve**.
3. Use the Approved budget as the planning baseline.
4. If it needs changes, the Grower chooses **Create revision**, edits the new draft version, submits and approves it.
5. Use the version selector to compare retained versions and review the current approved baseline.

Submitted and Approved budget facts are not ordinary editable drafts. A revision preserves the previous approved version. The current screen has no separate budget reject or cancel workflow; do not assume payroll's rejection controls apply here.

### 10.9 Review and print variance

Compare the approved budget to authoritative actual costs, category by category. Review budget/actual totals, differences and available per-hectare/per-tonne values. The actual-minus-budget variance is positive when actual exceeds budget; a percentage may be unavailable when its denominator is zero or missing. Use **Print variance** to print or save the report. Resolve questionable actuals at their source rather than editing the budget merely to hide variance.

## 11. Mill records: tickets, statements and reconciliation

### 11.1 Manage mills

1. Open **Finance → Mill records → Mills → New mill**.
2. Enter code, name and optional location/notes, then **Save mill**.
3. Use the pencil action to edit the mill reference.
4. Use **Deactivate** or **Reactivate** to control new use while retaining history.

Use the correct mill when creating tickets and statements. A mill is reference data, not a separate farm workspace.

### 11.2 Create and record a weighbridge ticket

1. Open Tickets and choose **New ticket**.
2. Select the mill; enter ticket reference, ticket date, gross tonnes, optional tare tonnes and net tonnes.
3. Assign a field and crop cycle when known; enter source reference and notes.
4. Choose **Save draft**.
5. Attach source evidence using **Attach** when available and inspect the saved weights. If tare is provided, net must be consistent with gross minus tare under server validation.
6. Edit the Draft if needed, then choose **Record**.

Net tonnes are entered and validated; do not assume the form calculates them automatically. The ticket's crop-harvest figure, when displayed, is context and remains unchanged by the ticket. Recorded evidence has stronger correction controls than an editable draft.

### 11.3 Create and record a grower statement

1. Open Statements and choose **New statement**.
2. Select mill; enter statement reference, period start/end, total tonnes, total amount USD and notes.
3. Choose **Save draft**.
4. Open the statement and choose **Upload original**; attach the real source document.
5. Review its contents against entered totals. A statement needs attached evidence before **Record statement** is enabled.
6. Choose **Record statement**.

Statements record authoritative mill totals and support reconciliation. They do not automatically create a Crop sales finance transaction or replace the manual crop harvest yield.

### 11.4 Upload and download evidence

Choose an active **Document category**, or Unclassified, then attach the file. The picker accepts images, PDF and CSV; the server validates uploads and caps evidence at 20 MB. Keep filenames meaningful and source documents legible. After upload, use the displayed filename link to download the evidence through the farm-authorized endpoint.

Mill evidence upload is implemented even though activity source-sheet and employee/owner photograph uploads are not. Categorizing a document does not upload it by itself. If an upload is rejected, check size/type, permission and the server message before retrying.

### 11.5 Match tickets to a recorded statement

1. Open the recorded statement and **Match another ticket**.
2. Select the eligible recorded candidate ticket and inspect its available net tonnes and any reuse warning.
3. Enter matched tonnes, or leave blank to use the available net tonnes.
4. Enter matched amount USD if known; this is optional and separate from tonnage matching.
5. Enter a reason for partial matching.
6. Select **Matching is complete** only when all intended ticket evidence has been added.
7. Choose **Add match** and review the refreshed reconciliation.

Each match uses the exact statement/ticket versions. A candidate marked used elsewhere needs review; do not assume it can be counted again without consequences. The system shows active and reversed match history.

### 11.6 Interpret reconciliation

Review Statement tonnes, Matched ticket tonnes, Tonnes variance, Statement amount and Matched amount. Match states include Unmatched, Partially matched, Matched and Variance. Tonnage and money reconciliation are separate. If matched amounts are incomplete, the amount result can be unavailable rather than zero.

Check warnings about cross-statement ticket reuse. A completed matching declaration with inconsistent totals requires investigation; it does not make the statement agree automatically. Compare each source document and confirm the mill, period, ticket references, units and partial allocations.

### 11.7 Correct recorded evidence and reverse matches

The Grower can choose **Correct** on eligible Recorded tickets or statements. Enter the correction reason and corrected details. Ticket correction appends correction evidence; statement correction creates a replacement draft that must receive original evidence and be recorded. Original records remain in history.

The Grower can choose **Reverse** on an active statement match and enter a reason. The match stays visible as Reversed and totals refresh. Review any matches after ticket or statement correction; do not assume they migrate to the replacement record.

### 11.8 Find, export and print mill records

Use Tickets/Statements/Mills tabs. Ticket and statement filters include date range, mill, match state and reference; Tickets also includes field, crop cycle and Draft/Recorded status. Choose **Apply** and use Previous/Next for 50-record pages. Inspect summary totals in the selected view.

Use the CSV export action for the active ticket/statement view and filters; use **Print** for the rendered records. Exports and evidence downloads remain permission-controlled. Spreadsheet opening may format numbers differently; preserve the original CSV when reconciling.

## 12. Administration, profile, access and audit

### 12.1 Administration Overview

Overview shows active users, pending invitations, active manager, active activity types, active units and application-rule count. Metric cards open the associated section. Review configuration attention notices and recent administration/audit events. Grower-only access and audit controls are omitted for Farm Managers; supervisors receive My profile only.

### 12.2 My profile

1. Choose your account/role link, or **Administration → My profile**.
2. Review login email, email-verification status and assigned role.
3. Edit **Display name** and **Contact number**.
4. Choose **Save profile** and confirm the success message.

This profile is for the signed-in account. Farm personnel and Farm Owner records are managed separately under Farm. It is not the full employee/owner profile editor and does not change login email, password, operational role or security role.

### 12.3 Invite a Farm Manager or Supervisor as Grower

1. First create or review the active operational person in Farm and ensure the correct operational role is effective.
2. Open **Administration → Users & Access → Access invitation**.
3. Select Farm manager or Supervisor and an eligible operational person.
4. Choose **Create invitation**.
5. Copy the displayed token immediately; it is shown once and only its hash is stored.
6. Share it privately with the intended person using your approved channel. Cane360 does not automatically email it from this screen.
7. Ask the person to register/log in and complete activation as described in chapter 2.
8. Check the invitation becomes Redeemed and the new user membership appears.

The Administration screen-generated invitation uses a 48-hour expiry. Review its displayed expiry. Changing the role selector refreshes eligible candidates; if none are available, fix the operational role/person setup first. Do not use a different person's record just to produce a token.

### 12.4 Revoke invitations or disable membership

Choose **Revoke** on an open unexpired invitation to prevent future redemption. Redeemed, expired and revoked invitations are history and cannot be reused.

Use **Disable access** on an eligible user membership to remove active farm access. Confirm the current user, role and linked person first. This disables membership rather than deleting the login, personnel record or employee history. The current screen has no general role-change, re-enable-membership or user-deletion editor; refer those access questions to authorized support.

### 12.5 Roles & Permissions

Review the fixed capability matrix and your role. It explains which actions are available to Grower, Farm Manager and Platform Administrator in its current presentation. Supervisor capture permissions are described in this manual and enforced by the server even though that matrix does not show a Supervisor column. Do not infer permission from a visible button or from holding an operational personnel role.

### 12.6 Activity Types and Units

Create activity types with code, name, quantity basis and planned/unplanned support. Active types can be renamed or archived. Create units with code, name, dimension and decimal precision; active units can be renamed or archived. Code/basis/dimension are not general editable history fields in the rename controls. Archiving retains historical references.

### 12.7 Rules & Tolerances and farm settings

Create effective application rules with item, activity type, coverage basis, rate, lower/upper tolerance and dates. Review the existing date ranges. Use the end action with **Last effective date** when replacing an old rule; preserve its historical application.

In **Farm settings**, add an effective version for **Reason required after days**, with a value from 0 to 30, effective-from and optional effective-to dates. End a setting version using its last effective date. This configures the late-entry setting exposed here. Some operational forms explicitly display a two-day prompt; follow server validation and ask your administrator when the configured rule and screen helper text differ.

### 12.8 Document Categories

1. Enter category code, name and optional description and choose **Add category**.
2. Edit an active category's name/description and **Save category**.
3. Archive a category when it should no longer be used for new attachments.

These categories classify uploaded mill evidence; they are not inventory item categories. Existing document references are retained. There is no general document-library screen or category-reactivation button here.

### 12.9 Audit search, detail and CSV

1. As Grower, open **Administration → Audit**.
2. Filter by From/To dates, Action, Subject, Authenticated user ID, Operational person ID or Correlation ID.
3. Open an event card to inspect details and the recorded source identifiers.
4. Use Previous/Next to review all 25-record pages.
5. Choose **Export CSV** for the filtered audit result.

Audit distinguishes the authenticated user from the named operational person and correlates related actions. Exports can contain confidential operational information; use your organization's sharing rules. An empty filtered result is not proof that an event never happened: broaden dates, verify identifiers and check pagination.

## 13. Reports and available outputs

### 13.1 Reports page status

The main **Reports** page currently shows a placeholder saying its first workflow is not yet available. It does not have a working report builder, scheduled reports or all-module export screen. This manual includes it because it is part of navigation, but it provides no invented instructions for missing controls.

### 13.2 Where to find working outputs

| Need | Available location |
| --- | --- |
| Farm/field/current crop summary | Dashboard, Farm, Fields and Crop Cycles |
| Crop lifecycle and field history | Crop-cycle logbook |
| Planned/actual activity diary and verification history | Activities list/calendar and chronological diary |
| Attendance and confirmed work | Labour date registers |
| Payroll readiness and calculation sources | Payroll runs |
| Printable operational payslip and cash register | Approved payroll → settlement |
| Stock positions, movements and accountability | Inventory tabs and Inputs |
| Leakage report and CSV | Supported service; authorized assistance needed |
| Transactions and source costs | Finance → Transaction register / Crop cost trace |
| Printable budget variance | Finance → Budgets & variance |
| Ticket/statement summaries, CSV and print | Finance → Mill records |
| Downloadable mill evidence | Attached filename links on tickets/statements |
| Audit detail and CSV | Administration → Audit, Grower only |

Filter before exporting and review the selected dates, farm and units. Browser printing produces what the current print layout supports; it is not an all-record export. For paged registers, CSV/service exports and screen printing can have different scopes.

## 14. Daily, month-end and crop-end operating checklists

### 14.1 Beginning of the day

1. Log in and verify your farm and role.
2. Review the relevant fields, current crop status and today's planned activities.
3. Check personnel roles, employee availability and effective rates for the work date.
4. Review input stock and approved requests before issuing.
5. Check whether a physical count is freezing store postings.

### 14.2 End of the day

1. Save actual activity work, quantities and source references.
2. Record attendance, including one field for each present worker.
3. Record work evidence and obtain supervisor attestation and manager confirmation.
4. Record field receipts, actual input applications, returns and loss requests.
5. Confirm applications and review outstanding/unacknowledged inputs.
6. Finish activity verification where appropriate; close only after blockers are resolved.
7. Review any draft finance transactions and source evidence; log out.

### 14.3 Payroll month-end

1. Review all dates in the period for missing attendance and work evidence.
2. Resolve rate gaps, duplicate scopes, verification gaps and corrections.
3. Check advances are properly approved, issued and scheduled.
4. Open the period, refresh readiness, calculate and inspect every worker.
5. Farm Manager submits; Grower reviews and approves the exact calculation.
6. Record real external payments, acknowledge cash and print operational documents.
7. Review outstanding amounts and close settlement when permitted.
8. Reconcile approved payroll costs in Finance and inspect the source trace.

### 14.4 Crop-end

1. Complete actual activities and confirmed labour evidence.
2. Reconcile all input requests, issues, receipts, applications, returns and losses.
3. Record ready-for-harvest status, harvest date and manual actual yield.
4. Record mill tickets and original statements, then reconcile tickets and amounts.
5. Review direct expenses, approved costs and budget variance.
6. Correct sources while still permitted; close the crop cycle and review history.
7. Review the next draft and activate it only when the field lifecycle permits.

### 14.5 Practice exercise for onboarding

Use an explicitly authorized training workspace and fictional records. Do not experiment on real operational records or payroll approvals.

Create a fictional field and draft crop; activate it; record an activity; save actual work; create a fictional employee with an effective daily rate; record attendance and work evidence; attest and confirm the evidence. Separately practise a small stock receipt and request, approval, issue, field receipt, application confirmation and return. Ask the trainer to review the linked histories and source totals. Practise payroll approval or irreversible closure only with the trainer's authorization.

## 15. Troubleshooting and support

| Problem | What to check next |
| --- | --- |
| Cannot log in | Email/password, correct website, temporary lockout; use administrator recovery if needed |
| Stuck on activation | Valid unexpired token; correct invited person/role; first Grower needs provisioning assistance |
| Module or button missing | Login role, operational role and record state; consult Roles & Permissions |
| Permission denied | Ask the authorized Grower/Farm Manager to perform or review the action; do not borrow their login |
| Farm/field not available | Active membership and farm context; do not create duplicate records |
| No field in activity form | Current crop must be Active or Ready for harvest |
| No supervisor or storekeeper | Active person with the required effective operational role on the event date |
| No worker in evidence form | Saved Present attendance and field allocation for the selected date |
| No eligible activity for worker | Actual activity on the same field and work date; correct crop/activity state |
| Missing rate or scope collision | Event-date rate coverage, matching activity basis, duplicate work and overlapping line/section scope |
| Payroll approval conflict | Refresh source facts; Farm Manager recalculates and resubmits a valid version |
| Record changed while editing | Refresh, review latest version, then retry the intended action |
| Stock posting frozen | Complete or cancel the active full-store count through authorized controls |
| Insufficient stock or blocked reversal | Posted quantities, lots, downstream application/returns, value constraints and freeze state |
| Activity cannot close | Incomplete labour verification or input-accountability exceptions |
| Application verification dialog lost | Retrieve the existing application through authorized assistance; do not capture it again |
| Receipt/source already used | Check the original source and existing records before entering another receipt |
| Statement cannot be recorded | Upload original evidence and check required mill, dates and totals |
| Mill variance or ticket reuse warning | Review net tonnes, partial matches, statement completeness and other active matches |
| Payment still outstanding | True external status, active/reversed amounts and cash acknowledgement |
| Export temporarily rejected | Permission, filter size or request rate; wait and retry when appropriate |
| Empty list | Filters, date, role scope, pagination and the activity page's 100-record limit |

When requesting support, provide the module, action, safe record reference, time, exact error text and correlation ID if shown. Describe what you expected and what happened. Exclude passwords, tokens, complete national IDs, full payment recipient numbers and database credentials. Redact screenshots and payroll details before sharing.

If a save fails or the network stops, reopen the record and check its state before creating another record. A disabled posting or confirmation button may indicate a request is still in progress. Preserve source evidence; do not make unrelated compensating entries to hide a discrepancy.

## 16. Glossary and functionality boundaries

### 16.1 Glossary

| Term | Meaning |
| --- | --- |
| Farm workspace / tenant | The isolated working boundary for one farm's users and records |
| Operational actor | The named person responsible for a real-world event |
| Authenticated user | The account signed in when an action is recorded |
| Effective date | The date from which a role, rate, profile or rule applies |
| Reporting hectares | The field area selected for reporting: declared or mapped |
| Plant cane / ratoon | Initial planted crop / subsequent regrowth crop with a ratoon number |
| Draft | Saved preparation; not automatically approved or posted |
| Attestation | The recorded fact that a named supervisor verified evidence |
| Manager confirmation | The authorized confirmation that makes specific work/application evidence usable downstream |
| Posting | The committed movement or transaction that affects the relevant ledger |
| Source snapshot | Retained rate, rule or quantity facts used for a decision/calculation |
| Calculation version | A retained payroll calculation of authoritative source facts |
| Settlement | Recording and reconciling payments after payroll approval |
| Reversal / correction | New evidence correcting a prior fact while retaining its history |
| Outstanding / unacknowledged input | Input still needing accountability / issue not yet acknowledged by field receipt |
| Cutoff posting sequence | The fixed ledger point used for a stock count |
| Correlation ID | An identifier linking related requests/events for support and audit |

### 16.2 Supported operations with incomplete screen controls

This list is part of the manual's coverage, not a promise that these are available as ordinary buttons. Authorized operators should use the existing service contract and permissions; new users should request assistance.

- First Grower farm provisioning when the account has no membership.
- Operational-person deactivation and ending a particular role assignment.
- Ending an existing employee rate period.
- Replacement corrections of work evidence, with history and payroll locks preserved.
- General draft request-line editing and request cancellation.
- Reopening retrieval of a captured input application for later verification/confirmation.
- Return reversal and Grower-decided correction of field receipts, confirmed applications, posted returns and approved losses.
- Physical count line entry, unexpected count lines and count cancellation.
- Stock adjustment draft, submission, decision, posting and reversal.
- Paged leakage report and CSV export with operational filters.
- Additional activity-register pages beyond the first 100 loaded on screen.
- Detailed payroll source/calculation lookups supported by the service beyond the currently shown row expansion.

The companion functionality inventory maps every current controller endpoint to its manual chapter, including health and account/session support operations. It is a maintenance reference for system owners; ordinary users follow the module procedures above.

### 16.3 Features without a current working workflow

Reports is a placeholder. The current client has no password-reset/change page, general multi-farm switcher, GIS boundary editor, general attachment library, automatic invitation email sender, activity document/photo upload, employee/owner photo upload, custom role designer, automatic bank/mobile-money funds transfer, statutory payroll tax engine or approved-payroll undo editor. A status or reference field alone does not implement those workflows. Keep these boundaries in mind when planning onboarding.

### 16.4 Trainer sign-off

Before independent work, the new user should be able to log in/activate/log out; identify their farm and role; navigate and find Help; distinguish personnel, employees and accounts; record the workflows appropriate to their role; interpret lifecycle states and blockers; find source history; print or export the supported outputs; and request help without exposing protected information.

Trainer: ____________________  New user: ____________________  Date: ____________________

Record the permitted modules and any remaining assisted procedures in your onboarding notes.
