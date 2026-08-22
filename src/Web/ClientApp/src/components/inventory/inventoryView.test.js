import test from 'node:test';
import assert from 'node:assert/strict';
import { duplicateReceiptReference, inventoryLabel, isoDate, quantity } from './inventoryView.js';

test('inventory labels split compact enum names', () => {
  assert.equal(inventoryLabel('SeedAndPlantingMaterial'), 'Seed And Planting Material');
});

test('item stock quantities retain decimal precision', () => {
  assert.equal(quantity(12.345679, 'KG'), '12.345679 KG');
});

test('dates preserve DateOnly wire shape', () => {
  assert.equal(isoDate('2026-08-22'), '2026-08-22');
  assert.throws(() => isoDate('22/08/2026'));
});

test('duplicate supplier references are detected without global assumptions', () => {
  const duplicate = duplicateReceiptReference(/** @type {any} */ ([
    { id: 'r1', supplierId: 's1', sourceReference: ' GRN-42 ' },
  ]), 's1', 'grn-42');
  assert.equal(duplicate?.id, 'r1');
  assert.equal(duplicateReceiptReference([], 's1', 'grn-42'), undefined);
});
