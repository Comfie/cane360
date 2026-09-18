import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import { FinanceClient, type CreateOperationalTransactionRequest } from '../../web-api-client.ts';
import { canApproveBudget, canEditBudget, canReviseBudget, canSubmitBudget, financeLabel, perUnit, usd, variancePercent } from './financeView.ts';

const financePageSource = readFileSync(new URL('../pages/FinancePage.tsx', import.meta.url), 'utf8');

test('created finance transaction resolves from a 201 response', async () => {
  const id = '00000000-0000-0000-0000-000000000001';
  const client = new FinanceClient('', {
    fetch: async () => new Response(JSON.stringify({ id }), { status: 201 }),
  });

  const result = await client.createFinanceTransaction({} as CreateOperationalTransactionRequest);

  assert.equal(result.id, id);
});

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

test('transaction register uses bounded server pages and authoritative filtered totals', () => {
  assert.match(financePageSource, /getFinanceTransactions\([\s\S]*page, 50\)/);
  assert.match(financePageSource, /setTransactions\(result\.items\)/);
  assert.match(financePageSource, /setTransactionTotals\(result\)/);
  assert.match(financePageSource, /Math\.ceil\(totals\.totalCount \/ 50\)/);
  assert.match(financePageSource, /onPage\(page \+ 1\)/);
});
