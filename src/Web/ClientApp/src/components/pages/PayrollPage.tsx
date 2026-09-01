import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { BadgeCheck, Calculator, CalendarDays, ChevronLeft, ChevronRight, CircleAlert, FileText, HandCoins, Link2, LockKeyhole, Printer, RefreshCw, ShieldAlert, WalletCards } from 'lucide-react';
import {
  PayrollClient,
  CancelPayrollPeriodRequest,
  CancelPayrollRunRequest,
  CancelWorkerAdvanceRequest,
  CreatePayrollPeriodRequest,
  CreatePayrollRunRequest,
  CreateWorkerAdvanceRequest,
  DecidePayrollRunRequest,
  DecideWorkerAdvanceRequest,
  IssueWorkerAdvanceRequest,
  PreviewAdvanceScheduleRequest,
  SubmitPayrollRunRequest,
  UpdateWorkerAdvanceRequest,
  VersionedPayrollRequest,
  ClosePayrollSettlementRequest,
  RecordPaymentAcknowledgementRequest,
  RecordPayrollPaymentRequest,
  ReopenPayrollSettlementRequest,
  ReversePayrollPaymentRequest,
  type AdvanceSchedulePreviewDto,
  type PayrollPeriodDto,
  type PayrollPreflightDto,
  type PayrollRunDto,
  type PayrollWorkspaceDto,
  type WorkerAdvanceDto,
  type CashPaymentRegisterDto,
  OperationalPayslipDto,
  type RunSettlementDto,
  type WorkerSettlementDto,
} from '../../web-api-client';
import { PageHeader } from '../PageHeader';
import { LoadingState } from '../LoadingState';
import { ValidationError } from '../ValidationError';
import { getApiError } from '../apiError';
import { advancePayload, apiStatus, canCalculatePayrollRun, canCancelPayrollRun, canCreatePayrollRun, canDecideAdvance, canDecidePayrollRun, canEditAdvance, canIssueAdvance, canSubmitAdvance, canSubmitPayrollRun, defaultAdvanceForm, defaultPeriodId, issuePayload, newIdempotencyKey, payrollDecisionPayload, payrollErrorMessage, periodPayload, schedulePayload } from '../payroll/payrollView';

const api = new PayrollClient();
const today = new Date().toISOString().slice(0, 10);

interface AdvanceFormState {
  id?: string;
  expectedVersion?: number;
  workerId: string;
  amountUsd: string | number;
  reason: string;
  requestedEventDate: string;
  recoveryStartPayrollPeriodId: string;
  installmentCount: string | number;
}

interface PreflightFilters {
  workerId: string;
  eligibility: '' | 'eligible' | 'blocked';
  evidenceType: string;
  page: number;
  pageSize: number;
}

interface IssueAdvanceState {
  advance: WorkerAdvanceDto;
  idempotencyKey: string;
}

type PayrollWorker = PayrollWorkspaceDto['workers'][number];
type PayingPerson = PayrollWorkspaceDto['payingPersons'][number];

