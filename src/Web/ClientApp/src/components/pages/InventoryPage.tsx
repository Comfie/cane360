import { createElement, useEffect, useMemo, useState, type FormEvent, type ReactNode } from 'react';
import { Archive, Boxes, ClipboardCheck, PackagePlus, Plus, ReceiptText, RotateCcw, Scale, ShieldCheck, Truck, X } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type {
  InventoryItemDto,
  InventoryWorkspaceDto,
  StockAdjustmentDto,
  StockCountDto,
  StockMovementDto,
  StockReceiptDto,
  UnitOfMeasureDto,
} from '../../web-api-client';
import { getApiError } from '../apiError';
import { DatePicker } from '../DatePicker';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { InputControlsWorkspace } from '../inventory/InputControlsWorkspace';
import {
  createItem,
  createLot,
  createReceipt,
  createSupplier,
  createStockCount,
  createUnit,
  decideOpeningBalance,
  inventoryClient,
  postReceipt,
  reverseReceipt,
  getStockAdjustments,
  getStockCounts,
  reviewStockCount,
  startStockCount,
  submitOpeningBalance,
} from '../inventory/inventoryApi';
import {
  duplicateReceiptReference,
  harareToday,
  inventoryLabel,
  itemCategories,
  lotPolicies,
  quantity,
  usd,
} from '../inventory/inventoryView';

