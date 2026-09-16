/** Synchronous entry guard: React state alone cannot reject two same-tick submissions. */
export async function withMutationGuard(gate: { current: boolean }, action: () => Promise<void>): Promise<boolean> {
  if (gate.current) return false;
  gate.current = true;
  try {
    await action();
    return true;
  } finally {
    gate.current = false;
  }
}
