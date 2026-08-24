import assert from 'node:assert/strict';
import test from 'node:test';
import { approvalLane, estimatedCostLabel, remainingIssueQuantity } from './inputControlView.js';

test('keeps Grower-required approvals unavailable to FarmManagers', () => {
  assert.deepEqual(approvalLane({ requiresGrower: true }, 'FarmManager'), {
    label: 'Grower approval required', canApprove: false,
  });
  assert.equal(approvalLane({ requiresGrower: false }, 'FarmManager').canApprove, true);
});

test('shows missing cost as unavailable instead of zero', () => {
  assert.equal(estimatedCostLabel(undefined), 'Estimated cost not available');
  assert.equal(estimatedCostLabel(0), '$0.00');
});

test('partial issue remaining quantity never renders negative', () => {
  assert.equal(remainingIssueQuantity(100, 40), 60);
  assert.equal(remainingIssueQuantity(100, 120), 0);
});
