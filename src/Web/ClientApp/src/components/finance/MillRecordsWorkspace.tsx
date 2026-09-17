import { useCallback, useEffect, useRef, useState, type FormEvent, type ReactNode } from 'react';
import { withMutationGuard } from '../mutationGuard';
import { useDialogFocus } from '../useDialogFocus';
import { FileDown, FilePlus2, Link2, Plus, Printer, Scale, Warehouse, X } from 'lucide-react';
import {
  AddStatementTicketMatchRequest,
  CorrectStatementRequest,
  CorrectTicketRequest,
  EvidenceUploadRequest,
  AdministrationClient,
  MillRecordsClient,
  MillRequest,
  RecordMillRecordRequest,
  ReverseStatementTicketMatchRequest,
  StatementRequest,
  TicketRequest,
  type CandidateTicketDto,
  type DocumentCategoryDto,
  type GrowerStatementDto,
  type MillDto,
  type MillRecordsSessionDto,
  type WeighbridgeTicketDto,
} from '../../web-api-client';
import { DatePicker } from '../DatePicker';
import { LoadingState } from '../LoadingState';
import { getApiError } from '../apiError';
import { financeLabel, newFinanceKey, usd } from './financeView';
import { amountReconciliation, canCorrectMillEvidence, matchWarning, tonnes } from './millRecordsView';

const api = new MillRecordsClient();
const administration = new AdministrationClient();
const today = new Date().toISOString().slice(0, 10);
type View = 'tickets' | 'statements' | 'mills';
type TicketEditor = { ticket: WeighbridgeTicketDto | null; correction: boolean };
type StatementEditor = { statement: GrowerStatementDto | null; correction: boolean };

