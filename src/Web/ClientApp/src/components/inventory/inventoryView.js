export const itemCategories = ['Fertiliser', 'Chemical', 'SeedAndPlantingMaterial', 'Other'];
export const lotPolicies = ['None', 'Optional', 'Required'];

/** @param {string} value */
export function inventoryLabel(value) {
  return value.replace(/([a-z])([A-Z])/g, '$1 $2');
}

/** @param {number} value */
export function usd(value) {
  return new Intl.NumberFormat('en-ZW', { style: 'currency', currency: 'USD' }).format(value);
}

/** @param {number} value @param {string} unit */
export function quantity(value, unit) {
  return `${new Intl.NumberFormat('en-ZW', { maximumFractionDigits: 6 }).format(value)} ${unit}`;
}

/** @param {Date | string} value */
export function isoDate(value) {
  const result = value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10);
  if (!/^\d{4}-\d{2}-\d{2}$/.test(result)) throw new Error('Date must use yyyy-MM-dd.');
  return result;
}

export function harareToday() {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit',
  }).format(new Date());
}

/** @param {import('../../web-api-client').StockReceiptDto[]} receipts @param {string} supplierId @param {string} sourceReference */
export function duplicateReceiptReference(receipts, supplierId, sourceReference) {
  const normalised = sourceReference.trim().toUpperCase();
  if (!normalised) return undefined;
  return receipts.find((receipt) => receipt.supplierId === supplierId && receipt.sourceReference.trim().toUpperCase() === normalised);
}
