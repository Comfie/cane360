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

/** @param {string} date */
export function localDate(date) {
  return new Date(`${date}T00:00:00`);
}

/** @param {{ fieldId: string, cropCycleId: string, activityTypeId: string, kind: string, plannedDate?: string, supervisorPersonId: string }} values */
export function createActivity(values) {
  return activitiesClient.activitiesPOST(new CreateActivityRequest({
    ...values,
    plannedDate: values.plannedDate ? localDate(values.plannedDate) : undefined,
  }));
}

/** @param {string} activityId @param {number} expectedVersion @param {string} actualAt @param {number | undefined} actualQuantity @param {string | undefined} lateEntryReason */
export function recordActual(activityId, expectedVersion, actualAt, actualQuantity, lateEntryReason) {
  return activitiesClient.actualWork(activityId, new RecordActualWorkRequest({
    expectedVersion,
    actualAt: new Date(actualAt),
    actualQuantity,
    lateEntryReason,
  }));
}

/** @param {string} activityId @param {string} action @param {number} expectedVersion @param {string | undefined} reason */
export function transitionActivity(activityId, action, expectedVersion, reason) {
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

/** @param {string} activityId @param {number} expectedVersion @param {string} reference @param {string} capturedDate */
export function addSourceReference(activityId, expectedVersion, reference, capturedDate) {
  return activitiesClient.sourceReferences(activityId, new AddSourceReferenceRequest({
    expectedVersion,
    sourceSheetReference: reference,
    capturedDate: localDate(capturedDate),
  }));
}
