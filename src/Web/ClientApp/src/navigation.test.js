import assert from 'node:assert/strict';
import test from 'node:test';
import { protectedNavigation } from './navigation.js';

const expectedRoutes = [
  '/',
  '/farm',
  '/fields',
  '/activities',
  '/labour',
  '/inventory',
  '/finance',
  '/reports',
  '/administration',
];

test('declares every protected Cane360 destination once', () => {
  assert.deepEqual(protectedNavigation.map((item) => item.path), expectedRoutes);
  assert.equal(new Set(protectedNavigation.map((item) => item.id)).size, expectedRoutes.length);
  assert.ok(protectedNavigation.every((item) => item.label && item.description));
});
