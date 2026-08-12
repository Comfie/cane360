import assert from 'node:assert/strict';
import test from 'node:test';
import { getApiError } from './apiError.js';

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
