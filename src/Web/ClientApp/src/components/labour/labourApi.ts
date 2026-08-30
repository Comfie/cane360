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
import type { IAttendanceEntryRequest, ICreateWorkerRateRequest, ICreateWorkerRequest, ICreateWorkRecordRequest, IWorkScopeRequest } from '../../web-api-client';
import { dateOnly } from './labourView';

export const workersClient = new WorkersClient();
export const ratesClient = new WorkerRatesClient();
export const attendanceClient = new AttendanceClient();
export const workRecordsClient = new WorkRecordsClient();

type CreateWorkerValues = Pick<ICreateWorkerRequest, 'displayName' | 'phone' | 'employmentType' | 'activeFrom' | 'nationalId'>;
type CreateRateValues = Pick<ICreateWorkerRateRequest, 'basis' | 'activityTypeId' | 'rateUsd' | 'effectiveFrom' | 'effectiveTo'>;
type AttendanceEntryValues = Pick<IAttendanceEntryRequest, 'workerId' | 'status' | 'fieldId' | 'expectedVersion'>;
type WorkScopeValues = Pick<IWorkScopeRequest, 'type' | 'startLine' | 'endLine' | 'sectionName'>;
type CreateWorkRecordValues = Omit<ICreateWorkRecordRequest, 'scope'> & { scope?: WorkScopeValues };

export function createWorker(values: CreateWorkerValues) {
  return workersClient.workersPOST(new CreateWorkerRequest({
    personId: undefined,
    displayName: values.displayName,
    phone: values.phone,
    employmentType: values.employmentType,
    activeFrom: dateOnly(values.activeFrom),
    nationalId: values.nationalId,
  }));
}

export function createRate(workerId: string, values: CreateRateValues) {
  return ratesClient.rates(workerId, new CreateWorkerRateRequest({
    basis: values.basis,
    activityTypeId: values.activityTypeId,
    rateUsd: values.rateUsd,
    effectiveFrom: dateOnly(values.effectiveFrom),
    effectiveTo: values.effectiveTo ? dateOnly(values.effectiveTo) : undefined,
  }));
}

export function getAttendance(date: string) { return attendanceClient.attendanceGET(dateOnly(date)); }

export function saveAttendance(date: string, lateReason: string | undefined, entries: readonly AttendanceEntryValues[]) {
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

export function createWorkRecord(values: CreateWorkRecordValues) {
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

export function verifyWork(workRecordId: string, supervisorPersonId: string, expectedVersion: number) {
  return workRecordsClient.supervisorVerification(workRecordId, new VerifyWorkRecordRequest({ supervisorPersonId, expectedVersion }));
}

export function confirmWork(workRecordId: string, expectedVersion: number) {
  return workRecordsClient.managerConfirmation2(workRecordId, new ConfirmWorkRecordRequest({ expectedVersion }));
}