export function PayrollPage() {
  const [workspace, setWorkspace] = useState<PayrollWorkspaceDto | null>(null);
  const [periods, setPeriods] = useState<PayrollPeriodDto[]>([]);
  const [advances, setAdvances] = useState<WorkerAdvanceDto[]>([]);
  const [runs, setRuns] = useState<PayrollRunDto[]>([]);
  const [selectedPeriodId, setSelectedPeriodId] = useState('');
  const [selectedAdvanceId, setSelectedAdvanceId] = useState('');
  const [selectedRunId, setSelectedRunId] = useState('');
  const [preflight, setPreflight] = useState<PayrollPreflightDto | null>(null);
  const [preflightFilters, setPreflightFilters] = useState<PreflightFilters>({ workerId: '', eligibility: '', evidenceType: '', page: 1, pageSize: 10 });
  const [loading, setLoading] = useState(true);
  const [preflightLoading, setPreflightLoading] = useState(false);
  const [pending, setPending] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [periodCancelId, setPeriodCancelId] = useState('');
  const [advanceForm, setAdvanceForm] = useState<AdvanceFormState | null>(null);
  const [schedulePreview, setSchedulePreview] = useState<AdvanceSchedulePreviewDto | null>(null);
  const [advanceFilter, setAdvanceFilter] = useState({ status: '', workerId: '' });
  const [decisionReason, setDecisionReason] = useState('');
  const [advanceCancelReason, setAdvanceCancelReason] = useState('');
  const [runDecisionReason, setRunDecisionReason] = useState('');
  const [runCancelReason, setRunCancelReason] = useState('');
  const [issueAdvance, setIssueAdvance] = useState<IssueAdvanceState | null>(null);
  const decisionKeys = useRef(new Map<string, string>());

  const selectedPeriod = periods.find((period) => period.id === selectedPeriodId);
  const selectedAdvance = advances.find((advance) => advance.id === selectedAdvanceId);
  const selectedRun = runs.find((run) => run.id === selectedRunId);

  const reloadCore = useCallback(async () => {
    const [nextWorkspace, nextPeriods, nextAdvances, nextRuns] = await Promise.all([api.workspace(), api.periodsAll(), api.advancesAll(), api.runsAll()]);
    setWorkspace(nextWorkspace); setPeriods(nextPeriods); setAdvances(nextAdvances); setRuns(nextRuns);
    setSelectedPeriodId((current) => defaultPeriodId(nextPeriods, current));
    setSelectedAdvanceId((current) => nextAdvances.some((advance) => advance.id === current) ? current : nextAdvances[0]?.id ?? '');
    setSelectedRunId((current) => nextRuns.some((run) => run.id === current) ? current : nextRuns[0]?.id ?? '');
  }, []);

  const handleError = useCallback(async (requestError: unknown, refresh = false) => {
    if (refresh && apiStatus(requestError) === 409) await reloadCore();
    setSuccess(''); setError(payrollErrorMessage(requestError) || getApiError(requestError));
  }, [reloadCore]);

  useEffect(() => {
    let current = true;
    reloadCore().catch((requestError) => { if (current) handleError(requestError); }).finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, [handleError, reloadCore]);

  const loadPreflight = useCallback(async () => {
    if (!selectedPeriodId) { setPreflight(null); return; }
    setPreflightLoading(true); setError('');
    try {
      const result = await api.preflight(selectedPeriodId, preflightFilters.workerId || undefined, preflightFilters.eligibility === '' ? undefined : preflightFilters.eligibility === 'eligible', preflightFilters.evidenceType || undefined, preflightFilters.page, preflightFilters.pageSize);
      setPreflight(result);
    } catch (requestError) { await handleError(requestError); } finally { setPreflightLoading(false); }
  }, [handleError, preflightFilters, selectedPeriodId]);

  useEffect(() => { loadPreflight(); }, [loadPreflight]);

  const runMutation = async (key: string, action: () => Promise<unknown>, message: string, refresh = true): Promise<boolean> => {
    if (pending) return false;
    setPending(key); setError(''); setSuccess('');
    try { await action(); if (refresh) await reloadCore(); setSuccess(message); return true; }
    catch (requestError) { await handleError(requestError, true); return false; }
    finally { setPending(''); }
  };

  const createPeriod = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    await runMutation('period-create', () => api.periods(new CreatePayrollPeriodRequest(periodPayload(String(data.get('year') ?? ''), String(data.get('month') ?? '')))), 'Draft payroll period created.');
  };

  const cancelPeriod = async (event: FormEvent<HTMLFormElement>, period: PayrollPeriodDto) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); const reason = String(data.get('reason') ?? '').trim();
    if (await runMutation(`period-cancel-${period.id}`, () => api.cancel4(period.id, new CancelPayrollPeriodRequest({ expectedVersion: period.version, reason })), 'Draft payroll period cancelled.')) setPeriodCancelId('');
  };

  const openAdvanceEditor = (advance: WorkerAdvanceDto | null) => {
    setSchedulePreview(null); setError('');
    setAdvanceForm(advance ? { id: advance.id, expectedVersion: advance.version, workerId: advance.workerId, amountUsd: String(advance.requestedAmountUsd), reason: advance.reason, requestedEventDate: isoDate(advance.requestedEventDate), recoveryStartPayrollPeriodId: advance.recoveryStartPayrollPeriodId, installmentCount: advance.installmentCount } : { ...defaultAdvanceForm, requestedEventDate: today, recoveryStartPayrollPeriodId: periods.find((period) => period.status !== 'Cancelled')?.id ?? '' });
  };

  const previewSchedule = async () => {
    if (!advanceForm || pending) return;
    setPending('schedule-preview'); setError('');
    try { setSchedulePreview(await api.schedulePreview(new PreviewAdvanceScheduleRequest(schedulePayload(advanceForm)))); }
    catch (requestError) { await handleError(requestError); } finally { setPending(''); }
  };

  const saveAdvance = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!advanceForm) return;
    if (!schedulePreview) { setError('Preview the authoritative installment schedule before saving.'); return; }
    const payload = advancePayload(advanceForm, schedulePreview.installments.map((item) => item.payrollPeriodId));
    const editingAdvance: { id: string; expectedVersion: number } | undefined = advanceForm.id !== undefined && advanceForm.expectedVersion !== undefined
      ? { id: advanceForm.id, expectedVersion: advanceForm.expectedVersion }
      : undefined;
    const action = editingAdvance
      ? () => api.advancesPUT(editingAdvance.id, new UpdateWorkerAdvanceRequest({ amountUsd: Number(advanceForm.amountUsd), reason: advanceForm.reason.trim(), requestedEventDate: advanceForm.requestedEventDate, recoveryStartPayrollPeriodId: advanceForm.recoveryStartPayrollPeriodId, installmentCount: Number(advanceForm.installmentCount), expectedVersion: editingAdvance.expectedVersion }))
      : () => api.advancesPOST(new CreateWorkerAdvanceRequest(payload));
    if (await runMutation(`advance-save-${advanceForm.id ?? 'new'}`, action, advanceForm.id ? 'Advance draft revised and refreshed.' : 'Advance draft created with its planned recovery schedule.')) { setAdvanceForm(null); setSchedulePreview(null); }
  };

  const decideAdvance = async (advance: WorkerAdvanceDto, approved: boolean) => {
    const keyName = `${advance.id}:${advance.version}:${approved}`;
    if (!decisionKeys.current.has(keyName)) decisionKeys.current.set(keyName, newIdempotencyKey('advance-decision'));
    await runMutation(`advance-decision-${advance.id}`, () => api.decision5(advance.id, new DecideWorkerAdvanceRequest({ expectedVersion: advance.version, approved, reason: approved ? undefined : decisionReason.trim(), idempotencyKey: decisionKeys.current.get(keyName) ?? '' })), approved ? 'Advance approved. No money has been issued.' : 'Advance rejected with an immutable decision record.');
    setDecisionReason('');
  };

  const submitIssue = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!issueAdvance) return; const form = event.currentTarget; const data = new FormData(form);
    const values = String(data.get('paymentMethod')) === 'MobileMoney'
      ? { paymentMethod: 'MobileMoney' as const, amountUsd: String(data.get('amountUsd')), issuedAt: String(data.get('issuedAt')), provider: String(data.get('provider') ?? ''), recipientNumber: String(data.get('recipientNumber') ?? ''), externalReference: String(data.get('externalReference') ?? ''), transactionStatus: String(data.get('transactionStatus') ?? '') }
      : { paymentMethod: 'Cash' as const, amountUsd: String(data.get('amountUsd')), issuedAt: String(data.get('issuedAt')), payingPersonId: String(data.get('payingPersonId') ?? ''), workerAcknowledged: data.get('workerAcknowledged') === 'on' };
    if (await runMutation(`advance-issue-${issueAdvance.advance.id}`, () => api.issue(issueAdvance.advance.id, new IssueWorkerAdvanceRequest(issuePayload(issueAdvance.advance, values, issueAdvance.idempotencyKey))), 'Advance issue evidence recorded. No external payment was executed.')) { form.reset(); setIssueAdvance(null); }
  };

  const createRun = async () => {
    const created = await runMutation('run-create', async () => { const result = await api.runsPOST(new CreatePayrollRunRequest({ payrollPeriodId: selectedPeriodId })); setSelectedRunId(result.id); }, 'Payroll run created. Calculate it from authoritative labour evidence.');
    return created;
  };

  const decideRun = async (approved: boolean) => {
    if (!selectedRun) return; const keyName = `${selectedRun.id}:${selectedRun.submittedCalculationVersion}:${approved}`;
    if (!decisionKeys.current.has(keyName)) decisionKeys.current.set(keyName, newIdempotencyKey('payroll-decision'));
    const payload = payrollDecisionPayload({ version: selectedRun.version, submittedCalculationVersion: selectedRun.submittedCalculationVersion ?? 0 }, approved, runDecisionReason, decisionKeys.current.get(keyName) ?? '');
    await runMutation(`run-decision-${selectedRun.id}`, () => api.decision6(selectedRun.id, new DecidePayrollRunRequest(payload)), approved ? 'Payroll approved and period closed. No money was paid.' : 'Payroll rejected without consuming evidence or recovering advances.');
    setRunDecisionReason('');
  };

  if (loading) return <LoadingState label="Loading payroll foundations" />;
  if (!workspace) return <div className="page-stack"><PageHeader eyebrow="Payroll" title="Payroll foundations" description="Payroll workspace could not be loaded." /><ValidationError message={error || 'Payroll workspace is unavailable.'} /><button type="button" onClick={() => globalThis.location.reload()}>Retry</button></div>;

  const filteredAdvances = advances.filter((advance) => (!advanceFilter.status || advance.status === advanceFilter.status) && (!advanceFilter.workerId || advance.workerId === advanceFilter.workerId));
  const pageCount = preflight ? Math.max(1, Math.ceil(preflight.totalCount / preflight.pageSize)) : 1;

  return <div className="page-stack payroll-page">
    <PageHeader eyebrow="Phase 6B · calculation and dual control" title="Payroll runs" description="Calculate exact monthly earnings and advance recoveries, then send the immutable result to the Grower for approval. Approval closes the period; no money is paid here." />
    <div className="payroll-boundary"><LockKeyhole size={16} aria-hidden="true" /><span><strong>Approval boundary:</strong> previews never consume evidence or record recovery. Only Grower approval locks facts and closes the period.</span><em>{workspace.role}</em></div>
    <div aria-live="polite">{success && <p className="success-banner"><BadgeCheck size={16} /> {success}</p>}<ValidationError message={error} /></div>

    <section className="record-panel payroll-period-panel">
      <header className="section-heading"><div><span className="eyebrow">Calendar-month control</span><h2><CalendarDays size={18} /> Payroll periods</h2></div><small>{periods.length} recorded</small></header>
      <form className="payroll-create-period" onSubmit={createPeriod}>
        <label>Year<input name="year" type="number" min="2000" max="9999" defaultValue={new Date().getFullYear()} required /></label>
        <label>Month<select name="month" defaultValue={new Date().getMonth() + 1}>{monthNames.map((name, index) => <option key={name} value={index + 1}>{name}</option>)}</select></label>
        <button className="primary-action" disabled={Boolean(pending)}>{pending === 'period-create' ? 'Creating…' : 'Create draft period'}</button>
      </form>
      {periods.length === 0 ? <EmptyState title="No payroll periods" copy="Create a calendar month to begin a read-only readiness review or plan advance recovery." /> : <div className="payroll-table period-table" role="table" aria-label="Payroll periods">
        <div className="payroll-table-heading" role="row"><span>Month</span><span>Exact dates</span><span>Status</span><span>Readiness</span><span>Actions</span></div>
        {periods.map((period) => <article key={period.id} className={selectedPeriodId === period.id ? 'is-selected' : ''} role="row">
          <button className="payroll-row-select" type="button" onClick={() => { setSelectedPeriodId(period.id); setPreflightFilters((current) => ({ ...current, page: 1 })); }}><strong>{period.displayName}</strong><small>version {period.version}</small></button>
          <span><b>{formatDate(period.startDate)}</b><small>to {formatDate(period.endDate)}</small></span>
          <StatusBadge status={period.status} />
          <span>{selectedPeriodId === period.id && preflight ? <><b>{preflight.eligibleCount} eligible</b><small>{preflight.blockedCount} blocked · {preflight.eligibleWorkerCount}/{preflight.blockedWorkerCount} workers</small></> : <small>Select to assess</small>}</span>
          <div className="row-actions">{period.status === 'Draft' && <><button type="button" disabled={Boolean(pending)} onClick={() => runMutation(`period-open-${period.id}`, () => api.open(period.id, new VersionedPayrollRequest({ expectedVersion: period.version })), 'Payroll period opened for readiness review.')}>Open</button><button type="button" className="secondary" disabled={Boolean(pending)} onClick={() => setPeriodCancelId(periodCancelId === period.id ? '' : period.id)}>Cancel</button></>}</div>
          {periodCancelId === period.id && <form className="inline-confirm" onSubmit={(event) => cancelPeriod(event, period)}><label>Cancellation reason<input name="reason" maxLength={500} required autoFocus /></label><span><button disabled={Boolean(pending)}>Confirm cancellation</button><button type="button" className="secondary" onClick={() => setPeriodCancelId('')}>Keep draft</button></span></form>}
        </article>)}
      </div>}
    </section>

    <section className="record-panel payroll-preflight-panel">
      <header className="section-heading"><div><span className="eyebrow">Authoritative read-only assessment</span><h2><ShieldAlert size={18} /> Labour eligibility preflight</h2><p>{selectedPeriod ? `${selectedPeriod.displayName} · ${formatDate(selectedPeriod.startDate)}–${formatDate(selectedPeriod.endDate)}` : 'Select a payroll period above.'}</p></div><button type="button" className="secondary" disabled={!selectedPeriodId || preflightLoading} onClick={loadPreflight}><RefreshCw size={15} /> Refresh</button></header>
      <form className="preflight-filters" onSubmit={(event) => { event.preventDefault(); setPreflightFilters((current) => ({ ...current, page: 1 })); loadPreflight(); }}>
        <label>Worker<select value={preflightFilters.workerId} onChange={(event) => setPreflightFilters({ ...preflightFilters, workerId: event.target.value, page: 1 })}><option value="">All workers</option>{workspace.workers.map((worker) => <option key={worker.id} value={worker.id}>{worker.displayName}{worker.status !== 'Active' ? ' · archived' : ''}</option>)}</select></label>
        <label>Eligibility<select value={preflightFilters.eligibility} onChange={(event) => setPreflightFilters({ ...preflightFilters, eligibility: event.target.value === 'eligible' || event.target.value === 'blocked' ? event.target.value : '', page: 1 })}><option value="">Eligible and blocked</option><option value="eligible">Eligible only</option><option value="blocked">Blocked only</option></select></label>
        <label>Evidence type<select value={preflightFilters.evidenceType} onChange={(event) => setPreflightFilters({ ...preflightFilters, evidenceType: event.target.value, page: 1 })}><option value="">All evidence types</option><option value="WorkRecord">Work record</option></select></label>
      </form>
      {!selectedPeriodId ? <EmptyState title="Select a period" copy="Readiness is assessed for one exact calendar month at a time." /> : preflightLoading ? <LoadingState label="Assessing labour evidence" /> : preflight ? <>
        <div className="preflight-summary"><span><small>Eligible evidence</small><strong>{preflight.eligibleCount}</strong></span><span><small>Blocked evidence</small><strong>{preflight.blockedCount}</strong></span><span><small>Eligible workers</small><strong>{preflight.eligibleWorkerCount}</strong></span><span><small>Blocked workers</small><strong>{preflight.blockedWorkerCount}</strong></span></div>
        <div className="monthly-proration-warning"><CircleAlert size={17} /><span><strong>Monthly proration not configured</strong>{preflight.monthlyProrationNotice}</span></div>
        <div className="preflight-group-totals"><div><strong>Worker totals</strong>{preflight.workerTotals.map((total) => <span key={total.workerId}>{total.workerName}<small>{total.eligibleCount} eligible · {total.blockedCount} blocked</small></span>)}</div><div><strong>Evidence-type totals</strong>{preflight.evidenceTypeTotals.map((total) => <span key={total.evidenceType}>{total.evidenceType}<small>{total.eligibleCount} eligible · {total.blockedCount} blocked</small></span>)}</div></div>
        {preflight.evidence.length === 0 ? <EmptyState title="No evidence matches" copy="Change the filters or review another payroll period." /> : <div className="payroll-table preflight-table" role="table" aria-label="Payroll eligibility evidence">
          <div className="payroll-table-heading" role="row"><span>Worker / evidence</span><span>Work context</span><span>Basis / rate</span><span>Readiness</span></div>
          {preflight.evidence.map((evidence) => <article key={evidence.evidenceId} role="row"><span><strong>{evidence.workerName}</strong><small>{evidence.evidenceType} · {formatDate(evidence.eventDate)}</small></span><span><b>{evidence.fieldName}</b><small>{evidence.cropCycleName}</small><small>{evidence.activityNames.join(', ') || 'No valid activity'}</small></span><span><b>{evidence.quantityOrAttendanceBasis}</b><small>Snapshot USD {money(evidence.appliedRateUsd)} · {evidence.payBasis}</small>{evidence.payBasis === 'Monthly' && <em className="monthly-mark">Submission blocker</em>}</span><span><StatusBadge status={evidence.eligible ? 'Eligible' : 'Blocked'} />{!evidence.eligible && <ul className="blocker-list">{evidence.blockerCodes.map((code, index) => <li key={code}><code>{code}</code><span>{evidence.blockerExplanations[index]}</span></li>)}</ul>}<details><summary><Link2 size={14} /> Source chain</summary><ol>{evidence.sourceChain.map((link) => <li key={`${link.sourceType}-${link.sourceId}`}><b>{link.sourceType}</b><span>{link.label}</span><code>{shortId(link.sourceId)}</code></li>)}</ol></details></span></article>)}
        </div>}
        <nav className="payroll-pagination" aria-label="Evidence pages"><button type="button" disabled={preflight.page <= 1} onClick={() => setPreflightFilters({ ...preflightFilters, page: preflight.page - 1 })}><ChevronLeft size={16} /> Previous</button><span>Page {preflight.page} of {pageCount} · {preflight.totalCount} complete results</span><button type="button" disabled={preflight.page >= pageCount} onClick={() => setPreflightFilters({ ...preflightFilters, page: preflight.page + 1 })}>Next <ChevronRight size={16} /></button></nav>
      </> : <EmptyState title="Readiness unavailable" copy="Refresh the selected period to try again." />}
    </section>

    <PayrollRunWorkspace role={workspace.role} periods={periods} selectedPeriod={selectedPeriod} runs={runs} selectedRun={selectedRun} setSelectedRunId={setSelectedRunId} pending={pending} decisionReason={runDecisionReason} setDecisionReason={setRunDecisionReason} cancelReason={runCancelReason} setCancelReason={setRunCancelReason} onCreate={createRun} onCalculate={() => selectedRun ? runMutation(`run-calculate-${selectedRun.id}`, () => api.calculate(selectedRun.id, new VersionedPayrollRequest({ expectedVersion: selectedRun.version })), 'New immutable calculation version created.') : Promise.resolve(false)} onSubmit={() => selectedRun ? runMutation(`run-submit-${selectedRun.id}`, () => api.submit5(selectedRun.id, new SubmitPayrollRunRequest({ expectedVersion: selectedRun.version, calculationVersion: selectedRun.latestCalculationVersion ?? 0 })), 'Exact calculation version sent to the Grower.') : Promise.resolve(false)} onDecide={decideRun} onCancel={async () => { if (selectedRun && await runMutation(`run-cancel-${selectedRun.id}`, () => api.cancel6(selectedRun.id, new CancelPayrollRunRequest({ expectedVersion: selectedRun.version, reason: runCancelReason.trim() })), 'Payroll run cancelled without consuming evidence.')) setRunCancelReason(''); }} />

    <section className="record-panel payroll-advance-panel">
      <header className="section-heading"><div><span className="eyebrow">Issued advance recovery source</span><h2><HandCoins size={18} /> Worker advances</h2></div><button className="primary-action" type="button" onClick={() => openAdvanceEditor(null)}>New advance</button></header>
      <div className="advance-filters"><label>Status<select value={advanceFilter.status} onChange={(event) => setAdvanceFilter({ ...advanceFilter, status: event.target.value })}><option value="">All statuses</option>{['Draft', 'PendingGrowerApproval', 'Approved', 'Rejected', 'Issued', 'Cancelled'].map((status) => <option key={status}>{status}</option>)}</select></label><label>Worker<select value={advanceFilter.workerId} onChange={(event) => setAdvanceFilter({ ...advanceFilter, workerId: event.target.value })}><option value="">All workers</option>{workspace.workers.map((worker) => <option key={worker.id} value={worker.id}>{worker.displayName}</option>)}</select></label></div>
      {advanceForm && <AdvanceEditor form={advanceForm} setForm={(next) => { setAdvanceForm(next); setSchedulePreview(null); }} workers={workspace.workers} periods={periods} preview={schedulePreview} pending={pending} onPreview={previewSchedule} onSubmit={saveAdvance} onClose={() => { setAdvanceForm(null); setSchedulePreview(null); }} />}
      {filteredAdvances.length === 0 ? <EmptyState title="No worker advances" copy="Create a draft to preview a precise monthly recovery plan." /> : <div className="advance-workspace">
        <div className="advance-register">{filteredAdvances.map((advance) => <button type="button" key={advance.id} className={selectedAdvanceId === advance.id ? 'is-selected' : ''} onClick={() => setSelectedAdvanceId(advance.id)}><span><strong>{advance.workerName}</strong><small>{advance.reason}</small></span><span><b>USD {money(advance.requestedAmountUsd)}</b><StatusBadge status={advance.status} /></span></button>)}</div>
        {selectedAdvance && <AdvanceDetails advance={selectedAdvance} periods={periods} role={workspace.role} pending={pending} decisionReason={decisionReason} setDecisionReason={setDecisionReason} cancelReason={advanceCancelReason} setCancelReason={setAdvanceCancelReason} onEdit={() => openAdvanceEditor(selectedAdvance)} onSubmit={() => runMutation(`advance-submit-${selectedAdvance.id}`, () => api.submit4(selectedAdvance.id, new VersionedPayrollRequest({ expectedVersion: selectedAdvance.version })), 'Exact advance version submitted for Grower approval.')} onDecide={(approved) => decideAdvance(selectedAdvance, approved)} onCancel={async () => { if (await runMutation(`advance-cancel-${selectedAdvance.id}`, () => api.cancel5(selectedAdvance.id, new CancelWorkerAdvanceRequest({ expectedVersion: selectedAdvance.version, reason: advanceCancelReason.trim() })), 'Advance cancelled without payroll effect.')) setAdvanceCancelReason(''); }} onIssue={() => setIssueAdvance({ advance: selectedAdvance, idempotencyKey: newIdempotencyKey('advance-issue') })} />}
      </div>}
      {issueAdvance && <IssueForm record={issueAdvance} people={workspace.payingPersons} pending={pending} onSubmit={submitIssue} onClose={() => setIssueAdvance(null)} />}
    </section>
  </div>;
}

