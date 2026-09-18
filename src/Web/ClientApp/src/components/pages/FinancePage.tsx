import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { useDialogFocus } from '../useDialogFocus';
import { CircleDollarSign, FileSearch, Landmark, Pencil, Plus, RefreshCw, RotateCcw, Send, X } from 'lucide-react';
import {
  CreateOperationalTransactionRequest,
  CropCyclesClient,
  FarmSetupClient,
  FinanceClient,
  PostOperationalTransactionRequest,
  ReverseOperationalTransactionRequest,
  SetTransactionAllocationsRequest,
  TransactionAllocationRequest,
  UpdateOperationalTransactionRequest,
  type CropCycleCostSummaryDto,
  type FarmDto,
  type FarmSetupDto,
  type OperationalTransactionDto,
} from '../../web-api-client';
import { ConfirmationDialog } from '../ConfirmationDialog';
import { DatePicker } from '../DatePicker';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { getApiError } from '../apiError';
import { financeCategories, financeLabel, newFinanceKey, perUnit, usd } from '../finance/financeView';
import { BudgetWorkspace, type FinanceCycleOption } from '../finance/BudgetWorkspace';
import { MillRecordsWorkspace } from '../finance/MillRecordsWorkspace';

const financeApi = new FinanceClient();
const farmApi = new FarmSetupClient();
const cyclesApi = new CropCyclesClient();
const today = new Date().toISOString().slice(0, 10);

interface AllocationRow { key: string; allocationType: 'CropCycleDirect' | 'Field' | 'FarmOverhead'; fieldId: string; cropCycleId: string; category: string; amountUsd: string; }

