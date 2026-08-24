/** @param {{requiresGrower: boolean}} request @param {string} sessionRole */
export function approvalLane(request, sessionRole) {
  if (request.requiresGrower) return { label: 'Grower approval required', canApprove: sessionRole === 'Grower' };
  return { label: 'FarmManager or Grower', canApprove: ['FarmManager', 'Grower'].includes(sessionRole) };
}

/** @param {number} approved @param {number} issued */
export function remainingIssueQuantity(approved, issued) {
  return Math.max(0, approved - issued);
}

/** @param {number | undefined | null} value */
export function estimatedCostLabel(value) {
  return value == null ? 'Estimated cost not available' : new Intl.NumberFormat('en-US', {
    style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2,
  }).format(value);
}

/** @param {{issuedQuantity: number, fieldReceivedQuantity: number, confirmedAppliedQuantity: number, postedReturnedQuantity: number, approvedLossQuantity: number}} row */
export function reconciliation(row) {
  const unaccounted = Math.max(0, row.issuedQuantity - row.confirmedAppliedQuantity
    - row.postedReturnedQuantity - row.approvedLossQuantity);
  return { ...row, unaccountedQuantity: unaccounted, isBlocking: unaccounted > 0 };
}

/** @param {number} hoursAfterWork */
export function requiresLateConfirmationReason(hoursAfterWork) {
  return hoursAfterWork > 48;
}

/** @param {string} sessionRole */
export function canDecideFieldLoss(sessionRole) {
  return sessionRole === 'Grower';
}

/** @param {string} status */
export function returnStockWarning(status) {
  return status === 'Posted'
    ? 'Store receipt posted: stock has been restored at the locked issue cost.'
    : 'A return does not restore stock until received and posted by the Store.';
}
