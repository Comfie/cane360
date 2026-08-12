import {
  CancelCropCycleRequest,
  CreateCropCycleRequest,
  CreateCropVarietyRequest,
  CropCyclesClient,
  CropVarietiesClient,
  HarvestCropCycleRequest,
  TransitionCropCycleRequest,
} from '../../web-api-client';

export const cropCyclesClient = new CropCyclesClient();
export const cropVarietiesClient = new CropVarietiesClient();

/** @param {FormDataEntryValue | null} value */
export function localDate(value) {
  return new Date(`${String(value)}T00:00:00`);
}

/** @param {string} fieldId @param {{ cycleType: string, ratoonNumber: number | undefined, cropVarietyId: string, startDate: Date, expectedHarvestStart: Date, expectedHarvestEnd: Date, expectedYieldTonnes: number }} values */
export function createCycle(fieldId, values) {
  return cropCyclesClient.cropCyclesPOST(fieldId, new CreateCropCycleRequest(values));
}

/** @param {string} code @param {string} name */
export function createVariety(code, name) {
  return cropVarietiesClient.cropVarieties(new CreateCropVarietyRequest({ code, name }));
}

/** @param {string} fieldId @param {string} cycleId @param {string} action @param {number} version @param {{ reason?: string, harvestDate?: Date, actualTonnes?: number }} values */
export function transitionCycle(fieldId, cycleId, action, version, values = {}) {
  if (action === 'Activate') return cropCyclesClient.activate(fieldId, cycleId, new TransitionCropCycleRequest({ expectedVersion: version }));
  if (action === 'Cancel') return cropCyclesClient.cancel(fieldId, cycleId, new CancelCropCycleRequest({ expectedVersion: version, reason: values.reason ?? '' }));
  if (action === 'ReadyForHarvest') return cropCyclesClient.readyForHarvest(fieldId, cycleId, new TransitionCropCycleRequest({ expectedVersion: version }));
  if (action === 'Harvest') {
    if (!values.harvestDate) throw new Error('A harvest date is required.');
    return cropCyclesClient.harvest(fieldId, cycleId, new HarvestCropCycleRequest({ expectedVersion: version, harvestDate: values.harvestDate, actualTonnes: values.actualTonnes ?? 0 }));
  }
  if (action === 'Close') return cropCyclesClient.close(fieldId, cycleId, new TransitionCropCycleRequest({ expectedVersion: version }));
  throw new Error(`Unknown crop-cycle transition: ${action}`);
}