function PayrollRunWorkspace({ role, periods, selectedPeriod, runs, selectedRun, setSelectedRunId, pending, decisionReason, setDecisionReason, cancelReason, setCancelReason, onCreate, onCalculate, onSubmit, onDecide, onCancel }: { role: string; periods: PayrollPeriodDto[]; selectedPeriod: PayrollPeriodDto | undefined; runs: PayrollRunDto[]; selectedRun: PayrollRunDto | undefined; setSelectedRunId: (id: string) => void; pending: string; decisionReason: string; setDecisionReason: (value: string) => void; cancelReason: string; setCancelReason: (value: string) => void; onCreate: () => Promise<boolean>; onCalculate: () => Promise<boolean>; onSubmit: () => Promise<boolean>; onDecide: (approved: boolean) => Promise<void>; onCancel: () => Promise<void> }) {
  const calculation = selectedRun?.calculation;
  return <section className="record-panel payroll-run-panel">
    <header className="section-heading"><div><span className="eyebrow">Monthly control ledger</span><h2><Calculator size={18} /> Payroll-run register</h2><p>Every calculation is an immutable version. Amounts below come from the API and are never recomputed in the browser.</p></div>{selectedPeriod && canCreatePayrollRun(role, selectedPeriod, runs) && <button type="button" className="primary-action" disabled={Boolean(pending)} onClick={onCreate}>{pending === 'run-create' ? 'Creating…' : `Create ${selectedPeriod.displayName} run`}</button>}</header>
    {runs.length === 0 ? <EmptyState title="No payroll runs" copy={selectedPeriod?.status === 'Open' ? 'Create the single payroll run for this open period.' : 'Open a payroll period, then create its run.'} /> : <div className="payroll-run-workspace">
      <nav className="payroll-run-register" aria-label="Payroll runs">{runs.map((run) => <button type="button" key={run.id} className={selectedRun?.id === run.id ? 'is-selected' : ''} onClick={() => setSelectedRunId(run.id)}><span><strong>{run.periodName}</strong><small>calculation v{run.latestCalculationVersion || '—'} · run v{run.version}</small></span><span><StatusBadge status={run.status} /><b>USD {money(run.calculation?.netAmountUsd)}</b></span></button>)}</nav>
      {selectedRun && <article className="payroll-run-detail">
        <header className="payroll-run-title"><div><span className="eyebrow">{selectedRun.periodName} · exact run version {selectedRun.version}</span><h3>{selectedRun.status === 'Approved' ? 'Approved payroll facts' : 'Calculation review'}</h3></div><StatusBadge status={selectedRun.status} /></header>
        {selectedRun.status === 'Approved' && <div className="payroll-lock-notice"><LockKeyhole size={17} /><span><strong>Approved and locked</strong>This period is {selectedRun.periodStatus.toLowerCase()}. Evidence consumption and advance recovery are immutable; no payment was executed.</span></div>}
        {selectedRun.status === 'Rejected' && <div className="monthly-proration-warning"><CircleAlert size={17} /><span><strong>Grower rejected calculation v{selectedRun.submittedCalculationVersion}</strong>{selectedRun.rejectionReason}</span></div>}
        {calculation ? <>
          <div className="payroll-proof-strip" aria-label="Authoritative payroll totals"><span><small>Workers</small><strong>{calculation.workerCount}</strong></span><span><small>Evidence</small><strong>{calculation.evidenceCount}</strong></span><span><small>Gross USD</small><strong>{money(calculation.grossAmountUsd)}</strong></span><span><small>Advance recovery</small><strong>{money(calculation.deductionAmountUsd)}</strong></span><span className="is-net"><small>Net USD</small><strong>{money(calculation.netAmountUsd)}</strong></span></div>
          <div className="payroll-calculation-proof"><span><b>Calculation v{calculation.calculationVersion}</b><small>{formatDateTime(calculation.calculatedAt)}</small></span><span><b>{calculation.blockerCount ? `${calculation.blockerCount} blocker${calculation.blockerCount === 1 ? '' : 's'}` : 'Complete and ready'}</b><small>source fingerprint {shortId(calculation.sourceFingerprint)}</small></span></div>
          {calculation.blockerCount > 0 && <div className="payroll-blockers"><strong>Submission blocked</strong><p>Resolve every authoritative source issue, then calculate a new version.</p><div>{calculation.blockerCodes.map((code) => <code key={code}>{code}</code>)}</div></div>}
          {selectedRun.status === 'PendingGrowerApproval' && <div className="stale-calculation-warning"><RefreshCw size={16} /><span><strong>Approval revalidates every source.</strong>If labour evidence, verification, snapshots, worker state, advances, balances, or the period changed, approval returns a conflict and creates no facts.</span></div>}
          <div className="payroll-worker-table" role="table" aria-label="Worker payroll calculation"><div className="payroll-worker-heading" role="row"><span>Worker / source</span><span>Gross</span><span>Advance</span><span>Net</span></div>{calculation.workers.map((worker) => <details key={worker.id} className="payroll-worker-row"><summary><span><strong>{worker.workerName}</strong><small>{worker.earnings.length} earning line{worker.earnings.length === 1 ? '' : 's'}</small></span><b>USD {money(worker.grossAmountUsd)}</b><b>USD {money(worker.deductionAmountUsd)}</b><strong>USD {money(worker.netAmountUsd)}</strong></summary><div className="payroll-source-chain"><section><h4>Earning evidence</h4>{worker.earnings.map((line) => <article key={line.id}><span><b>{line.rateType} · {formatDate(line.workDate)}</b><small>{line.quantity} {line.unit} × USD {line.rateAmountUsd}</small></span><strong>USD {money(line.earningAmountUsd)}</strong><small>Evidence {shortId(line.evidenceId)} · attendance {shortId(line.attendanceId)} · field {shortId(line.fieldId)} · rate v{line.rateVersion}</small></article>)}</section><section><h4>Advance allocation</h4>{worker.advanceDeductions.length ? worker.advanceDeductions.map((deduction) => <article key={deduction.id}><span><b>Installment {deduction.installmentSequence}</b><small>Outstanding before USD {money(deduction.outstandingBeforeUsd)}</small></span><strong>USD {money(deduction.amountUsd)}</strong><small>Advance {shortId(deduction.workerAdvanceId)} · due period {periods.find((period) => period.id === deduction.recoveryPayrollPeriodId)?.displayName ?? shortId(deduction.recoveryPayrollPeriodId)}</small></article>) : <p>No due issued advance recovery.</p>}</section></div></details>)}</div>
          {selectedRun.status === 'Approved' && <SettlementWorkspace run={selectedRun} role={role} />}
        </> : <EmptyState title="Not calculated" copy="Calculate to include every authoritative eligible labour record automatically." />}
        {canDecidePayrollRun(role, selectedRun.status) && <label className="rejection-reason">Rejection reason<input value={decisionReason} onChange={(event) => setDecisionReason(event.target.value)} maxLength={500} placeholder="Required only when rejecting" /></label>}
        {canCancelPayrollRun(role, selectedRun.status) && <label className="rejection-reason">Cancellation reason<input value={cancelReason} onChange={(event) => setCancelReason(event.target.value)} maxLength={500} placeholder="Required to cancel this run" /></label>}
        <footer className="payroll-run-actions">{canCalculatePayrollRun(role, selectedRun.status) && <button type="button" disabled={Boolean(pending)} onClick={onCalculate}>{calculation ? 'Calculate new version' : 'Calculate all evidence'}</button>}{canSubmitPayrollRun(role, selectedRun) && <button type="button" disabled={Boolean(pending)} onClick={onSubmit}>Send calculation v{selectedRun.latestCalculationVersion} to Grower</button>}{canDecidePayrollRun(role, selectedRun.status) && <><button type="button" disabled={Boolean(pending)} onClick={() => onDecide(true)}>Approve exact version and close period</button><button type="button" className="secondary" disabled={Boolean(pending) || !decisionReason.trim()} onClick={() => onDecide(false)}>Reject with reason</button></>}{canCancelPayrollRun(role, selectedRun.status) && <button type="button" className="text-action" disabled={Boolean(pending) || !cancelReason.trim()} onClick={onCancel}>Cancel run</button>}{role === 'FarmManager' && selectedRun.status === 'PendingGrowerApproval' && <small>Grower decision required. Manager approval attempts are rejected by the API.</small>}</footer>
      </article>}
    </div>}
  </section>;
}

