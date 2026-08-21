import {
  AttendanceClient,
  AttendanceEntryRequest,
  ConfirmWorkRecordRequest,
  CreateWorkerRateRequest,
  CreateWorkerRequest,
  CreateWorkRecordRequest,
  RecordAttendanceRequest,
  VerifyWorkRecordRequest,
  WorkerRatesClient,
  WorkersClient,
  WorkRecordsClient,
  WorkScopeRequest,
} from '../../web-api-client';
import { dateOnly } from './labourView';

export const workersClient = new WorkersClient();
export const ratesClient = new WorkerRatesClient();
export const attendanceClient = new AttendanceClient();
export const workRecordsClient = new WorkRecordsClient();

/** @param {{displayName: string, phone?: string, employmentType: string, activeFrom: string, nationalId: string}} values */
export function createWorker(values) {
  return workersClient.workersPOST(new CreateWorkerRequest({
    personId: undefined,
    displayName: values.displayName,
    phone: values.phone,
    employmentType: values.employmentType,
    activeFrom: dateOnly(values.activeFrom),
    nationalId: values.nationalId,
  }));
}

/** @param {string} workerId @param {{basis: string, activityTypeId?: string, rateUsd: number, effectiveFrom: string, effectiveTo?: string}} values */
export function createRate(workerId, values) {
  return ratesClient.rates(workerId, new CreateWorkerRateRequest({
    basis: values.basis,
    activityTypeId: values.activityTypeId,
    rateUsd: values.rateUsd,
    effectiveFrom: dateOnly(values.effectiveFrom),
    effectiveTo: values.effectiveTo ? dateOnly(values.effectiveTo) : undefined,
  }));
}

/** @param {string} date */
export function getAttendance(date) { return attendanceClient.attendanceGET(dateOnly(date)); }

/** @param {string} date @param {string | undefined} lateReason @param {{workerId: string, status: string, fieldId?: string, expectedVersion?: number}[]} entries */
export function saveAttendance(date, lateReason, entries) {
  return attendanceClient.attendancePUT(new RecordAttendanceRequest({
    workDate: dateOnly(date),
    lateEntryReason: lateReason,
    entries: entries.map((entry) => new AttendanceEntryRequest({
      workerId: entry.workerId,
      status: entry.status,
      fieldId: entry.fieldId,
      expectedVersion: entry.expectedVersion,
    })),
  }));
}

/** @param {{workerId: string, workDate: string, payBasis: string, activityIds: string[], quantity?: number, scope?: {type: string, startLine?: number, endLine?: number, sectionName?: string}, lateEntryReason?: string}} values */
export function createWorkRecord(values) {
  return workRecordsClient.workRecords(new CreateWorkRecordRequest({
    workerId: values.workerId,
    payBasis: values.payBasis,
    activityIds: values.activityIds,
    quantity: values.quantity,
    lateEntryReason: values.lateEntryReason,
    workDate: dateOnly(values.workDate),
    scope: values.scope ? new WorkScopeRequest({
      type: values.scope.type,
      startLine: values.scope.startLine,
      endLine: values.scope.endLine,
      sectionName: values.scope.sectionName,
    }) : undefined,
  }));
}

/** @param {string} workRecordId @param {string} supervisorPersonId @param {number} expectedVersion */
export function verifyWork(workRecordId, supervisorPersonId, expectedVersion) {
  return workRecordsClient.supervisorVerification(workRecordId, new VerifyWorkRecordRequest({ supervisorPersonId, expectedVersion }));
}

/** @param {string} workRecordId @param {number} expectedVersion */
export function confirmWork(workRecordId, expectedVersion) {
  return workRecordsClient.managerConfirmation2(workRecordId, new ConfirmWorkRecordRequest({ expectedVersion }));
}
