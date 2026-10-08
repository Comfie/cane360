import assert from 'node:assert/strict';
import test from 'node:test';
import {invitationRoleOptions, postLoginDestination, roleLabel, roleNavigationIds} from './activationView.ts';
import {protectedNavigation} from '../../navigation.ts';

test('routes an unlinked account to activation', () => {
    assert.equal(postLoginDestination({hasTenant: false}, '/farm'), '/activate');
});

test('honours a safe return url for a linked account', () => {
    assert.equal(postLoginDestination({hasTenant: true}, '/farm'), '/farm');
    assert.equal(postLoginDestination({hasTenant: true}, '//evil.com'), '/');
    assert.equal(postLoginDestination({hasTenant: true}, undefined), '/');
});

test('scopes navigation for a Supervisor', () => {
    assert.deepEqual(roleNavigationIds('Supervisor'), ['dashboard', 'fields', 'activities', 'administration']);
    assert.ok(roleNavigationIds('Grower').includes('administration'));
    assert.deepEqual(roleNavigationIds(null), ['dashboard']);
});

test('derives full navigation from the shared navigation list so it cannot drift', () => {
    const expected = protectedNavigation.map((item) => item.id);
    assert.deepEqual(roleNavigationIds('Grower'), expected);
    assert.deepEqual(roleNavigationIds('FarmManager'), expected);
});

test('offers only invitable roles', () => {
    assert.deepEqual(invitationRoleOptions().map((option) => option.value), ['FarmManager', 'Supervisor']);
});

test('shows recognisable names for assigned roles and does not assume an owner role', () => {
    assert.equal(roleLabel('Grower'), 'Farm Owner (Grower)');
    assert.equal(roleLabel('FarmManager'), 'Farm Manager');
    assert.equal(roleLabel('Supervisor'), 'Supervisor');
    assert.equal(roleLabel(null), 'No farm role assigned');
    assert.equal(roleLabel('unknown'), 'No farm role assigned');
});
