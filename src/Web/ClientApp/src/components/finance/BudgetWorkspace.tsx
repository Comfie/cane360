import { useCallback, useState, type FormEvent } from 'react';
import { FileSearch, History, Plus, Printer, Send, ShieldCheck, Trash2 } from 'lucide-react';
import {
  ApproveBudgetRequest,
  BudgetActionRequest,
  BudgetLineRequest,
  CreateBudgetRequest,
  CreateBudgetRevisionRequest,
  FinanceClient,
  UpdateBudgetRequest,
  type BudgetDto,
  type BudgetVarianceReportDto,
} from '../../web-api-client';
import { LoadingState } from '../LoadingState';
import { getApiError } from '../apiError';
import { budgetCategories, canApproveBudget, canEditBudget, canReviseBudget, canSubmitBudget, financeLabel, newFinanceKey, perUnit, usd, variancePercent } from './financeView';

const api = new FinanceClient();

export interface FinanceCycleOption { id: string; fieldId: string; label: string; status: string; }

export function BudgetWorkspace({ cycles, role, onError, onSuccess }: {
  cycles: FinanceCycleOption[];
  role: string;
  onError: (message: string) => void;
  onSuccess: (message: string) => void;
}) {
  const [cycleId, setCycleId] = useState('');
  const [budgets, setBudgets] = useState<BudgetDto[]>([]);
  const [selectedId, setSelectedId] = useState('');
  const [report, setReport] = useState<BudgetVarianceReportDto | null>(null);
  const [pending, setPending] = useState('');

  const load = useCallback(async (nextCycleId: string, preferredId?: string) => {
    if (!nextCycleId) { setBudgets([]); setSelectedId(''); setReport(null); return; }
    setPending('load'); onError('');
    try {
      const next = await api.getFinanceBudgets(nextCycleId);
      setBudgets(next);
      setSelectedId(preferredId && next.some((x) => x.id === preferredId)
        ? preferredId : (next.find((x) => x.status === 'Draft' || x.status === 'Submitted') ?? next[0])?.id ?? '');
      if (next.some((x) => x.status === 'Approved')) setReport(await api.getFinanceBudgetVariance(nextCycleId));
      else setReport(null);
    } catch (error) { onError(getApiError(error)); }
    finally { setPending(''); }
  }, [onError]);

  const mutate = async (key: string, action: () => Promise<BudgetDto>, message: string) => {
    setPending(key); onError('');
    try {
      const result = await action();
      await load(result.cropCycleId, result.id);
      onSuccess(message);
    } catch (error) { onError(getApiError(error)); }
    finally { setPending(''); }
  };

  const selected = budgets.find((budget) => budget.id === selectedId) ?? null;
  const selectedCycle = cycles.find((cycle) => cycle.id === cycleId);
  const closed = ['Harvested', 'Closed', 'Cancelled'].includes(selectedCycle?.status ?? '');

  return <div className="budget-workspace">
    <section className="budget-selector record-panel">
      <label>Crop cycle<select value={cycleId} onChange={(event) => {
        const value = event.target.value; setCycleId(value); load(value).catch(() => undefined);
      }}><option value="">Choose a crop cycle</option>{cycles.map((cycle) =>
        <option key={cycle.id} value={cycle.id}>{cycle.label}</option>)}</select></label>
      <p>Approved plans compare only with signed authoritative OperationalCostPosting values.</p>
    </section>
    {pending === 'load' && <LoadingState label="Loading budget history" />}
    {cycleId && !budgets.length && pending !== 'load' && !closed &&
      <CreateBudgetForm cycleId={cycleId} pending={Boolean(pending)} onCreate={(request) =>
        mutate('create', () => api.createFinanceBudget(request), 'Budget draft created.')} />}
    {cycleId && !budgets.length && closed && <section className="record-panel budget-empty"><strong>Historical view only</strong><p>Ordinary budget creation is closed for this crop cycle.</p></section>}
    {budgets.length > 0 && <>
      <section className="budget-history record-panel">
        <header><History size={17} /><strong>Version history</strong></header>
        <div>{budgets.map((budget) => <button key={budget.id} type="button"
          aria-current={budget.id === selectedId} onClick={() => setSelectedId(budget.id)}>
          v{budget.version} <span className={`status-pill status-${budget.status.toLowerCase()}`}>{budget.status}</span>
        </button>)}</div>
      </section>
      {selected && <BudgetCard budget={selected} role={role} pending={Boolean(pending)}
        onUpdate={(request) => mutate('update', () => api.updateFinanceBudget(selected.id, request), 'Budget draft updated.')}
        onAddLine={(request) => mutate('line-add', () => api.addFinanceBudgetLine(selected.id, request), 'Budget line added.')}
        onUpdateLine={(lineId, request) => mutate(`line-${lineId}`, () => api.updateFinanceBudgetLine(selected.id, lineId, request), 'Budget line updated.')}
        onRemoveLine={(lineId) => mutate(`remove-${lineId}`, () => api.removeFinanceBudgetLine(selected.id, lineId, selected.rowVersion), 'Budget line removed.')}
        onSubmit={() => mutate('submit', () => api.submitFinanceBudget(selected.id,
          new BudgetActionRequest({ expectedRowVersion: selected.rowVersion })), 'Budget submitted for Grower approval.')}
        onApprove={() => mutate('approve', () => api.approveFinanceBudget(selected.id,
          new ApproveBudgetRequest({ expectedRowVersion: selected.rowVersion,
            idempotencyKey: newFinanceKey('budget-approval') })), 'Immutable budget version approved.')}
        onRevise={() => mutate('revision', () => api.createFinanceBudgetRevision(selected.id,
          new CreateBudgetRevisionRequest({ name: undefined, notes: undefined })), 'Draft revision copied from the approved version.')} />}
      {report && <VarianceReport report={report} />}
    </>}
  </div>;
}

