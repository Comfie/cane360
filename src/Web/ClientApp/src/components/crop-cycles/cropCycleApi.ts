import {
  CancelCropCycleRequest,
  CreateCropCycleRequest,
  CreateCropVarietyRequest,
  CropCyclesClient,
  CropVarietiesClient,
  HarvestCropCycleRequest,
  TransitionCropCycleRequest,
} from '../../web-api-client';
import type { ICreateCropCycleRequest } from '../../web-api-client';

export const cropCyclesClient = new CropCyclesClient();
export const cropVarietiesClient = new CropVarietiesClient();

export type CropCycleTransitionAction = 'Activate' | 'Cancel' | 'ReadyForHarvest' | 'Harvest' | 'Close';

interface CropCycleTransitionValues {
  reason?: string;
  harvestDate?: Date;
  actualTonnes?: number;
}

export function localDate(value: string | File | null): Date {
  return new Date(`${String(value)}T00:00:00`);
}

export function createCycle(fieldId: string, values: ICreateCropCycleRequest) {
  return cropCyclesClient.cropCyclesPOST(fieldId, new CreateCropCycleRequest(values));
}

export function createVariety(code: string, name: string) {
  return cropVarietiesClient.cropVarieties(new CreateCropVarietyRequest({ code, name }));
}

export function transitionCycle(fieldId: string, cycleId: string, action: CropCycleTransitionAction, version: number, values: CropCycleTransitionValues = {}) {
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