export function FinancePage() {
  const [transactions, setTransactions] = useState<OperationalTransactionDto[]>([]);
  const [farm, setFarm] = useState<FarmSetupDto | null>(null);
  const [cycles, setCycles] = useState<FinanceCycleOption[]>([]);
  const [cost, setCost] = useState<CropCycleCostSummaryDto | null>(null);
  const [role, setRole] = useState('');
  const [tab, setTab] = useState<'transactions' | 'cost' | 'budgets' | 'mill-records'>('transactions');
  const [filters, setFilters] = useState({ from: '', to: '', type: '', category: '', status: '', search: '' });
  const [transactionPage, setTransactionPage] = useState(1);
  const [transactionTotals, setTransactionTotals] = useState({ totalCount: 0, postedExpenseUsd: 0, postedIncomeUsd: 0, draftCount: 0 });
  const [editor, setEditor] = useState<OperationalTransactionDto | 'new' | null>(null);
  const [allocating, setAllocating] = useState<OperationalTransactionDto | null>(null);
  const [posting, setPosting] = useState<OperationalTransactionDto | null>(null);
  const [reversing, setReversing] = useState<OperationalTransactionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadTransactions = useCallback(async (page = transactionPage) => {
    const result = await financeApi.getFinanceTransactions(filters.from || undefined, filters.to || undefined,
      filters.type || undefined, filters.category || undefined, filters.status || undefined,
      filters.search || undefined, page, 50);
    setTransactions(result.items); setTransactionTotals(result); setTransactionPage(page);
  }, [filters, transactionPage]);

  useEffect(() => {
    let current = true;
    Promise.all([financeApi.getFinanceTransactions(undefined, undefined, undefined, undefined, undefined, undefined, 1, 50), farmApi.farmSetup(), financeApi.getFinanceSession()])
      .then(async ([nextTransactions, setup, session]) => {
        const collections = await Promise.all((setup.farm?.fields ?? []).map((field) => cyclesApi.getCropCycles(field.id)));
        if (!current) return;
        setTransactions(nextTransactions.items); setTransactionTotals(nextTransactions); setFarm(setup); setRole(session.securityRole);
        setCycles(collections.flatMap((collection) => collection.cropCycles.map((cycle) => ({
          id: cycle.id, fieldId: collection.field.id,
          label: `${collection.field.code} · ${cycle.variety} · ${financeLabel(cycle.status)}`,
          status: cycle.status,
        }))));
      })
      .catch((requestError) => { if (current) setError(getApiError(requestError)); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, []);

  const refresh = async () => { await loadTransactions(); };
  const mutate = async (key: string, action: () => Promise<unknown>, message: string) => {
    setPending(key); setError(''); setSuccess('');
    try { await action(); await refresh(); setSuccess(message); return true; }
    catch (requestError) { setError(getApiError(requestError)); return false; }
    finally { setPending(''); }
  };

  const confirmPost = async () => {
    if (!posting) return;
    const ok = await mutate(`post-${posting.id}`, () => financeApi.postFinanceTransaction(posting.id,
      new PostOperationalTransactionRequest({ expectedVersion: posting.version,
        idempotencyKey: newFinanceKey('transaction-post') })), 'Transaction posted and eligible crop cost projected.');
    if (ok) setPosting(null);
  };

  const confirmReverse = async (reason: string) => {
    if (!reversing) return;
    const ok = await mutate(`reverse-${reversing.id}`, () => financeApi.reverseFinanceTransaction(reversing.id,
      new ReverseOperationalTransactionRequest({ reason, idempotencyKey: newFinanceKey('transaction-reversal') })),
    'Reversal appended; original facts remain unchanged.');
    if (ok) setReversing(null);
  };

  const loadCost = async (cycleId: string) => {
    if (!cycleId) { setCost(null); return; }
    setPending('cost'); setError('');
    try { setCost(await financeApi.getFinanceCropCycleCost(cycleId)); }
    catch (requestError) { setError(getApiError(requestError)); }
    finally { setPending(''); }
  };

  if (loading) return <LoadingState label="Opening the finance daybook" />;
  return <div className="page-stack finance-page">
    <PageHeader eyebrow="Operational finance" title="Finance" description="Record USD operating facts and trace crop cost to approved work, confirmed inputs, losses, and posted expense allocations.">
      {tab === 'transactions' && <button className="primary-action" onClick={() => setEditor('new')}><Plus size={17} /> New transaction</button>}
    </PageHeader>
    <ValidationError message={error} />
    {success && <p className="success-banner">{success}</p>}
    <section className="finance-commandbar record-panel">
      <nav aria-label="Finance views">
        <button aria-current={tab === 'transactions'} onClick={() => setTab('transactions')}>Transaction register</button>
        <button aria-current={tab === 'cost'} onClick={() => setTab('cost')}>Crop cost trace</button>
        <button aria-current={tab === 'budgets'} onClick={() => setTab('budgets')}>Budgets &amp; variance</button>
        <button aria-current={tab === 'mill-records'} onClick={() => setTab('mill-records')}>Mill records</button>
      </nav>
      <button className="text-action" disabled={pending === 'reconcile'} onClick={() => mutate('reconcile', () => financeApi.reconcileFinancePayrollCosts(), 'Approved payroll cost reconciliation completed.')}><RefreshCw size={15} /> Reconcile payroll costs</button>
    </section>

    {tab === 'transactions' && <>
      <form className="finance-filters record-panel" onSubmit={(event) => { event.preventDefault(); loadTransactions().catch((e) => setError(getApiError(e))); }}>
        <label>From<DatePicker name="from" value={filters.from} onChange={(value) => { setTransactionPage(1); setFilters({ ...filters, from: value }); }} /></label>
        <label>To<DatePicker name="to" value={filters.to} onChange={(value) => { setTransactionPage(1); setFilters({ ...filters, to: value }); }} /></label>
        <label>Direction<select value={filters.type} onChange={(event) => { setTransactionPage(1); setFilters({ ...filters, type: event.target.value }); }}><option value="">All</option><option>Expense</option><option>Income</option></select></label>
        <label>Status<select value={filters.status} onChange={(event) => { setTransactionPage(1); setFilters({ ...filters, status: event.target.value }); }}><option value="">All</option><option>Draft</option><option>Posted</option><option>Reversed</option><option>Cancelled</option></select></label>
        <label>Category<select value={filters.category} onChange={(event) => { setTransactionPage(1); setFilters({ ...filters, category: event.target.value }); }}><option value="">All</option>{financeCategories.map((value) => <option key={value} value={value}>{financeLabel(value)}</option>)}</select></label>
        <label className="finance-search">Payee or source<input value={filters.search} onChange={(event) => { setTransactionPage(1); setFilters({ ...filters, search: event.target.value }); }} placeholder="Search recorded evidence" /></label>
        <button disabled={pending !== ''}>Apply filters</button>
      </form>
      <TransactionRegister transactions={transactions} totals={transactionTotals} page={transactionPage} pending={pending}
        onPage={(page) => loadTransactions(page).catch((error) => setError(getApiError(error)))} onEdit={setEditor}
        onAllocate={setAllocating} onPost={setPosting} onReverse={setReversing} />
    </>}
    {tab === 'cost' && <CostWorkspace cycles={cycles} cost={cost} loading={pending === 'cost'} onSelect={loadCost} />}
    {tab === 'budgets' && <BudgetWorkspace cycles={cycles} role={role} onError={setError} onSuccess={setSuccess} />}
    {tab === 'mill-records' && <MillRecordsWorkspace onError={setError} onSuccess={setSuccess} />}
    {editor && <FinanceDialog title={editor === 'new' ? 'Record transaction draft' : 'Edit transaction draft'} onClose={() => setEditor(null)}><TransactionForm transaction={editor === 'new' ? null : editor} onSaved={async () => { setEditor(null); await refresh(); }} onError={setError} /></FinanceDialog>}
    {allocating && <FinanceDialog title="Allocate full transaction amount" onClose={() => setAllocating(null)}><AllocationForm transaction={allocating} farm={farm?.farm ?? null} cycles={cycles} onSaved={async () => { setAllocating(null); await refresh(); }} onError={setError} /></FinanceDialog>}
    {posting && <ConfirmationDialog title="Post transaction" description={`Post ${usd(posting.amountUsd)} to ${posting.payeeOrPayer}? Amount, date, category, source, and allocations become immutable.`} confirmLabel="Post transaction" isBusy={pending === `post-${posting.id}`} onConfirm={confirmPost} onCancel={() => setPosting(null)} />}
    {reversing && <ReversalConfirmation transaction={reversing} isBusy={pending === `reverse-${reversing.id}`} onConfirm={confirmReverse} onCancel={() => setReversing(null)} />}
  </div>;
}

function TransactionRegister({ transactions, totals, page, pending, onPage, onEdit, onAllocate, onPost, onReverse }: { transactions: OperationalTransactionDto[]; totals: { totalCount: number; postedExpenseUsd: number; postedIncomeUsd: number; draftCount: number }; page: number; pending: string; onPage: (page: number) => void; onEdit: (value: OperationalTransactionDto) => void; onAllocate: (value: OperationalTransactionDto) => void; onPost: (value: OperationalTransactionDto) => void; onReverse: (value: OperationalTransactionDto) => void; }) {
  return <section className="finance-register record-panel">
    <header className="finance-register-summary"><div><small>Posted expenses · all filtered</small><strong>{usd(totals.postedExpenseUsd)}</strong></div><div><small>Posted income · all filtered</small><strong>{usd(totals.postedIncomeUsd)}</strong></div><div><small>Drafts awaiting allocation</small><strong>{totals.draftCount}</strong></div></header>
    <div className="finance-ledger-head"><span>Date / source</span><span>Payee or payer</span><span>Category</span><span>Amount</span><span>Status / action</span></div>
    {transactions.length ? transactions.map((transaction) => <article className="finance-row" key={transaction.id}>
      <span><strong>{transaction.eventDate}</strong><small>{transaction.sourceReference || 'No source reference'}</small></span>
      <span><strong>{transaction.payeeOrPayer}</strong><small>{transaction.allocations.length} allocation{transaction.allocations.length === 1 ? '' : 's'} · v{transaction.version}</small></span>
      <span>{financeLabel(transaction.category)}<small>{transaction.type}</small></span>
      <b className={transaction.type === 'Income' ? 'finance-income' : 'finance-expense'}>{transaction.type === 'Income' ? '+' : '−'}{usd(transaction.amountUsd)}</b>
      <span className="finance-row-actions"><em className={`status-pill status-${transaction.status.toLowerCase()}`}>{transaction.status}</em>{transaction.status === 'Draft' && <><button type="button" className="row-icon-button" onClick={() => onEdit(transaction)} aria-label="Edit transaction" title="Edit transaction"><Pencil size={15} /></button><button onClick={() => onAllocate(transaction)}>Allocate</button><button type="button" className="row-icon-button" disabled={!transaction.allocations.length || pending !== ''} onClick={() => onPost(transaction)} aria-label="Post transaction" title="Post transaction"><Send size={15} /></button></>}{transaction.status === 'Posted' && <button className="text-action" disabled={pending !== ''} onClick={() => onReverse(transaction)}><RotateCcw size={14} /> Reverse</button>}</span>
    </article>) : <div className="finance-empty"><Landmark size={26} /><strong>No operational transactions match these filters.</strong><span>Create a draft or widen the date and status filters.</span></div>}
    <nav className="payroll-pagination" aria-label="Transaction pages"><button disabled={page <= 1 || pending !== ''} onClick={() => onPage(page - 1)}>Previous transactions</button><span aria-live="polite">Page {page} of {Math.max(1, Math.ceil(totals.totalCount / 50))}</span><button disabled={page * 50 >= totals.totalCount || pending !== ''} onClick={() => onPage(page + 1)}>Next transactions</button></nav>
  </section>;
}

function TransactionForm({ transaction, onSaved, onError }: { transaction: OperationalTransactionDto | null; onSaved: () => Promise<void>; onError: (message: string) => void; }) {
  const [saving, setSaving] = useState(false);
  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); onError('');
    const body = { type: String(data.get('type')), category: String(data.get('category')), eventDate: String(data.get('eventDate')), payeeOrPayer: String(data.get('payeeOrPayer')).trim(), amountUsd: Number(data.get('amountUsd')), sourceReference: String(data.get('sourceReference') || '').trim() || undefined, notes: String(data.get('notes') || '').trim() || undefined };
    try { if (transaction) await financeApi.updateFinanceTransaction(transaction.id, new UpdateOperationalTransactionRequest({ ...body, expectedVersion: transaction.version })); else await financeApi.createFinanceTransaction(new CreateOperationalTransactionRequest(body)); await onSaved(); }
    catch (requestError) { onError(getApiError(requestError)); }
    finally { setSaving(false); }
  };
  return <form className="finance-form" onSubmit={submit}><div className="form-grid"><label>Direction<select name="type" defaultValue={transaction?.type || 'Expense'}><option>Expense</option><option>Income</option></select></label><label>Category<select name="category" defaultValue={transaction?.category || 'OtherExpense'}>{financeCategories.map((value) => <option key={value} value={value}>{financeLabel(value)}</option>)}</select></label><label>Event date<DatePicker name="eventDate" defaultValue={transaction?.eventDate || today} required /></label><label>Amount (USD)<input name="amountUsd" type="number" min="0.01" step="0.01" defaultValue={transaction?.amountUsd} required /></label><label className="is-wide">Payee or payer<input name="payeeOrPayer" maxLength={160} defaultValue={transaction?.payeeOrPayer} required /></label><label>Source reference<input name="sourceReference" maxLength={160} defaultValue={transaction?.sourceReference} /></label><label className="is-wide">Notes<textarea name="notes" maxLength={1000} defaultValue={transaction?.notes} /></label></div><footer className="form-actions"><span>Currency is fixed to USD. Drafts do not affect crop cost.</span><button disabled={saving}>{saving ? 'Saving…' : 'Save draft'}</button></footer></form>;
}

function AllocationForm({ transaction, farm, cycles, onSaved, onError }: { transaction: OperationalTransactionDto; farm: FarmDto | null; cycles: FinanceCycleOption[]; onSaved: () => Promise<void>; onError: (message: string) => void; }) {
  const initial = transaction.allocations.length ? transaction.allocations.map((item) => ({ key: item.id, allocationType: item.allocationType as AllocationRow['allocationType'], fieldId: item.fieldId || '', cropCycleId: item.cropCycleId || '', category: item.category, amountUsd: String(item.amountUsd) })) : [{ key: newFinanceKey('allocation'), allocationType: 'FarmOverhead' as const, fieldId: '', cropCycleId: '', category: transaction.category, amountUsd: String(transaction.amountUsd) }];
  const [rows, setRows] = useState<AllocationRow[]>(initial); const [saving, setSaving] = useState(false);
  const total = rows.reduce((sum, row) => sum + Number(row.amountUsd || 0), 0);
  const patchRow = (key: string, patch: Partial<AllocationRow>) => setRows((value) => value.map((row) => row.key === key ? { ...row, ...patch } : row));
  const submit = async (event: FormEvent) => { event.preventDefault(); setSaving(true); onError(''); try { await financeApi.setFinanceTransactionAllocations(transaction.id, new SetTransactionAllocationsRequest({ expectedVersion: transaction.version, allocations: rows.map((row) => new TransactionAllocationRequest({ allocationType: row.allocationType, fieldId: row.fieldId || undefined, cropCycleId: row.cropCycleId || undefined, category: row.category, amountUsd: Number(row.amountUsd) })) })); await onSaved(); } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); } };
  return <form className="allocation-form" onSubmit={submit}><p className={total === transaction.amountUsd ? 'allocation-reconciled' : 'allocation-unbalanced'}>Allocated {usd(total)} of {usd(transaction.amountUsd)} · {total === transaction.amountUsd ? 'exactly reconciled' : `${usd(Math.abs(transaction.amountUsd - total))} remaining`}</p>{rows.map((row) => <fieldset key={row.key} className="allocation-row"><label>Scope<select value={row.allocationType} onChange={(event) => patchRow(row.key, { allocationType: event.target.value as AllocationRow['allocationType'], fieldId: '', cropCycleId: '' })}><option value="CropCycleDirect">Crop-cycle direct</option><option value="Field">Field only</option><option value="FarmOverhead">Farm overhead</option></select></label>{row.allocationType !== 'FarmOverhead' && <label>Field<select value={row.fieldId} onChange={(event) => patchRow(row.key, { fieldId: event.target.value, cropCycleId: '' })} required><option value="">Select field</option>{farm?.fields.map((field) => <option key={field.id} value={field.id}>{field.code} · {field.name}</option>)}</select></label>}{row.allocationType === 'CropCycleDirect' && <label>Crop cycle<select value={row.cropCycleId} onChange={(event) => { const cycle = cycles.find((item) => item.id === event.target.value); patchRow(row.key, { cropCycleId: event.target.value, fieldId: cycle?.fieldId || row.fieldId }); }} required><option value="">Select cycle</option>{cycles.filter((cycle) => !row.fieldId || cycle.fieldId === row.fieldId).map((cycle) => <option key={cycle.id} value={cycle.id}>{cycle.label}</option>)}</select></label>}<label className="allocation-amount">Amount (USD)<input type="number" min="0.01" step="0.01" value={row.amountUsd} onChange={(event) => patchRow(row.key, { amountUsd: event.target.value })} required /></label><button type="button" className="icon-button allocation-remove" aria-label="Remove allocation" title="Remove allocation" disabled={rows.length === 1} onClick={() => setRows((value) => value.filter((item) => item.key !== row.key))}><X size={16} /></button></fieldset>)}<footer className="form-actions"><button type="button" className="secondary" onClick={() => setRows((value) => [...value, { key: newFinanceKey('allocation'), allocationType: 'FarmOverhead', fieldId: '', cropCycleId: '', category: transaction.category, amountUsd: '' }])}><Plus size={15} /> Add allocation</button><button disabled={saving || total !== transaction.amountUsd}>Save exact allocations</button></footer></form>;
}