function SettlementWorkspace({ run, role }: { run: PayrollRunDto; role: string }) {
  const [summary, setSummary] = useState<RunSettlementDto | null>(null);
  const [paymentWorker, setPaymentWorker] = useState<WorkerSettlementDto | null>(null);
  const [method, setMethod] = useState('Cash');
  const [busy, setBusy] = useState('');
  const [message, setMessage] = useState('');
  const [settlementError, setSettlementError] = useState('');
  const [document, setDocument] = useState<OperationalPayslipDto | CashPaymentRegisterDto | null>(null);
  const [reversalPayment, setReversalPayment] = useState<{ id: string; amount: number } | null>(null);
  const [reversalReason, setReversalReason] = useState('');
  const [reopenReason, setReopenReason] = useState('');

  const load = useCallback(async () => setSummary(await api.settlement(run.id)), [run.id]);
  useEffect(() => { load().catch(async (requestError) => setSettlementError(getApiError(requestError))); }, [load]);

  const mutate = async (key: string, action: () => Promise<unknown>, success: string) => {
    if (busy) return; setBusy(key); setSettlementError(''); setMessage('');
    try { await action(); await load(); setPaymentWorker(null); setMessage(success); }
    catch (requestError) { setSettlementError(getApiError(requestError)); }
    finally { setBusy(''); }
  };

  const recordPayment = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!paymentWorker || !summary) return;
    const data = new FormData(event.currentTarget); const selectedMethod = String(data.get('method'));
    await mutate('payment', () => api.payments(run.id, new RecordPayrollPaymentRequest({
      calculationVersion: summary.calculationVersion, payrollWorkerLineId: paymentWorker.payrollWorkerLineId,
      method: selectedMethod, amountUsd: Number(data.get('amountUsd')), paymentDate: String(data.get('paymentDate')),
      provider: selectedMethod === 'MobileMoney' ? String(data.get('provider')) : undefined,
      recipientNumber: selectedMethod === 'MobileMoney' ? String(data.get('recipientNumber')) : undefined,
      transactionReference: selectedMethod === 'MobileMoney' ? String(data.get('transactionReference')) : undefined,
      externalStatus: selectedMethod === 'MobileMoney' ? String(data.get('externalStatus')) : undefined,
      idempotencyKey: newIdempotencyKey('payroll-payment'),
    })), 'Payment evidence recorded. No external payment was executed.');
  };

  const acknowledge = (paymentId: string) => mutate(`ack-${paymentId}`, () => api.acknowledgement(paymentId,
    new RecordPaymentAcknowledgementRequest({ status: 'Acknowledged', acknowledgedByPersonId: undefined,
      acknowledgedAt: new Date(), evidenceReference: undefined,
      idempotencyKey: newIdempotencyKey('payment-acknowledgement') })), 'Payment acknowledgement recorded.');

  const reverse = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); if (!reversalPayment || !reversalReason.trim()) return;
    await mutate(`reverse-${reversalPayment.id}`, () => api.reversal(reversalPayment.id, new ReversePayrollPaymentRequest({
      amountUsd: reversalPayment.amount, reason: reversalReason.trim(), idempotencyKey: newIdempotencyKey('payment-reversal') })), 'Payment reversal recorded; the original remains visible.');
    setReversalPayment(null); setReversalReason('');
  };

  const showPayslip = async (worker: WorkerSettlementDto) => {
    if (!summary) return; setBusy(`payslip-${worker.payrollWorkerLineId}`); setSettlementError('');
    try { setDocument(await api.payslip(run.id, summary.calculationVersion, worker.payrollWorkerLineId)); }
    catch (requestError) { setSettlementError(getApiError(requestError)); } finally { setBusy(''); }
  };
  const showRegister = async () => {
    if (!summary) return; setBusy('cash-register'); setSettlementError('');
    try { setDocument(await api.cashRegister(run.id, summary.calculationVersion)); }
    catch (requestError) { setSettlementError(getApiError(requestError)); } finally { setBusy(''); }
  };

  if (!summary) return <LoadingState label="Loading authoritative settlement" />;
  return <section className="settlement-workspace">
    <header className="section-heading"><div><span className="eyebrow">Post-approval settlement</span><h3><HandCoins size={18} /> Payment and settlement</h3><p>Calculation v{summary.calculationVersion} · payment evidence only; Cane360 does not execute funds.</p></div><StatusBadge status={summary.settlementStatus} /></header>
    <ValidationError message={settlementError} />{message && <p className="success-banner"><BadgeCheck size={16} />{message}</p>}
    <div className="settlement-metrics"><span><small>Net payroll</small><strong>USD {money(summary.netAmountUsd)}</strong></span><span><small>Paid</small><strong>USD {money(summary.paidAmountUsd)}</strong></span><span><small>Outstanding</small><strong>USD {money(summary.outstandingAmountUsd)}</strong></span><span><small>Workers settled</small><strong>{summary.workersSettled}/{summary.workerCount}</strong></span><span><small>Acknowledgement exceptions</small><strong>{summary.acknowledgementExceptions}</strong></span></div>
    {summary.isClosed && <div className="payroll-lock-notice"><LockKeyhole size={17} /><span><strong>Settlement closed</strong>Payment actions are read-only. The Phase 6B labour and approved calculation locks remain unchanged.</span></div>}
    <div className="settlement-worker-list" role="table" aria-label="Payroll worker settlement">
      <div className="settlement-worker-heading" role="row"><span>Worker</span><span>Gross</span><span>Deductions</span><span>Net</span><span>Paid</span><span>Outstanding</span><span>Status / actions</span></div>
      {summary.workers.map((worker) => <article key={worker.payrollWorkerLineId} className="settlement-worker-row">
        <span><strong>{worker.workerName}</strong><small>{worker.paymentMethodSummary || 'No active payment'}</small></span><b>USD {money(worker.grossAmountUsd)}</b><b>USD {money(worker.deductionAmountUsd)}</b><b>USD {money(worker.approvedNetUsd)}</b><b>USD {money(worker.validPaidAmountUsd)}</b><strong>USD {money(worker.outstandingAmountUsd)}</strong>
        <div className="row-actions"><StatusBadge status={worker.settlementStatus} />{!summary.isClosed && worker.outstandingAmountUsd > 0 && <button type="button" onClick={() => setPaymentWorker(worker)}>Record payment</button>}<button type="button" className="secondary" disabled={Boolean(busy)} onClick={() => showPayslip(worker)}><FileText size={14} /> Payslip</button></div>
        {worker.payments.length > 0 && <details className="payment-chain"><summary>{worker.payments.length} payment record{worker.payments.length === 1 ? '' : 's'} · view trace</summary>{worker.payments.map((payment) => <div key={payment.id}><span><b>{payment.method} · USD {money(payment.amountUsd)}</b><small>{formatDate(payment.paymentDate)} · {payment.externalStatus}{payment.provider ? ` · ${payment.provider}` : ''}{payment.maskedRecipientNumber ? ` · ${payment.maskedRecipientNumber}` : ''}{payment.transactionReference ? ` · ${payment.transactionReference}` : ''}</small></span><span><small>Recorded {formatDateTime(payment.createdAt)} · active USD {money(payment.activeAmountUsd)}</small><small>{payment.acknowledgement ? `Acknowledgement: ${payment.acknowledgement.status}` : payment.method === 'Cash' ? 'Acknowledgement outstanding' : 'Acknowledgement not required'}</small></span><div className="row-actions">{!summary.isClosed && payment.method === 'Cash' && !payment.acknowledgement && payment.activeAmountUsd > 0 && <button type="button" disabled={Boolean(busy)} onClick={() => acknowledge(payment.id)}>Acknowledge</button>}{!summary.isClosed && payment.activeAmountUsd > 0 && <button type="button" className="text-action" disabled={Boolean(busy)} onClick={() => setReversalPayment({ id: payment.id, amount: payment.activeAmountUsd })}>Reverse</button>}</div>{payment.reversals.map((item) => <small key={item.id} className="reversal-line">Reversed USD {money(item.amountUsd)} · {item.reason} · {formatDateTime(item.reversedAt)}</small>)}</div>)}</details>}
      </article>)}
    </div>
    <footer className="settlement-actions"><button type="button" className="secondary" disabled={Boolean(busy)} onClick={showRegister}><Printer size={15} /> Cash register</button>{!summary.isClosed && <button type="button" disabled={Boolean(busy) || !summary.canClose} onClick={() => mutate('close-settlement', () => api.close2(run.id, new ClosePayrollSettlementRequest({ calculationVersion: summary.calculationVersion, idempotencyKey: newIdempotencyKey('settlement-close') })), 'Final payroll settlement closed.')}>Close settlement</button>}{summary.isClosed && role === 'Grower' && <><label>Reopen reason<input value={reopenReason} maxLength={500} onChange={(event) => setReopenReason(event.target.value)} /></label><button type="button" className="secondary" disabled={Boolean(busy) || !reopenReason.trim()} onClick={() => mutate('reopen-settlement', () => api.reopen(run.id, new ReopenPayrollSettlementRequest({ calculationVersion: summary.calculationVersion, reason: reopenReason.trim(), idempotencyKey: newIdempotencyKey('settlement-reopen') })), 'Settlement reopened for payment-side correction only.')}>Reopen settlement</button></>}</footer>
    {paymentWorker && <form className="payment-form" onSubmit={recordPayment}><header><div><span className="eyebrow">Exact approved worker line</span><h4>Record payment · {paymentWorker.workerName}</h4></div><button type="button" className="secondary" onClick={() => setPaymentWorker(null)}>Close</button></header><p>Outstanding USD {money(paymentWorker.outstandingAmountUsd)}. Client totals are revalidated transactionally by the server.</p><div className="form-grid"><label>Method<select name="method" value={method} onChange={(event) => setMethod(event.target.value)}><option value="Cash">Cash</option><option value="MobileMoney">Mobile money</option></select></label><label>Amount (USD)<input name="amountUsd" type="number" min="0.01" max={paymentWorker.outstandingAmountUsd} step="0.01" defaultValue={paymentWorker.outstandingAmountUsd} required /></label><label>Actual payment date<input name="paymentDate" type="date" defaultValue={today} required /></label>{method === 'MobileMoney' && <><label>Provider<input name="provider" maxLength={80} required autoComplete="off" /></label><label>Recipient number<input name="recipientNumber" type="tel" minLength={4} maxLength={20} required autoComplete="off" /><small>Encrypted at rest; only the masked value is displayed.</small></label><label>Transaction reference<input name="transactionReference" maxLength={160} required autoComplete="off" /></label><label>External status<select name="externalStatus" defaultValue="Successful"><option>Successful</option><option>Posted</option><option>Pending</option><option>Failed</option></select></label></>}</div><footer className="form-actions"><span>Retry-safe idempotency is generated for this submission.</span><button disabled={Boolean(busy)}>{busy === 'payment' ? 'Recording…' : 'Record payment evidence'}</button></footer></form>}
    {reversalPayment && <form className="payment-form" onSubmit={reverse}><header><div><span className="eyebrow">Append-only correction</span><h4>Reverse USD {money(reversalPayment.amount)}</h4></div><button type="button" className="secondary" onClick={() => setReversalPayment(null)}>Close</button></header><label>Mandatory reason<textarea value={reversalReason} maxLength={500} required onChange={(event) => setReversalReason(event.target.value)} /></label><footer className="form-actions"><span>The original payment remains unchanged and visible.</span><button disabled={Boolean(busy) || !reversalReason.trim()}>Record reversal</button></footer></form>}
    {document && <PrintablePayrollDocument document={document} onClose={() => setDocument(null)} />}
  </section>;
}

