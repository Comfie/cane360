import assert from 'node:assert/strict';
import test from 'node:test';
import { FinanceClient, MillRecordsClient, WorkersClient } from '../web-api-client.ts';
import { getApiError } from './apiError.ts';

test('generated create clients accept 201 responses', async () => {
  const id = '00000000-0000-0000-0000-000000000001';
  const http = { fetch: async () => new Response(JSON.stringify({ id, worker: { id } }), { status: 201 }) };
  const finance = new FinanceClient('', http);
  const millRecords = new MillRecordsClient('', http);
  const workers = new WorkersClient('', http);

  for (const create of [
    () => finance.createFinanceBudget({} as never),
    () => millRecords.createWeighbridgeTicket({} as never),
    () => millRecords.createGrowerStatement({} as never),
    () => workers.createWorkers({} as never),
  ]) {
    assert.ok(await create());
  }
});

test('extracts validation messages from generated client results', () => {
  assert.equal(getApiError({
    errors: { transition: ['Unplanned activity requires actual work before it can be planned.'] },
    title: 'One or more validation errors occurred.',
  }), 'Unplanned activity requires actual work before it can be planned.');
});

test('extracts validation messages from raw Swagger responses', () => {
  assert.equal(getApiError({
    response: JSON.stringify({ errors: { quantity: ['Coverage exceeds the field reporting hectares.'] } }),
    result: null,
  }), 'Coverage exceeds the field reporting hectares.');
});

test('falls back to conflict detail before generic titles', () => {
  assert.equal(getApiError({ detail: 'Refresh the activity and try again.', title: 'Conflict' }),
    'Refresh the activity and try again.');
});
test('unexpected failures show only safe guidance and a support reference', () => {
  assert.equal(getApiError({ status: 500, result: { detail: 'PRIVATE_SQL_DETAIL', traceId: 'AUTOTEST-P8A' } }),
    'Cane360 could not complete the request. Refresh to check whether the action completed before trying again. Support reference: AUTOTEST-P8A.');
});

test('foreign record errors do not echo internal identifiers', () => {
  assert.equal(getApiError({ status: 404, result: { detail: 'foreign-record-id' } }),
    'This record is unavailable in your current workspace. Refresh the list and try again.');
});
