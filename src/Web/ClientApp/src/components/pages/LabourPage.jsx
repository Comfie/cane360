import { useEffect, useMemo, useState } from 'react';
import { BadgeCheck, CalendarCheck, ClipboardList, Plus, ShieldCheck, UserRound, UsersRound, X } from 'lucide-react';
import { getApiError } from '../apiError';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { ActivityTypesClient } from '../../web-api-client';
import { DatePicker } from '../DatePicker';
import {
  confirmWork,
  createRate,
  createWorker,
  createWorkRecord,
  getAttendance,
  saveAttendance,
  verifyWork,
  workersClient,
  workRecordsClient,
} from '../labour/labourApi';
import { activitySelectionError, dateOnly, employmentTypes, evidenceAmount, harareToday, label, payBases } from '../labour/labourView';

export function LabourPage() {
  const activityTypesClient = useMemo(() => new ActivityTypesClient(), []);
  const [tab, setTab] = useState('workers');
  const [date, setDate] = useState(harareToday());
  const [workers, setWorkers] = useState(/** @type {import('../../web-api-client').WorkerListItemDto[]} */ ([]));
  const [attendance, setAttendance] = useState(/** @type {import('../../web-api-client').AttendanceRegisterDto | null} */ (null));
  const [records, setRecords] = useState(/** @type {import('../../web-api-client').WorkRecordDto[]} */ ([]));
  const [references, setReferences] = useState(/** @type {import('../../web-api-client').LabourReferenceDataDto | null} */ (null));
  const [activityTypes, setActivityTypes] = useState(/** @type {import('../../web-api-client').ActivityTypeDto[]} */ ([]));
  const [selectedWorker, setSelectedWorker] = useState(/** @type {import('../../web-api-client').WorkerDetailsDto | null} */ (null));
  const [showWorker, setShowWorker] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const reloadWorkers = async () => setWorkers(await workersClient.workersAll());
  /** @param {string} [nextDate] */
  const reloadDate = async (nextDate = date) => {
    const [register, work, referenceData] = await Promise.all([
      getAttendance(nextDate),
      workRecordsClient.workRecordsAll(dateOnly(nextDate), undefined, undefined),
      workRecordsClient.referenceData(dateOnly(nextDate)),
    ]);
    setAttendance(register); setRecords(work); setReferences(referenceData);
  };

  useEffect(() => {
    let current = true;
    const initialDate = harareToday();
    Promise.all([workersClient.workersAll(), getAttendance(initialDate), workRecordsClient.workRecordsAll(dateOnly(initialDate), undefined, undefined), workRecordsClient.referenceData(dateOnly(initialDate)), activityTypesClient.activityTypesAll()])
      .then(([workerResult, register, work, referenceData, typeResult]) => {
        if (!current) return;
        setWorkers(workerResult); setAttendance(register); setRecords(work); setReferences(referenceData); setActivityTypes(typeResult);
      })
      .catch((requestError) => { if (current) setError(getApiError(requestError)); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, [activityTypesClient]); // Date changes are explicit so unsaved register choices are not discarded.

  /** @param {string} nextDate */
  const changeDate = async (nextDate) => {
    setDate(nextDate); setError('');
    try { await reloadDate(nextDate); } catch (requestError) { setError(getApiError(requestError)); }
  };

  /** @param {string} workerId */
  const openWorker = async (workerId) => {
    try { setSelectedWorker(await workersClient.workersGET(workerId)); }
    catch (requestError) { setError(getApiError(requestError)); }
  };

  if (loading) return <LoadingState label="Loading the labour ledger" />;

  return <div className="page-stack labour-page">
    <PageHeader eyebrow="Payroll-ready evidence" title="Labour" description="Register workers, allocate daily attendance, and confirm traceable work evidence—without calculating payroll.">
      <button type="button" className="primary-action" onClick={() => setShowWorker(true)}><Plus size={17} /> Register worker</button>
    </PageHeader>
    <ValidationError message={error} />
    <section className="labour-toolbar record-panel">
      <nav className="labour-tabs" aria-label="Labour sections">
        <button type="button" aria-current={tab === 'workers'} onClick={() => setTab('workers')}><UsersRound size={16} /> Workers <span>{workers.length}</span></button>
        <button type="button" aria-current={tab === 'attendance'} onClick={() => setTab('attendance')}><CalendarCheck size={16} /> Attendance</button>
        <button type="button" aria-current={tab === 'evidence'} onClick={() => setTab('evidence')}><ClipboardList size={16} /> Work evidence <span>{records.length}</span></button>
      </nav>
      {tab !== 'workers' && <label className="ledger-date">Work date<DatePicker value={date} max={harareToday()} onChange={changeDate} /></label>}
    </section>

    {tab === 'workers' && <WorkerRegister workers={workers} onOpen={openWorker} />}
    {tab === 'attendance' && attendance && <AttendanceLedger register={attendance} onSaved={(result) => setAttendance(result)} onError={setError} />}
    {tab === 'evidence' && attendance && references && <EvidenceLedger date={date} register={attendance} references={references} records={records} onChanged={reloadDate} onError={setError} />}

    {showWorker && <LabourDialog title="Register worker" onClose={() => setShowWorker(false)}><WorkerForm onSaved={async (details) => { setShowWorker(false); setSelectedWorker(details); await reloadWorkers(); }} onError={setError} /></LabourDialog>}
    {selectedWorker && <LabourDialog title={selectedWorker.worker.displayName} onClose={() => setSelectedWorker(null)}><WorkerDetails details={selectedWorker} activityTypes={activityTypes} onChanged={setSelectedWorker} onError={setError} /></LabourDialog>}
  </div>;
}

/** @param {{workers: import('../../web-api-client').WorkerListItemDto[], onOpen: (workerId: string) => void}} props */
function WorkerRegister({ workers, onOpen }) {
  if (!workers.length) return <section className="record-panel labour-empty"><UsersRound size={30} /><h2>No workers registered</h2><p>Add the first worker. National IDs stay encrypted and masked in this register.</p></section>;
  return <section className="worker-register record-panel" aria-label="Worker register">
    <div className="ledger-heading"><span>Worker</span><span>Employment</span><span>National ID</span><span>Status</span></div>
    {workers.map((worker) => <button key={worker.id} type="button" className="worker-row" onClick={() => onOpen(worker.id)}>
      <span className="worker-identity"><i><UserRound size={16} /></i><span><strong>{worker.displayName}</strong><small>{worker.phone || 'No phone recorded'}</small></span></span>
      <span>{label(worker.employmentType)}<small>From {formatDate(worker.activeFrom)}</small></span>
      <span className="masked-id">{worker.nationalIdMask}<small>Protected value</small></span>
      <em className={`status-pill status-${worker.status.toLowerCase()}`}>{worker.status}</em>
    </button>)}
  </section>;
}

/** @param {{onSaved: (details: import('../../web-api-client').WorkerDetailsDto) => void, onError: (message: string) => void}} props */
function WorkerForm({ onSaved, onError }) {
  const [saving, setSaving] = useState(false);
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setSaving(true); onError('');
    try { onSaved(await createWorker({ displayName: String(data.get('displayName')), phone: String(data.get('phone')) || undefined, employmentType: String(data.get('employmentType')), activeFrom: String(data.get('activeFrom')), nationalId: String(data.get('nationalId')) })); }
    catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <form className="labour-form" onSubmit={save}><div className="form-grid">
    <label>Worker name<input name="displayName" maxLength={120} autoComplete="name" required /></label>
    <label>Phone<input name="phone" maxLength={30} autoComplete="tel" /></label>
    <label>Employment type<select name="employmentType">{employmentTypes.map((type) => <option key={type} value={type}>{label(type)}</option>)}</select></label>
    <label>Active from<DatePicker name="activeFrom" defaultValue={harareToday()} required /></label>
    <label className="is-wide">National ID<input name="nationalId" type="password" autoComplete="off" maxLength={80} required /><small>Encrypted on submission. Only a safe mask is shown afterward.</small></label>
  </div><footer className="form-actions"><span className="security-note"><ShieldCheck size={15} /> Encrypted · farm-scoped duplicate check</span><button disabled={saving}>{saving ? 'Protecting…' : 'Register worker'}</button></footer></form>;
}

/** @param {{details: import('../../web-api-client').WorkerDetailsDto, activityTypes: import('../../web-api-client').ActivityTypeDto[], onChanged: (details: import('../../web-api-client').WorkerDetailsDto) => void, onError: (message: string) => void}} props */
function WorkerDetails({ details, activityTypes, onChanged, onError }) {
  const worker = details.worker; const [saving, setSaving] = useState(false); const [rateBasis, setRateBasis] = useState('Daily');
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveRate = async (event) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); const basis = String(data.get('basis')); setSaving(true); onError('');
    try { onChanged(await createRate(worker.id, { basis, activityTypeId: ['Hectare', 'StandardLine'].includes(basis) ? String(data.get('activityTypeId')) : undefined, rateUsd: Number(data.get('rateUsd')), effectiveFrom: String(data.get('effectiveFrom')), effectiveTo: String(data.get('effectiveTo')) || undefined })); form.reset(); }
    catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <div className="worker-details">
    <div className="overview-facts"><span><small>Employment</small><strong>{label(worker.employmentType)}</strong></span><span><small>National ID</small><strong className="masked-id">{worker.nationalIdMask}</strong></span><span><small>Active from</small><strong>{formatDate(worker.activeFrom)}</strong></span><span><small>Status</small><strong>{worker.status}</strong></span></div>
    <section><div className="section-heading"><div><span className="eyebrow">Effective dated</span><h3>Pay rates</h3></div></div>
      <div className="rate-list">{details.rates.length ? details.rates.map((rate) => <article key={rate.id}><strong>${rate.rateUsd.toFixed(4)} / {label(rate.basis)}</strong><span>{rate.activityTypeName || 'Worker-wide'} · {formatDate(rate.effectiveFrom)} → {rate.effectiveTo ? formatDate(rate.effectiveTo) : 'Open'}</span></article>) : <p>No rates configured.</p>}</div>
    </section>
    {worker.status === 'Active' && <form className="subrecord-form" onSubmit={saveRate}><h3>Add rate period</h3><div className="form-grid"><label>Pay basis<select name="basis" value={rateBasis} onChange={(event) => setRateBasis(event.target.value)}>{payBases.map((basis) => <option key={basis}>{label(basis)}</option>)}</select></label><label>Activity type <small>Piece rates only</small><select name="activityTypeId" required={['Hectare', 'StandardLine'].includes(rateBasis)} disabled={!['Hectare', 'StandardLine'].includes(rateBasis)} defaultValue=""><option value="">{['Hectare', 'StandardLine'].includes(rateBasis) ? 'Select activity type' : 'Worker-wide'}</option>{activityTypes.filter((type) => type.status === 'Active' && (rateBasis !== 'Hectare' || type.quantityBasis === 'Hectares') && (rateBasis !== 'StandardLine' || type.quantityBasis === 'StandardLines')).map((type) => <option key={type.id} value={type.id}>{type.name} · {label(type.quantityBasis)}</option>)}</select></label><label>USD rate<input name="rateUsd" type="number" min="0.0001" step="0.0001" required /></label><label>Effective from<DatePicker name="effectiveFrom" defaultValue={harareToday()} required /></label><label>Effective to <small>Optional</small><DatePicker name="effectiveTo" /></label></div><button disabled={saving}>{saving ? 'Adding…' : 'Add rate'}</button></form>}
  </div>;
}

/** @param {{register: import('../../web-api-client').AttendanceRegisterDto, onSaved: (result: import('../../web-api-client').AttendanceRegisterDto) => void, onError: (message: string) => void}} props */
function AttendanceLedger({ register, onSaved, onError }) {
  const [entries, setEntries] = useState(() => /** @type {Record<string, {status: string, fieldId: string, expectedVersion?: number}>} */ (Object.fromEntries(register.rows.map((row) => [row.workerId, { status: row.status || '', fieldId: row.fieldId || '', expectedVersion: row.version }]))));
  const [saving, setSaving] = useState(false);
  useEffect(() => setEntries(Object.fromEntries(register.rows.map((row) => [row.workerId, { status: row.status || '', fieldId: row.fieldId || '', expectedVersion: row.version }]))), [register]);
  /** @param {string} workerId @param {Partial<{status: string, fieldId: string, expectedVersion?: number}>} patch */
  const setEntry = (workerId, patch) => setEntries((current) => ({ ...current, [workerId]: { ...current[workerId], ...patch } }));
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const submit = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); const selected = Object.entries(entries).filter(([, value]) => value.status).map(([workerId, value]) => ({ workerId, status: value.status, fieldId: value.status === 'Present' ? value.fieldId : undefined, expectedVersion: value.expectedVersion }));
    setSaving(true); onError(''); try { onSaved(await saveAttendance(formatIsoDate(register.workDate), String(data.get('lateReason')) || undefined, selected)); } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <form className="attendance-ledger record-panel" onSubmit={submit}>
    <header><div><span className="eyebrow">Daily register</span><h2>{formatDate(register.workDate)}</h2></div><span>{register.rows.filter((row) => row.status === 'Present').length} present · {register.rows.filter((row) => row.status === 'Absent').length} absent</span></header>
    <div className="attendance-grid"><div className="ledger-heading"><span>Worker</span><span>Present / absent</span><span>One field allocation</span></div>{register.rows.map((row) => { const value = entries[row.workerId] || {}; return <div className="attendance-row" key={row.workerId}><span><strong>{row.workerName}</strong><small>{label(row.employmentType)}</small></span><div className="attendance-choice"><button type="button" aria-pressed={value.status === 'Present'} onClick={() => setEntry(row.workerId, { status: 'Present' })}>Present</button><button type="button" aria-pressed={value.status === 'Absent'} onClick={() => setEntry(row.workerId, { status: 'Absent', fieldId: '' })}>Absent</button></div><select aria-label={`Field for ${row.workerName}`} disabled={value.status !== 'Present'} required={value.status === 'Present'} value={value.fieldId || ''} onChange={(event) => setEntry(row.workerId, { fieldId: event.target.value })}><option value="">Select field</option>{register.fields.map((field) => <option key={field.id} value={field.id}>{field.code} · {field.name}</option>)}</select></div>; })}</div>
    <footer><label>Late-entry reason <small>Required when entered more than two calendar days late</small><input name="lateReason" maxLength={500} /></label><button disabled={saving || !Object.values(entries).some((entry) => entry.status)}>{saving ? 'Saving…' : 'Save attendance'}</button></footer>
  </form>;
}

/** @param {{date: string, register: import('../../web-api-client').AttendanceRegisterDto, references: import('../../web-api-client').LabourReferenceDataDto, records: import('../../web-api-client').WorkRecordDto[], onChanged: () => Promise<void>, onError: (message: string) => void}} props */
function EvidenceLedger({ date, register, references, records, onChanged, onError }) {
  const present = register.rows.filter((row) => row.status === 'Present'); const [saving, setSaving] = useState(false);
  const [basis, setBasis] = useState('Daily'); const [workerId, setWorkerId] = useState(''); const worker = present.find((row) => row.workerId === workerId);
  const activities = references.activities.filter((activity) => !worker?.fieldId || activity.fieldId === worker.fieldId);
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const submit = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); const activityIds = data.getAll('activityIds').map(String); const scopeType = String(data.get('scopeType') || ''); onError('');
    const selectionError = activitySelectionError(activityIds);
    if (selectionError) { onError(selectionError); return; }
    setSaving(true);
    const scope = ['Hectare', 'StandardLine'].includes(basis) ? { type: scopeType, startLine: scopeType === 'LineRange' ? Number(data.get('startLine')) : undefined, endLine: scopeType === 'LineRange' ? Number(data.get('endLine')) : undefined, sectionName: scopeType === 'NamedSection' ? String(data.get('sectionName')) : undefined } : undefined;
    try { await createWorkRecord({ workerId, workDate: date, payBasis: basis, activityIds, quantity: basis === 'Hectare' || (basis === 'StandardLine' && scopeType === 'NamedSection') ? Number(data.get('quantity')) : undefined, scope, lateEntryReason: String(data.get('lateReason')) || undefined }); await onChanged(); event.currentTarget.reset(); }
    catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  /** @param {import('../../web-api-client').WorkRecordDto} record @param {'verify' | 'confirm'} kind @param {string} [supervisorId] */
  const action = async (record, kind, supervisorId) => { setSaving(true); onError(''); try { if (kind === 'verify') await verifyWork(record.id, supervisorId || '', record.version); else await confirmWork(record.id, record.version); await onChanged(); } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); } };
  return <div className="evidence-layout">
    <form className="evidence-entry record-panel" onSubmit={submit}><header><div><span className="eyebrow">Event-date rate</span><h2>Record work</h2></div><BadgeCheck size={21} /></header><div className="form-grid"><label>Present worker<select required value={workerId} onChange={(event) => setWorkerId(event.target.value)}><option value="">Select worker</option>{present.map((row) => <option key={row.workerId} value={row.workerId}>{row.workerName} · {row.fieldName}</option>)}</select></label><label>Pay basis<select value={basis} onChange={(event) => setBasis(event.target.value)}>{payBases.map((item) => <option key={item}>{label(item)}</option>)}</select></label><fieldset className="is-wide activity-checks"><legend>Activities on allocated field</legend>{activities.length ? activities.map((activity) => <label className="activity-choice" key={activity.id}><input type="checkbox" name="activityIds" value={activity.id} required={false} /><span className="activity-choice-box" aria-hidden="true" /><span>{activity.name}</span><small>{label(activity.quantityBasis)}</small></label>) : <small>Select a present worker with actual activities on this date.</small>}</fieldset>{['Hectare', 'StandardLine'].includes(basis) && <><label>Scope type<select name="scopeType" defaultValue={basis === 'StandardLine' ? 'LineRange' : 'NamedSection'}><option value="NamedSection">Named section</option>{basis === 'StandardLine' && <option value="LineRange">Line range</option>}</select></label><label>Quantity <small>For hectares or named lines</small><input name="quantity" type="number" min="0.0001" step={basis === 'StandardLine' ? '1' : '0.0001'} /></label><label>Start line<input name="startLine" type="number" min="1" step="1" /></label><label>End line<input name="endLine" type="number" min="1" step="1" /></label><label className="is-wide">Section name<input name="sectionName" maxLength={120} /></label></>}<label className="is-wide">Late-entry reason<input name="lateReason" maxLength={500} /></label></div><button disabled={saving || !workerId || !activities.length}>{saving ? 'Recording…' : 'Record evidence'}</button></form>
    <section className="evidence-register record-panel"><header><div><span className="eyebrow">Verification chain</span><h2>Evidence for {formatDate(date)}</h2></div><span>{records.filter((record) => record.status === 'Confirmed').length} confirmed</span></header>{records.length ? records.map((record) => <article key={record.id}><div className="evidence-main"><strong>{record.workerName}</strong><span>{record.activityNames.join(', ')} · {label(record.payBasis)}</span><small>{record.fieldName} · {evidenceAmount(record.payBasis, record.calculatedAmountUsd, record.quantity)}</small></div><div className="proof-strip"><span className="is-done"><b>1</b><small>Entered</small></span><span className={record.verification ? 'is-done' : ''}><b>2</b><small>Supervisor</small></span><span className={record.status === 'Confirmed' ? 'is-done' : ''}><b>3</b><small>Manager</small></span></div>{record.status === 'Draft' && <div className="evidence-action"><select aria-label="Named supervisor" defaultValue=""><option value="">Supervisor…</option>{references.supervisors.map((supervisor) => <option key={supervisor.id} value={supervisor.id}>{supervisor.displayName}</option>)}</select><button type="button" disabled={saving} onClick={(event) => { const select = /** @type {HTMLSelectElement | null} */ (event.currentTarget.previousElementSibling); if (select?.value) action(record, 'verify', select.value); }}>Record attestation</button></div>}{record.status === 'SupervisorVerified' && <button type="button" className="primary-action" disabled={saving} onClick={() => action(record, 'confirm')}>Manager confirm</button>}{record.status === 'Confirmed' && <span className="confirmed-mark"><ShieldCheck size={16} /> Payroll-eligible evidence</span>}</article>) : <p className="empty-copy">No work evidence recorded for this date.</p>}</section>
  </div>;
}

/** @param {{title: string, onClose: () => void, children: import('react').ReactNode}} props */
function LabourDialog({ title, onClose, children }) { return <dialog open className="activity-dialog labour-dialog"><article><header><div><span className="eyebrow">Labour ledger</span><h2>{title}</h2></div><button type="button" className="dialog-close" onClick={onClose} aria-label="Close"><X /></button></header>{children}</article></dialog>; }
/** @param {Date | string} value */
function formatIsoDate(value) { return value instanceof Date ? value.toISOString().slice(0, 10) : String(value).slice(0, 10); }
/** @param {Date | string} value */
function formatDate(value) { const iso = formatIsoDate(value); return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${iso}T00:00:00`)); }
