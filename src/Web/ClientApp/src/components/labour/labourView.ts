export const employmentTypes = ['Permanent', 'Seasonal', 'Casual', 'Contract', 'TaskBased'] as const;
export const payBases = ['Daily', 'Monthly', 'Hectare', 'StandardLine'] as const;

export function label(value: string): string {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

export function evidenceAmount(basis: string, amount: number | undefined, quantity: number | undefined): string {
  if (basis === 'Monthly') return 'Deferred to payroll';
  if (amount == null) return basis === 'Daily' ? 'Pending confirmation' : `${quantity ?? 0} ${basis === 'Hectare' ? 'ha' : 'lines'}`;
  return `$${amount.toFixed(2)}`;
}

export function dateOnly(date: string): string {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) throw new Error('Date must use yyyy-MM-dd.');
  return date;
}

export function harareToday(): string {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

export function activitySelectionError(activityIds: readonly string[]): string | undefined {
  return activityIds.length > 0 ? undefined : 'Select at least one activity on the allocated field before recording evidence.';
}