function CreateBudgetForm({ cycleId, pending, onCreate }: { cycleId: string; pending: boolean; onCreate: (request: CreateBudgetRequest) => void; }) {
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    onCreate(new CreateBudgetRequest({ cropCycleId: cycleId, name: String(data.get('name')).trim(), reportingAreaHa: undefined,
      expectedProductionTonnes: Number(data.get('tonnes')) || undefined,
      notes: String(data.get('notes') || '').trim() || undefined }));
  };
  return <form className="budget-create record-panel" onSubmit={submit}><header><div><span className="eyebrow">Version 1</span><h2>Create crop-cycle budget</h2></div></header><div className="form-grid"><label>Name / reference<input name="name" maxLength={120} required /></label><label>Expected production tonnes<input name="tonnes" type="number" min="0.001" step="0.001" /></label><label className="is-wide">Notes<textarea name="notes" maxLength={1000} /></label></div><footer className="form-actions"><span>Reporting area is snapshotted from the selected field.</span><button disabled={pending}><Plus size={15} /> Create draft</button></footer></form>;
}

function BudgetCard({ budget, role, pending, onUpdate, onAddLine, onUpdateLine, onRemoveLine,
  onSubmit, onApprove, onRevise }: { budget: BudgetDto; role: string; pending: boolean;
    onUpdate: (request: UpdateBudgetRequest) => void; onAddLine: (request: BudgetLineRequest) => void;
    onUpdateLine: (lineId: string, request: BudgetLineRequest) => void; onRemoveLine: (lineId: string) => void;
    onSubmit: () => void; onApprove: () => void; onRevise: () => void; }) {
  const saveDetails = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const data = new FormData(event.currentTarget);
    onUpdate(new UpdateBudgetRequest({ name: String(data.get('name')).trim(),
      reportingAreaHa: Number(data.get('area')) || undefined,
      expectedProductionTonnes: Number(data.get('tonnes')) || undefined,
      notes: String(data.get('notes') || '').trim() || undefined, expectedRowVersion: budget.rowVersion }));
  };
  return <section className="budget-card record-panel">
    <header><div><span className="eyebrow">Approved planning facts</span><h2>{budget.name}</h2><p>Version {budget.version} · {budget.lines.length} line{budget.lines.length === 1 ? '' : 's'}</p></div><div><span className={`status-pill status-${budget.status.toLowerCase()}`}>{budget.status}</span><strong>{usd(budget.totalBudgetUsd)}</strong></div></header>
    {canEditBudget(budget.status) && <form className="budget-details-form" onSubmit={saveDetails}><label>Name<input name="name" defaultValue={budget.name} required /></label><label>Reporting area ha<input name="area" type="number" min="0.0001" step="0.0001" defaultValue={budget.reportingAreaHa} /></label><label>Expected tonnes<input name="tonnes" type="number" min="0.001" step="0.001" defaultValue={budget.expectedProductionTonnes} /></label><label>Notes<input name="notes" defaultValue={budget.notes} /></label><button disabled={pending}>Save details</button></form>}
    <div className="budget-line-heading"><span>Category / description</span><span>Quantity / rate</span><span>Amount</span><span>Action</span></div>
    <div className="budget-lines">{budget.lines.map((line) => <article key={line.id}><span><strong>{financeLabel(line.category)}</strong><small>{line.description}</small></span><span><strong>{line.quantity == null ? '—' : `${line.quantity} ${line.unit}`}</strong><small>{line.unitRateUsd == null ? 'No unit rate' : `${usd(line.unitRateUsd)} / ${line.unit}`}</small></span><b>{usd(line.amountUsd)}</b><span>{canEditBudget(budget.status) && <><button type="button" className="text-action" disabled={pending} onClick={() => {
      const description = globalThis.prompt('Line description', line.description)?.trim();
      const amount = Number(globalThis.prompt('Amount USD', String(line.amountUsd)));
      if (description && amount > 0) onUpdateLine(line.id, new BudgetLineRequest({ category: line.category,
        description, amountUsd: amount, quantity: line.quantity, unit: line.unit,
        unitRateUsd: line.unitRateUsd, notes: line.notes, expectedRowVersion: budget.rowVersion }));
    }}>Edit</button><button type="button" className="icon-button" aria-label={`Remove ${line.description}`} disabled={pending} onClick={() => onRemoveLine(line.id)}><Trash2 size={15} /></button></>}</span></article>)}</div>
    {canEditBudget(budget.status) && <BudgetLineForm budget={budget} pending={pending} onAdd={onAddLine} />}
    <footer className="budget-actions">
      {canSubmitBudget(role, budget.status) && <button type="button" disabled={pending || !budget.lines.length} onClick={onSubmit}><Send size={15} /> Submit exact draft</button>}
      {canApproveBudget(role, budget.status) && <button type="button" disabled={pending} onClick={onApprove}><ShieldCheck size={15} /> Grower approve version {budget.version}</button>}
      {role === 'FarmManager' && budget.status === 'Submitted' && <small>Grower approval required. Submitted lines are locked.</small>}
      {canReviseBudget(role, budget.status) && <button type="button" className="secondary" disabled={pending} onClick={onRevise}><History size={15} /> Create revision</button>}
    </footer>
  </section>;
}

