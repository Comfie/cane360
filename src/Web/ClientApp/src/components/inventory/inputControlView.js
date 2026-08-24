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