function PrintablePayrollDocument({ document, onClose }: { document: OperationalPayslipDto | CashPaymentRegisterDto; onClose: () => void }) {
  const isPayslip = document instanceof OperationalPayslipDto;
  return <section className="print-document"><header className="print-toolbar"><button type="button" className="secondary" onClick={onClose}>Close</button><button type="button" onClick={() => globalThis.print()}><Printer size={15} /> Print</button></header>{isPayslip ? <><span className="eyebrow">Cane360 · {document.farmName}</span><h2>Operational payslip</h2><p><strong>{document.documentStatement}</strong></p><dl><div><dt>Payroll period</dt><dd>{document.payrollPeriod}</dd></div><div><dt>Worker</dt><dd>{document.workerName}</dd></div><div><dt>Worker identifier</dt><dd>{document.maskedWorkerIdentifier}</dd></div><div><dt>Approved version</dt><dd>Calculation v{document.calculationVersion} · {shortId(document.payrollRunId)}</dd></div></dl><table><thead><tr><th>Earning</th><th>Quantity / rate</th><th>USD</th></tr></thead><tbody>{document.earnings.map((line) => <tr key={line.id}><td>{line.rateType} · {formatDate(line.workDate)}</td><td>{line.quantity} {line.unit} × {money(line.rateAmountUsd)}</td><td>{money(line.earningAmountUsd)}</td></tr>)}</tbody><tfoot><tr><th colSpan={2}>Gross</th><td>{money(document.grossAmountUsd)}</td></tr><tr><th colSpan={2}>Deductions / advance recovery</th><td>{money(document.deductionAmountUsd)}</td></tr><tr><th colSpan={2}>Net pay</th><td>{money(document.netAmountUsd)}</td></tr><tr><th colSpan={2}>Paid / outstanding</th><td>{money(document.paidAmountUsd)} / {money(document.outstandingAmountUsd)}</td></tr></tfoot></table><small>Generated {formatDateTime(document.generatedAt)} · {document.documentReference}</small></> : <><span className="eyebrow">Cane360 · {document.farmName}</span><h2>Cash payment register</h2><p>{document.payrollPeriod} · calculation v{document.calculationVersion}</p><table><thead><tr><th>Worker</th><th>Identifier</th><th>Approved net</th><th>Cash paid</th><th>Payment date</th><th>Acknowledgement</th><th>Outstanding</th></tr></thead><tbody>{document.workers.map((worker) => <tr key={worker.payrollWorkerLineId}><td>{worker.workerName}</td><td>{worker.maskedWorkerIdentifier}</td><td>{money(worker.approvedNetUsd)}</td><td>{money(worker.cashAmountPaidUsd)}</td><td>{worker.lastCashPaymentDate ? formatDate(worker.lastCashPaymentDate) : '—'}</td><td>{worker.acknowledgementState}</td><td>{money(worker.outstandingAmountUsd)}</td></tr>)}</tbody><tfoot><tr><th colSpan={2}>Totals</th><td>{money(document.totalApprovedNetUsd)}</td><td>{money(document.totalActiveCashPaidUsd)}</td><td colSpan={2}></td><td>{money(document.totalOutstandingUsd)}</td></tr></tfoot></table><small>Generated {formatDateTime(document.generatedAt)} · {document.documentReference}</small></>}</section>;
}