export function MillRecordsWorkspace({ onError, onSuccess }: {
  onError: (message: string) => void;
  onSuccess: (message: string) => void;
}) {
  const [session, setSession] = useState<MillRecordsSessionDto | null>(null);
  const [mills, setMills] = useState<MillDto[]>([]);
  const [tickets, setTickets] = useState<WeighbridgeTicketDto[]>([]);
  const [statements, setStatements] = useState<GrowerStatementDto[]>([]);
  const [documentCategories, setDocumentCategories] = useState<DocumentCategoryDto[]>([]);
  const [view, setView] = useState<View>('tickets');
  const [filters, setFilterValues] = useState({ from: '', to: '', millId: '', fieldId: '', cropCycleId: '', status: '', matchStatus: '', search: '' });
  const [ticketPage, setTicketPage] = useState(1);
  const [ticketTotals, setTicketTotals] = useState({ totalCount: 0, unmatchedCount: 0, recordedNetTonnes: 0 });
  const [statementPage, setStatementPage] = useState(1);
  const [statementTotal, setStatementTotal] = useState(0);
  const loadGeneration = useRef(0);
  const mutationInFlight = useRef(false);
  const setFilters = (next: typeof filters) => { setTicketPage(1); setStatementPage(1); setFilterValues(next); };
  const [ticketEditor, setTicketEditor] = useState<TicketEditor | null>(null);
  const [statementEditor, setStatementEditor] = useState<StatementEditor | null>(null);
  const [millEditor, setMillEditor] = useState<MillDto | 'new' | null>(null);
  const [selectedStatementId, setSelectedStatementId] = useState('');
  const [candidates, setCandidates] = useState<CandidateTicketDto[]>([]);
  const [pending, setPending] = useState('');
  const [loading, setLoading] = useState(true);
  const [refreshing, setRefreshing] = useState(false);

  const load = useCallback(async () => {
    const generation = ++loadGeneration.current;
    setRefreshing(true);
    try {
      const [nextSession, nextMills, nextTickets, nextStatements] = await Promise.all([
        api.getMillRecordsSession(), api.getMills(true),
        view === 'tickets' ? api.getWeighbridgeTickets(filters.from || undefined, filters.to || undefined,
          filters.millId || undefined, filters.fieldId || undefined,
          filters.cropCycleId || undefined, filters.status || undefined,
          filters.matchStatus || undefined, filters.search || undefined, ticketPage, 50) : Promise.resolve(null),
        view === 'statements' ? api.getGrowerStatements(filters.from || undefined, filters.to || undefined,
          filters.millId || undefined, filters.matchStatus || undefined,
          filters.search || undefined, statementPage, 50) : Promise.resolve(null),
      ]);
      if (generation !== loadGeneration.current) return;
      setSession(nextSession); setMills(nextMills);
      if (nextTickets) { setTickets(nextTickets.items); setTicketTotals(nextTickets); }
      if (nextStatements) {
        setStatements(nextStatements.items); setStatementTotal(nextStatements.totalCount);
        setSelectedStatementId((current) => nextStatements.items.some((x) => x.id === current)
          ? current : nextStatements.items.find((x) => x.isCurrent)?.id ?? '');
      }
    } finally {
      if (generation === loadGeneration.current) setRefreshing(false);
    }
  }, [filters, ticketPage, statementPage, view]);
  const refresh = useRef(load);
  useEffect(() => { refresh.current = load; }, [load]);
  useEffect(() => {
    let current = true;
    administration.getCategoriesAdministration().then((items) => {
      if (current) setDocumentCategories(items.filter((item) => item.active));
    }).catch((error) => { if (current) onError(getApiError(error)); });
    return () => { current = false; };
  }, [onError]);
  const invalidateLoad = useCallback(() => { loadGeneration.current++; }, []);

  useEffect(() => { let current = true; load().catch((error) => { if (current) onError(getApiError(error)); })
    .finally(() => { if (current) setLoading(false); }); return () => { current = false; invalidateLoad(); }; }, [load, onError, invalidateLoad]);

  useEffect(() => {
    let current = true;
    if (!selectedStatementId || view !== 'statements') { setCandidates([]); return; }
    setCandidates([]);
    api.getStatementCandidateTickets(selectedStatementId).then((next) => { if (current) setCandidates(next); })
      .catch((error) => { if (current) onError(getApiError(error)); });
    return () => { current = false; };
  }, [selectedStatementId, statements, onError, view]);

  const mutate = async (key: string, action: () => Promise<unknown>, message: string) => {
    try { return await withMutationGuard(mutationInFlight, async () => {
      setPending(key); onError(''); onSuccess('');
      try {
        await action();
        // A failed refresh does not undo a persisted command or invite its resubmission.
        try { await refresh.current(); }
        catch (error) { onError(`Saved, but the workspace could not refresh. ${getApiError(error)}`); }
        onSuccess(message);
      } finally { setPending(''); }
    }); }
    catch (error) { onError(getApiError(error)); return false; }
  };

  const activeMills = mills.filter((mill) => mill.active);
  const selectedStatement = statements.find((statement) => statement.id === selectedStatementId);
  const currentTickets = tickets.filter((ticket) => ticket.isCurrent);
  const currentStatements = statements.filter((statement) => statement.isCurrent);

  if (loading) return <LoadingState label="Opening mill evidence" />;
  return <div className="mill-workspace" aria-busy={refreshing}>
    <section className="mill-toolbar record-panel">
      <div className="mill-view-switch" role="group" aria-label="Mill record view">
        <button aria-current={view === 'tickets'} onClick={() => {
          if (!['', 'Matched', 'Unmatched'].includes(filters.matchStatus)) setFilters({ ...filters, matchStatus: '' });
          setView('tickets');
        }}>Tickets</button>
        <button aria-current={view === 'statements'} onClick={() => setView('statements')}>Statements</button>
        <button aria-current={view === 'mills'} onClick={() => setView('mills')}>Mills</button>
      </div>
      <div className="mill-toolbar-actions">
        {view !== 'mills' && <a className="secondary" href={exportUrl(view, filters)} download><FileDown size={15} /> Export CSV</a>}
        <button className="secondary" onClick={() => globalThis.print()}><Printer size={15} /> Print</button>
        {view === 'tickets' && <button onClick={() => setTicketEditor({ ticket: null, correction: false })}><Plus size={15} /> New ticket</button>}
        {view === 'statements' && <button onClick={() => setStatementEditor({ statement: null, correction: false })}><Plus size={15} /> New statement</button>}
        {view === 'mills' && <button onClick={() => setMillEditor('new')}><Plus size={15} /> New mill</button>}
      </div>
    </section>

    {view !== 'mills' && <form className="mill-filters record-panel" onSubmit={(event) => { event.preventDefault(); load().catch((error) => onError(getApiError(error))); }}>
      <label>From<DatePicker name="from" value={filters.from} onChange={(from) => setFilters({ ...filters, from })} /></label>
      <label>To<DatePicker name="to" value={filters.to} onChange={(to) => setFilters({ ...filters, to })} /></label>
      <label>Mill<select value={filters.millId} onChange={(event) => setFilters({ ...filters, millId: event.target.value })}><option value="">All mills</option>{mills.map((mill) => <option key={mill.id} value={mill.id}>{mill.code} · {mill.name}{mill.active ? '' : ' · inactive'}</option>)}</select></label>
      {view === 'tickets' && <label>Field<select value={filters.fieldId} onChange={(event) => setFilters({ ...filters, fieldId: event.target.value, cropCycleId: '' })}><option value="">All fields</option>{session?.fields.map((field) => <option key={field.id} value={field.id}>{field.code} · {field.name}</option>)}</select></label>}
      {view === 'tickets' && <label>Crop cycle<select value={filters.cropCycleId} onChange={(event) => setFilters({ ...filters, cropCycleId: event.target.value })}><option value="">All cycles</option>{session?.fields.filter((field) => !filters.fieldId || field.id === filters.fieldId).flatMap((field) => field.cropCycles.map((cycle) => <option key={cycle.id} value={cycle.id}>{field.code} · {cycle.variety}</option>))}</select></label>}
      {view === 'tickets' && <label>Status<select value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}><option value="">All states</option><option>Draft</option><option>Recorded</option></select></label>}
      <label>Match state<select value={filters.matchStatus} onChange={(event) => setFilters({ ...filters, matchStatus: event.target.value })}><option value="">All states</option>{view === 'tickets' ? <><option>Matched</option><option>Unmatched</option></> : <><option>Unmatched</option><option>PartiallyMatched</option><option>Matched</option><option>Variance</option></>}</select></label>
      <label className="mill-search">Reference<input value={filters.search} onChange={(event) => setFilters({ ...filters, search: event.target.value })} placeholder="Ticket or statement" /></label>
      <button disabled={pending !== ''}>Apply</button>
    </form>}

    {view === 'tickets' && <section className="mill-register record-panel">
      <header className="mill-summary"><span><small>Recorded net tonnes · all filtered tickets</small><strong>{ticketTotals.recordedNetTonnes.toLocaleString(undefined, { maximumFractionDigits: 3 })} t</strong></span><span><small>Current tickets</small><strong>{ticketTotals.totalCount}</strong></span><span><small>Unmatched</small><strong>{ticketTotals.unmatchedCount}</strong></span></header>
      <div className="mill-table-head"><span>Date / ticket</span><span>Mill / field</span><span>Weights</span><span>Evidence / match</span><span>State / actions</span></div>
      {currentTickets.length ? currentTickets.map((ticket) => <TicketRow key={ticket.id} ticket={ticket} role={session?.role ?? ''} pending={pending} categories={documentCategories} onEdit={(correction) => setTicketEditor({ ticket, correction })} onMutate={mutate} />) : <Empty label="No weighbridge tickets match these filters." />}
      <nav className="payroll-pagination" aria-label="Ticket pages">
        <button disabled={refreshing || ticketPage <= 1 || pending !== ''} onClick={() => setTicketPage((page) => page - 1)}>Previous tickets</button>
        <span aria-live="polite">Page {ticketPage} of {Math.max(1, Math.ceil(ticketTotals.totalCount / 50))}</span>
        <button disabled={refreshing || ticketPage * 50 >= ticketTotals.totalCount || pending !== ''} onClick={() => setTicketPage((page) => page + 1)}>Next tickets</button>
      </nav>
    </section>}

    {view === 'statements' && <div className="statement-layout">
      <section className="statement-list record-panel">{currentStatements.length ? currentStatements.map((statement) => <button key={statement.id} className="statement-card" aria-current={statement.id === selectedStatementId} onClick={() => setSelectedStatementId(statement.id)}><span><strong>{statement.statementReference}</strong><small>{statement.millCode} · {statement.periodStart} to {statement.periodEnd}</small></span><span><b>{statement.totalTonnes.toLocaleString()} t</b><em className={`status-pill status-${statement.reconciliation.status.toLowerCase()}`}>{financeLabel(statement.reconciliation.status)}</em></span></button>) : <Empty label="No grower statements match these filters." />}<nav className="payroll-pagination" aria-label="Statement pages"><button disabled={refreshing || statementPage <= 1 || pending !== ''} onClick={() => setStatementPage((page) => page - 1)}>Previous statements</button><span aria-live="polite">Page {statementPage} of {Math.max(1, Math.ceil(statementTotal / 50))}</span><button disabled={refreshing || statementPage * 50 >= statementTotal || pending !== ''} onClick={() => setStatementPage((page) => page + 1)}>Next statements</button></nav></section>
      {selectedStatement && <StatementDetail statement={selectedStatement} candidates={candidates} role={session?.role ?? ''} pending={pending} categories={documentCategories} onEdit={(correction) => setStatementEditor({ statement: selectedStatement, correction })} onMutate={mutate} />}
    </div>}

    {view === 'mills' && <section className="mill-reference-list record-panel">{mills.map((mill) => <article key={mill.id}><span><strong>{mill.code}</strong><small>{mill.name}{mill.location ? ` · ${mill.location}` : ''}</small></span><em className={`status-pill status-${mill.active ? 'active' : 'inactive'}`}>{mill.active ? 'Active' : 'Inactive'}</em><button className="secondary" onClick={() => setMillEditor(mill)}>Edit</button>{mill.active && <button className="text-action" disabled={pending !== ''} onClick={() => mutate(`mill-${mill.id}`, () => api.deactivateMill(mill.id, new RecordMillRecordRequest({ expectedVersion: mill.version, idempotencyKey: newFinanceKey('mill-deactivate') })), 'Mill deactivated; historical evidence remains visible.')}>Deactivate</button>}</article>)}</section>}

    {ticketEditor && <Editor title={ticketEditor.correction ? 'Correct recorded ticket' : ticketEditor.ticket ? 'Edit ticket draft' : 'New weighbridge ticket'} onClose={() => setTicketEditor(null)}><TicketForm editor={ticketEditor} mills={activeMills} session={session} pending={pending} onSaved={async (action, message) => { if (await mutate('ticket-save', action, message)) setTicketEditor(null); }} /></Editor>}
    {statementEditor && <Editor title={statementEditor.correction ? 'Correct recorded statement' : statementEditor.statement ? 'Edit statement draft' : 'New grower statement'} onClose={() => setStatementEditor(null)}><StatementForm editor={statementEditor} mills={activeMills} pending={pending} onSaved={async (action, message) => { if (await mutate('statement-save', action, message)) setStatementEditor(null); }} /></Editor>}
    {millEditor && <Editor title={millEditor === 'new' ? 'New mill reference' : 'Edit mill reference'} onClose={() => setMillEditor(null)}><MillForm mill={millEditor === 'new' ? null : millEditor} pending={pending} onSaved={async (action, message) => { if (await mutate('mill-save', action, message)) setMillEditor(null); }} /></Editor>}
  </div>;
}