export function InventoryPage() {
  const [workspace, setWorkspace] = useState<InventoryWorkspaceDto | null>(null);
  const [tab, setTab] = useState<'stock' | 'receipts' | 'ledger' | 'catalogue' | 'inputs' | 'counts' | 'adjustments'>('stock');
  const [dialog, setDialog] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  const [counts, setCounts] = useState<StockCountDto[]>([]);
  const [adjustments, setAdjustments] = useState<StockAdjustmentDto[]>([]);

  const reload = async () => { const [nextWorkspace, nextCounts, nextAdjustments] = await Promise.all([inventoryClient.inventory(), getStockCounts(), getStockAdjustments()]); setWorkspace(nextWorkspace); setCounts(nextCounts); setAdjustments(nextAdjustments); };
  useEffect(() => {
    let current = true;
    Promise.all([inventoryClient.inventory(), getStockCounts(), getStockAdjustments()])
      .then(([result, nextCounts, nextAdjustments]) => { if (current) { setWorkspace(result); setCounts(nextCounts); setAdjustments(nextAdjustments); } })
      .catch((requestError) => { if (current) setError(getApiError(requestError)); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, []);

  const changed = async () => { setDialog(''); await reload(); };
  if (loading) return <LoadingState label="Opening the store daybook" />;
  if (!workspace) return <ValidationError message={error || 'Inventory could not be loaded.'} />;

  return <div className="page-stack inventory-page">
    <PageHeader eyebrow="Immutable store ledger" title="Inventory" description="Receive controlled inputs, preserve their lot and cost identity, and reproduce stock from posted movements.">
      <button type="button" className="primary-action" onClick={() => setDialog('receipt')}><PackagePlus size={17} /> New receipt</button>
    </PageHeader>
    <ValidationError message={error} />
    <section className="inventory-commandbar record-panel">
      <div className="store-identity"><span><Boxes size={17} /></span><div><small>Active store</small><strong>{workspace.storeCode} · {workspace.storeName}</strong></div></div>
      <nav className="inventory-tabs" aria-label="Inventory views">
        <button type="button" aria-current={tab === 'stock'} onClick={() => setTab('stock')}>Stock on hand <span>{workspace.stockOnHand.length}</span></button>
        <button type="button" aria-current={tab === 'receipts'} onClick={() => setTab('receipts')}>Receipts <span>{workspace.receipts.length}</span></button>
        <button type="button" aria-current={tab === 'ledger'} onClick={() => setTab('ledger')}>Movement ledger <span>{workspace.recentMovements.length}</span></button>
        <button type="button" aria-current={tab === 'catalogue'} onClick={() => setTab('catalogue')}>Catalogue</button>
        <button type="button" aria-current={tab === 'inputs'} onClick={() => setTab('inputs')}>Inputs</button>
        <button type="button" aria-current={tab === 'counts'} onClick={() => setTab('counts')}>Counts <span>{counts.filter((count) => count.status === 'InProgress').length}</span></button>
        <button type="button" aria-current={tab === 'adjustments'} onClick={() => setTab('adjustments')}>Adjustments <span>{adjustments.filter((adjustment) => adjustment.status === 'PendingGrowerApproval').length}</span></button>
      </nav>
    </section>

    {tab === 'stock' && <StockRegister workspace={workspace} />}
    {tab === 'receipts' && <ReceiptRegister receipts={workspace.receipts} onChanged={reload} onError={setError} />}
    {tab === 'ledger' && <MovementLedger movements={workspace.recentMovements} />}
    {tab === 'catalogue' && <Catalogue workspace={workspace} onOpen={setDialog} />}
    {tab === 'inputs' && <InputControlsWorkspace onError={setError} />}
    {tab === 'counts' && <CountRegister counts={counts} onChanged={reload} onError={setError} onOpen={() => setDialog('count')} />}
    {tab === 'adjustments' && <AdjustmentRegister adjustments={adjustments} />}

    {dialog === 'receipt' && <InventoryDialog title="Record stock receipt" onClose={() => setDialog('')}><ReceiptForm workspace={workspace} onSaved={changed} onError={setError} /></InventoryDialog>}
    {dialog === 'unit' && <InventoryDialog title="Add stock unit" onClose={() => setDialog('')}><UnitForm onSaved={changed} onError={setError} /></InventoryDialog>}
    {dialog === 'item' && <InventoryDialog title="Add inventory item" onClose={() => setDialog('')}><ItemForm units={workspace.units} onSaved={changed} onError={setError} /></InventoryDialog>}
    {dialog === 'supplier' && <InventoryDialog title="Add supplier" onClose={() => setDialog('')}><SupplierForm onSaved={changed} onError={setError} /></InventoryDialog>}
    {dialog === 'lot' && <InventoryDialog title="Add item lot" onClose={() => setDialog('')}><LotForm items={workspace.items} onSaved={changed} onError={setError} /></InventoryDialog>}
    {dialog === 'count' && <InventoryDialog title="Start a full-store count" onClose={() => setDialog('')}><CountForm onSaved={changed} onError={setError} /></InventoryDialog>}
  </div>;
}

function CountRegister({ counts, onChanged, onError, onOpen }: { counts: StockCountDto[]; onChanged: () => Promise<void>; onError: (message: string) => void; onOpen: () => void }) {
  const [working, setWorking] = useState('');
  const action = async (count: StockCountDto, kind: 'start' | 'review') => { setWorking(count.id); onError(''); try { if (kind === 'start') await startStockCount(count.id, count.version); if (kind === 'review') await reviewStockCount(count.id, count.version); await onChanged(); } catch (requestError) { onError(getApiError(requestError)); } finally { setWorking(''); } };
  return <section className="count-register record-panel"><header className="ledger-title"><div><span className="eyebrow">Physical evidence</span><h2>Full-store counts</h2></div><button className="primary-action" onClick={onOpen}><ClipboardCheck size={16} /> New count</button></header><p className="count-warning">Starting a count freezes receipt, issue, return, reversal and adjustment postings until review or cancellation.</p>{counts.length ? counts.map((count) => <article className="count-row" key={count.id}><span><strong>{count.status} · cut-off {count.cutoffPostingSequence ?? 'not started'}</strong><small>{count.countingPersons} · {count.lines.filter((line) => line.countedQuantity != null).length}/{count.lines.length} counted</small></span><b>{count.lines.reduce((total, line) => total + Math.abs(line.varianceQuantity), 0)} variance</b><span className="row-actions">{count.status === 'Draft' && <button disabled={working === count.id} onClick={() => action(count, 'start')}>Start & freeze</button>}{count.status === 'InProgress' && <button disabled={working === count.id} onClick={() => action(count, 'review')}>Review & release</button>}</span></article>) : <InventoryEmpty icon={ClipboardCheck} title="No physical counts" copy="Create a count when the Store can pause postings." />}</section>;
}

function CountForm({ onSaved, onError }: { onSaved: () => Promise<void>; onError: (message: string) => void }) {
  const [saving, setSaving] = useState(false);
  const save = async (event: FormEvent<HTMLFormElement>) => { event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); onError(''); try { await createStockCount({ eventDate: harareToday(), notes: String(data.get('notes') || ''), countingPersons: String(data.get('countingPersons')) }); await onSaved(); } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); } };
  return <form className="inventory-form" onSubmit={save}><p className="context-note">This is a full-store count. Stock is not changed by count entry; non-zero variances require Grower-approved adjustments.</p><div className="form-grid"><label>Named counting persons<input name="countingPersons" maxLength={1000} required /></label><label className="is-wide">Count notes<input name="notes" maxLength={1000} /></label></div><footer className="form-actions"><span>Starting later will capture a fixed ledger cut-off.</span><button disabled={saving}>Create draft count</button></footer></form>;
}

