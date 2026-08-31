import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';

test('existing forms use the shared picker instead of native date controls', async () => {
  const componentPaths = [
    './crop-cycles/CropCycleForm.tsx',
    './farm-setup/LineProfileForm.tsx',
    './farm-setup/PersonnelRegister.tsx',
    './pages/ActivitiesPage.tsx',
    './pages/CropCycleOverviewPage.tsx',
  ];

  for (const componentPath of componentPaths) {
    const source = await readFile(new URL(componentPath, import.meta.url), 'utf8');
    assert.doesNotMatch(source, /<input\b[^>]*\btype="(?:date|datetime-local)"/);
  }
});

test('the shared picker does not expose a native date input', async () => {
  const source = await readFile(new URL('./DatePicker.tsx', import.meta.url), 'utf8');

  assert.doesNotMatch(source, /<input\b[^>]*\btype="(?:date|datetime-local)"/);
});

test('the shared picker provides direct month and year controls', async () => {
  const source = await readFile(new URL('./DatePicker.tsx', import.meta.url), 'utf8');

  assert.match(source, /aria-label="Month"/);
  assert.match(source, /aria-label="Year"/);
});

test('the shared picker renders its popover outside scrollable form containers', async () => {
  const source = await readFile(new URL('./DatePicker.tsx', import.meta.url), 'utf8');

  assert.match(source, /createPortal\(/);
  assert.match(source, /document\.body/);
  assert.match(source, /document\.addEventListener\('scroll', positionPopover, true\)/);
});
