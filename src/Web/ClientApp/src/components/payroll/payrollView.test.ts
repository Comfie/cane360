import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import test from 'node:test';
import {
    advancePayload,
    canCalculatePayrollRun,
    canCancelPayrollRun,
    canCreatePayrollRun,
    canDecideAdvance,
    canDecidePayrollRun,
    canEditAdvance,
    canIssueAdvance,
    canSubmitAdvance,
    canSubmitPayrollRun,
    defaultPeriodId,
    issuePayload,
    payrollDecisionPayload,
    payrollErrorMessage,
    periodPayload,
    schedulePayload
} from './payrollView.ts';

const pageSource = readFileSync(new URL('../pages/PayrollPage.tsx', import.meta.url), 'utf8');
const labourPageSource = readFileSync(new URL('../pages/LabourPage.tsx', import.meta.url), 'utf8');
const routesSource = readFileSync(new URL('../../AppRoutes.tsx', import.meta.url), 'utf8');
const navigationSource = readFileSync(new URL('../../navigation.ts', import.meta.url), 'utf8');
const navigationComponentSource = readFileSync(new URL('../Navigation.tsx', import.meta.url), 'utf8');
const stylesSource = readFileSync(new URL('../../styles.scss', import.meta.url), 'utf8');

test('protected payroll route and Labour and Payroll navigation target the real workspace', () => {
    assert.match(routesSource, /<Route\s*path="\/payroll"\s*element={<PayrollPage\s*\/>}\s*\/>/);
    assert.match(routesSource, /<Route\s*path="\/labour"\s*element={<LabourPage\s*\/>}\s*\/>/);
    assert.match(navigationSource, /path:\s*'\/payroll',[\s\S]*label:\s*'Labour\s*and\s*Payroll'/);
    assert.match(pageSource, /to="\/labour">Manage\s*workers\s*&amp;\s*rates/);
    assert.match(labourPageSource, /to="\/payroll">Open\s*payroll\s*runs/);
    assert.match(navigationComponentSource, /location\.pathname\s*===\s*'\/labour'\s*\|\|\s*location\.pathname\s*===\s*'\/payroll'/);
});

test('period form emits numeric calendar payload', () => assert.deepEqual(periodPayload('2028', '2'), {
    year: 2028,
    month: 2
}));

test('period mutations submit exact versions and refetch', () => {
    assert.match(pageSource, /api\.open\(period\.id,\s*new\s*VersionedPayrollRequest\(\{\s*expectedVersion:\s*period\.version\s*\}\)\)/);
    assert.match(pageSource, /api\.cancelPeriodPayroll\(period\.id,\s*new\s*CancelPayrollPeriodRequest\(\{\s*expectedVersion:\s*period\.version,\s*reason\s*\}\)\)/);
    assert.match(pageSource, /await\s*reloadCore\(\)/);
});

test('period refresh prefers an open or draft period over cancelled history', () => {
    const periods = [{id: 'cancelled', status: 'Cancelled'}, {id: 'draft', status: 'Draft'}, {
        id: 'open',
        status: 'Open'
    }];
    assert.equal(defaultPeriodId(periods), 'open');
    assert.equal(defaultPeriodId(periods, 'draft'), 'draft');
    assert.equal(defaultPeriodId(periods, 'cancelled'), 'open');
});

test('409 receives a refresh-and-retry message', () => assert.match(payrollErrorMessage({status: 409}), /latest version has been refreshed/i));
test('403 receives a clear permission message', () => assert.match(payrollErrorMessage({status: 403}), /do not have permission/i));
test('404 receives a tenant-safe unavailable-record message', () => assert.match(payrollErrorMessage({status: 404}), /unavailable in your farm/i));
test('400 validation falls through to generated problem details', () => {
    assert.equal(payrollErrorMessage({status: 400}), '');
    assert.match(pageSource, /payrollErrorMessage\(requestError\) \|\| getApiError\(requestError\)/);
});

