// @ts-nocheck
export const defaultAdvanceForm = Object.freeze({ workerId: '', amountUsd: '', reason: '', requestedEventDate: '', recoveryStartPayrollPeriodId: '', installmentCount: 3 });

export function defaultPeriodId(periods, currentId = '') {
  if (currentId && periods.some((period) => period.id === currentId && period.status !== 'Cancelled')) return currentId;
  return periods.find((period) => period.status === 'Open')?.id
    ?? periods.find((period) => period.status === 'Draft')?.id
    ?? '';
}

export function canSubmitAdvance(role, status) { return role === 'FarmManager' && status === 'Draft'; }
export function canDecideAdvance(role, status) { return role === 'Grower' && status === 'PendingGrowerApproval'; }
export function canEditAdvance(status) { return status === 'Draft' || status === 'Rejected'; }
export function canIssueAdvance(status) { return status === 'Approved'; }

export function advancePayload(form, installmentPeriodIds = []) {
  return {
    workerId: form.workerId,
    amountUsd: Number(form.amountUsd),
    reason: form.reason.trim(),
    requestedEventDate: form.requestedEventDate,
    recoveryStartPayrollPeriodId: form.recoveryStartPayrollPeriodId,
    installmentCount: Number(form.installmentCount),
    installmentPeriodIds,
  };
}

export function periodPayload(year, month) { return { year: Number(year), month: Number(month) }; }
export function schedulePayload(form) { return { amountUsd: Number(form.amountUsd), recoveryStartPayrollPeriodId: form.recoveryStartPayrollPeriodId, installmentCount: Number(form.installmentCount) }; }

export function issuePayload(advance, form, idempotencyKey) {
  const paymentMethod = form.paymentMethod === 'Cash' ? 0 : 1;
  const shared = { expectedVersion: advance.version, paymentMethod, amountUsd: Number(form.amountUsd), issuedAt: new Date(form.issuedAt), idempotencyKey };
  if (form.paymentMethod === 'Cash') return { ...shared, payingPersonId: form.payingPersonId, workerAcknowledged: Boolean(form.workerAcknowledged), provider: undefined, recipientNumber: undefined, externalReference: undefined, transactionStatus: undefined };
  return { ...shared, payingPersonId: undefined, workerAcknowledged: undefined, provider: form.provider.trim(), recipientNumber: form.recipientNumber.trim(), externalReference: form.externalReference.trim(), transactionStatus: form.transactionStatus.trim() };
}

export function apiStatus(error) {
  if (!error || typeof error !== 'object') return 0;
  return Number(error.status ?? error.result?.status ?? 0);
}

export function payrollErrorMessage(error) {
  const status = apiStatus(error);
  if (status === 403) return 'You do not have permission to perform this payroll action.';
  if (status === 404) return 'This payroll record is unavailable in your farm. Refresh and try again.';
  if (status === 409) return 'This record changed while you were working. The latest version has been refreshed; review it and retry.';
  return '';
}

export function newIdempotencyKey(prefix = 'p6a') {
  return `${prefix}-${globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`}`;
}