function AdvanceEditor({ form, setForm, workers, periods, preview, pending, onPreview, onSubmit, onClose }: { form: AdvanceFormState; setForm: (form: AdvanceFormState) => void; workers: PayrollWorker[]; periods: PayrollPeriodDto[]; preview: AdvanceSchedulePreviewDto | null; pending: string; onPreview: () => Promise<void>; onSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>; onClose: () => void }) {
  const activeWorkers = workers.filter((worker) => worker.status === 'Active');
  return <form className="advance-editor" onSubmit={onSubmit}><header><div><span className="eyebrow">Authoritative installment plan</span><h3>{form.id ? 'Revise advance draft' : 'Create advance draft'}</h3></div><button type="button" className="secondary" onClick={onClose}>Close</button></header><div className="form-grid">
    <label>Worker<select required value={form.workerId} disabled={Boolean(form.id)} onChange={(event) => setForm({ ...form, workerId: event.target.value })}><option value="">Select active worker</option>{activeWorkers.map((worker) => <option key={worker.id} value={worker.id}>{worker.displayName}</option>)}</select></label>
    <label>Amount (USD)<input required type="number" min="0.01" step="0.01" value={form.amountUsd} onChange={(event) => setForm({ ...form, amountUsd: event.target.value })} /></label>
    <label className="is-wide">Reason<textarea required maxLength={500} value={form.reason} onChange={(event) => setForm({ ...form, reason: event.target.value })} /></label>
    <label>Requested event date<input required type="date" value={form.requestedEventDate} onChange={(event) => setForm({ ...form, requestedEventDate: event.target.value })} /></label>
    <label>Recovery starts<select required value={form.recoveryStartPayrollPeriodId} onChange={(event) => setForm({ ...form, recoveryStartPayrollPeriodId: event.target.value })}><option value="">Select payroll period</option>{periods.filter((period) => period.status !== 'Cancelled').slice().reverse().map((period) => <option key={period.id} value={period.id}>{period.displayName} · {period.status}</option>)}</select></label>
    <label>Installment count<input required type="number" min="1" max="60" step="1" value={form.installmentCount} onChange={(event) => setForm({ ...form, installmentCount: event.target.value })} /><small>Defaults to three; choose another positive count deliberately.</small></label>
  </div><div className="schedule-actions"><button type="button" className="secondary" disabled={Boolean(pending)} onClick={onPreview}>{pending === 'schedule-preview' ? 'Previewing…' : 'Preview authoritative schedule'}</button>{preview && <SchedulePreview preview={preview} periods={periods} />}</div><footer className="form-actions"><span>{preview ? `Exact schedule total: USD ${money(preview.scheduleTotalUsd)}` : 'Preview required before saving'}</span><button disabled={Boolean(pending) || !preview}>{pending.startsWith('advance-save') ? 'Saving…' : 'Save advance draft'}</button></footer></form>;
}