function AdjustmentRegister({ adjustments }: { adjustments: StockAdjustmentDto[] }) { return <section className="count-register record-panel"><header className="ledger-title"><div><span className="eyebrow">Grower control</span><h2>Store adjustments</h2></div></header>{adjustments.length ? adjustments.map((adjustment) => <article className="count-row" key={adjustment.id}><span><strong>{adjustment.itemCode} · {adjustment.adjustmentType}</strong><small>{adjustment.reason} · {adjustment.status}</small></span><b>{adjustment.signedQuantity > 0 ? '+' : ''}{quantity(adjustment.signedQuantity, adjustment.unitCode)}</b><strong>{usd(adjustment.signedValueUsdSnapshot || 0)}</strong></article>) : <InventoryEmpty icon={ShieldCheck} title="No adjustment decisions" copy="Draft write-offs and discoveries require exact-version Grower approval." />}</section>; }

function StockRegister({ workspace }: { workspace: InventoryWorkspaceDto }) {
  const totalValue = workspace.stockOnHand.reduce((total, row) => total + row.stockValueUsd, 0);
  const low = workspace.stockOnHand.filter((row) => row.reorderLevel != null && row.quantity <= row.reorderLevel);
  return <div className="inventory-register-layout">
    <section className="stock-register record-panel">
      <header className="ledger-title"><div><span className="eyebrow">Movement-derived</span><h2>Stock on hand</h2></div><strong>{usd(totalValue)}</strong></header>
      <div className="inventory-ledger-head"><span>Item / lot</span><span>Quantity</span><span>Average cost</span><span>Stock value</span></div>
      {workspace.stockOnHand.length ? workspace.stockOnHand.map((row) => <article key={row.stockPositionId} className="inventory-stock-row">
        <span><strong>{row.itemCode} · {row.itemName}</strong><small>{row.lotCode ? `Lot ${row.lotCode}` : 'Unbatched stock'}</small></span>
        <b>{quantity(row.quantity, row.unitCode)}</b>
        <span>{usd(row.weightedAverageUnitCostUsd)}<small>posting-order WMA</small></span>
        <strong>{usd(row.stockValueUsd)}</strong>
      </article>) : <InventoryEmpty icon={Boxes} title="No stock positions yet" copy="Add an item, then post an opening balance or purchase receipt." />}
    </section>
    <aside className="inventory-control-rail record-panel">
      <span className="eyebrow">Control summary</span><h2>{low.length ? `${low.length} reorder alert${low.length === 1 ? '' : 's'}` : 'Stock reconciles'}</h2>
      <p>Balances are calculated from immutable posted movements. Draft receipts do not affect this total.</p>
      <div><Scale size={18} /><span><strong>{workspace.recentMovements.length}</strong><small>ledger entries shown</small></span></div>
      <div><ShieldCheck size={18} /><span><strong>Item stock units</strong><small>no implicit conversion</small></span></div>
    </aside>
  </div>;
}