function TicketRow({ ticket, role, pending, categories, onEdit, onMutate }: { ticket: WeighbridgeTicketDto; role: string; pending: string; categories: DocumentCategoryDto[]; onEdit: (correction: boolean) => void; onMutate: (key: string, action: () => Promise<unknown>, message: string) => Promise<boolean>; }) {
  return <article className="mill-ticket-row">
    <span><strong>{ticket.ticketReference}</strong><small>{ticket.ticketDate}{ticket.correctsTicketId ? ' · correction' : ''}</small></span>
    <span><strong>{ticket.millCode} · {ticket.millName}</strong><small>{ticket.fieldName || 'Field not assigned'}{ticket.cropCycleLabel ? ` · ${ticket.cropCycleLabel}` : ''}</small></span>
    <span className="ticket-weights"><b>{tonnes(ticket.netTonnes)} net</b><small>{ticket.grossTonnes} gross{ticket.tareTonnes == null ? ' · tare unavailable' : ` − ${ticket.tareTonnes} tare`}</small>{ticket.recordedHarvestTonnes != null && <small>{ticket.recordedHarvestTonnes} t crop-cycle harvest result · unchanged</small>}</span>
    <span><small>{ticket.evidence.length ? `${ticket.evidence.length} evidence file${ticket.evidence.length === 1 ? '' : 's'}` : 'No attached evidence'}</small><small>{ticket.matchedStatementIds.length ? `${ticket.matchedStatementIds.length} statement match${ticket.matchedStatementIds.length === 1 ? '' : 'es'}` : 'Unmatched'}{ticket.matchedToAnotherStatement ? ' · review reuse' : ''}</small>{ticket.evidence.map((evidence) => <a key={evidence.id} href={`/api/finance/mill-records/evidence/${evidence.id}`}>{evidence.originalFileName}</a>)}</span>
    <span className="mill-row-actions"><em className={`status-pill status-${ticket.status.toLowerCase()}`}>{ticket.status}</em>{ticket.status === 'Draft' && <><button className="secondary" onClick={() => onEdit(false)}>Edit</button><button disabled={pending !== ''} onClick={() => onMutate(`record-${ticket.id}`, () => api.recordWeighbridgeTicket(ticket.id, new RecordMillRecordRequest({ expectedVersion: ticket.version, idempotencyKey: newFinanceKey('ticket-record') })), 'Ticket recorded as authoritative mill evidence.')}>Record</button></>}{canCorrectMillEvidence(role, ticket.status) && <button className="secondary" onClick={() => onEdit(true)}>Correct</button>}<EvidenceUploadButton label="Attach" disabled={pending !== ''} categories={categories} onFile={(file, categoryId) => onMutate(`evidence-${ticket.id}`, async () => api.uploadWeighbridgeTicketEvidence(ticket.id, await evidenceRequest(file, categoryId)), 'Ticket evidence attached.')} /></span>
  </article>;
}