function SchedulePreview({ preview, periods }: { preview: AdvanceSchedulePreviewDto; periods: PayrollPeriodDto[] }) {
  return <div className="schedule-preview"><header><strong>{preview.installmentCount} planned installments</strong><span>USD {money(preview.scheduleTotalUsd)}</span></header>{preview.installments.map((installment, index) => <span key={installment.sequence}><b>{installment.sequence}</b><small>{periods.find((period) => period.id === installment.payrollPeriodId)?.displayName ?? 'Payroll period'}</small><strong>USD {money(installment.amountUsd)}</strong>{index === preview.installments.length - 1 && <em>final residual</em>}</span>)}</div>;
}

function AdvanceDetails({ advance, periods, role, pending, decisionReason, setDecisionReason, cancelReason, setCancelReason, onEdit, onSubmit, onDecide, onCancel, onIssue }: { advance: WorkerAdvanceDto; periods: PayrollPeriodDto[]; role: string; pending: string; decisionReason: string; setDecisionReason: (value: string) => void; cancelReason: string; setCancelReason: (value: string) => void; onEdit: () => void; onSubmit: () => Promise<boolean>; onDecide: (approved: boolean) => Promise<void>; onCancel: () => Promise<void>; onIssue: () => void }) {
  const scheduleTotal = advance.installments.reduce((total, installment) => total + installment.amountUsd, 0);
  return <article className="advance-details"><header><div><span className="eyebrow">Authoritative advance record</span><h3>{advance.workerName}</h3><small>Requested {formatDate(advance.requestedEventDate)} · version {advance.version}</small></div><StatusBadge status={advance.status} /></header><div className="advance-metrics"><span><small>Requested</small><strong>USD {money(advance.requestedAmountUsd)}</strong></span><span><small>Approved</small><strong>{advance.approvedAmountUsd == null ? '—' : `USD ${money(advance.approvedAmountUsd)}`}</strong></span><span><small>Outstanding</small><strong>USD {money(advance.outstandingAmountUsd)}</strong></span><span><small>Installments</small><strong>{advance.installmentCount}</strong></span></div><p>{advance.reason}</p>
    <div className="planned-installments"><header><strong>Recovery schedule</strong><span>Total USD {money(scheduleTotal)}</span></header>{advance.installments.map((installment) => <span key={installment.sequence}><b>{installment.sequence}</b><small>{periods.find((period) => period.id === installment.payrollPeriodId)?.displayName ?? shortId(installment.payrollPeriodId)}</small><strong>USD {money(installment.amountUsd)}</strong></span>)}<p>Due issued installments become immutable recovery facts only on Grower approval.</p></div>
    <section className="approval-history"><h4>Approval history</h4>{advance.approvalHistory.length ? advance.approvalHistory.map((approval) => <span key={`${approval.advanceVersion}-${approval.decidedAt.toISOString()}`}><StatusBadge status={approval.approved ? 'Approved' : 'Rejected'} /><b>version {approval.advanceVersion}</b><small>{formatDateTime(approval.decidedAt)}{approval.reason ? ` · ${approval.reason}` : ''}</small></span>) : <p>No Grower decision recorded.</p>}</section>
    <section className="issue-history"><h4>Issue evidence</h4>{advance.issue ? <div><StatusBadge status="Issued" /><strong>{advance.issue.paymentMethod} · USD {money(advance.issue.amountUsd)}</strong><small>{formatDateTime(advance.issue.issuedAt)}</small>{advance.issue.paymentMethod === 'Cash' ? <small>Paying person recorded · receiving worker acknowledged</small> : <small>{advance.issue.provider} · {advance.issue.maskedRecipientNumber} · {advance.issue.externalReference} · {advance.issue.transactionStatus}</small>}</div> : <p>Not issued. Approval never issues money automatically.</p>}</section>
    {canDecideAdvance(role, advance.status) && <label className="rejection-reason">Rejection reason<input value={decisionReason} onChange={(event) => setDecisionReason(event.target.value)} maxLength={500} placeholder="Required only when rejecting" /></label>}
    {canEditAdvance(advance.status) && <label className="rejection-reason">Cancellation reason<input value={cancelReason} onChange={(event) => setCancelReason(event.target.value)} maxLength={500} placeholder="Required to cancel this draft" /></label>}
    <footer className="advance-actions">{canEditAdvance(advance.status) && <><button type="button" className="secondary" onClick={onEdit}>Edit draft</button><button type="button" className="text-action" disabled={Boolean(pending) || !cancelReason.trim()} onClick={onCancel}>Cancel advance</button></>}{canSubmitAdvance(role, advance.status) && <button type="button" disabled={Boolean(pending)} onClick={onSubmit}>Submit exact version</button>}{canDecideAdvance(role, advance.status) && <><button type="button" disabled={Boolean(pending)} onClick={() => onDecide(true)}>Approve</button><button type="button" className="secondary" disabled={Boolean(pending) || !decisionReason.trim()} onClick={() => onDecide(false)}>Reject</button></>}{canIssueAdvance(advance.status) && <button type="button" disabled={Boolean(pending)} onClick={onIssue}><WalletCards size={15} /> Record issue evidence</button>}{role === 'FarmManager' && advance.status === 'PendingGrowerApproval' && <small>Grower approval required. The API rejects manager decision attempts with 403.</small>}</footer>
  </article>;
}

