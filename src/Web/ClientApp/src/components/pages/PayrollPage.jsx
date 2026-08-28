// @ts-nocheck
import { useCallback, useEffect, useRef, useState } from 'react';
import { BadgeCheck, CalendarDays, ChevronLeft, ChevronRight, CircleAlert, HandCoins, Link2, LockKeyhole, RefreshCw, ShieldAlert, WalletCards } from 'lucide-react';
import { PayrollClient } from '../../web-api-client';
import { PageHeader } from '../PageHeader';
import { LoadingState } from '../LoadingState';
import { ValidationError } from '../ValidationError';
import { getApiError } from '../apiError';
import { advancePayload, apiStatus, canDecideAdvance, canEditAdvance, canIssueAdvance, canSubmitAdvance, defaultAdvanceForm, defaultPeriodId, issuePayload, newIdempotencyKey, payrollErrorMessage, periodPayload, schedulePayload } from '../payroll/payrollView';

const api = new PayrollClient();
const today = new Date().toISOString().slice(0, 10);

export function PayrollPage() {
  const [workspace, setWorkspace] = useState(null);
  const [periods, setPeriods] = useState([]);
  const [advances, setAdvances] = useState([]);
  const [selectedPeriodId, setSelectedPeriodId] = useState('');
  const [selectedAdvanceId, setSelectedAdvanceId] = useState('');
  const [preflight, setPreflight] = useState(null);
  const [preflightFilters, setPreflightFilters] = useState({ workerId: '', eligibility: '', evidenceType: '', page: 1, pageSize: 10 });
  const [loading, setLoading] = useState(true);
  const [preflightLoading, setPreflightLoading] = useState(false);
  const [pending, setPending] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [periodCancelId, setPeriodCancelId] = useState('');
  const [advanceForm, setAdvanceForm] = useState(null);
  const [schedulePreview, setSchedulePreview] = useState(null);
  const [advanceFilter, setAdvanceFilter] = useState({ status: '', workerId: '' });
  const [decisionReason, setDecisionReason] = useState('');
  const [advanceCancelReason, setAdvanceCancelReason] = useState('');
  const [issueAdvance, setIssueAdvance] = useState(null);
  const decisionKeys = useRef(new Map());

  const selectedPeriod = periods.find((period) => period.id === selectedPeriodId);
  const selectedAdvance = advances.find((advance) => advance.id === selectedAdvanceId);

  const reloadCore = useCallback(async () => {
    const [nextWorkspace, nextPeriods, nextAdvances] = await Promise.all([api.workspace(), api.periodsAll(), api.advancesAll()]);
    setWorkspace(nextWorkspace); setPeriods(nextPeriods); setAdvances(nextAdvances);
    setSelectedPeriodId((current) => defaultPeriodId(nextPeriods, current));
    setSelectedAdvanceId((current) => nextAdvances.some((advance) => advance.id === current) ? current : nextAdvances[0]?.id ?? '');
  }, []);

  const handleError = useCallback(async (requestError, refresh = false) => {
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

  const runMutation = async (key, action, message, refresh = true) => {
    if (pending) return false;
    setPending(key); setError(''); setSuccess('');
    try { await action(); if (refresh) await reloadCore(); setSuccess(message); return true; }
    catch (requestError) { await handleError(requestError, true); return false; }
    finally { setPending(''); }
  };

  const createPeriod = async (event) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    await runMutation('period-create', () => api.periods(periodPayload(data.get('year'), data.get('month'))), 'Draft payroll period created.');
  };

  const cancelPeriod = async (event, period) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); const reason = String(data.get('reason') ?? '').trim();
    if (await runMutation(`period-cancel-${period.id}`, () => api.cancel4(period.id, { expectedVersion: period.version, reason }), 'Draft payroll period cancelled.')) setPeriodCancelId('');
  };

  const openAdvanceEditor = (advance) => {
    setSchedulePreview(null); setError('');
    setAdvanceForm(advance ? { id: advance.id, expectedVersion: advance.version, workerId: advance.workerId, amountUsd: String(advance.requestedAmountUsd), reason: advance.reason, requestedEventDate: isoDate(advance.requestedEventDate), recoveryStartPayrollPeriodId: advance.recoveryStartPayrollPeriodId, installmentCount: advance.installmentCount } : { ...defaultAdvanceForm, requestedEventDate: today, recoveryStartPayrollPeriodId: periods.find((period) => period.status !== 'Cancelled')?.id ?? '' });
  };

  const previewSchedule = async () => {
    if (!advanceForm || pending) return;
    setPending('schedule-preview'); setError('');
    try { setSchedulePreview(await api.schedulePreview(schedulePayload(advanceForm))); }
    catch (requestError) { await handleError(requestError); } finally { setPending(''); }
  };

  const saveAdvance = async (event) => {
    event.preventDefault(); if (!advanceForm) return;
    if (!schedulePreview) { setError('Preview the authoritative installment schedule before saving.'); return; }
    const payload = advancePayload(advanceForm, schedulePreview.installments.map((item) => item.payrollPeriodId));
    const action = advanceForm.id ? () => api.advancesPUT(advanceForm.id, { amountUsd: payload.amountUsd, reason: payload.reason, requestedEventDate: payload.requestedEventDate, recoveryStartPayrollPeriodId: payload.recoveryStartPayrollPeriodId, installmentCount: payload.installmentCount, expectedVersion: advanceForm.expectedVersion }) : () => api.advancesPOST(payload);
    if (await runMutation(`advance-save-${advanceForm.id ?? 'new'}`, action, advanceForm.id ? 'Advance draft revised and refreshed.' : 'Advance draft created with its planned recovery schedule.')) { setAdvanceForm(null); setSchedulePreview(null); }
  };

  const decideAdvance = async (advance, approved) => {
    const keyName = `${advance.id}:${advance.version}:${approved}`;
    if (!decisionKeys.current.has(keyName)) decisionKeys.current.set(keyName, newIdempotencyKey('advance-decision'));
    await runMutation(`advance-decision-${advance.id}`, () => api.decision5(advance.id, { expectedVersion: advance.version, approved, reason: approved ? undefined : decisionReason.trim(), idempotencyKey: decisionKeys.current.get(keyName) }), approved ? 'Advance approved. No money has been issued.' : 'Advance rejected with an immutable decision record.');
    setDecisionReason('');
  };

  const submitIssue = async (event) => {
    event.preventDefault(); if (!issueAdvance) return; const form = event.currentTarget; const data = new FormData(form);
    const values = { paymentMethod: String(data.get('paymentMethod')), amountUsd: String(data.get('amountUsd')), issuedAt: String(data.get('issuedAt')), payingPersonId: String(data.get('payingPersonId') ?? ''), workerAcknowledged: data.get('workerAcknowledged') === 'on', provider: String(data.get('provider') ?? ''), recipientNumber: String(data.get('recipientNumber') ?? ''), externalReference: String(data.get('externalReference') ?? ''), transactionStatus: String(data.get('transactionStatus') ?? '') };
    if (await runMutation(`advance-issue-${issueAdvance.advance.id}`, () => api.issue(issueAdvance.advance.id, issuePayload(issueAdvance.advance, values, issueAdvance.idempotencyKey)), 'Advance issue evidence recorded. No external payment was executed.')) { form.reset(); setIssueAdvance(null); }
  };

  if (loading) return <LoadingState label="Loading payroll foundations" />;
  if (!workspace) return <div className="page-stack"><PageHeader title="Payroll foundations" /><ValidationError message={error || 'Payroll workspace is unavailable.'} /><button type="button" onClick={() => globalThis.location.reload()}>Retry</button></div>;

  const filteredAdvances = advances.filter((advance) => (!advanceFilter.status || advance.status === advanceFilter.status) && (!advanceFilter.workerId || advance.workerId === advanceFilter.workerId));
  const pageCount = preflight ? Math.max(1, Math.ceil(preflight.totalCount / preflight.pageSize)) : 1;

  return <div className="page-stack payroll-page">
    <PageHeader eyebrow="Phase 6A · readiness and advances" title="Payroll foundations" description="Manage calendar periods, inspect verified labour readiness, and record advance evidence. Gross pay, deductions, payroll runs, and salary payments are not available in this phase." />
    <div className="payroll-boundary"><LockKeyhole size={16} aria-hidden="true" /><span><strong>Readiness boundary:</strong> labour evidence remains read-only and unconsumed. Advance installments are planned, not deducted.</span><em>{workspace.role}</em></div>
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
          <div className="row-actions">{period.status === 'Draft' && <><button type="button" disabled={Boolean(pending)} onClick={() => runMutation(`period-open-${period.id}`, () => api.open(period.id, { expectedVersion: period.version }), 'Payroll period opened for readiness review.')}>Open</button><button type="button" className="secondary" disabled={Boolean(pending)} onClick={() => setPeriodCancelId(periodCancelId === period.id ? '' : period.id)}>Cancel</button></>}</div>
          {periodCancelId === period.id && <form className="inline-confirm" onSubmit={(event) => cancelPeriod(event, period)}><label>Cancellation reason<input name="reason" maxLength={500} required autoFocus /></label><span><button disabled={Boolean(pending)}>Confirm cancellation</button><button type="button" className="secondary" onClick={() => setPeriodCancelId('')}>Keep draft</button></span></form>}
        </article>)}
      </div>}
    </section>

    <section className="record-panel payroll-preflight-panel">
      <header className="section-heading"><div><span className="eyebrow">Authoritative read-only assessment</span><h2><ShieldAlert size={18} /> Labour eligibility preflight</h2><p>{selectedPeriod ? `${selectedPeriod.displayName} · ${formatDate(selectedPeriod.startDate)}–${formatDate(selectedPeriod.endDate)}` : 'Select a payroll period above.'}</p></div><button type="button" className="secondary" disabled={!selectedPeriodId || preflightLoading} onClick={loadPreflight}><RefreshCw size={15} /> Refresh</button></header>
      <form className="preflight-filters" onSubmit={(event) => { event.preventDefault(); setPreflightFilters((current) => ({ ...current, page: 1 })); loadPreflight(); }}>
        <label>Worker<select value={preflightFilters.workerId} onChange={(event) => setPreflightFilters({ ...preflightFilters, workerId: event.target.value, page: 1 })}><option value="">All workers</option>{workspace.workers.map((worker) => <option key={worker.id} value={worker.id}>{worker.displayName}{worker.status !== 'Active' ? ' · archived' : ''}</option>)}</select></label>
        <label>Eligibility<select value={preflightFilters.eligibility} onChange={(event) => setPreflightFilters({ ...preflightFilters, eligibility: event.target.value, page: 1 })}><option value="">Eligible and blocked</option><option value="eligible">Eligible only</option><option value="blocked">Blocked only</option></select></label>
        <label>Evidence type<select value={preflightFilters.evidenceType} onChange={(event) => setPreflightFilters({ ...preflightFilters, evidenceType: event.target.value, page: 1 })}><option value="">All evidence types</option><option value="WorkRecord">Work record</option></select></label>
      </form>
      {!selectedPeriodId ? <EmptyState title="Select a period" copy="Readiness is assessed for one exact calendar month at a time." /> : preflightLoading ? <LoadingState label="Assessing labour evidence" /> : preflight ? <>
        <div className="preflight-summary"><span><small>Eligible evidence</small><strong>{preflight.eligibleCount}</strong></span><span><small>Blocked evidence</small><strong>{preflight.blockedCount}</strong></span><span><small>Eligible workers</small><strong>{preflight.eligibleWorkerCount}</strong></span><span><small>Blocked workers</small><strong>{preflight.blockedWorkerCount}</strong></span></div>
        <div className="monthly-proration-warning"><CircleAlert size={17} /><span><strong>Monthly-rate decision deferred</strong>{preflight.monthlyProrationNotice}</span></div>
        <div className="preflight-group-totals"><div><strong>Worker totals</strong>{preflight.workerTotals.map((total) => <span key={total.workerId}>{total.workerName}<small>{total.eligibleCount} eligible · {total.blockedCount} blocked</small></span>)}</div><div><strong>Evidence-type totals</strong>{preflight.evidenceTypeTotals.map((total) => <span key={total.evidenceType}>{total.evidenceType}<small>{total.eligibleCount} eligible · {total.blockedCount} blocked</small></span>)}</div></div>
        {preflight.evidence.length === 0 ? <EmptyState title="No evidence matches" copy="Change the filters or review another payroll period." /> : <div className="payroll-table preflight-table" role="table" aria-label="Payroll eligibility evidence">
          <div className="payroll-table-heading" role="row"><span>Worker / evidence</span><span>Work context</span><span>Basis / rate</span><span>Readiness</span></div>
          {preflight.evidence.map((evidence) => <article key={evidence.evidenceId} role="row"><span><strong>{evidence.workerName}</strong><small>{evidence.evidenceType} · {formatDate(evidence.eventDate)}</small></span><span><b>{evidence.fieldName}</b><small>{evidence.cropCycleName}</small><small>{evidence.activityNames.join(', ') || 'No valid activity'}</small></span><span><b>{evidence.quantityOrAttendanceBasis}</b><small>Snapshot USD {money(evidence.appliedRateUsd)} · {evidence.payBasis}</small>{evidence.payBasis === 'Monthly' && <em className="monthly-mark">Awaiting Phase 6B proration</em>}</span><span><StatusBadge status={evidence.eligible ? 'Eligible' : 'Blocked'} />{!evidence.eligible && <ul className="blocker-list">{evidence.blockerCodes.map((code, index) => <li key={code}><code>{code}</code><span>{evidence.blockerExplanations[index]}</span></li>)}</ul>}<details><summary><Link2 size={14} /> Source chain</summary><ol>{evidence.sourceChain.map((link) => <li key={`${link.sourceType}-${link.sourceId}`}><b>{link.sourceType}</b><span>{link.label}</span><code>{shortId(link.sourceId)}</code></li>)}</ol></details></span></article>)}
        </div>}
        <nav className="payroll-pagination" aria-label="Evidence pages"><button type="button" disabled={preflight.page <= 1} onClick={() => setPreflightFilters({ ...preflightFilters, page: preflight.page - 1 })}><ChevronLeft size={16} /> Previous</button><span>Page {preflight.page} of {pageCount} · {preflight.totalCount} complete results</span><button type="button" disabled={preflight.page >= pageCount} onClick={() => setPreflightFilters({ ...preflightFilters, page: preflight.page + 1 })}>Next <ChevronRight size={16} /></button></nav>
      </> : <EmptyState title="Readiness unavailable" copy="Refresh the selected period to try again." />}
    </section>

    <section className="record-panel payroll-advance-panel">
      <header className="section-heading"><div><span className="eyebrow">Planned recovery · no deductions</span><h2><HandCoins size={18} /> Worker advances</h2></div><button className="primary-action" type="button" onClick={() => openAdvanceEditor(null)}>New advance</button></header>
      <div className="advance-filters"><label>Status<select value={advanceFilter.status} onChange={(event) => setAdvanceFilter({ ...advanceFilter, status: event.target.value })}><option value="">All statuses</option>{['Draft', 'PendingGrowerApproval', 'Approved', 'Rejected', 'Issued', 'Cancelled'].map((status) => <option key={status}>{status}</option>)}</select></label><label>Worker<select value={advanceFilter.workerId} onChange={(event) => setAdvanceFilter({ ...advanceFilter, workerId: event.target.value })}><option value="">All workers</option>{workspace.workers.map((worker) => <option key={worker.id} value={worker.id}>{worker.displayName}</option>)}</select></label></div>
      {advanceForm && <AdvanceEditor form={advanceForm} setForm={(next) => { setAdvanceForm(next); setSchedulePreview(null); }} workers={workspace.workers} periods={periods} preview={schedulePreview} pending={pending} onPreview={previewSchedule} onSubmit={saveAdvance} onClose={() => { setAdvanceForm(null); setSchedulePreview(null); }} />}
      {filteredAdvances.length === 0 ? <EmptyState title="No worker advances" copy="Create a draft to preview a precise monthly recovery plan." /> : <div className="advance-workspace">
        <div className="advance-register">{filteredAdvances.map((advance) => <button type="button" key={advance.id} className={selectedAdvanceId === advance.id ? 'is-selected' : ''} onClick={() => setSelectedAdvanceId(advance.id)}><span><strong>{advance.workerName}</strong><small>{advance.reason}</small></span><span><b>USD {money(advance.requestedAmountUsd)}</b><StatusBadge status={advance.status} /></span></button>)}</div>
        {selectedAdvance && <AdvanceDetails advance={selectedAdvance} periods={periods} role={workspace.role} pending={pending} decisionReason={decisionReason} setDecisionReason={setDecisionReason} cancelReason={advanceCancelReason} setCancelReason={setAdvanceCancelReason} onEdit={() => openAdvanceEditor(selectedAdvance)} onSubmit={() => runMutation(`advance-submit-${selectedAdvance.id}`, () => api.submit4(selectedAdvance.id, { expectedVersion: selectedAdvance.version }), 'Exact advance version submitted for Grower approval.')} onDecide={(approved) => decideAdvance(selectedAdvance, approved)} onCancel={async () => { if (await runMutation(`advance-cancel-${selectedAdvance.id}`, () => api.cancel5(selectedAdvance.id, { expectedVersion: selectedAdvance.version, reason: advanceCancelReason.trim() }), 'Advance cancelled without payroll effect.')) setAdvanceCancelReason(''); }} onIssue={() => setIssueAdvance({ advance: selectedAdvance, idempotencyKey: newIdempotencyKey('advance-issue') })} />}
      </div>}
      {issueAdvance && <IssueForm record={issueAdvance} people={workspace.payingPersons} pending={pending} onSubmit={submitIssue} onClose={() => setIssueAdvance(null)} />}
    </section>
  </div>;
}

