import assert from 'node:assert/strict';
import test from 'node:test';
import { canApproveBudget, canEditBudget, canReviseBudget, canSubmitBudget, financeLabel, perUnit, usd, variancePercent } from './financeView.ts';

test('missing cost denominators are not displayed as zero dollars', () => {
  assert.equal(perUnit(undefined, 'ha'), 'Not available');
  assert.equal(perUnit(undefined, 't'), 'Not available');
});

test('finance values use explicit USD formatting', () => {
  assert.equal(usd(1250.5), '$1,250.50');
  assert.equal(financeLabel('DirectExpense'), 'Direct Expense');
});

test('budget workflow separates draft preparation from Grower approval', () => {
  assert.equal(canEditBudget('Draft'), true);
  assert.equal(canSubmitBudget('FarmManager', 'Draft'), true);
  assert.equal(canApproveBudget('FarmManager', 'Submitted'), false);
  assert.equal(canApproveBudget('Grower', 'Submitted'), true);
  assert.equal(canReviseBudget('Grower', 'Approved'), true);
});

test('zero-budget variance percent stays unavailable', () => {
  assert.equal(variancePercent(undefined), 'Not available');
  assert.equal(variancePercent(12.5), '+12.50%');
});
