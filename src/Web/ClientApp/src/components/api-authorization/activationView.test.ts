import assert from 'node:assert/strict';
import test from 'node:test';
import { postLoginDestination, roleNavigationIds, invitationRoleOptions } from './activationView.ts';

test('routes an unlinked account to activation', () => {
  assert.equal(postLoginDestination({ hasTenant: false }, '/farm'), '/activate');
});

test('honours a safe return url for a linked account', () => {
  assert.equal(postLoginDestination({ hasTenant: true }, '/farm'), '/farm');
  assert.equal(postLoginDestination({ hasTenant: true }, '//evil.com'), '/');
  assert.equal(postLoginDestination({ hasTenant: true }, undefined), '/');
});

test('scopes navigation for a Supervisor', () => {
  assert.deepEqual(roleNavigationIds('Supervisor'), ['dashboard', 'fields', 'activities']);
  assert.ok(roleNavigationIds('Grower').includes('administration'));
  assert.deepEqual(roleNavigationIds(null), ['dashboard']);
});

test('offers only invitable roles', () => {
  assert.deepEqual(invitationRoleOptions().map((option) => option.value), ['FarmManager', 'Supervisor']);
});
