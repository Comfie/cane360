import assert from 'node:assert/strict';
import test from 'node:test';
import { withMutationGuard } from './mutationGuard.ts';

test('same-tick duplicate submission invokes the authoritative request only once', async () => {
  const gate = { current: false };
  let release!: () => void;
  let requests = 0;
  const response = new Promise<void>((resolve) => { release = resolve; });
  const action = async () => { requests++; await response; };
  const first = withMutationGuard(gate, action);
  assert.equal(await withMutationGuard(gate, action), false);
  assert.equal(requests, 1);
  assert.equal(gate.current, true);
  release();
  assert.equal(await first, true);
  assert.equal(gate.current, false);
});

test('failed request does not report success and releases the guard for a deliberate retry', async () => {
  const gate = { current: false };
  let successes = 0;
  await assert.rejects(withMutationGuard(gate, async () => { throw new Error('Synthetic failure'); }), /Synthetic failure/);
  assert.equal(gate.current, false);
  assert.equal(await withMutationGuard(gate, async () => { successes++; }), true);
  assert.equal(successes, 1);
});