function IssueForm({ record, people, pending, onSubmit, onClose }: { record: IssueAdvanceState; people: PayingPerson[]; pending: string; onSubmit: (event: FormEvent<HTMLFormElement>) => Promise<void>; onClose: () => void }) {
  const [method, setMethod] = useState('Cash'); const approved = record.advance.approvedAmountUsd;
  return <form className="issue-form" onSubmit={onSubmit}><header><div><span className="eyebrow">Operational evidence only</span><h3>Record advance issue</h3></div><button type="button" className="secondary" onClick={onClose}>Close</button></header><p>No external provider call or payment execution occurs. Amount is locked to the exact Grower-approved USD amount.</p><div className="form-grid"><label>Issue method<select name="paymentMethod" value={method} onChange={(event) => setMethod(event.target.value)}><option value="Cash">Cash</option><option value="MobileMoney">Mobile Money</option></select></label><label>Exact approved amount (USD)<input name="amountUsd" type="number" step="0.01" value={approved ?? ''} readOnly required /></label><label>Issue / transaction date and time<input name="issuedAt" type="datetime-local" defaultValue={localDateTimeInputValue()} required /></label>{method === 'Cash' ? <><label>Paying person<select name="payingPersonId" required defaultValue=""><option value="">Select paying person</option>{people.map((person) => <option key={person.id} value={person.id}>{person.displayName}</option>)}</select></label><label>Receiving worker<input value={record.advance.workerName} readOnly /></label><label className="acknowledgement"><input name="workerAcknowledged" type="checkbox" required /><span>Worker acknowledgement received</span></label></> : <><label>Provider<input name="provider" maxLength={100} required autoComplete="off" /></label><label>Recipient number<input name="recipientNumber" type="tel" minLength={4} maxLength={32} required autoComplete="off" aria-describedby="recipient-mask-note" /><small id="recipient-mask-note">Only a masked value is returned after recording.</small></label><label>External reference<input name="externalReference" maxLength={160} required autoComplete="off" /></label><label>Transaction status<input name="transactionStatus" maxLength={64} required placeholder="e.g. Confirmed" /></label></>}</div><footer className="form-actions"><span>Retry identity is retained until this form succeeds or closes.</span><button disabled={Boolean(pending)}>{pending.startsWith('advance-issue') ? 'Recording…' : 'Record issue evidence'}</button></footer></form>;
}

function StatusBadge({ status }: { status: string }) { return <em className={`status-pill status-${String(status).toLowerCase()}`}>{status === 'PendingGrowerApproval' ? 'Pending Grower approval' : status}</em>; }
function EmptyState({ title, copy }: { title: string; copy: string }) { return <div className="payroll-empty"><CircleAlert size={20} /><span><strong>{title}</strong><small>{copy}</small></span></div>; }
function formatDate(value: Date | string | undefined) { const date = value instanceof Date ? value : new Date(value ?? ''); return Number.isNaN(date.getTime()) ? String(value ?? '') : date.toLocaleDateString('en-ZW', { day: '2-digit', month: 'short', year: 'numeric' }); }
function formatDateTime(value: Date | string | undefined) { const date = value instanceof Date ? value : new Date(value ?? ''); return Number.isNaN(date.getTime()) ? String(value ?? '') : date.toLocaleString('en-ZW', { dateStyle: 'medium', timeStyle: 'short' }); }
function isoDate(value: Date | string | undefined) { const date = value instanceof Date ? value : new Date(value ?? ''); return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10); }
function localDateTimeInputValue() { const now = new Date(); return new Date(now.getTime() - (now.getTimezoneOffset() * 60000)).toISOString().slice(0, 16); }
function money(value: number | undefined | null) { return Number(value ?? 0).toFixed(2); }
function shortId(value: string | undefined | null) { return value ? `${String(value).slice(0, 8)}…` : '—'; }
const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