function BudgetLineForm({ budget, pending, onAdd }: { budget: BudgetDto; pending: boolean; onAdd: (request: BudgetLineRequest) => void; }) {
  const submit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form);
    const quantity = Number(data.get('quantity')) || undefined; const unitRate = Number(data.get('unitRate')) || undefined;
    onAdd(new BudgetLineRequest({ category: String(data.get('category')), description: String(data.get('description')).trim(),
      amountUsd: Number(data.get('amount')), quantity, unit: quantity ? String(data.get('unit')).trim() : undefined,
      unitRateUsd: quantity ? unitRate : undefined, notes: String(data.get('notes') || '').trim() || undefined,
      expectedRowVersion: budget.rowVersion }));
  };
  return <form className="budget-line-form" onSubmit={submit}><label>Category<select name="category">{budgetCategories.map((category) => <option key={category} value={category}>{financeLabel(category)}</option>)}</select></label><label>Description<input name="description" maxLength={240} required /></label><label>Amount USD<input name="amount" type="number" min="0.01" step="0.01" required /></label><label>Quantity<input name="quantity" type="number" min="0.000001" step="any" /></label><label>Unit<input name="unit" maxLength={32} /></label><label>Unit rate USD<input name="unitRate" type="number" min="0.000001" step="any" /></label><label>Notes<input name="notes" maxLength={500} /></label><button disabled={pending}><Plus size={15} /> Add line</button></form>;
}

function VarianceReport({ report }: { report: BudgetVarianceReportDto }) {
  return <section className="variance-report record-panel">
    <header><div><span className="eyebrow">Budget versus actual</span><h2>{report.fieldName} · budget v{report.budgetVersion}</h2><p>Crop cycle {report.cropCycleId} · approved {report.budgetApprovedAt.toLocaleString()} · actual cost as of {report.actualCostAsOf.toLocaleString()}</p></div><button type="button" className="secondary print-hide" onClick={() => globalThis.print()}><Printer size={15} /> Print report</button></header>
    <div className="variance-metrics"><span><small>Budget / ha</small><strong>{perUnit(report.budgetCostPerHectareUsd, 'ha')}</strong></span><span><small>Actual / ha</small><strong>{perUnit(report.actualCostPerHectareUsd, 'ha')}</strong></span><span><small>Budget / tonne</small><strong>{perUnit(report.budgetCostPerTonneUsd, 't')}</strong></span><span><small>Actual / tonne</small><strong>{perUnit(report.actualCostPerTonneUsd, 't')}</strong></span></div>
    <div className="variance-table"><div className="variance-heading"><span>Category</span><span>Budget</span><span>Actual</span><span>Variance</span><span>Variance %</span></div>{report.categories.map((row) => <div key={row.category} className="variance-row"><strong>{financeLabel(row.category)}</strong><span>{usd(row.budgetUsd)}</span><span>{usd(row.actualUsd)}</span><span className={`variance-${row.status.toLowerCase()}`}>{row.varianceUsd > 0 ? '+' : ''}{usd(row.varianceUsd)}</span><span>{variancePercent(row.variancePercent)}</span></div>)}<div className="variance-row variance-total"><strong>Total</strong><span>{usd(report.totalBudgetUsd)}</span><span>{usd(report.totalActualUsd)}</span><span>{report.totalVarianceUsd > 0 ? '+' : ''}{usd(report.totalVarianceUsd)}</span><span>{variancePercent(report.totalVariancePercent)}</span></div></div>
    <details className="variance-drilldown"><summary><FileSearch size={16} /> Actual-cost source drill-down ({report.actualSources.length})</summary>{report.actualSources.length ? report.actualSources.map((source) => <article key={source.id}><span><strong>{financeLabel(source.category)}</strong><small>OperationalCostPosting {source.id}</small></span><b>{usd(source.amountUsd)}</b><small>{source.sourceDescription}</small></article>) : <p>No authoritative actual-cost postings yet.</p>}</details>
    <footer className="print-report-footer">{report.farmName} · generated {report.actualCostAsOf.toLocaleString()}</footer>
  </section>;
}