function StatementDetail({ statement, candidates, role, pending, categories, onEdit, onMutate }: { statement: GrowerStatementDto; candidates: CandidateTicketDto[]; role: string; pending: string; categories: DocumentCategoryDto[]; onEdit: (correction: boolean) => void; onMutate: (key: string, action: () => Promise<unknown>, message: string) => Promise<boolean>; }) {
  const reconciliation = statement.reconciliation;
  return <section className="statement-detail record-panel">
    <header><div><small>{statement.millName} · {statement.periodStart} to {statement.periodEnd}</small><h2>{statement.statementReference}</h2></div><div className="mill-row-actions">{statement.status === 'Draft' && <><button className="secondary" onClick={() => onEdit(false)}>Edit</button><button disabled={pending !== '' || statement.evidence.length === 0} onClick={() => onMutate(`record-${statement.id}`, () => api.recordGrowerStatement(statement.id, new RecordMillRecordRequest({ expectedVersion: statement.version, idempotencyKey: newFinanceKey('statement-record') })), 'Statement recorded; reconciliation is server-derived.')}>Record statement</button></>}{statement.status === 'Recorded' && role === 'Grower' && <button className="secondary" onClick={() => onEdit(true)}>Correct</button>}<EvidenceUploadButton label={statement.evidence.length ? 'Add evidence' : 'Upload original'} disabled={pending !== ''} categories={categories} onFile={(file, categoryId) => onMutate(`statement-evidence-${statement.id}`, async () => api.uploadGrowerStatementEvidence(statement.id, await evidenceRequest(file, categoryId)), 'Statement evidence uploaded privately.')} /></div></header>
    <div className="reconciliation-strip"><span><small>Statement</small><strong>{tonnes(statement.totalTonnes)}</strong></span><span><small>Matched tickets</small><strong>{tonnes(reconciliation.matchedTicketTonnes)}</strong></span><span><small>Variance</small><strong>{tonnes(reconciliation.tonnesVariance)}</strong></span><span><small>Statement amount</small><strong>{usd(statement.totalAmountUsd)}</strong></span><span><small>Matched amount</small><strong>{amountReconciliation(reconciliation.matchedAmountUsd, reconciliation.amountStatus, usd)}</strong></span></div>
    <div className="statement-status-line"><em className={`status-pill status-${reconciliation.status.toLowerCase()}`}>{financeLabel(reconciliation.status)}</em>{matchWarning(reconciliation.hasCrossStatementTicketReuse) && <strong className="variance-warning">{matchWarning(true)}</strong>}</div>
    <div className="evidence-list">{statement.evidence.map((evidence) => <a key={evidence.id} href={`/api/finance/mill-records/evidence/${evidence.id}`}><FileDown size={14} /> {evidence.originalFileName}</a>)}</div>
    <div className="match-ledger"><h3>Matched tickets</h3>{reconciliation.matches.length ? reconciliation.matches.map((match) => <article key={match.id}><span><strong>{match.ticketReference}</strong><small>{match.ticketDate} · {match.ticketNetTonnes} t ticket net</small></span><b>{match.matchedTonnes} t{match.matchedAmountUsd == null ? '' : ` · ${usd(match.matchedAmountUsd)}`}</b><em className={`status-pill status-${match.active ? 'active' : 'reversed'}`}>{match.active ? 'Active' : 'Reversed'}</em>{match.active && role === 'Grower' && <button className="text-action" onClick={() => { const reason = globalThis.prompt('Reason for reversing this match:')?.trim(); if (reason) onMutate(`reverse-${match.id}`, () => api.reverseStatementTicketMatch(statement.id, match.id, new ReverseStatementTicketMatchRequest({ reason, idempotencyKey: newFinanceKey('match-reversal') })), 'Match reversal appended; original match preserved.'); }}>Reverse</button>}</article>) : <p>No tickets matched yet.</p>}</div>
    {statement.status === 'Recorded' && <MatchForm statement={statement} candidates={candidates} pending={pending} onMutate={onMutate} />}
  </section>;
}