test('preflight sends filters and page to the generated client', () => assert.match(pageSource, /api\.preflight\(selectedPeriodId, preflightFilters\.workerId[\s\S]*preflightFilters\.page, preflightFilters\.pageSize\)/));
test('preflight renders authoritative worker and evidence-type totals', () => {
    assert.match(pageSource, /preflight\.workerTotals\.map/);
    assert.match(pageSource, /preflight\.evidenceTypeTotals\.map/);
    assert.doesNotMatch(pageSource, /\.filter\([^)]*eligible[^)]*\)\.length/);
});

test('blocked evidence presents stable codes, explanations, and source chain', () => {
    assert.match(pageSource, /evidence\.blockerCodes\.map/);
    assert.match(pageSource, /evidence\.blockerExplanations\[index\]/);
    assert.match(pageSource, /evidence\.sourceChain\.map/);
});

test('monthly evidence shows its calendar-day basis without a blanket submission warning', () => {
    assert.match(pageSource, /One verified calendar day/);
    assert.doesNotMatch(pageSource, /Monthly work requires a proration policy|Submission blocker/);
});

test('payroll run capabilities preserve manager and grower dual control', () => {
    const period = {id: 'period', status: 'Open'};
    assert.equal(canCreatePayrollRun('FarmManager', period, []), true);
    assert.equal(canCreatePayrollRun('Grower', period, []), false);
    assert.equal(canCreatePayrollRun('FarmManager', period, [{
        payrollPeriodId: 'period',
        status: 'Calculated'
    }]), false);
    assert.equal(canCalculatePayrollRun('FarmManager', 'Rejected'), true);
    assert.equal(canCalculatePayrollRun('Grower', 'Calculated'), false);
    assert.equal(canDecidePayrollRun('Grower', 'PendingGrowerApproval'), true);
    assert.equal(canDecidePayrollRun('FarmManager', 'PendingGrowerApproval'), false);
    assert.equal(canCancelPayrollRun('FarmManager', 'Calculated'), true);
    assert.equal(canCancelPayrollRun('FarmManager', 'Approved'), false);
});

test('submission uses authoritative blocker and evidence counts', () => {
    assert.equal(canSubmitPayrollRun('FarmManager', {
        status: 'Calculated',
        calculation: {blockerCount: 0, evidenceCount: 2}
    }), true);
    assert.equal(canSubmitPayrollRun('FarmManager', {
        status: 'Calculated',
        calculation: {blockerCount: 1, evidenceCount: 2}
    }), false);
    assert.equal(canSubmitPayrollRun('Grower', {
        status: 'Calculated',
        calculation: {blockerCount: 0, evidenceCount: 2}
    }), false);
});

test('grower decision payload binds exact run and calculation versions with retry identity', () => {
    const payload = payrollDecisionPayload({
        version: 7,
        submittedCalculationVersion: 3
    }, false, ' stale source ', 'payroll-key');
    assert.deepEqual(payload, {
        expectedVersion: 7,
        calculationVersion: 3,
        approved: false,
        reason: 'stale source',
        idempotencyKey: 'payroll-key'
    });
});

test('payroll run mutations call generated client methods and refresh authoritative data', () => {
    for (const method of ['getRunsPayroll', 'createRunPayroll', 'calculate', 'submitRunPayroll', 'decideRunPayroll', 'cancelRunPayroll']) assert.match(pageSource, new RegExp(`api\\.${method}\\(`));
    assert.match(pageSource, /payroll-decision/);
    assert.match(pageSource, /if \(pending\) return false/);
    assert.match(pageSource, /await reloadCore\(\)/);
});

test('run review renders server totals, blockers, source chain, locking, and stale warning', () => {
    for (const source of ['calculation.grossAmountUsd', 'calculation.deductionAmountUsd', 'calculation.netAmountUsd', 'calculation.blockerCodes.map', 'worker.earnings.map', 'worker.advanceDeductions.map']) assert.match(pageSource, new RegExp(source.replace(/\./g, '\\.')));
    assert.match(pageSource, /Approval revalidates every source/);
    assert.match(pageSource, /Approved and locked/);
    assert.doesNotMatch(pageSource, /reduce\([^)]*grossAmountUsd/);
});

