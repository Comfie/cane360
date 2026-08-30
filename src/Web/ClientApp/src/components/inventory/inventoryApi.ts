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
import type {
  ICreateInventoryItemRequest,
  ICreateInventoryLotRequest,
  ICreateStockCountRequest,
  ICreateStockReceiptLineRequest,
  ICreateSupplierRequest,
  ICreateUnitOfMeasureRequest,
  StockAdjustmentDto,
  StockCountDto,
} from '../../web-api-client';

export const inventoryClient = new InventoryClient();

interface StockReceiptValues {
  receiptType: string;
  supplierId: string | undefined;
  receiptDate: string;
  receivedByPersonId: string | undefined;
  sourceReference: string;
  reason: string | undefined;
  lateEntryReason: string | undefined;
  lines: readonly ICreateStockReceiptLineRequest[];
}

export function createUnit(values: ICreateUnitOfMeasureRequest) {
  return inventoryClient.units(new CreateUnitOfMeasureRequest(values));
}

export function createItem(values: ICreateInventoryItemRequest) {
  return inventoryClient.items(new CreateInventoryItemRequest(values));
}

export function createSupplier(values: ICreateSupplierRequest) {
  return inventoryClient.suppliers(new CreateSupplierRequest(values));
}

export function createLot(values: ICreateInventoryLotRequest) {
  return inventoryClient.lots(new CreateInventoryLotRequest(values));
}

export function createReceipt(values: StockReceiptValues) {
  return inventoryClient.receiptsPOST(new CreateStockReceiptRequest({
    ...values,
    lines: values.lines.map((line) => new CreateStockReceiptLineRequest(line)),
  }));
}

export function submitOpeningBalance(receiptId: string, expectedVersion: number) {
  return inventoryClient.submitOpeningBalance(receiptId, new VersionedInventoryRequest({ expectedVersion }));
}

export function decideOpeningBalance(receiptId: string, expectedVersion: number, outcome: 'Approved' | 'Rejected', reason: string | undefined) {
  return inventoryClient.openingBalanceDecision(receiptId, new DecideOpeningBalanceRequest({
    expectedVersion,
    outcome,
    reason,
    idempotencyKey: operationKey('opening-decision'),
  }));
}

export function postReceipt(receiptId: string, expectedVersion: number) {
  return inventoryClient.post3(receiptId, new PostStockReceiptRequest({
    expectedVersion,
    idempotencyKey: operationKey('receipt-post'),
  }));
}

export function reverseReceipt(receiptId: string, expectedVersion: number, reason: string) {
  return inventoryClient.reverse3(receiptId, new ReverseStockReceiptRequest({
    expectedVersion,
    reason,
    idempotencyKey: operationKey('receipt-reversal'),
  }));
}

export function createStockCount(values: ICreateStockCountRequest) { return inventoryClient.counts(new CreateStockCountRequest(values)); }
export function startStockCount(countId: string, expectedVersion: number) { return inventoryClient.start(countId, new VersionedInventoryRequest({ expectedVersion })); }
export function reviewStockCount(countId: string, expectedVersion: number) { return inventoryClient.review(countId, new VersionedInventoryRequest({ expectedVersion })); }
export function getStockCounts(): Promise<StockCountDto[]> { return inventoryClient.countsAll(); }
export function getStockAdjustments(): Promise<StockAdjustmentDto[]> { return inventoryClient.adjustmentsAll(); }

export function operationKey(operation: string): string {
  const random = globalThis.crypto?.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(16).slice(2)}`;
  return `p5a-ui-${operation}-${random}`;
}
