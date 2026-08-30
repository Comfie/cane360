import {
  ActivitiesClient,
  ActivityTypesClient,
  AddSourceReferenceRequest,
  CreateActivityRequest,
  RecordActualWorkRequest,
  TransitionActivityRequest,
} from '../../web-api-client';

export const activitiesClient = new ActivitiesClient();
export const activityTypesClient = new ActivityTypesClient();

interface CreateActivityValues {
  fieldId: string;
  cropCycleId: string;
  activityTypeId: string;
  kind: string;
  plannedDate?: string;
  supervisorPersonId: string;
}
export type ActivityTransitionAction = 'Planned' | 'InProgress' | 'AwaitingVerification' | 'ManagerConfirmation' | 'Completed' | 'Closed' | 'Cancelled';

export function localDate(date: string): Date {
  return new Date(`${date}T00:00:00`);
}

export function createActivity(values: CreateActivityValues) {
  return activitiesClient.activitiesPOST(new CreateActivityRequest({
    ...values,
    plannedDate: values.plannedDate ? localDate(values.plannedDate) : undefined,
  }));
}

export function recordActual(activityId: string, expectedVersion: number, actualAt: string, actualQuantity: number | undefined, lateEntryReason: string | undefined) {
  return activitiesClient.actualWork(activityId, new RecordActualWorkRequest({
    expectedVersion,
    actualAt: new Date(actualAt).toISOString(),
    actualQuantity,
    lateEntryReason,
  }));
}

export function transitionActivity(activityId: string, action: string, expectedVersion: number, reason: string | undefined) {
  const request = new TransitionActivityRequest({ expectedVersion, reason });
  if (action === 'Planned') return activitiesClient.planned(activityId, request);
  if (action === 'InProgress') return activitiesClient.inProgress(activityId, request);
  if (action === 'AwaitingVerification') return activitiesClient.awaitingVerification(activityId, request);
  if (action === 'ManagerConfirmation') return activitiesClient.managerConfirmation(activityId, request);
  if (action === 'Completed') return activitiesClient.completed(activityId, request);
  if (action === 'Closed') return activitiesClient.closed(activityId, request);
  if (action === 'Cancelled') return activitiesClient.cancelled(activityId, request);
  throw new Error(`Unknown activity transition: ${action}`);
}

export function addSourceReference(activityId: string, expectedVersion: number, reference: string, capturedDate: string) {
  return activitiesClient.sourceReferences(activityId, new AddSourceReferenceRequest({
    expectedVersion,
    sourceSheetReference: reference,
    capturedDate: localDate(capturedDate),
  }));
}
