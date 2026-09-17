import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';

const cases = [
  { fn: 'submitInputRequest', signature: /(\w+)\(requestId: string, body: PostStockReceiptRequest\): Promise<void>/ },
  { fn: 'decideInputRequest', signature: /(\w+)\(requestId: string, body: DecideInputRequestRequest\): Promise<void>/ },
  { fn: 'postStockIssue', signature: /(\w+)\(issueId: string, body: PostStockReceiptRequest\): Promise<void>/ },
  { fn: 'reverseStockIssue', signature: /(\w+)\(issueId: string, body: ReverseStockIssueRequest\): Promise<void>/ },
  { fn: 'postStockReturn', signature: /(\w+)\(stockReturnId: string, body: PostStockReturnRequest\): Promise<void>/ },
  { fn: 'submitInventoryLoss', signature: /(\w+)\(lossId: string, body: VersionedInventoryRequest\): Promise<void>/ },
  { fn: 'decideInventoryLoss', signature: /(\w+)\(lossId: string, body: DecideInventoryLossRequest\): Promise<void>/ },
];

for (const { fn, signature } of cases) {
  test(`${fn} calls the generated InputControlsClient method matching its own route`, async () => {
    const client = await readFile(new URL('../../web-api-client.ts', import.meta.url), 'utf8');
    const api = await readFile(new URL('./inputControlApi.ts', import.meta.url), 'utf8');

    const match = client.match(signature);
    assert.ok(match, `expected a generated InputControlsClient method matching ${signature}`);
    const expectedMethod = match![1];

    const fnBody = api.match(new RegExp(`export const ${fn} = [\\s\\S]*?;`))?.[0];
    assert.ok(fnBody, `expected a ${fn} export in inputControlApi.ts`);
    assert.match(fnBody!, new RegExp(`inputControlsClient\\.${expectedMethod}\\(`),
      `${fn} must call inputControlsClient.${expectedMethod}()`);
  });
}