function ReceiptRegister({ receipts, onChanged, onError }: { receipts: StockReceiptDto[]; onChanged: () => Promise<void>; onError: (message: string) => void }) {
  const [working, setWorking] = useState('');
  const run = async (receipt: StockReceiptDto, action: 'submit' | 'approve' | 'reject' | 'post' | 'reverse') => {
    setWorking(receipt.id); onError('');
    try {
      if (action === 'submit') await submitOpeningBalance(receipt.id, receipt.version);
      if (action === 'approve') await decideOpeningBalance(receipt.id, receipt.version, 'Approved', undefined);
      if (action === 'reject') {
        const reason = globalThis.prompt('Reason for rejecting this opening balance:')?.trim();
        if (!reason) return;
        await decideOpeningBalance(receipt.id, receipt.version, 'Rejected', reason);
      }
      if (action === 'post') await postReceipt(receipt.id, receipt.version);
      if (action === 'reverse') {
        const reason = globalThis.prompt('Grower-authorised reversal reason:')?.trim();
        if (!reason) return;
        await reverseReceipt(receipt.id, receipt.version, reason);
      }
      await onChanged();
    } catch (requestError) { onError(getApiError(requestError)); }
    finally { setWorking(''); }
  };
  return <section className="receipt-register record-panel">
    <header className="ledger-title"><div><span className="eyebrow">Posting queue</span><h2>Receipts and opening balances</h2></div><small>Issue is not consumption · Phase 5A receipt controls only</small></header>
    <div className="receipt-ledger-head"><span>Reference</span><span>Event date</span><span>Supplier / source</span><span>Value</span><span>Status / action</span></div>
    {receipts.length ? receipts.map((receipt) => <article className="receipt-row" key={receipt.id}>
      <span><strong>{receipt.sourceReference}</strong><small>{inventoryLabel(receipt.receiptType)} · v{receipt.version}</small></span>
      <time>{formatDate(receipt.receiptDate)}</time>
      <span>{receipt.supplierName || receipt.reason || 'Opening source recorded'}<small>{receipt.lines.length} line{receipt.lines.length === 1 ? '' : 's'}</small></span>
      <b>{usd(receipt.totalValueUsd)}</b>
      <span className="receipt-state"><em className={`status-pill status-${receipt.status.toLowerCase()}`}>{inventoryLabel(receipt.status)}</em><ReceiptActions receipt={receipt} disabled={working === receipt.id} run={run} /></span>
    </article>) : <InventoryEmpty icon={ReceiptText} title="No receipts recorded" copy="Create a purchase receipt or authorised opening balance." />}
  </section>;
}

function ReceiptActions({ receipt, disabled, run }: { receipt: StockReceiptDto; disabled: boolean; run: (receipt: StockReceiptDto, action: 'submit' | 'approve' | 'reject' | 'post' | 'reverse') => void }) {
  if (receipt.receiptType === 'OpeningBalance' && receipt.status === 'Draft') return <button disabled={disabled} onClick={() => run(receipt, 'submit')}>Submit</button>;
  if (receipt.status === 'PendingApproval') return <span className="row-actions"><button disabled={disabled} onClick={() => run(receipt, 'approve')}>Approve</button><button className="secondary" disabled={disabled} onClick={() => run(receipt, 'reject')}>Reject</button></span>;
  if (receipt.status === 'Draft' || receipt.status === 'Approved') return <button disabled={disabled} onClick={() => run(receipt, 'post')}>Post receipt</button>;
  if (receipt.status === 'Posted') return <button className="text-action" disabled={disabled} onClick={() => run(receipt, 'reverse')}><RotateCcw size={14} /> Reverse</button>;
  return null;
}

