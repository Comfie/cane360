import type {
  ICreatePayrollPeriodRequest,
  ICreateWorkerAdvanceRequest,
  IDecidePayrollRunRequest,
  IIssueWorkerAdvanceRequest,
  IPreviewAdvanceScheduleRequest,
  PayrollCalculationDto,
  PayrollPeriodDto,
  PayrollRunDto,
  WorkerAdvanceDto,
} from '../../web-api-client';

type NumericInput = string | number;

interface AdvanceFormValues {
  workerId: string;
  amountUsd: NumericInput;
  reason: string;
  requestedEventDate: string;
  recoveryStartPayrollPeriodId: string;
  installmentCount: NumericInput;
}

interface BaseIssueFormValues {
  amountUsd: NumericInput;
  issuedAt: string;
}

type CashIssueFormValues = BaseIssueFormValues & {
  paymentMethod: 'Cash';
  payingPersonId: string;
  workerAcknowledged: boolean;
};

type MobileMoneyIssueFormValues = BaseIssueFormValues & {
  paymentMethod: 'MobileMoney';
  provider: string;
  recipientNumber: string;
  externalReference: string;
  transactionStatus: string;
};

type IssueFormValues = CashIssueFormValues | MobileMoneyIssueFormValues;

type PayrollPeriodSelection = Pick<PayrollPeriodDto, 'id' | 'status'>;
type PayrollRunSelection = Pick<PayrollRunDto, 'payrollPeriodId' | 'status'>;
type PayrollRunCalculation = Pick<PayrollCalculationDto, 'blockerCount' | 'evidenceCount'>;
type PayrollRunSubmission = Pick<PayrollRunDto, 'status'> & { calculation: PayrollRunCalculation | undefined };
type PayrollRunDecision = Pick<PayrollRunDto, 'version'> & { submittedCalculationVersion: number };
type AdvanceIssue = Pick<WorkerAdvanceDto, 'version'>;

function isRecord(value: unknown): value is Record<string, unknown> {
  return value !== null && typeof value === 'object';
}

export const defaultAdvanceForm: Readonly<AdvanceFormValues> = Object.freeze({
  workerId: '',
  amountUsd: '',
  reason: '',
  requestedEventDate: '',
  recoveryStartPayrollPeriodId: '',
  installmentCount: 3,
});

export function defaultPeriodId(periods: readonly PayrollPeriodSelection[], currentId = ''): string {
  if (currentId && periods.some((period) => period.id === currentId && period.status !== 'Cancelled')) return currentId;
  return periods.find((period) => period.status === 'Open')?.id
    ?? periods.find((period) => period.status === 'Draft')?.id
    ?? '';
}

export function canSubmitAdvance(role: string, status: string): boolean { return role === 'FarmManager' && status === 'Draft'; }
export function canDecideAdvance(role: string, status: string): boolean { return role === 'Grower' && status === 'PendingGrowerApproval'; }
export function canEditAdvance(status: string): boolean { return status === 'Draft' || status === 'Rejected'; }
export function canIssueAdvance(status: string): boolean { return status === 'Approved'; }
export function canCreatePayrollRun(role: string, period: PayrollPeriodSelection | undefined, runs: readonly PayrollRunSelection[]): boolean { return role === 'FarmManager' && period?.status === 'Open' && !runs.some((run) => run.payrollPeriodId === period.id && run.status !== 'Cancelled'); }
export function canCalculatePayrollRun(role: string, status: string): boolean { return role === 'FarmManager' && ['Draft', 'Calculated', 'Rejected'].includes(status); }
export function canSubmitPayrollRun(role: string, run: PayrollRunSubmission | undefined): boolean { return role === 'FarmManager' && run?.status === 'Calculated' && run.calculation?.blockerCount === 0 && run.calculation?.evidenceCount > 0; }
export function canDecidePayrollRun(role: string, status: string): boolean { return role === 'Grower' && status === 'PendingGrowerApproval'; }
export function canCancelPayrollRun(role: string, status: string): boolean { return role === 'FarmManager' && ['Draft', 'Calculated', 'Rejected'].includes(status); }
export function payrollDecisionPayload(run: PayrollRunDecision, approved: boolean, reason: string, idempotencyKey: string): IDecidePayrollRunRequest { return { expectedVersion: run.version, calculationVersion: run.submittedCalculationVersion, approved, reason: approved ? undefined : reason.trim(), idempotencyKey }; }

export function advancePayload(form: AdvanceFormValues, installmentPeriodIds: string[] = []): ICreateWorkerAdvanceRequest {
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

export function periodPayload(year: NumericInput, month: NumericInput): ICreatePayrollPeriodRequest { return { year: Number(year), month: Number(month) }; }
export function schedulePayload(form: Pick<AdvanceFormValues, 'amountUsd' | 'recoveryStartPayrollPeriodId' | 'installmentCount'>): IPreviewAdvanceScheduleRequest { return { amountUsd: Number(form.amountUsd), recoveryStartPayrollPeriodId: form.recoveryStartPayrollPeriodId, installmentCount: Number(form.installmentCount) }; }

export function issuePayload(advance: AdvanceIssue, form: IssueFormValues, idempotencyKey: string): IIssueWorkerAdvanceRequest {
  const paymentMethod = form.paymentMethod === 'Cash' ? 0 : 1;
  const shared = { expectedVersion: advance.version, paymentMethod, amountUsd: Number(form.amountUsd), issuedAt: new Date(form.issuedAt), idempotencyKey };
  if (form.paymentMethod === 'Cash') return { ...shared, payingPersonId: form.payingPersonId, workerAcknowledged: Boolean(form.workerAcknowledged), provider: undefined, recipientNumber: undefined, externalReference: undefined, transactionStatus: undefined };
  return { ...shared, payingPersonId: undefined, workerAcknowledged: undefined, provider: form.provider.trim(), recipientNumber: form.recipientNumber.trim(), externalReference: form.externalReference.trim(), transactionStatus: form.transactionStatus.trim() };
}

export function apiStatus(error: unknown): number {
  if (!isRecord(error)) return 0;
  const nestedStatus = isRecord(error.result)
    ? error.result.status
    : undefined;
  return Number(error.status ?? nestedStatus ?? 0);
}

export function payrollErrorMessage(error: unknown): string {
  const status = apiStatus(error);
  if (status === 403) return 'You do not have permission to perform this payroll action.';
  if (status === 404) return 'This payroll record is unavailable in your farm. Refresh and try again.';
  if (status === 409) return 'This record changed while you were working. The latest version has been refreshed; review it and retry.';
  return '';
}

export function newIdempotencyKey(prefix = 'p6a'): string {
  return `${prefix}-${globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`}`;
}
