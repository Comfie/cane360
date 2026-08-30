import type { CropCycleFieldDto, CropCycleListItemDto } from '../../web-api-client';

export type CycleGroup = 'drafts' | 'current' | 'awaiting-close' | 'history';

export const currentCycleStatuses: ReadonlySet<string> = new Set(['Active', 'ReadyForHarvest']);
export const historicalCycleStatuses: ReadonlySet<string> = new Set(['Closed', 'Cancelled']);

export function cycleGroup(status: string): CycleGroup {
  if (status === 'Draft') return 'drafts';
  if (currentCycleStatuses.has(status)) return 'current';
  if (status === 'Harvested') return 'awaiting-close';
  return 'history';
}

export function formatCycleStatus(status: string): string {
  return status === 'ReadyForHarvest' ? 'Ready for harvest' : status;
}

export function filterCycles<T extends Pick<CropCycleListItemDto, 'status'>>(cycles: readonly T[], filter: string): readonly T[] {
  if (filter === 'all') return cycles;
  return cycles.filter((cycle) => cycleGroup(cycle.status) === filter);
}

export interface FlattenedCropCycle<TField = CropCycleFieldDto, TCycle = CropCycleListItemDto> {
  field: TField;
  cropCycle: TCycle;
}

type CropCycleCollection<TField, TCycle> = {
  field: TField;
  cropCycles: readonly TCycle[];
};

export function flattenCycleCollections<
  TField extends Pick<CropCycleFieldDto, 'id'>,
  TCycle extends Pick<CropCycleListItemDto, 'id' | 'status' | 'startDate'>,
>(
  collections: readonly CropCycleCollection<TField, TCycle>[],
): FlattenedCropCycle<TField, TCycle>[] {
  return collections
    .flatMap((collection) => collection.cropCycles.map((cropCycle) => ({ field: collection.field, cropCycle })))
    .sort((left, right) => right.cropCycle.startDate.localeCompare(left.cropCycle.startDate));
}
