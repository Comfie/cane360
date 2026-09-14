export function tonnes(value: number): string {
  return `${value.toLocaleString(undefined, { maximumFractionDigits: 3 })} t`;
}

export function amountReconciliation(value: number | undefined, status: string,
  formatter: (amount: number) => string): string {
  return value == null ? status.replace(/([a-z])([A-Z])/g, '$1 $2') : formatter(value);
}

export function canCorrectMillEvidence(role: string, status: string): boolean {
  return role === 'Grower' && status === 'Recorded';
}

export function matchWarning(reused: boolean): string | undefined {
  return reused ? 'A matched ticket is also active on another statement.' : undefined;
}
