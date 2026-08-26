import {
  CreateInventoryItemRequest,
  CreateInventoryLotRequest,
  CreateStockReceiptLineRequest,
  CreateStockReceiptRequest,
  CreateSupplierRequest,
  CreateUnitOfMeasureRequest,
  DecideOpeningBalanceRequest,
  InventoryClient,
  PostStockReceiptRequest,
  ReverseStockReceiptRequest,
  VersionedInventoryRequest,
  CreateStockCountRequest,
} from '../../web-api-client';

export const inventoryClient = new InventoryClient();

/** @param {{code: string, name: string, dimension: string, decimalPlaces: number}} values */
export function createUnit(values) {
  return inventoryClient.units(new CreateUnitOfMeasureRequest(values));
}

/** @param {{code: string, name: string, category: string, stockUnitId: string, reorderLevel: number | undefined, lotTrackingPolicy: string, expiryPolicy: string}} values */
export function createItem(values) {
  return inventoryClient.items(new CreateInventoryItemRequest(values));
}

/** @param {{code: string, name: string, contact: string | undefined}} values */
export function createSupplier(values) {
  return inventoryClient.suppliers(new CreateSupplierRequest(values));
}

/** @param {{inventoryItemId: string, code: string, expiryDate: string | undefined}} values */
export function createLot(values) {
  return inventoryClient.lots(new CreateInventoryLotRequest(values));
}

/** @param {{receiptType: string, supplierId: string | undefined, receiptDate: string, receivedByPersonId: string | undefined, sourceReference: string, reason: string | undefined, lateEntryReason: string | undefined, lines: {inventoryItemId: string, inventoryLotId: string | undefined, quantity: number, unitCostUsd: number}[]}} values */
export function createReceipt(values) {
  return inventoryClient.receiptsPOST(new CreateStockReceiptRequest({
    ...values,
    lines: values.lines.map((line) => new CreateStockReceiptLineRequest(line)),
  }));
}

/** @param {string} receiptId @param {number} expectedVersion */
export function submitOpeningBalance(receiptId, expectedVersion) {
  return inventoryClient.submitOpeningBalance(receiptId, new VersionedInventoryRequest({ expectedVersion }));
}

/** @param {string} receiptId @param {number} expectedVersion @param {'Approved' | 'Rejected'} outcome @param {string | undefined} reason */
export function decideOpeningBalance(receiptId, expectedVersion, outcome, reason) {
  return inventoryClient.openingBalanceDecision(receiptId, new DecideOpeningBalanceRequest({
    expectedVersion,
    outcome,
    reason,
    idempotencyKey: operationKey('opening-decision'),
  }));
}

/** @param {string} receiptId @param {number} expectedVersion */
export function postReceipt(receiptId, expectedVersion) {
  return inventoryClient.post3(receiptId, new PostStockReceiptRequest({
    expectedVersion,
    idempotencyKey: operationKey('receipt-post'),
  }));
}

/** @param {string} receiptId @param {number} expectedVersion @param {string} reason */
export function reverseReceipt(receiptId, expectedVersion, reason) {
  return inventoryClient.reverse3(receiptId, new ReverseStockReceiptRequest({
    expectedVersion,
    reason,
    idempotencyKey: operationKey('receipt-reversal'),
  }));
}

/** @param {{eventDate: string, notes: string, countingPersons: string}} values */
export function createStockCount(values) { return inventoryClient.counts(new CreateStockCountRequest(values)); }
/** @param {string} countId @param {number} expectedVersion */
export function startStockCount(countId, expectedVersion) { return inventoryClient.start(countId, new VersionedInventoryRequest({ expectedVersion })); }
/** @param {string} countId @param {number} expectedVersion */
export function reviewStockCount(countId, expectedVersion) { return inventoryClient.review(countId, new VersionedInventoryRequest({ expectedVersion })); }
/** @returns {Promise<import('../../web-api-client').StockCountDto[]>} */
export function getStockCounts() { return inventoryClient.countsAll(); }
/** @returns {Promise<import('../../web-api-client').StockAdjustmentDto[]>} */
export function getStockAdjustments() { return inventoryClient.adjustmentsAll(); }

/** @param {string} operation */
export function operationKey(operation) {
  const random = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return `p5a-ui-${operation}-${random}`;
}
