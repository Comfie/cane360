interface ApprovalRequest {
  requiresGrower: boolean;
}

interface ReconciliationRow {
  issuedQuantity: number;
  fieldReceivedQuantity: number;
  confirmedAppliedQuantity: number;
  postedReturnedQuantity: number;
  approvedLossQuantity: number;
}

export function approvalLane(request: ApprovalRequest, sessionRole: string): { label: string; canApprove: boolean } {
  if (request.requiresGrower) return { label: 'Grower approval required', canApprove: sessionRole === 'Grower' };
  return { label: 'FarmManager or Grower', canApprove: ['FarmManager', 'Grower'].includes(sessionRole) };
}

export function remainingIssueQuantity(approved: number, issued: number): number {
  return Math.max(0, approved - issued);
}

export function estimatedCostLabel(value: number | undefined | null): string {
  return value == null ? 'Estimated cost not available' : new Intl.NumberFormat('en-US', {
    style: 'currency', currency: 'USD', minimumFractionDigits: 2, maximumFractionDigits: 2,
  }).format(value);
}

export function reconciliation<T extends ReconciliationRow>(row: T): T & { unaccountedQuantity: number; isBlocking: boolean } {
  const unaccounted = Math.max(0, row.issuedQuantity - row.confirmedAppliedQuantity
    - row.postedReturnedQuantity - row.approvedLossQuantity);
  return { ...row, unaccountedQuantity: unaccounted, isBlocking: unaccounted > 0 };
}

export function requiresLateConfirmationReason(hoursAfterWork: number): boolean {
  return hoursAfterWork > 48;
}

export function canDecideFieldLoss(sessionRole: string): boolean {
  return sessionRole === 'Grower';
}

export function returnStockWarning(status: string): string {
  return status === 'Posted'
    ? 'Store receipt posted: stock has been restored at the locked issue cost.'
    : 'A return does not restore stock until received and posted by the Store.';
}
