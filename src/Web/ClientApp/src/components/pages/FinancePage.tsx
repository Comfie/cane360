import { useCallback, useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react';
import { CircleDollarSign, FileSearch, Landmark, Plus, RefreshCw, RotateCcw, Send, X } from 'lucide-react';
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
import { DatePicker } from '../DatePicker';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { getApiError } from '../apiError';
import { financeCategories, financeLabel, newFinanceKey, perUnit, usd } from '../finance/financeView';

const financeApi = new FinanceClient();
const farmApi = new FarmSetupClient();
const cyclesApi = new CropCyclesClient();
const today = new Date().toISOString().slice(0, 10);

interface CycleOption { id: string; fieldId: string; label: string; status: string; }
interface AllocationRow { key: string; allocationType: 'CropCycleDirect' | 'Field' | 'FarmOverhead'; fieldId: string; cropCycleId: string; category: string; amountUsd: string; }

export function FinancePage() {
  const [transactions, setTransactions] = useState<OperationalTransactionDto[]>([]);
  const [farm, setFarm] = useState<FarmSetupDto | null>(null);
  const [cycles, setCycles] = useState<CycleOption[]>([]);
  const [cost, setCost] = useState<CropCycleCostSummaryDto | null>(null);
  const [tab, setTab] = useState<'transactions' | 'cost'>('transactions');
  const [filters, setFilters] = useState({ from: '', to: '', type: '', category: '', status: '', search: '' });
  const [editor, setEditor] = useState<OperationalTransactionDto | 'new' | null>(null);
  const [allocating, setAllocating] = useState<OperationalTransactionDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [pending, setPending] = useState('');
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const loadTransactions = useCallback(async () => {
    const result = await financeApi.getFinanceTransactions(filters.from || undefined, filters.to || undefined,
      filters.type || undefined, filters.category || undefined, filters.status || undefined,
      filters.search || undefined);
    setTransactions(result);
  }, [filters]);

  useEffect(() => {
    let current = true;
    Promise.all([financeApi.getFinanceTransactions(undefined, undefined, undefined, undefined, undefined, undefined), farmApi.farmSetup()])
      .then(async ([nextTransactions, setup]) => {
        const collections = await Promise.all((setup.farm?.fields ?? []).map((field) => cyclesApi.cropCyclesGET(field.id)));
        if (!current) return;
        setTransactions(nextTransactions); setFarm(setup);
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

  const post = async (transaction: OperationalTransactionDto) => {
    if (!globalThis.confirm('Post this transaction? Amount, date, category, source, and allocations become immutable.')) return;
    await mutate(`post-${transaction.id}`, () => financeApi.postFinanceTransaction(transaction.id,
      new PostOperationalTransactionRequest({ expectedVersion: transaction.version,
        idempotencyKey: newFinanceKey('transaction-post') })), 'Transaction posted and eligible crop cost projected.');
  };

  const reverse = async (transaction: OperationalTransactionDto) => {
    const reason = globalThis.prompt('Grower reversal reason:')?.trim();
    if (!reason) return;
    await mutate(`reverse-${transaction.id}`, () => financeApi.reverseFinanceTransaction(transaction.id,
      new ReverseOperationalTransactionRequest({ reason, idempotencyKey: newFinanceKey('transaction-reversal') })),
    'Reversal appended; original facts remain unchanged.');
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
      <button className="primary-action" onClick={() => setEditor('new')}><Plus size={17} /> New transaction</button>
    </PageHeader>
    <ValidationError message={error} />
    {success && <p className="success-banner">{success}</p>}
    <section className="finance-commandbar record-panel">
      <nav aria-label="Finance views">
        <button aria-current={tab === 'transactions'} onClick={() => setTab('transactions')}>Transaction register</button>
        <button aria-current={tab === 'cost'} onClick={() => setTab('cost')}>Crop cost trace</button>
      </nav>
      <button className="text-action" disabled={pending === 'reconcile'} onClick={() => mutate('reconcile', () => financeApi.reconcileFinancePayrollCosts(), 'Approved payroll cost reconciliation completed.')}><RefreshCw size={15} /> Reconcile payroll costs</button>
    </section>

    {tab === 'transactions' && <>
      <form className="finance-filters record-panel" onSubmit={(event) => { event.preventDefault(); loadTransactions().catch((e) => setError(getApiError(e))); }}>
        <label>From<DatePicker name="from" value={filters.from} onChange={(value) => setFilters({ ...filters, from: value })} /></label>
        <label>To<DatePicker name="to" value={filters.to} onChange={(value) => setFilters({ ...filters, to: value })} /></label>
        <label>Direction<select value={filters.type} onChange={(event) => setFilters({ ...filters, type: event.target.value })}><option value="">All</option><option>Expense</option><option>Income</option></select></label>
        <label>Status<select value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}><option value="">All</option><option>Draft</option><option>Posted</option><option>Reversed</option><option>Cancelled</option></select></label>
        <label>Category<select value={filters.category} onChange={(event) => setFilters({ ...filters, category: event.target.value })}><option value="">All</option>{financeCategories.map((value) => <option key={value} value={value}>{financeLabel(value)}</option>)}</select></label>
        <label className="finance-search">Payee or source<input value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} placeholder="Search recorded evidence" /></label>
        <button disabled={pending !== ''}>Apply filters</button>
      </form>
      <TransactionRegister transactions={transactions} pending={pending} onEdit={setEditor} onAllocate={setAllocating} onPost={post} onReverse={reverse} />
    </>}
    {tab === 'cost' && <CostWorkspace cycles={cycles} cost={cost} loading={pending === 'cost'} onSelect={loadCost} />}
    {editor && <FinanceDialog title={editor === 'new' ? 'Record transaction draft' : 'Edit transaction draft'} onClose={() => setEditor(null)}><TransactionForm transaction={editor === 'new' ? null : editor} onSaved={async () => { setEditor(null); await refresh(); }} onError={setError} /></FinanceDialog>}
    {allocating && <FinanceDialog title="Allocate full transaction amount" onClose={() => setAllocating(null)}><AllocationForm transaction={allocating} farm={farm?.farm ?? null} cycles={cycles} onSaved={async () => { setAllocating(null); await refresh(); }} onError={setError} /></FinanceDialog>}
  </div>;
}

function TransactionRegister({ transactions, pending, onEdit, onAllocate, onPost, onReverse }: { transactions: OperationalTransactionDto[]; pending: string; onEdit: (value: OperationalTransactionDto) => void; onAllocate: (value: OperationalTransactionDto) => void; onPost: (value: OperationalTransactionDto) => void; onReverse: (value: OperationalTransactionDto) => void; }) {
  const totals = useMemo(() => ({ income: transactions.filter((x) => x.type === 'Income' && x.status === 'Posted').reduce((n, x) => n + x.amountUsd, 0), expense: transactions.filter((x) => x.type === 'Expense' && x.status === 'Posted').reduce((n, x) => n + x.amountUsd, 0), drafts: transactions.filter((x) => x.status === 'Draft').length }), [transactions]);
  return <section className="finance-register record-panel">
    <header className="finance-register-summary"><div><small>Posted expenses</small><strong>{usd(totals.expense)}</strong></div><div><small>Posted income</small><strong>{usd(totals.income)}</strong></div><div><small>Drafts awaiting allocation</small><strong>{totals.drafts}</strong></div></header>
    <div className="finance-ledger-head"><span>Date / source</span><span>Payee or payer</span><span>Category</span><span>Amount</span><span>Status / action</span></div>
    {transactions.length ? transactions.map((transaction) => <article className="finance-row" key={transaction.id}>
      <span><strong>{transaction.eventDate}</strong><small>{transaction.sourceReference || 'No source reference'}</small></span>
      <span><strong>{transaction.payeeOrPayer}</strong><small>{transaction.allocations.length} allocation{transaction.allocations.length === 1 ? '' : 's'} · v{transaction.version}</small></span>
      <span>{financeLabel(transaction.category)}<small>{transaction.type}</small></span>
      <b className={transaction.type === 'Income' ? 'finance-income' : 'finance-expense'}>{transaction.type === 'Income' ? '+' : '−'}{usd(transaction.amountUsd)}</b>
      <span className="finance-row-actions"><em className={`status-pill status-${transaction.status.toLowerCase()}`}>{transaction.status}</em>{transaction.status === 'Draft' && <><button onClick={() => onEdit(transaction)}>Edit</button><button onClick={() => onAllocate(transaction)}>Allocate</button><button disabled={!transaction.allocations.length || pending !== ''} onClick={() => onPost(transaction)}><Send size={14} /> Post</button></>}{transaction.status === 'Posted' && <button className="text-action" disabled={pending !== ''} onClick={() => onReverse(transaction)}><RotateCcw size={14} /> Reverse</button>}</span>
    </article>) : <div className="finance-empty"><Landmark size={26} /><strong>No operational transactions match these filters.</strong><span>Create a draft or widen the date and status filters.</span></div>}
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

function AllocationForm({ transaction, farm, cycles, onSaved, onError }: { transaction: OperationalTransactionDto; farm: FarmDto | null; cycles: CycleOption[]; onSaved: () => Promise<void>; onError: (message: string) => void; }) {
  const initial = transaction.allocations.length ? transaction.allocations.map((item) => ({ key: item.id, allocationType: item.allocationType as AllocationRow['allocationType'], fieldId: item.fieldId || '', cropCycleId: item.cropCycleId || '', category: item.category, amountUsd: String(item.amountUsd) })) : [{ key: newFinanceKey('allocation'), allocationType: 'FarmOverhead' as const, fieldId: '', cropCycleId: '', category: transaction.category, amountUsd: String(transaction.amountUsd) }];
  const [rows, setRows] = useState<AllocationRow[]>(initial); const [saving, setSaving] = useState(false);
  const total = rows.reduce((sum, row) => sum + Number(row.amountUsd || 0), 0);
  const patchRow = (key: string, patch: Partial<AllocationRow>) => setRows((value) => value.map((row) => row.key === key ? { ...row, ...patch } : row));
  const submit = async (event: FormEvent) => { event.preventDefault(); setSaving(true); onError(''); try { await financeApi.setFinanceTransactionAllocations(transaction.id, new SetTransactionAllocationsRequest({ expectedVersion: transaction.version, allocations: rows.map((row) => new TransactionAllocationRequest({ allocationType: row.allocationType, fieldId: row.fieldId || undefined, cropCycleId: row.cropCycleId || undefined, category: row.category, amountUsd: Number(row.amountUsd) })) })); await onSaved(); } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); } };
  return <form className="allocation-form" onSubmit={submit}><p className={total === transaction.amountUsd ? 'allocation-reconciled' : 'allocation-unbalanced'}>Allocated {usd(total)} of {usd(transaction.amountUsd)} · {total === transaction.amountUsd ? 'exactly reconciled' : `${usd(Math.abs(transaction.amountUsd - total))} remaining`}</p>{rows.map((row) => <fieldset key={row.key} className="allocation-row"><label>Scope<select value={row.allocationType} onChange={(event) => patchRow(row.key, { allocationType: event.target.value as AllocationRow['allocationType'], fieldId: '', cropCycleId: '' })}><option value="CropCycleDirect">Crop-cycle direct</option><option value="Field">Field only</option><option value="FarmOverhead">Farm overhead</option></select></label>{row.allocationType !== 'FarmOverhead' && <label>Field<select value={row.fieldId} onChange={(event) => patchRow(row.key, { fieldId: event.target.value, cropCycleId: '' })} required><option value="">Select field</option>{farm?.fields.map((field) => <option key={field.id} value={field.id}>{field.code} · {field.name}</option>)}</select></label>}{row.allocationType === 'CropCycleDirect' && <label>Crop cycle<select value={row.cropCycleId} onChange={(event) => { const cycle = cycles.find((item) => item.id === event.target.value); patchRow(row.key, { cropCycleId: event.target.value, fieldId: cycle?.fieldId || row.fieldId }); }} required><option value="">Select cycle</option>{cycles.filter((cycle) => !row.fieldId || cycle.fieldId === row.fieldId).map((cycle) => <option key={cycle.id} value={cycle.id}>{cycle.label}</option>)}</select></label>}<label>Amount (USD)<input type="number" min="0.01" step="0.01" value={row.amountUsd} onChange={(event) => patchRow(row.key, { amountUsd: event.target.value })} required /></label><button type="button" className="icon-button" aria-label="Remove allocation" disabled={rows.length === 1} onClick={() => setRows((value) => value.filter((item) => item.key !== row.key))}><X size={16} /></button></fieldset>)}<footer className="form-actions"><button type="button" className="secondary" onClick={() => setRows((value) => [...value, { key: newFinanceKey('allocation'), allocationType: 'FarmOverhead', fieldId: '', cropCycleId: '', category: transaction.category, amountUsd: '' }])}><Plus size={15} /> Add allocation</button><button disabled={saving || total !== transaction.amountUsd}>Save exact allocations</button></footer></form>;
}

function CostWorkspace({ cycles, cost, loading, onSelect }: { cycles: CycleOption[]; cost: CropCycleCostSummaryDto | null; loading: boolean; onSelect: (id: string) => void; }) {
  return <div className="cost-workspace"><section className="cost-selector record-panel"><label>Crop cycle<select onChange={(event) => onSelect(event.target.value)} defaultValue=""><option value="">Choose a crop cycle</option>{cycles.map((cycle) => <option key={cycle.id} value={cycle.id}>{cycle.label}</option>)}</select></label><p>Totals come only from signed `OperationalCostPosting` rows. Income and unallocated overhead never alter crop cost.</p></section>{loading && <LoadingState label="Tracing cost sources" />}{cost && <><section className="cost-summary record-panel"><header><div><small>{cost.fieldName}</small><h2>{usd(cost.totalCostUsd)}</h2><span>Total controlled crop cost</span></div><div className="cost-unit-rates"><strong>{perUnit(cost.costPerHectareUsd, 'ha')}</strong><small>{cost.reportingHectares == null ? 'Reporting area missing' : `${cost.reportingHectares} reporting ha`}</small><strong>{perUnit(cost.costPerTonneUsd, 't')}</strong><small>{cost.actualHarvestedTonnes == null ? 'Actual harvest missing' : `${cost.actualHarvestedTonnes} actual t`}</small></div></header><div className="cost-breakdown"><span><small>Labour</small><b>{usd(cost.labourUsd)}</b></span><span><small>Applied inputs</small><b>{usd(cost.appliedInputsUsd)}</b></span><span><small>Direct expenses</small><b>{usd(cost.directExpensesUsd)}</b></span><span><small>Approved loss</small><b>{usd(cost.approvedInventoryLossUsd)}</b></span></div></section><section className="cost-trace record-panel"><header><FileSearch size={19} /><div><h2>Authoritative source trace</h2><p>Every amount below binds to one operational source.</p></div></header>{cost.sources.map((source) => <article key={source.id}><span className="trace-node" /><span><strong>{financeLabel(source.category)}</strong><small>{source.sourceType} · {source.sourceId}</small>{source.payrollRunId && <small>Payroll run {source.payrollRunId} · calculation v{source.payrollCalculationVersion} · worker line {source.payrollWorkerLineId} · work record {source.workRecordId}</small>}{source.operationalTransactionId && <small>Transaction {source.operationalTransactionId} · allocation {source.transactionAllocationId}</small>}<small>{source.reversalOfId ? `Reverses posting ${source.reversalOfId}` : source.sourceDescription}</small></span><b className={source.amountUsd < 0 ? 'finance-income' : ''}>{usd(source.amountUsd)}</b></article>)}</section></> }</div>;
}

function FinanceDialog({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) { return <div className="dialog-backdrop" role="presentation"><section className="inventory-dialog finance-dialog" role="dialog" aria-modal="true" aria-label={title}><header><div><CircleDollarSign size={19} /><h2>{title}</h2></div><button className="icon-button" onClick={onClose} aria-label="Close"><X size={18} /></button></header>{children}</section></div>; }