test('advance payload defaults and serializes the authoritative schedule', () => {
    const payload = advancePayload({
        workerId: 'worker',
        amountUsd: '100',
        reason: ' Transport ',
        requestedEventDate: '2028-02-01',
        recoveryStartPayrollPeriodId: 'period',
        installmentCount: '3'
    }, ['a', 'b', 'c']);
    assert.deepEqual(payload, {
        workerId: 'worker',
        amountUsd: 100,
        reason: 'Transport',
        requestedEventDate: '2028-02-01',
        recoveryStartPayrollPeriodId: 'period',
        installmentCount: 3,
        installmentPeriodIds: ['a', 'b', 'c']
    });
    assert.deepEqual(schedulePayload(payload), {
        amountUsd: 100,
        recoveryStartPayrollPeriodId: 'period',
        installmentCount: 3
    });
});

test('schedule preview displays exact total and final residual marker', () => {
    assert.match(pageSource, /preview\.scheduleTotalUsd/);
    assert.match(pageSource, /final residual/);
});

test('role capabilities separate manager submission from grower decision', () => {
    assert.equal(canSubmitAdvance('FarmManager', 'Draft'), true);
    assert.equal(canSubmitAdvance('Grower', 'Draft'), false);
    assert.equal(canDecideAdvance('Grower', 'PendingGrowerApproval'), true);
    assert.equal(canDecideAdvance('FarmManager', 'PendingGrowerApproval'), false);
});

test('draft and rejected advances are editable while only approved advances issue', () => {
    assert.equal(canEditAdvance('Draft'), true);
    assert.equal(canEditAdvance('Rejected'), true);
    assert.equal(canEditAdvance('Issued'), false);
    assert.equal(canIssueAdvance('Approved'), true);
    assert.equal(canIssueAdvance('PendingGrowerApproval'), false);
});

