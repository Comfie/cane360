import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import { formatActivityStatus, groupActivitiesByDate, monthGridDates, orderedActions, quantityLabel } from './activityView.ts';

test('formats lifecycle and quantity labels for the compact UI', () => {
  assert.equal(formatActivityStatus('AwaitingVerification'), 'Awaiting verification');
  assert.equal(quantityLabel('Hectares'), 'Actual coverage (ha)');
  assert.equal(quantityLabel('StandardLines'), 'Completed standard lines');
});

test('orders only allowed lifecycle actions', () => {
  assert.deepEqual(orderedActions(['Cancelled', 'InProgress']), ['InProgress', 'Cancelled']);
});

test('groups actual work ahead of planned dates for diary placement', () => {
  const activities = [
    { id: '1', plannedDate: '2026-08-12' },
    { id: '2', plannedDate: '2026-08-12', actualAt: '2026-08-13T08:00:00Z' },
  ];
  const groups = groupActivitiesByDate(activities);
  assert.equal(groups['2026-08-12'].length, 1);
  assert.equal(groups['2026-08-13'].length, 1);
});

test('actual-work entry defaults to the current Harare minute, not noon', async () => {
  const source = await readFile(new URL('../pages/ActivitiesPage.tsx', import.meta.url), 'utf8');

  assert.match(source, /\|\| harareNow\(\)/);
  assert.doesNotMatch(source, /\$\{harareToday\(\)\}T12:00/);
});

test('places dates in a Monday-first desktop month grid', () => {
  const dates = monthGridDates(2026, 7);
  assert.equal(dates.length, 42);
  assert.equal(dates[5], '2026-08-01');
  assert.equal(dates[35], '2026-08-31');
});
