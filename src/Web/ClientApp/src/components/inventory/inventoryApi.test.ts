import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

test('postReceipt calls the generated client method that actually posts a stock receipt', async () => {
  const client = await readFile(new URL('../../web-api-client.ts', import.meta.url), 'utf8');
  const api = await readFile(new URL('./inventoryApi.ts', import.meta.url), 'utf8');

  const match = client.match(/(\w+)\(receiptId: string, body: PostStockReceiptRequest\): Promise<StockReceiptDto>/);
  assert.ok(match, 'expected a generated InventoryClient method that posts a StockReceiptDto by receiptId');
  const receiptPostMethod = match![1];

  const postReceiptBody = api.match(/export function postReceipt\([\s\S]*?\n\}/)?.[0];
  assert.ok(postReceiptBody, 'expected a postReceipt function in inventoryApi.ts');
  assert.match(postReceiptBody!, new RegExp(`inventoryClient\\.${receiptPostMethod}\\(`),
    `postReceipt must call inventoryClient.${receiptPostMethod}(), the method wired to POST receipts/{receiptId}/post`);
});

test('reverseReceipt calls the generated client method that actually reverses a stock receipt', async () => {
  const client = await readFile(new URL('../../web-api-client.ts', import.meta.url), 'utf8');
  const api = await readFile(new URL('./inventoryApi.ts', import.meta.url), 'utf8');

  const match = client.match(/(\w+)\(receiptId: string, body: ReverseStockReceiptRequest\): Promise<StockReceiptDto>/);
  assert.ok(match, 'expected a generated InventoryClient method that reverses a StockReceiptDto by receiptId');
  const receiptReverseMethod = match![1];

  const reverseReceiptBody = api.match(/export function reverseReceipt\([\s\S]*?\n\}/)?.[0];
  assert.ok(reverseReceiptBody, 'expected a reverseReceipt function in inventoryApi.ts');
  assert.match(reverseReceiptBody!, new RegExp(`inventoryClient\\.${receiptReverseMethod}\\(`),
    `reverseReceipt must call inventoryClient.${receiptReverseMethod}(), the method wired to POST receipts/{receiptId}/reverse`);
});