test('advance cancellation uses labelled inline validation and the real endpoint', () => {
    assert.match(pageSource, /Cancellation reason<input/);
    assert.match(pageSource, /api\.cancelAdvancePayroll\(selectedAdvance\.id/);
    assert.doesNotMatch(pageSource, /globalThis\.prompt|window\.prompt/);
});

test('cash issue payload requires acknowledgement fields and exact version', () => {
    const payload = issuePayload({version: 4}, {
        paymentMethod: 'Cash',
        amountUsd: '10.00',
        issuedAt: '2028-02-01T09:00',
        payingPersonId: 'person',
        workerAcknowledged: true
    }, 'stable-key');
    assert.equal(payload.expectedVersion, 4);
    assert.equal(payload.paymentMethod, 0);
    assert.equal(payload.payingPersonId, 'person');
    assert.equal(payload.workerAcknowledged, true);
    assert.equal(payload.idempotencyKey, 'stable-key');
    assert.equal(payload.provider, undefined);
});

test('mobile money payload changes required fields without exposing a masked input contract', () => {
    const payload = issuePayload({version: 5}, {
        paymentMethod: 'MobileMoney',
        amountUsd: '20',
        issuedAt: '2028-02-01T09:00',
        provider: ' EcoCash ',
        recipientNumber: ' 0770000123 ',
        externalReference: ' REF-1 ',
        transactionStatus: ' Confirmed '
    }, 'retry-key');
    assert.equal(payload.paymentMethod, 1);
    assert.equal(payload.recipientNumber, '0770000123');
    assert.equal(payload.provider, 'EcoCash');
    assert.equal(payload.externalReference, 'REF-1');
    assert.equal(payload.idempotencyKey, 'retry-key');
    assert.equal(payload.payingPersonId, undefined);
});

test('issue retry identity is retained in component state and double submits are disabled', () => {
    assert.match(pageSource, /idempotencyKey: newIdempotencyKey\('advance-issue'\)/);
    assert.match(pageSource, /issuePayload\(issueAdvance\.advance, values, issueAdvance\.idempotencyKey\)/);
    assert.match(pageSource, /if \(pending\) return/);
    assert.match(pageSource, /disabled=\{Boolean\(pending\)\}/);
});

test('persisted issue history renders only the server masked recipient', () => {
    assert.match(pageSource, /advance\.issue\.maskedRecipientNumber/);
    assert.doesNotMatch(pageSource, /advance\.issue\.recipientNumber/);
});

test('workspace includes loading, empty, validation, server, conflict, forbidden, and success states', () => {
    assert.match(pageSource, /Loading payroll foundations/);
    assert.match(pageSource, /No payroll periods/);
    assert.match(pageSource, /ValidationError/);
    assert.match(pageSource, /payrollErrorMessage/);
    assert.match(pageSource, /success-banner/);
});

test('responsive desktop tables and mobile cards keep workflows available', () => {
    assert.match(stylesSource, /\.payroll-table-heading,\s*\.payroll-table\s*>\s*article\s*\{\s*display:\s*grid/);
    assert.match(stylesSource, /\.payroll-page\s*>\s*\*\s*\{\s*min-width:\s*0;\s*max-width:\s*100%;\s*\}/);
    assert.match(stylesSource, /\.payroll-page\s*\.section-heading[\s\S]*padding:\s*var\(--space-4\)\s*var\(--space-5\)/);
    assert.match(stylesSource, /\.payroll-page\s*\.record-panel\s*\{\s*min-width:\s*0;\s*overflow:\s*hidden;\s*\}/);
    assert.match(stylesSource, /@media\s*\(max-width:\s*39\.99rem\)[\s\S]*\.period-table\s*>\s*article,\s*\.preflight-table\s*>\s*article\s*\{\s*display:\s*grid;\s*grid-template-columns:\s*1fr/);
    assert.match(stylesSource, /\.advance-actions\s*\{\s*display:\s*flex;\s*flex-wrap:\s*wrap/);
    assert.doesNotMatch(stylesSource, /\.advance-actions \{ position: sticky/);
    assert.match(stylesSource, /\.advance-actions\s*button\s*\{\s*min-height:\s*2\.75rem/);
    assert.match(stylesSource, /\.settlement-worker-heading,\s*\.settlement-worker-row\s*\{\s*display:\s*grid;\s*grid-template-columns:/);
    assert.match(stylesSource, /@media\s*\(max-width:\s*47\.5rem\)[\s\S]*\.settlement-worker-heading\s*\{\s*display:\s*none;\s*\}/);
    assert.match(stylesSource, /\.settlement-worker-row\s*button,\s*\.settlement-actions\s*button\s*\{\s*min-height:\s*44px;\s*width:\s*100%;\s*\}/);
    assert.match(stylesSource, /@media\s*print[\s\S]*\.print-document/);
    assert.match(stylesSource, /\.print-document\s*thead\s*\{\s*display:\s*table-header-group;\s*\}/);
    assert.match(stylesSource, /\.print-document\s*tr\s*\{\s*break-inside:\s*avoid;\s*page-break-inside:\s*avoid;\s*\}/);
});

test('Phase 6C settlement uses generated-client payment and document operations', () => {
    for (const method of ['settlement', 'payments', 'acknowledgement', 'reversal', 'closeSettlementPayroll', 'reopen', 'payslip', 'cashRegister']) assert.match(pageSource, new RegExp(`api\\.${method}\\(`));
    assert.match(pageSource, /Operational\s*payslip/);
    assert.match(pageSource, /Encrypted\s*at\s*rest;\s*only\s*the\s*masked\s*value\s*is\s*displayed/);
    assert.doesNotMatch(pageSource, /globalThis\.prompt|window\.prompt/);
});

test('production workspace calls only generated PayrollClient methods', () => {
    for (const method of ['workspace', 'getPeriodsPayroll', 'getAdvancesPayroll', 'preflight', 'createPeriodPayroll', 'open', 'cancelPeriodPayroll', 'schedulePreview', 'createAdvancePayroll', 'updateWorkerAdvance', 'submitAdvancePayroll', 'decideAdvancePayroll', 'cancelAdvancePayroll', 'issue']) assert.match(pageSource, new RegExp(`api\\.${method}\\(`));
    assert.doesNotMatch(pageSource, /mock|localStorage|sessionStorage|simulate/i);
});