function AdvanceEditor({ form, setForm, workers, periods, preview, pending, onPreview, onSubmit, onClose }) {
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

function SchedulePreview({ preview, periods }) {
  return <div className="schedule-preview"><header><strong>{preview.installmentCount} planned installments</strong><span>USD {money(preview.scheduleTotalUsd)}</span></header>{preview.installments.map((installment, index) => <span key={installment.sequence}><b>{installment.sequence}</b><small>{periods.find((period) => period.id === installment.payrollPeriodId)?.displayName ?? 'Payroll period'}</small><strong>USD {money(installment.amountUsd)}</strong>{index === preview.installments.length - 1 && <em>final residual</em>}</span>)}</div>;
}

function AdvanceDetails({ advance, periods, role, pending, decisionReason, setDecisionReason, cancelReason, setCancelReason, onEdit, onSubmit, onDecide, onCancel, onIssue }) {
  const scheduleTotal = advance.installments.reduce((total, installment) => total + installment.amountUsd, 0);
  return <article className="advance-details"><header><div><span className="eyebrow">Authoritative advance record</span><h3>{advance.workerName}</h3><small>Requested {formatDate(advance.requestedEventDate)} · version {advance.version}</small></div><StatusBadge status={advance.status} /></header><div className="advance-metrics"><span><small>Requested</small><strong>USD {money(advance.requestedAmountUsd)}</strong></span><span><small>Approved</small><strong>{advance.approvedAmountUsd == null ? '—' : `USD ${money(advance.approvedAmountUsd)}`}</strong></span><span><small>Outstanding</small><strong>USD {money(advance.outstandingAmountUsd)}</strong></span><span><small>Installments</small><strong>{advance.installmentCount}</strong></span></div><p>{advance.reason}</p>
    <div className="planned-installments"><header><strong>Planned future deductions</strong><span>Total USD {money(scheduleTotal)}</span></header>{advance.installments.map((installment) => <span key={installment.sequence}><b>{installment.sequence}</b><small>{periods.find((period) => period.id === installment.payrollPeriodId)?.displayName ?? shortId(installment.payrollPeriodId)}</small><strong>USD {money(installment.amountUsd)}</strong></span>)}<p>No installment is deducted in Phase 6A.</p></div>
    <section className="approval-history"><h4>Approval history</h4>{advance.approvalHistory.length ? advance.approvalHistory.map((approval) => <span key={`${approval.advanceVersion}-${approval.decidedAt.toISOString()}`}><StatusBadge status={approval.approved ? 'Approved' : 'Rejected'} /><b>version {approval.advanceVersion}</b><small>{formatDateTime(approval.decidedAt)}{approval.reason ? ` · ${approval.reason}` : ''}</small></span>) : <p>No Grower decision recorded.</p>}</section>
    <section className="issue-history"><h4>Issue evidence</h4>{advance.issue ? <div><StatusBadge status="Issued" /><strong>{advance.issue.paymentMethod} · USD {money(advance.issue.amountUsd)}</strong><small>{formatDateTime(advance.issue.issuedAt)}</small>{advance.issue.paymentMethod === 'Cash' ? <small>Paying person recorded · receiving worker acknowledged</small> : <small>{advance.issue.provider} · {advance.issue.maskedRecipientNumber} · {advance.issue.externalReference} · {advance.issue.transactionStatus}</small>}</div> : <p>Not issued. Approval never issues money automatically.</p>}</section>
    {canDecideAdvance(role, advance.status) && <label className="rejection-reason">Rejection reason<input value={decisionReason} onChange={(event) => setDecisionReason(event.target.value)} maxLength={500} placeholder="Required only when rejecting" /></label>}
    {canEditAdvance(advance.status) && <label className="rejection-reason">Cancellation reason<input value={cancelReason} onChange={(event) => setCancelReason(event.target.value)} maxLength={500} placeholder="Required to cancel this draft" /></label>}
    <footer className="advance-actions">{canEditAdvance(advance.status) && <><button type="button" className="secondary" onClick={onEdit}>Edit draft</button><button type="button" className="text-action" disabled={Boolean(pending) || !cancelReason.trim()} onClick={onCancel}>Cancel advance</button></>}{canSubmitAdvance(role, advance.status) && <button type="button" disabled={Boolean(pending)} onClick={onSubmit}>Submit exact version</button>}{canDecideAdvance(role, advance.status) && <><button type="button" disabled={Boolean(pending)} onClick={() => onDecide(true)}>Approve</button><button type="button" className="secondary" disabled={Boolean(pending) || !decisionReason.trim()} onClick={() => onDecide(false)}>Reject</button></>}{canIssueAdvance(advance.status) && <button type="button" disabled={Boolean(pending)} onClick={onIssue}><WalletCards size={15} /> Record issue evidence</button>}{role === 'FarmManager' && advance.status === 'PendingGrowerApproval' && <small>Grower approval required. The API rejects manager decision attempts with 403.</small>}</footer>
  </article>;
}

function IssueForm({ record, people, pending, onSubmit, onClose }) {
  const [method, setMethod] = useState('Cash'); const approved = record.advance.approvedAmountUsd;
  return <form className="issue-form" onSubmit={onSubmit}><header><div><span className="eyebrow">Operational evidence only</span><h3>Record advance issue</h3></div><button type="button" className="secondary" onClick={onClose}>Close</button></header><p>No external provider call or payment execution occurs. Amount is locked to the exact Grower-approved USD amount.</p><div className="form-grid"><label>Issue method<select name="paymentMethod" value={method} onChange={(event) => setMethod(event.target.value)}><option value="Cash">Cash</option><option value="MobileMoney">Mobile Money</option></select></label><label>Exact approved amount (USD)<input name="amountUsd" type="number" step="0.01" value={approved ?? ''} readOnly required /></label><label>Issue / transaction date and time<input name="issuedAt" type="datetime-local" defaultValue={localDateTimeInputValue()} required /></label>{method === 'Cash' ? <><label>Paying person<select name="payingPersonId" required defaultValue=""><option value="">Select paying person</option>{people.map((person) => <option key={person.id} value={person.id}>{person.displayName}</option>)}</select></label><label>Receiving worker<input value={record.advance.workerName} readOnly /></label><label className="acknowledgement"><input name="workerAcknowledged" type="checkbox" required /><span>Worker acknowledgement received</span></label></> : <><label>Provider<input name="provider" maxLength={100} required autoComplete="off" /></label><label>Recipient number<input name="recipientNumber" type="tel" minLength={4} maxLength={32} required autoComplete="off" aria-describedby="recipient-mask-note" /><small id="recipient-mask-note">Only a masked value is returned after recording.</small></label><label>External reference<input name="externalReference" maxLength={160} required autoComplete="off" /></label><label>Transaction status<input name="transactionStatus" maxLength={64} required placeholder="e.g. Confirmed" /></label></>}</div><footer className="form-actions"><span>Retry identity is retained until this form succeeds or closes.</span><button disabled={Boolean(pending)}>{pending.startsWith('advance-issue') ? 'Recording…' : 'Record issue evidence'}</button></footer></form>;
}

function StatusBadge({ status }) { return <em className={`status-pill status-${String(status).toLowerCase()}`}>{status === 'PendingGrowerApproval' ? 'Pending Grower approval' : status}</em>; }
function EmptyState({ title, copy }) { return <div className="payroll-empty"><CircleAlert size={20} /><span><strong>{title}</strong><small>{copy}</small></span></div>; }
function formatDate(value) { const date = value instanceof Date ? value : new Date(value); return Number.isNaN(date.getTime()) ? String(value ?? '') : date.toLocaleDateString('en-ZW', { day: '2-digit', month: 'short', year: 'numeric' }); }
function formatDateTime(value) { const date = value instanceof Date ? value : new Date(value); return Number.isNaN(date.getTime()) ? String(value ?? '') : date.toLocaleString('en-ZW', { dateStyle: 'medium', timeStyle: 'short' }); }
function isoDate(value) { const date = value instanceof Date ? value : new Date(value); return Number.isNaN(date.getTime()) ? '' : date.toISOString().slice(0, 10); }
function localDateTimeInputValue() { const now = new Date(); return new Date(now.getTime() - (now.getTimezoneOffset() * 60000)).toISOString().slice(0, 16); }
function money(value) { return Number(value ?? 0).toFixed(2); }
function shortId(value) { return value ? `${String(value).slice(0, 8)}…` : '—'; }
const monthNames = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
