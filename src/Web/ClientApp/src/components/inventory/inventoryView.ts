import type { StockReceiptDto } from '../../web-api-client';

export const itemCategories = ['Fertiliser', 'Chemical', 'SeedAndPlantingMaterial', 'Other'] as const;
export const lotPolicies = ['None', 'Optional', 'Required'] as const;

export function inventoryLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function usd(value: number): string {
  return new Intl.NumberFormat('en-ZW', { style: 'currency', currency: 'USD' }).format(value);
}

export function quantity(value: number, unit: string): string {
  return `${new Intl.NumberFormat('en-ZW', { maximumFractionDigits: 6 }).format(value)} ${unit}`;
}

export function isoDate(value: Date | string): string {
  const result = value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result)) throw new Error('Date must use yyyy-MM-dd.');
  return result;
}

export function harareToday(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit',
  }).format(new Date());
}

type ReceiptReference = Pick<StockReceiptDto, 'supplierId' | 'sourceReference'>;

export function duplicateReceiptReference<T extends ReceiptReference>(receipts: readonly T[], supplierId: string, sourceReference: string): T | undefined {
  const normalised = sourceReference.trim().toUpperCase();
  if (!normalised) return undefined;
  return receipts.find((receipt) => receipt.supplierId === supplierId && receipt.sourceReference.trim().toUpperCase() === normalised);
}
