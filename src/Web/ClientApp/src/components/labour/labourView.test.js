import assert from 'node:assert/strict';
import test from 'node:test';
import { activitySelectionError, dateOnly, evidenceAmount, label } from './labourView.js';

test('monthly evidence stays deferred instead of inventing proration', () => {
  assert.equal(evidenceAmount('Monthly', undefined, undefined), 'Deferred to payroll');
});

test('confirmed piece evidence displays the snapshotted amount', () => {
  assert.equal(evidenceAmount('StandardLine', 12.5, 5), '$12.50');
  assert.equal(label('TaskBased'), 'Task Based');
});

test('date-only values retain the selected calendar date near UTC boundaries', () => {
  assert.equal(dateOnly('2026-08-18'), '2026-08-18');
  assert.equal(dateOnly('2026-12-31'), '2026-12-31');
  assert.throws(() => dateOnly('2026-08-18T00:00:00.000Z'), /yyyy-MM-dd/);
});

test('work evidence requires at least one allocated-field activity before submission', () => {
  assert.equal(activitySelectionError([]), 'Select at least one activity on the allocated field before recording evidence.');
  assert.equal(activitySelectionError(['activity-1']), undefined);
});