function MovementLedger({ movements }: { movements: StockMovementDto[] }) {
  return <section className="movement-register record-panel">
    <header className="ledger-title"><div><span className="eyebrow">Append-only sequence</span><h2>Stock movement ledger</h2></div><small>Event time and posting time remain separate</small></header>
    <div className="movement-ledger-head"><span>Sequence / item</span><span>Event date</span><span>Movement</span><span>Quantity</span><span>Value</span></div>
    {movements.length ? movements.map((movement) => <article className="movement-row" key={movement.id}>
      <span><strong>#{movement.postingSequence} · {movement.itemCode}</strong><small>{movement.itemName}{movement.lotCode ? ` · lot ${movement.lotCode}` : ''}</small></span>
      <span><time>{formatDate(movement.eventDate)}</time><small>posted {formatTimestamp(movement.postedAt)}</small></span>
      <em className={movement.signedQuantity > 0 ? 'movement-in' : 'movement-out'}>{inventoryLabel(movement.movementType)}</em>
      <b>{movement.signedQuantity > 0 ? '+' : ''}{quantity(movement.signedQuantity, movement.unitCode)}</b>
      <strong>{movement.signedValueUsd > 0 ? '+' : ''}{usd(movement.signedValueUsd)}</strong>
    </article>) : <InventoryEmpty icon={Archive} title="The ledger is empty" copy="Posting a receipt creates positive immutable movement rows atomically." />}
  </section>;
}

function Catalogue({ workspace, onOpen }: { workspace: InventoryWorkspaceDto; onOpen: (dialog: string) => void }) {
  return <div className="catalogue-grid">
    <CatalogueSection icon={Boxes} title="Inventory items" action="Add item" onAdd={() => onOpen('item')}><div className="catalogue-list">{workspace.items.map((item) => <article key={item.id}><span><strong>{item.code} · {item.name}</strong><small>{inventoryLabel(item.category)} · stock unit {item.stockUnitCode}</small></span><em>{inventoryLabel(item.lotTrackingPolicy)} lots</em></article>)}</div></CatalogueSection>
    <CatalogueSection icon={Scale} title="Stock units" action="Add unit" onAdd={() => onOpen('unit')}><div className="catalogue-list">{workspace.units.map((unit) => <article key={unit.id}><span><strong>{unit.code} · {unit.name}</strong><small>{unit.dimension} · {unit.decimalPlaces} decimal places</small></span></article>)}</div></CatalogueSection>
    <CatalogueSection icon={Truck} title="Suppliers" action="Add supplier" onAdd={() => onOpen('supplier')}><div className="catalogue-list">{workspace.suppliers.map((supplier) => <article key={supplier.id}><span><strong>{supplier.code} · {supplier.name}</strong><small>{supplier.contact || 'No contact recorded'}</small></span></article>)}</div></CatalogueSection>
    <CatalogueSection icon={ClipboardCheck} title="Lots and expiry" action="Add lot" onAdd={() => onOpen('lot')}><div className="catalogue-list">{workspace.lots.map((lot) => { const item = workspace.items.find((candidate) => candidate.id === lot.inventoryItemId); return <article key={lot.id}><span><strong>{lot.code}</strong><small>{item?.name || 'Item'} · {lot.expiryDate ? `expires ${formatDate(lot.expiryDate)}` : 'no expiry date'}</small></span></article>; })}</div></CatalogueSection>
  </div>;
}

function CatalogueSection({ icon, title, action, onAdd, children }: { icon: LucideIcon; title: string; action: string; onAdd: () => void; children: ReactNode }) { return <section className="catalogue-section record-panel"><header><div>{createElement(icon, { size: 18 })}<h2>{title}</h2></div><button type="button" className="text-action" onClick={onAdd}><Plus size={15} /> {action}</button></header>{children}</section>; }

interface ReceiptLineState { key: number; inventoryItemId: string; inventoryLotId: string; quantity: string; unitCostUsd: string; }