function MatchForm({ statement, candidates, pending, onMutate }: { statement: GrowerStatementDto; candidates: CandidateTicketDto[]; pending: string; onMutate: (key: string, action: () => Promise<unknown>, message: string) => Promise<boolean>; }) {
  const submit = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); const candidate = candidates.find((x) => x.id === data.get('ticketId')); if (!candidate) return; const tonnes = String(data.get('tonnes')).trim(); const amount = String(data.get('amount')).trim(); if (await onMutate('match-add', () => api.addStatementTicketMatch(statement.id, new AddStatementTicketMatchRequest({ weighbridgeTicketId: candidate.id, matchedTonnes: tonnes ? Number(tonnes) : undefined, matchedAmountUsd: amount ? Number(amount) : undefined, completesMatching: data.get('complete') === 'on', reason: String(data.get('reason')).trim() || undefined, idempotencyKey: newFinanceKey('statement-match'), expectedStatementVersion: statement.version, expectedTicketVersion: candidate.version })), 'Ticket matched; reconciliation totals refreshed.')) form.reset(); };
  return <form className="match-form" onSubmit={submit}><h3>Match another ticket</h3><label>Candidate ticket<select name="ticketId" required><option value="">Select recorded ticket</option>{candidates.map((ticket) => <option key={ticket.id} value={ticket.id}>{ticket.ticketReference} · {ticket.availableTonnes} t available{ticket.matchedToAnotherStatement ? ' · used elsewhere' : ''}</option>)}</select></label><label>Matched tonnes<input name="tonnes" type="number" min="0.001" step="0.001" placeholder="Defaults to available net" /></label><label>Matched amount USD<input name="amount" type="number" min="0" step="0.01" placeholder="Optional" /></label><label>Reason<input name="reason" maxLength={500} placeholder="Required for a partial match" /></label><label className="match-complete"><input name="complete" type="checkbox" /> Matching is complete</label><button disabled={pending !== '' || candidates.length === 0}><Link2 size={15} /> Add match</button></form>;
}

