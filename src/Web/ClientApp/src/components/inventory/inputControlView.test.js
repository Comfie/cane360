import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import test from 'node:test';
import {
  approvalLane,
  canDecideFieldLoss,
  estimatedCostLabel,
  reconciliation,
  remainingIssueQuantity,
  requiresLateConfirmationReason,
  returnStockWarning,
} from './inputControlView.js';

const workspaceSource = readFileSync(new URL('./InputControlsWorkspace.jsx', import.meta.url), 'utf8');

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

test('reconciliation strip exposes a blocking unaccounted quantity', () => {
  assert.deepEqual(reconciliation({
    issuedQuantity: 10, fieldReceivedQuantity: 10, confirmedAppliedQuantity: 4,
    postedReturnedQuantity: 3, approvedLossQuantity: 2,
  }), {
    issuedQuantity: 10, fieldReceivedQuantity: 10, confirmedAppliedQuantity: 4,
    postedReturnedQuantity: 3, approvedLossQuantity: 2, unaccountedQuantity: 1, isBlocking: true,
  });
  assert.equal(reconciliation({
    issuedQuantity: 10, fieldReceivedQuantity: 10, confirmedAppliedQuantity: 4,
    postedReturnedQuantity: 3, approvedLossQuantity: 3,
  }).isBlocking, false);
});

test('late confirmation reason appears only beyond the exact forty-eight-hour boundary', () => {
  assert.equal(requiresLateConfirmationReason(48), false);
  assert.equal(requiresLateConfirmationReason(48.000001), true);
});

test('field-loss decision and store-return warning remain role and status specific', () => {
  assert.equal(canDecideFieldLoss('FarmManager'), false);
  assert.equal(canDecideFieldLoss('Grower'), true);
  assert.match(returnStockWarning('Draft'), /does not restore stock/);
  assert.match(returnStockWarning('Posted'), /has been restored/);
});

test('compact workflow keeps partial field receipt and application coverage capture available', () => {
  assert.match(workspaceSource, /function FieldReceiptForm/);
  assert.match(workspaceSource, /Partial quantities received in the field/);
  assert.match(workspaceSource, /function ApplicationForm/);
  assert.match(workspaceSource, /Verified coverage/);
});

test('compact workflow keeps supervisor attestation and manager confirmation visibly separate', () => {
  assert.match(workspaceSource, /1\. Supervisor attestation/);
  assert.match(workspaceSource, /2\. Manager confirmation/);
  assert.match(workspaceSource, /Late-confirmation reason/);
});

test('compact workflow exposes store-received return and Grower-only loss decision controls', () => {
  assert.match(workspaceSource, /A return restores stock only when this Store-received posting succeeds/);
  assert.match(workspaceSource, /workspace\.session\.role !== 'Grower'/);
  assert.match(workspaceSource, /Blocking condition: record a confirmed application/);
});