function ReceiptForm({ workspace, onSaved, onError }: { workspace: InventoryWorkspaceDto; onSaved: () => void | Promise<void>; onError: (message: string) => void }) {
  const [type, setType] = useState<'Purchase' | 'OpeningBalance'>('Purchase'); const [saving, setSaving] = useState(false);
  const [lines, setLines] = useState<ReceiptLineState[]>([{ key: 1, inventoryItemId: '', inventoryLotId: '', quantity: '', unitCostUsd: '' }]);
  const [duplicate, setDuplicate] = useState('');
  const lotsByItem = useMemo(() => new Map(workspace.items.map((item) => [item.id, workspace.lots.filter((lot) => lot.inventoryItemId === item.id)])), [workspace]);
  const patchLine = (key: number, patch: Partial<Omit<ReceiptLineState, 'key'>>) => setLines((current) => current.map((line) => line.key === key ? { ...line, ...patch } : line));
  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setSaving(true); onError('');
    try {
      await createReceipt({ receiptType: type, supplierId: type === 'Purchase' ? String(data.get('supplierId')) : undefined, receiptDate: String(data.get('receiptDate')), receivedByPersonId: undefined, sourceReference: String(data.get('sourceReference')), reason: String(data.get('reason')) || undefined, lateEntryReason: String(data.get('lateEntryReason')) || undefined, lines: lines.map((line) => ({ inventoryItemId: line.inventoryItemId, inventoryLotId: line.inventoryLotId || undefined, quantity: Number(line.quantity), unitCostUsd: Number(line.unitCostUsd) })) });
      onSaved();
    } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <form className="inventory-form" onSubmit={submit}><div className="form-grid">
    <label>Receipt type<select value={type} onChange={(event) => { setType(event.target.value === 'OpeningBalance' ? 'OpeningBalance' : 'Purchase'); setDuplicate(''); }}><option value="Purchase">Purchase receipt</option><option value="OpeningBalance">Opening balance</option></select></label>
    {type === 'Purchase' && <label>Supplier<select name="supplierId" required defaultValue=""><option value="">Select supplier</option>{workspace.suppliers.filter((supplier) => supplier.status === 'Active').map((supplier) => <option key={supplier.id} value={supplier.id}>{supplier.code} · {supplier.name}</option>)}</select></label>}
    <label>Receipt date<DatePicker name="receiptDate" defaultValue={harareToday()} max={harareToday()} required /></label>
    <label>Source reference<input name="sourceReference" maxLength={120} required onBlur={(event) => { const form = event.currentTarget.form; if (!form) return; const supplierId = String(new FormData(form).get('supplierId') || ''); const found = duplicateReceiptReference(workspace.receipts, supplierId, event.currentTarget.value); setDuplicate(found ? `Reference already appears on receipt ${found.sourceReference} (${found.status}). Confirm before continuing.` : ''); }} />{duplicate && <small className="warning-copy">{duplicate}</small>}</label>
    {type === 'OpeningBalance' && <label className="is-wide">Opening-balance reason<input name="reason" maxLength={500} required /></label>}
    <label className="is-wide">Late-entry reason <small>Required after two calendar days</small><input name="lateEntryReason" maxLength={500} /></label>
  </div><fieldset className="receipt-lines"><legend>Stock-unit lines</legend>{lines.map((line, index) => { const item = workspace.items.find((candidate) => candidate.id === line.inventoryItemId); const lots = lotsByItem.get(line.inventoryItemId) || []; return <div className="receipt-line" key={line.key}>
    <label>Item<select required value={line.inventoryItemId} onChange={(event) => patchLine(line.key, { inventoryItemId: event.target.value, inventoryLotId: '' })}><option value="">Select item</option>{workspace.items.filter((candidate) => candidate.status === 'Active').map((candidate) => <option key={candidate.id} value={candidate.id}>{candidate.code} · {candidate.name}</option>)}</select></label>
    <label>Lot<select value={line.inventoryLotId} required={item?.lotTrackingPolicy === 'Required'} disabled={item?.lotTrackingPolicy === 'None'} onChange={(event) => patchLine(line.key, { inventoryLotId: event.target.value })}><option value="">{item?.lotTrackingPolicy === 'Required' ? 'Select required lot' : 'Unbatched'}</option>{lots.map((lot) => <option key={lot.id} value={lot.id}>{lot.code}{lot.expiryDate ? ` · ${formatDate(lot.expiryDate)}` : ''}</option>)}</select></label>
    <label>Quantity <small>{item?.stockUnitCode || 'stock unit'}</small><input type="number" min="0.000001" step="0.000001" required value={line.quantity} onChange={(event) => patchLine(line.key, { quantity: event.target.value })} /></label>
    <label>Unit cost USD<input type="number" min="0" step="0.000001" required value={line.unitCostUsd} onChange={(event) => patchLine(line.key, { unitCostUsd: event.target.value })} /></label>
    <button type="button" className="line-remove" aria-label={`Remove line ${index + 1}`} disabled={lines.length === 1} onClick={() => setLines((current) => current.filter((candidate) => candidate.key !== line.key))}><X size={16} /></button>
  </div>; })}<button type="button" className="text-action" onClick={() => setLines((current) => [...current, { key: Math.max(...current.map((line) => line.key)) + 1, inventoryItemId: '', inventoryLotId: '', quantity: '', unitCostUsd: '' }])}><Plus size={15} /> Add receipt line</button></fieldset><footer className="form-actions"><span>{type === 'OpeningBalance' ? 'Grower approval required before posting' : 'Posting creates positive ledger movements'}</span><button disabled={saving}>{saving ? 'Recording…' : 'Record draft receipt'}</button></footer></form>;
}

