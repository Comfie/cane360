export const currentCycleStatuses = new Set(['Active', 'ReadyForHarvest']);
export const historicalCycleStatuses = new Set(['Closed', 'Cancelled']);

/** @param {string} status */
export function cycleGroup(status) {
  if (status === 'Draft') return 'drafts';
  if (currentCycleStatuses.has(status)) return 'current';
  if (status === 'Harvested') return 'awaiting-close';
  return 'history';
}

/** @param {string} status */
export function formatCycleStatus(status) {
  return status === 'ReadyForHarvest' ? 'Ready for harvest' : status;
}

/** @param {import('../../web-api-client').CropCycleListItemDto[]} cycles @param {string} filter */
export function filterCycles(cycles, filter) {
  if (filter === 'all') return cycles;
  return cycles.filter((cycle) => cycleGroup(cycle.status) === filter);
}

/** @param {import('../../web-api-client').CropCycleCollectionDto[]} collections */
export function flattenCycleCollections(collections) {
  return collections
    .flatMap((collection) => collection.cropCycles.map((cropCycle) => ({ field: collection.field, cropCycle })))
    .sort((left, right) => right.cropCycle.startDate.localeCompare(left.cropCycle.startDate));
}