function TicketForm({ editor, mills, session, pending, onSaved }: { editor: TicketEditor; mills: MillDto[]; session: MillRecordsSessionDto | null; pending: string; onSaved: (action: () => Promise<unknown>, message: string) => Promise<void>; }) {
  const ticket = editor.ticket; const [fieldId, setFieldId] = useState(ticket?.fieldId ?? '');
  const field = session?.fields.find((x) => x.id === fieldId);
  const submit = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); const request = new TicketRequest({ millId: String(data.get('millId')), ticketReference: String(data.get('reference')).trim(), ticketDate: String(data.get('date')), grossTonnes: Number(data.get('gross')), tareTonnes: String(data.get('tare')).trim() ? Number(data.get('tare')) : undefined, netTonnes: Number(data.get('net')), fieldId: fieldId || undefined, cropCycleId: String(data.get('cycleId')).trim() || undefined, sourceReference: String(data.get('source')).trim() || undefined, notes: String(data.get('notes')).trim() || undefined, expectedVersion: ticket?.version ?? 0 }); if (!ticket) return onSaved(() => api.createWeighbridgeTicket(request), 'Ticket draft created.'); if (!editor.correction) return onSaved(() => api.updateWeighbridgeTicket(ticket.id, request), 'Ticket draft updated.'); const reason = String(data.get('correctionReason')).trim(); return onSaved(() => api.correctWeighbridgeTicket(ticket.id, new CorrectTicketRequest({ reason, idempotencyKey: newFinanceKey('ticket-correction'), replacement: request })), 'Recorded replacement ticket appended.'); };
  return <form className="mill-form" onSubmit={submit}>{editor.correction && <label className="is-wide">Correction reason<textarea name="correctionReason" required maxLength={500} /></label>}<label>Mill<select name="millId" defaultValue={ticket?.millId ?? ''} required><option value="">Select mill</option>{mills.map((mill) => <option key={mill.id} value={mill.id}>{mill.code} · {mill.name}</option>)}</select></label><label>Ticket reference<input name="reference" defaultValue={ticket?.ticketReference} maxLength={120} required /></label><label>Ticket date<DatePicker name="date" defaultValue={ticket?.ticketDate ?? today} required /></label><label>Gross tonnes<input name="gross" type="number" min="0" step="0.001" defaultValue={ticket?.grossTonnes} required /></label><label>Tare tonnes<input name="tare" type="number" min="0" step="0.001" defaultValue={ticket?.tareTonnes} /></label><label>Net tonnes<input name="net" type="number" min="0.001" step="0.001" defaultValue={ticket?.netTonnes} required /></label><label>Field<select value={fieldId} onChange={(event) => setFieldId(event.target.value)}><option value="">Not assigned</option>{session?.fields.map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label><label>Crop cycle<select name="cycleId" defaultValue={ticket?.cropCycleId ?? ''}><option value="">Not assigned</option>{field?.cropCycles.map((cycle) => <option key={cycle.id} value={cycle.id}>{cycle.variety} · {financeLabel(cycle.status)}</option>)}</select></label><label>Source reference<input name="source" maxLength={200} defaultValue={ticket?.sourceReference} /></label><label className="is-wide">Notes<textarea name="notes" maxLength={1000} defaultValue={ticket?.notes} /></label><footer className="form-actions"><span>Net tonnes remain source evidence and are never silently recalculated.</span><button disabled={pending !== ''}>{editor.correction ? 'Append correction' : 'Save draft'}</button></footer></form>;
}

