import type { ActivityListItemDto } from '../../web-api-client';

export const activityStatuses = Object.freeze([
  'Draft',
  'Planned',
  'InProgress',
  'AwaitingVerification',
  'ManagerConfirmation',
  'Completed',
  'Closed',
  'Cancelled',
] as const);

export function formatActivityStatus(status: string): string {
  return ({
    InProgress: 'In progress',
    AwaitingVerification: 'Awaiting verification',
    ManagerConfirmation: 'Manager confirmation',
  })[status] ?? status;
}

export function quantityLabel(basis: string): string {
  if (basis === 'Hectares') return 'Actual coverage (ha)';
  if (basis === 'StandardLines') return 'Completed standard lines';
  return '';
}

export function orderedActions(allowedTransitions: readonly string[]): string[] {
  const order = ['Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled'];
  return order.filter((action) => allowedTransitions.includes(action));
}

type ActivityScheduleItem = Partial<Pick<ActivityListItemDto, 'actualAt' | 'plannedDate'>>;

export function groupActivitiesByDate<T extends ActivityScheduleItem>(activities: readonly T[]): Record<string, T[]> {
  return activities.reduce<Record<string, T[]>>((groups, activity) => {
    const key = activity.actualAt?.slice(0, 10) ?? activity.plannedDate ?? 'Unscheduled';
    groups[key] = [...(groups[key] ?? []), activity];
    return groups;
  }, {});
}

export function monthGridDates(year: number, monthIndex: number): Array<string | null> {
  const first = new Date(Date.UTC(year, monthIndex, 1));
  const daysInMonth = new Date(Date.UTC(year, monthIndex + 1, 0)).getUTCDate();
  const mondayOffset = (first.getUTCDay() + 6) % 7;
  const cells: Array<string | null> = [
    ...Array(mondayOffset).fill(null),
    ...Array.from({ length: daysInMonth }, (_, index) =>
      new Date(Date.UTC(year, monthIndex, index + 1)).toISOString().slice(0, 10)),
  ];
  while (cells.length % 7 !== 0) cells.push(null);
  return cells;
}