function CostWorkspace({ cycles, cost, loading, onSelect }: { cycles: FinanceCycleOption[]; cost: CropCycleCostSummaryDto | null; loading: boolean; onSelect: (id: string) => void; }) {
  return <div className="cost-workspace"><section className="cost-selector record-panel"><label>Crop cycle<select onChange={(event) => onSelect(event.target.value)} defaultValue=""><option value="">Choose a crop cycle</option>{cycles.map((cycle) => <option key={cycle.id} value={cycle.id}>{cycle.label}</option>)}</select></label><p>Totals come only from signed `OperationalCostPosting` rows. Income and unallocated overhead never alter crop cost.</p></section>{loading && <LoadingState label="Tracing cost sources" />}{cost && <><section className="cost-summary record-panel"><header><div><small>{cost.fieldName}</small><h2>{usd(cost.totalCostUsd)}</h2><span>Total controlled crop cost</span></div><div className="cost-unit-rates"><strong>{perUnit(cost.costPerHectareUsd, 'ha')}</strong><small>{cost.reportingHectares == null ? 'Reporting area missing' : `${cost.reportingHectares} reporting ha`}</small><strong>{perUnit(cost.costPerTonneUsd, 't')}</strong><small>{cost.actualHarvestedTonnes == null ? 'Actual harvest missing' : `${cost.actualHarvestedTonnes} actual t`}</small></div></header><div className="cost-breakdown"><span><small>Labour</small><b>{usd(cost.labourUsd)}</b></span><span><small>Applied inputs</small><b>{usd(cost.appliedInputsUsd)}</b></span><span><small>Direct expenses</small><b>{usd(cost.directExpensesUsd)}</b></span><span><small>Approved loss</small><b>{usd(cost.approvedInventoryLossUsd)}</b></span></div></section><section className="cost-trace record-panel"><header><FileSearch size={19} /><div><h2>Authoritative source trace</h2><p>Every amount below binds to one operational source.</p></div></header>{cost.sources.map((source) => <article key={source.id}><span className="trace-node" /><span><strong>{financeLabel(source.category)}</strong><small>{source.sourceType} · {source.sourceId}</small>{source.payrollRunId && <small>Payroll run {source.payrollRunId} · calculation v{source.payrollCalculationVersion} · worker line {source.payrollWorkerLineId} · work record {source.workRecordId}</small>}{source.operationalTransactionId && <small>Transaction {source.operationalTransactionId} · allocation {source.transactionAllocationId}</small>}<small>{source.reversalOfId ? `Reverses posting ${source.reversalOfId}` : source.sourceDescription}</small></span><b className={source.amountUsd < 0 ? 'finance-income' : ''}>{usd(source.amountUsd)}</b></article>)}</section></> }</div>;
}