function StatementForm({ editor, mills, pending, onSaved }: { editor: StatementEditor; mills: MillDto[]; pending: string; onSaved: (action: () => Promise<unknown>, message: string) => Promise<void>; }) {
  const statement = editor.statement; const submit = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); const request = new StatementRequest({ millId: String(data.get('millId')), statementReference: String(data.get('reference')).trim(), periodStart: String(data.get('start')), periodEnd: String(data.get('end')), totalTonnes: Number(data.get('tonnes')), totalAmountUsd: Number(data.get('amount')), notes: String(data.get('notes')).trim() || undefined, expectedVersion: statement?.version ?? 0 }); if (!statement) return onSaved(() => api.createGrowerStatement(request), 'Statement draft created; upload original evidence before recording.'); if (!editor.correction) return onSaved(() => api.updateGrowerStatement(statement.id, request), 'Statement draft updated.'); return onSaved(() => api.correctGrowerStatement(statement.id, new CorrectStatementRequest({ reason: String(data.get('correctionReason')).trim(), idempotencyKey: newFinanceKey('statement-correction'), replacement: request })), 'Replacement statement draft appended; upload its original evidence before recording.'); };
  return <form className="mill-form" onSubmit={submit}>{editor.correction && <label className="is-wide">Correction reason<textarea name="correctionReason" required maxLength={500} /></label>}<label>Mill<select name="millId" defaultValue={statement?.millId ?? ''} required><option value="">Select mill</option>{mills.map((mill) => <option key={mill.id} value={mill.id}>{mill.code} · {mill.name}</option>)}</select></label><label>Statement reference<input name="reference" defaultValue={statement?.statementReference} maxLength={120} required /></label><label>Period start<DatePicker name="start" defaultValue={statement?.periodStart ?? today} required /></label><label>Period end<DatePicker name="end" defaultValue={statement?.periodEnd ?? today} required /></label><label>Total tonnes<input name="tonnes" type="number" min="0" step="0.001" defaultValue={statement?.totalTonnes} required /></label><label>Total amount USD<input name="amount" type="number" min="0" step="0.01" defaultValue={statement?.totalAmountUsd} required /></label><label className="is-wide">Notes<textarea name="notes" maxLength={1000} defaultValue={statement?.notes} /></label><footer className="form-actions"><span>Statement USD is evidence metadata, not income or settlement.</span><button disabled={pending !== ''}>{editor.correction ? 'Create replacement draft' : 'Save draft'}</button></footer></form>;
}

