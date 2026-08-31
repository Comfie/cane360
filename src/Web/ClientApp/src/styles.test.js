import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('date controls leave the browser calendar indicator unmodified', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');

  assert.doesNotMatch(styles, /::-webkit-calendar-picker-indicator/);
});

test('forms use the shared date picker instead of native date controls', async () => {
  const componentPaths = [
    './components/crop-cycles/CropCycleForm.tsx',
    './components/farm-setup/LineProfileForm.tsx',
    './components/farm-setup/PersonnelRegister.tsx',
    './components/pages/ActivitiesPage.tsx',
    './components/pages/CropCycleOverviewPage.tsx',
    './components/pages/LabourPage.tsx',
  ];

  for (const componentPath of componentPaths) {
    const source = await readFile(new URL(componentPath, import.meta.url), 'utf8');
    assert.doesNotMatch(source, /<input\b[^>]*\btype="(?:date|datetime-local)"/);
  }
});

test('labour dialog content keeps its fields and guidance inside the modal padding', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');

  assert.match(styles, /\.labour-form, \.worker-details \{[^}]*padding: var\(--space-5\);/);
  assert.match(styles, /\.labour-form \.form-grid label > small \{[^}]*display: block;/);
});

test('activity-type checkboxes keep their native control sizing and save-action spacing', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/pages/ActivitiesPage.tsx', import.meta.url), 'utf8');

  assert.match(styles, /\.form-grid :is\(input:not\(\[type="checkbox"\]/);
  assert.match(styles, /\.activity-form > form > button \{[^}]*margin: var\(--space-4\) 0 0;/);
  assert.match(styles, /\.toggle-control-track/);
  assert.match(component, /className="toggle-control"/);
  assert.doesNotMatch(component, /name="supports(?:Planned|Unplanned)" defaultChecked/);
  assert.match(component, /Select Planned, Unplanned, or both\./);
});

test('personnel creation and editing use a modal, with the primary-manager switch only for Farm managers', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/farm-setup/PersonnelRegister.tsx', import.meta.url), 'utf8');

  assert.match(component, /<dialog open className="activity-dialog personnel-dialog"/);
  assert.match(component, /role === 'FarmManager' && <label className="toggle-control personnel-primary-toggle">/);
  assert.match(component, /checked=\{isPrimaryManager\}/);
  assert.match(component, /setIsPrimaryManager\(false\)/);
  assert.match(component, /farmPersonnelPUT\(editingPerson\.id, new UpdatePersonRequest/);
  assert.match(component, /onClick=\{\(\) => openEditPerson\(person\)\}/);
  assert.match(component, /aria-label=\{`Edit \$\{person\.displayName\}`\}/);
  assert.match(styles, /\.personnel-primary-toggle \{[^}]*grid-column: 1 \/ -1;/);
  assert.match(styles, /\.personnel-form-actions \{[^}]*justify-content: flex-end;/);
});

test('farm information is edited in a modal with a compact summary action', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const summary = await readFile(new URL('./components/farm-setup/FarmSummary.tsx', import.meta.url), 'utf8');
  const editor = await readFile(new URL('./components/farm-setup/FarmProfileEditor.tsx', import.meta.url), 'utf8');

  assert.match(summary, /aria-label="Edit farm information"/);
  assert.match(editor, /<dialog open className="activity-dialog farm-profile-dialog"/);
  assert.match(editor, /farmSetupClient\.farmPUT\(new UpdateFarmInformationRequest/);
  assert.match(styles, /\.farm-summary-edit \{[^}]*width: 2\.25rem;/);
  assert.match(styles, /\.farm-profile-dialog \{[^}]*width: min\(54rem/);
});

test('record-evidence actions are separated from the final form field', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');

  assert.match(styles, /\.evidence-entry > button \{[^}]*margin-top: var\(--space-4\);/);
});

test('record-work activity selections use accessible checkbox controls', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/pages/LabourPage.tsx', import.meta.url), 'utf8');

  assert.match(styles, /\.activity-choice input:checked \+ \.activity-choice-box/);
  assert.match(component, /className="activity-choice"/);
});

test('evidence attestation controls use a dedicated action row', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');

  assert.match(styles, /\.evidence-action \{[^}]*grid-column: 1 \/ -1;/);
  assert.match(styles, /\.evidence-action button \{ white-space: nowrap;/);
});

test('evidence verification uses numbered step markers', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/pages/LabourPage.tsx', import.meta.url), 'utf8');

  assert.match(styles, /\.proof-strip span b \{[^}]*border-radius: 50%;/);
  assert.match(component, /<b>1<\/b><small>Entered<\/small>/);
});

test('shared errors use a dismissible responsive toast instead of an inline card', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/ValidationError.tsx', import.meta.url), 'utf8');

  assert.match(styles, /\.toast-region \{[^}]*position: fixed;/);
  assert.match(styles, /\.app-toast-dismiss \{[^}]*min-width: 2\.5rem;/);
  assert.match(component, /createPortal\(/);
  assert.match(component, /role="alert"/);
  assert.match(component, /aria-label="Dismiss error"/);
});
