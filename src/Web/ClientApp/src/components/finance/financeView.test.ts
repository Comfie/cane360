import assert from 'node:assert/strict';
import test from 'node:test';
import { financeLabel, perUnit, usd } from './financeView.ts';

test('missing cost denominators are not displayed as zero dollars', () => {
  assert.equal(perUnit(undefined, 'ha'), 'Not available');
  assert.equal(perUnit(undefined, 't'), 'Not available');
});

test('finance values use explicit USD formatting', () => {
  assert.equal(usd(1250.5), '$1,250.50');
  assert.equal(financeLabel('DirectExpense'), 'Direct Expense');
});