function ReversalConfirmation({ transaction, isBusy, onConfirm, onCancel }: { transaction: OperationalTransactionDto; isBusy: boolean; onConfirm: (reason: string) => void | Promise<void>; onCancel: () => void; }) {
  const form = useRef<HTMLFormElement>(null);
  const submit = () => {
    if (!form.current || !form.current.reportValidity()) return;
    const reason = (new FormData(form.current).get('reason') as string).trim();
    onConfirm(reason);
  };
  return <ConfirmationDialog title="Reverse transaction" description={`Reverse ${usd(transaction.amountUsd)} to ${transaction.payeeOrPayer}? The original transaction remains unchanged; a reversal entry is appended.`} confirmLabel="Reverse transaction" isBusy={isBusy} onConfirm={submit} onCancel={onCancel}>
    <form ref={form} className="confirmation-form" onSubmit={(event: FormEvent<HTMLFormElement>) => event.preventDefault()}>
      <label>Grower reversal reason<textarea name="reason" rows={3} maxLength={500} required autoFocus /></label>
    </form>
  </ConfirmationDialog>;
}

function FinanceDialog({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) { const dialogRef = useDialogFocus<HTMLElement>(onClose); return <div className="dialog-backdrop" role="presentation"><section ref={dialogRef} className="inventory-dialog finance-dialog" role="dialog" aria-modal="true" aria-label={title}><header><div><CircleDollarSign size={19} /><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Close"><X size={18} /></button></header>{children}</section></div>; }
