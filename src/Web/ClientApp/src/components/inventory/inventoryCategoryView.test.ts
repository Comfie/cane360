import test from 'node:test';
import assert from 'node:assert/strict';
import {activeCategories, categoryLabel} from './inventoryCategoryView.ts';
import {readFile} from 'node:fs/promises';

const categories = [
  {id: 'a', code: 'Fertiliser', name: 'Fertiliser & Soil Amendments', active: false, displayOrder: 0},
  {id: 'b', code: 'FUEL', name: 'Fuel', active: true, displayOrder: 5},
  {id: 'c', code: 'PPE', name: 'PPE', active: true, displayOrder: 2},
];

test('inactive category excluded from new item assignments', () => {
  assert.deepEqual(activeCategories(categories).map(category => category.code), ['PPE', 'FUEL']);
});
test('existing inactive category displays current name and inactive state', () => {
  assert.equal(categoryLabel(categories, 'Fertiliser'), 'Fertiliser & Soil Amendments (inactive)');
});
test('category sorting does not mutate server state', () => {
  activeCategories(categories);
  assert.deepEqual(categories.map(category => category.id), ['a', 'b', 'c']);
});
test('reactivation makes category assignable without changing identity', () => {
  const reactivated = categories.map(category => ({...category, active: true}));
  assert.equal(activeCategories(reactivated)[0].id, 'a');
  assert.equal(categoryLabel(reactivated, 'Fertiliser'), 'Fertiliser & Soil Amendments');
});
test('missing legacy category remains readable by its stable code', () => {
  assert.equal(categoryLabel([], 'Other'), 'Other');
});
test('category UI gates administration and preserves immutable code', async () => {
  const source = await readFile(new URL('../pages/InventoryPage.tsx', import.meta.url), 'utf8');
  assert.match(source, /canManage && <button[^>]*[\s\S]*?Add category/);
  assert.match(source, /readOnly=\{!!category\}/);
  assert.match(source, /activeCategories\(categories\)/);
  assert.match(source, /categoryLabel\(workspace.categories, item.category\)/);
});
test('deactivation confirmation explains retained history', async () => {
  const source = await readFile(new URL('../pages/InventoryPage.tsx', import.meta.url), 'utf8');
  assert.match(source, /window.confirm\(`Deactivate[\s\S]*?Existing items and stock history remain available/);
});