function MillForm({ mill, pending, onSaved }: { mill: MillDto | null; pending: string; onSaved: (action: () => Promise<unknown>, message: string) => Promise<void>; }) { const submit = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); const request = new MillRequest({ code: String(data.get('code')).trim(), name: String(data.get('name')).trim(), location: String(data.get('location')).trim() || undefined, expectedVersion: mill?.version ?? 0 }); await onSaved(() => mill ? api.updateMill(mill.id, request) : api.createMill(request), mill ? 'Mill reference updated.' : 'Mill reference created.'); }; return <form className="mill-form" onSubmit={submit}><label>Code<input name="code" maxLength={40} defaultValue={mill?.code} required /></label><label>Name<input name="name" maxLength={160} defaultValue={mill?.name} required /></label><label className="is-wide">Location or description<textarea name="location" maxLength={500} defaultValue={mill?.location} /></label><footer className="form-actions"><span>Codes are normalized and unique within this farm.</span><button disabled={pending !== ''}>Save mill</button></footer></form>; }

function EvidenceUploadButton({ label, disabled, categories, onFile }: { label: string; disabled: boolean; categories: DocumentCategoryDto[]; onFile: (file: File, categoryId?: string) => void }) { const [categoryId, setCategoryId] = useState(''); return <span className="evidence-upload-group">{categories.length > 0 && <select aria-label="Document category" value={categoryId} onChange={(event) => setCategoryId(event.target.value)}><option value="">Unclassified</option>{categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}</select>}<label className="evidence-upload secondary"><FilePlus2 size={14} /> {label}<input type="file" accept="image/*,application/pdf,text/csv" disabled={disabled} onChange={(event) => { const file = event.target.files?.[0]; if (file) onFile(file, categoryId || undefined); event.target.value = ''; }} /></label></span>; }
async function evidenceRequest(file: File, categoryId?: string): Promise<EvidenceUploadRequest> { const dataUrl = await new Promise<string>((resolve, reject) => { const reader = new FileReader(); reader.onerror = () => reject(reader.error); reader.onload = () => resolve(String(reader.result)); reader.readAsDataURL(file); }); return new EvidenceUploadRequest({ fileName: file.name, contentType: file.type || 'application/octet-stream', contentBase64: dataUrl.slice(dataUrl.indexOf(',') + 1), documentCategoryId: categoryId }); }
function Editor({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) { const dialogRef = useDialogFocus<HTMLElement>(onClose); return <div className="dialog-backdrop" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}><section ref={dialogRef} className="mill-editor-dialog finance-dialog" role="dialog" aria-modal="true" aria-label={title}><header><div><Scale size={19} /><h2>{title}</h2></div><button className="icon-button" aria-label="Close" onClick={onClose}><X size={18} /></button></header>{children}</section></div>; }
function Empty({ label }: { label: string }) { return <div className="finance-empty"><Warehouse size={26} /><strong>{label}</strong><span>Adjust filters or capture a new record.</span></div>; }
function exportUrl(view: View, filters: Record<string, string>) { const path = view === 'statements' ? 'statements/export' : 'tickets/export'; const params = new URLSearchParams(Object.entries(filters).filter(([, value]) => value)); return `/api/finance/mill-records/${path}?${params}`; }
