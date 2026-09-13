export const financeCategories = [
  'Fuel', 'RepairsAndMaintenance', 'Utilities', 'Transport', 'ContractServices',
  'CropInputs', 'CropSales', 'OtherExpense', 'OtherIncome',
] as const;

export function usd(value: number): string {
  return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(value);
}

export function financeLabel(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function perUnit(value: number | undefined, unit: string): string {
  return value == null ? 'Not available' : `${usd(value)} / ${unit}`;
}

export function newFinanceKey(prefix: string): string {
  return `${prefix}-${globalThis.crypto.randomUUID()}`;
}

export const budgetCategories = ['Labour', 'AppliedInput', 'DirectExpense', 'ApprovedVarianceCost'] as const;

export function canEditBudget(status: string): boolean { return status === 'Draft'; }
export function canSubmitBudget(role: string, status: string): boolean {
  return ['Grower', 'FarmManager'].includes(role) && status === 'Draft';
}
export function canApproveBudget(role: string, status: string): boolean {
  return role === 'Grower' && status === 'Submitted';
}
export function canReviseBudget(role: string, status: string): boolean {
  return role === 'Grower' && status === 'Approved';
}
export function variancePercent(value: number | undefined): string {
  return value == null ? 'Not available' : `${value > 0 ? '+' : ''}${value.toFixed(2)}%`;
}
