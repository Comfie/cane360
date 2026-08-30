import assert from 'node:assert/strict';
import test from 'node:test';
import { cycleGroup, filterCycles, flattenCycleCollections, formatCycleStatus } from './cropCycleView.ts';

test('cycle states map to honest current and historical groups', () => {
  assert.equal(cycleGroup('Active'), 'current');
  assert.equal(cycleGroup('ReadyForHarvest'), 'current');
  assert.equal(cycleGroup('Harvested'), 'awaiting-close');
  assert.equal(cycleGroup('Closed'), 'history');
  assert.equal(cycleGroup('Cancelled'), 'history');
  assert.equal(formatCycleStatus('ReadyForHarvest'), 'Ready for harvest');
});

test('cycle filter does not treat harvested as closed history', () => {
  const cycles = [
    { id: 'draft', status: 'Draft' },
    { id: 'active', status: 'Active' },
    { id: 'harvested', status: 'Harvested' },
    { id: 'closed', status: 'Closed' },
  ];

  assert.deepEqual(filterCycles(cycles, 'current').map((cycle) => cycle.id), ['active']);
  assert.deepEqual(filterCycles(cycles, 'awaiting-close').map((cycle) => cycle.id), ['harvested']);
  assert.deepEqual(filterCycles(cycles, 'history').map((cycle) => cycle.id), ['closed']);
});

test('field collections flatten into reverse chronological order', () => {
  const collections = [
    {
      field: { id: 'field-a' },
      cropCycles: [
        { id: 'older', status: 'Closed', startDate: '2024-06-01' },
        { id: 'newer', status: 'Active', startDate: '2026-08-01' },
      ],
    },
  ];

  assert.deepEqual(flattenCycleCollections(collections).map((entry) => entry.cropCycle.id), ['newer', 'older']);
});