function UnitForm({ onSaved, onError }: { onSaved: () => void; onError: (message: string) => void }) { return <SimpleForm submit={(data) => createUnit({ code: value(data, 'code'), name: value(data, 'name'), dimension: value(data, 'dimension'), decimalPlaces: numberValue(data, 'decimalPlaces') })} onSaved={onSaved} onError={onError} action="Add unit"><label>Code<input name="code" maxLength={20} required /></label><label>Name<input name="name" maxLength={80} required /></label><label>Dimension<input name="dimension" maxLength={40} placeholder="Mass, Volume, Count" required /></label><label>Decimal places<input name="decimalPlaces" type="number" min="0" max="6" defaultValue="3" required /></label></SimpleForm>; }
function SupplierForm({ onSaved, onError }: { onSaved: () => void; onError: (message: string) => void }) { return <SimpleForm submit={(data) => createSupplier({ code: value(data, 'code'), name: value(data, 'name'), contact: optionalValue(data, 'contact') })} onSaved={onSaved} onError={onError} action="Add supplier"><label>Code<input name="code" maxLength={30} required /></label><label>Name<input name="name" maxLength={120} required /></label><label className="is-wide">Contact<input name="contact" maxLength={240} /></label></SimpleForm>; }
function ItemForm({ units, onSaved, onError }: { units: UnitOfMeasureDto[]; onSaved: () => void; onError: (message: string) => void }) { const [lotPolicy, setLotPolicy] = useState('Optional'); const [expiryPolicy, setExpiryPolicy] = useState('Optional'); return <SimpleForm submit={(data) => createItem({ code: value(data, 'code'), name: value(data, 'name'), category: value(data, 'category'), stockUnitId: value(data, 'stockUnitId'), reorderLevel: optionalNumberValue(data, 'reorderLevel'), lotTrackingPolicy: value(data, 'lotTrackingPolicy'), expiryPolicy: value(data, 'expiryPolicy') })} onSaved={onSaved} onError={onError} action="Add item"><label>Code<input name="code" maxLength={30} required /></label><label>Name<input name="name" maxLength={120} required /></label><label>Category<select name="category">{itemCategories.map((value) => <option key={value} value={value}>{inventoryLabel(value)}</option>)}</select></label><label>Stock unit<select name="stockUnitId" required defaultValue=""><option value="">Select unit</option>{units.filter((unit) => unit.status === 'Active').map((unit) => <option key={unit.id} value={unit.id}>{unit.code} · {unit.name}</option>)}</select></label><label>Reorder level<input name="reorderLevel" type="number" min="0" step="0.000001" /></label><label>Lot tracking<select name="lotTrackingPolicy" value={lotPolicy} onChange={(event) => { const value = event.target.value; setLotPolicy(value); if (value === 'None') setExpiryPolicy('None'); else if (expiryPolicy === 'None') setExpiryPolicy('Optional'); }}>{lotPolicies.map((value) => <option key={value}>{value}</option>)}</select></label><label>Expiry policy<select name="expiryPolicy" value={expiryPolicy} disabled={lotPolicy === 'None'} onChange={(event) => setExpiryPolicy(event.target.value)}>{lotPolicies.map((value) => <option key={value}>{value}</option>)}</select>{lotPolicy === 'None' && <input type="hidden" name="expiryPolicy" value="None" />}</label></SimpleForm>; }
function LotForm({ items, onSaved, onError }: { items: InventoryItemDto[]; onSaved: () => void; onError: (message: string) => void }) { return <SimpleForm submit={(data) => createLot({ inventoryItemId: value(data, 'inventoryItemId'), code: value(data, 'code'), expiryDate: optionalValue(data, 'expiryDate') })} onSaved={onSaved} onError={onError} action="Add lot"><label>Inventory item<select name="inventoryItemId" required defaultValue=""><option value="">Select lot-tracked item</option>{items.filter((item) => item.status === 'Active' && item.lotTrackingPolicy !== 'None').map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label><label>Lot / batch code<input name="code" maxLength={60} required /></label><label>Expiry date <small>As configured by item</small><DatePicker name="expiryDate" /></label></SimpleForm>; }

function SimpleForm({ submit, onSaved, onError, action, children }: { submit: (data: FormData) => Promise<unknown>; onSaved: () => void; onError: (message: string) => void; action: string; children: ReactNode }) {
  const [saving, setSaving] = useState(false);
  const save = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setSaving(true); onError('');
    try { await submit(new FormData(event.currentTarget)); onSaved(); }
    catch (requestError) { onError(getApiError(requestError)); }
    finally { setSaving(false); }
  };
  return <form className="inventory-form" onSubmit={save}><div className="form-grid">{children}</div><footer className="form-actions"><span>Archived reference data remains available in history.</span><button disabled={saving}>{saving ? 'Saving…' : action}</button></footer></form>;
}

function InventoryDialog({ title, onClose, children }: { title: string; onClose: () => void; children: ReactNode }) { return <dialog open className="activity-dialog inventory-dialog"><article><header><div><span className="eyebrow">Store daybook</span><h2>{title}</h2></div><button type="button" className="dialog-close" onClick={onClose} aria-label="Close"><X /></button></header>{children}</article></dialog>; }
function InventoryEmpty({ icon, title, copy }: { icon: LucideIcon; title: string; copy: string }) { return <div className="inventory-empty">{createElement(icon, { size: 26 })}<div><strong>{title}</strong><span>{copy}</span></div></div>; }
function formatDate(value: Date | string): string { const iso = value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10); return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${iso}T00:00:00`)); }
function formatTimestamp(value: Date | string): string { return new Intl.DateTimeFormat('en-ZW', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'Africa/Harare' }).format(new Date(value)); }

function value(data: FormData, key: string): string { return String(data.get(key) ?? ''); }
function optionalValue(data: FormData, key: string): string | undefined { const result = value(data, key); return result || undefined; }
function numberValue(data: FormData, key: string): number { return Number(value(data, key)); }
function optionalNumberValue(data: FormData, key: string): number | undefined { const result = value(data, key); return result ? Number(result) : undefined; }
