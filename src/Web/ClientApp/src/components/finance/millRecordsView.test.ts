import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { amountReconciliation, canCorrectMillEvidence, matchWarning, tonnes } from './millRecordsView.ts';

test('tonnage keeps mill precision without inventing decimals', () => {
  assert.equal(tonnes(12.345), '12.345 t');
});

test('missing matched amount stays unavailable instead of becoming zero', () => {
  assert.equal(amountReconciliation(undefined, 'NotAvailable', String), 'Not Available');
});

test('captured matched amount uses the supplied USD formatter', () => {
  assert.equal(amountReconciliation(25, 'Matched', (value) => `$${value}.00`), '$25.00');
});

test('recorded evidence correction remains Grower-only', () => {
  assert.equal(canCorrectMillEvidence('Grower', 'Recorded'), true);
  assert.equal(canCorrectMillEvidence('FarmManager', 'Recorded'), false);
  assert.equal(canCorrectMillEvidence('Grower', 'Draft'), false);
});

test('cross-statement ticket reuse is surfaced explicitly', () => {
  assert.match(matchWarning(true) ?? '', /another statement/);
  assert.equal(matchWarning(false), undefined);
});

test('workspace uses only generated client mutations', async () => {
  const source = await readFile(new URL('./MillRecordsWorkspace.tsx', import.meta.url), 'utf8');
  assert.match(source, /new MillRecordsClient\(\)/);
  assert.doesNotMatch(source, /fetch\s*\(/);
});

test('workspace preserves responsive cards and stacked matching hooks', async () => {
  const styles = await readFile(new URL('../../styles.scss', import.meta.url), 'utf8');
  assert.match(styles, /\.mill-ticket-row/);
  assert.match(styles, /\.match-form \{ grid-template-columns: 1fr; \}/);
});

test('grower statement editor uses the fixed dialog backdrop', async () => {
  const source = await readFile(new URL('./MillRecordsWorkspace.tsx', import.meta.url), 'utf8');
  const styles = await readFile(new URL('../../styles.scss', import.meta.url), 'utf8');
  assert.match(source, /statementEditor && <Editor title=/);
  assert.match(source, /className="dialog-backdrop" role="presentation"/);
  assert.match(source, /className="mill-editor-dialog finance-dialog" role="dialog" aria-modal="true"/);
  assert.match(styles, /\.mill-editor-dialog \{[^}]*max-height:[^}]*overflow-y: auto/);
});

test('statement evidence remains distinct from operational income', async () => {
  const source = await readFile(new URL('./MillRecordsWorkspace.tsx', import.meta.url), 'utf8');
  assert.match(source, /not income or settlement/i);
  assert.doesNotMatch(source, /createFinanceTransaction/);
});
