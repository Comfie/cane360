import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('date controls leave the browser calendar indicator unmodified', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');

  assert.doesNotMatch(styles, /::-webkit-calendar-picker-indicator/);
});

test('forms use the shared date picker instead of native date controls', async () => {
  const componentPaths = [
    './components/crop-cycles/CropCycleForm.jsx',
    './components/farm-setup/LineProfileForm.jsx',
    './components/farm-setup/PersonnelRegister.jsx',
    './components/pages/ActivitiesPage.jsx',
    './components/pages/CropCycleOverviewPage.jsx',
    './components/pages/LabourPage.jsx',
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

test('shared errors use a dismissible responsive toast instead of an inline card', async () => {
  const styles = await readFile(new URL('./styles.scss', import.meta.url), 'utf8');
  const component = await readFile(new URL('./components/ValidationError.jsx', import.meta.url), 'utf8');

  assert.match(styles, /\.toast-region \{[^}]*position: fixed;/);
  assert.match(styles, /\.app-toast-dismiss \{[^}]*min-width: 2\.5rem;/);
  assert.match(component, /createPortal\(/);
  assert.match(component, /role="alert"/);
  assert.match(component, /aria-label="Dismiss error"/);
});
