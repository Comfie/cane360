export const activityStatuses = Object.freeze([
  'Draft',
  'Planned',
  'InProgress',
  'AwaitingVerification',
  'ManagerConfirmation',
  'Completed',
  'Closed',
  'Cancelled',
]);

/** @param {string} status */
export function formatActivityStatus(status) {
  return ({
    InProgress: 'In progress',
    AwaitingVerification: 'Awaiting verification',
    ManagerConfirmation: 'Manager confirmation',
  })[status] ?? status;
}

/** @param {string} basis */
export function quantityLabel(basis) {
  if (basis === 'Hectares') return 'Actual coverage (ha)';
  if (basis === 'StandardLines') return 'Completed standard lines';
  return '';
}

/** @param {string[]} allowedTransitions */
export function orderedActions(allowedTransitions) {
  const order = ['Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled'];
  return order.filter((action) => allowedTransitions.includes(action));
}

/** @param {import('../../web-api-client').ActivityListItemDto[]} activities */
export function groupActivitiesByDate(activities) {
  return activities.reduce((groups, activity) => {
    const key = activity.actualAt?.slice(0, 10) ?? activity.plannedDate ?? 'Unscheduled';
    groups[key] = [...(groups[key] ?? []), activity];
    return groups;
  }, /** @type {Record<string, import('../../web-api-client').ActivityListItemDto[]>} */ ({}));
}

/** @param {number} year @param {number} monthIndex */
export function monthGridDates(year, monthIndex) {
  const first = new Date(Date.UTC(year, monthIndex, 1));
  const daysInMonth = new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
  const mondayOffset = (first.getUTCDay() + 6) % 7;
  const cells = [
    ...Array(mondayOffset).fill(null),
    ...Array.from({ length: daysInMonth }, (_, index) =>
      new Date(Date.UTC(year, monthIndex, index + 1)).toISOString().slice(0, 10)),
  ];
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}
