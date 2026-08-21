export const employmentTypes = ['Permanent', 'Seasonal', 'Casual', 'Contract', 'TaskBased'];
export const payBases = ['Daily', 'Monthly', 'Hectare', 'StandardLine'];

/** @param {string} value */
export function label(value) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

/** @param {string} basis @param {number | undefined} amount @param {number | undefined} quantity */
export function evidenceAmount(basis, amount, quantity) {
  if (basis === 'Monthly') return 'Deferred to payroll';
  if (amount == null) return basis === 'Daily' ? 'Pending confirmation' : `${quantity ?? 0} ${basis === 'Hectare' ? 'ha' : 'lines'}`;
  return `$${amount.toFixed(2)}`;
}

/** @param {string} date */
export function dateOnly(date) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(date)) throw new Error('Date must use yyyy-MM-dd.');
  return date;
}

export function harareToday() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}
