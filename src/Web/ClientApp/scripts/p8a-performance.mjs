import { readFile } from 'node:fs/promises';
import { performance } from 'node:perf_hooks';

// Read-only HTTP sampling. The fixture owner must supply a measured synthetic dataset manifest.
// Credentials belong only in process environment; they are never written to the result.
const baseUrl = new URL(process.env.P8A_BASE_URL ?? 'http://127.0.0.1:5050');
if (!['127.0.0.1', 'localhost', '[::1]'].includes(baseUrl.hostname)) {
  throw new Error('Only the local API connected to verified Railway Development is permitted.');
}
if (process.env.CANE360_ACCEPTANCE_TARGET !== 'RailwayDevelopment') {
  throw new Error('Verify Railway Development and set CANE360_ACCEPTANCE_TARGET=RailwayDevelopment.');
}
const manifestPath = process.argv[2];
if (!manifestPath) throw new Error('Supply a JSON manifest containing profile and endpoints.');
const manifest = JSON.parse(await readFile(manifestPath, 'utf8'));
const minimums = { farms: 1, fields: 50, cycles: 100, activities: 2000, workers: 250,
  attendanceWork: 5000, inventoryMovements: 3000, payrollLines: 1000,
  financeTransactions: 2000, costPostings: 500, tickets: 2000, statements: 100, auditEvents: 10000 };
for (const [key, minimum] of Object.entries(minimums)) {
  if (!Number.isSafeInteger(manifest.profile?.[key]) || manifest.profile[key] < minimum) {
    throw new Error(`Pilot-size fixture profile requires ${key} >= ${minimum}.`);
  }
}
const runs = Number(process.env.P8A_RUNS ?? 50);
if (!Number.isSafeInteger(runs) || runs < 50 || runs > 1000) throw new Error('P8A_RUNS must be 50–1000.');
const authorization = process.env.P8A_AUTHORIZATION;
if (!authorization) throw new Error('Supply synthetic-account authorization via P8A_AUTHORIZATION.');
const headers = { authorization, accept: 'application/json' };
const setupResponse = await fetch(new URL('/api/FarmSetup', baseUrl), { headers });
if (!setupResponse.ok) throw new Error(`Synthetic workspace verification failed: HTTP ${setupResponse.status}.`);
const setup = await setupResponse.json();
if (!setup.farm?.name?.startsWith('AUTOTEST-P8A-') && !setup.farm?.name?.startsWith('AUTOTEST-P8A ')) {
  throw new Error('The authenticated farm must be labelled AUTOTEST-P8A.');
}
if (!Array.isArray(manifest.endpoints) || manifest.endpoints.length < 13) {
  throw new Error('Include at least the thirteen representative endpoint/report workloads.');
}
const results = [];
for (const endpoint of manifest.endpoints) {
  if (typeof endpoint.path !== 'string' || !endpoint.path.startsWith('/api/') ||
      /export|download|payslip|cash-register|\.csv|evidence\//i.test(endpoint.path)) {
    throw new Error('Only operational/report JSON GET endpoints are permitted; transfers and audited exports are excluded.');
  }
  if (!['common', 'complex'].includes(endpoint.kind)) throw new Error('Endpoint kind must be common or complex.');
  const samples = [];
  for (let index = 0; index < runs + 3; index++) {
    const start = performance.now();
    const response = await fetch(new URL(endpoint.path, baseUrl), { headers, signal: AbortSignal.timeout(30000) });
    await response.arrayBuffer();
    if (!response.ok) throw new Error(`Workload ${results.length + 1} failed: HTTP ${response.status}.`);
    if (index >= 3) samples.push(performance.now() - start);
  }
  samples.sort((left, right) => left - right);
  const percentile = (p) => Math.round(samples[Math.ceil(p * samples.length) - 1] * 100) / 100;
  const thresholdMs = endpoint.kind === 'complex' ? 4000 : 2000;
  results.push({ workload: endpoint.name, sampleSize: endpoint.sampleSize, runs,
    p50Ms: percentile(.5), p95Ms: percentile(.95), maxMs: percentile(1), thresholdMs,
    passed: percentile(.95) < thresholdMs });
}
console.log(JSON.stringify({ fixtureProfile: manifest.profile, warmups: 3, results }, null, 2));
if (results.some((result) => !result.passed)) process.exitCode = 1;
