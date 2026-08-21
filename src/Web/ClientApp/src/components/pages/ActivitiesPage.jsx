import { useEffect, useMemo, useState } from 'react';
import { CalendarDays, ChevronLeft, ChevronRight, ClipboardCheck, List, Plus, Sheet, TriangleAlert, X } from 'lucide-react';
import { CreateActivityTypeRequest, FarmPersonnelClient } from '../../web-api-client';
import { DatePicker } from '../DatePicker';
import { getApiError, useFarmSetup } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import {
  activitiesClient,
  activityTypesClient,
  addSourceReference,
  createActivity,
  recordActual,
  transitionActivity,
} from '../activities/activityApi';
import { formatActivityStatus, groupActivitiesByDate, monthGridDates, orderedActions, quantityLabel } from '../activities/activityView';

const personnelClient = new FarmPersonnelClient();

export function ActivitiesPage() {
  const { setup, error: setupError, isLoading: setupLoading } = useFarmSetup();
  const [activities, setActivities] = useState(/** @type {import('../../web-api-client').ActivityListItemDto[]} */ ([]));
  const [types, setTypes] = useState(/** @type {import('../../web-api-client').ActivityTypeDto[]} */ ([]));
  const [personnel, setPersonnel] = useState(/** @type {import('../../web-api-client').PersonnelRegisterDto | null} */ (null));
  const [selected, setSelected] = useState(/** @type {import('../../web-api-client').ActivityDetailsDto | null} */ (null));
  const [view, setView] = useState('list');
  const [showCreate, setShowCreate] = useState(false);
  const [showTypeForm, setShowTypeForm] = useState(false);
  const [filters, setFilters] = useState({ field: '', type: '', status: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  const reload = async () => {
    try {
      const result = await activitiesClient.activitiesGET(
        undefined, undefined, undefined, undefined, undefined, undefined, 1, 100);
      setActivities(result.items);
    } catch (requestError) { setError(getApiError(requestError)); }
  };

  useEffect(() => {
    let current = true;
    Promise.all([activitiesClient.activitiesGET(undefined, undefined, undefined, undefined, undefined, undefined, 1, 100), activityTypesClient.activityTypesAll(), personnelClient.farmPersonnelGET()])
      .then(([activityResult, typeResult, personnelResult]) => {
        if (!current) return;
        setActivities(activityResult.items); setTypes(typeResult); setPersonnel(personnelResult);
      })
      .catch((requestError) => { if (current) setError(getApiError(requestError)); })
      .finally(() => { if (current) setLoading(false); });
    return () => { current = false; };
  }, []);

  const fields = setup?.farm?.fields ?? [];
  const supervisors = personnel?.persons.filter((person) =>
    person.status === 'Active' && person.roles.some((role) => role.role === 'Supervisor' && !role.effectiveTo)) ?? [];
  const activeTypes = types.filter((type) => type.status === 'Active');
  const visibleActivities = useMemo(() => activities.filter((activity) =>
    (!filters.field || activity.fieldId === filters.field) &&
    (!filters.type || activity.activityTypeId === filters.type) &&
    (!filters.status || activity.status === filters.status)), [activities, filters]);
  const grouped = useMemo(() => groupActivitiesByDate(visibleActivities), [visibleActivities]);

  if (setupLoading || loading) return <LoadingState label="Loading the field diary" />;
  if (!setup) return <ValidationError title="Activities unavailable" message={setupError || error} persistent />;

  /** @param {string} id */
  const openDetails = async (id) => {
    try { setSelected(await activitiesClient.activitiesGET2(id)); }
    catch (requestError) { setError(getApiError(requestError)); }
  };

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Field diary" title="Activities" description="Plan field work, capture what happened, and keep each verification step traceable.">
        <div className="page-actions"><button type="button" className="secondary-action" onClick={() => { setError(''); setShowTypeForm(true); }}>Activity types</button><button type="button" className="primary-action" onClick={() => { setError(''); setShowCreate(true); }}><Plus size={17} /> Record activity</button></div>
      </PageHeader>
      <ValidationError message={error} />

      <section className="activity-toolbar record-panel" aria-label="Activity filters">
        <div className="activity-filters">
          <label>Field<select value={filters.field} onChange={(event) => setFilters({ ...filters, field: event.target.value })}><option value="">All fields</option>{fields.map((field) => <option key={field.id} value={field.id}>{field.code} · {field.name}</option>)}</select></label>
          <label>Activity type<select value={filters.type} onChange={(event) => setFilters({ ...filters, type: event.target.value })}><option value="">All types</option>{types.map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
          <label>Status<select value={filters.status} onChange={(event) => setFilters({ ...filters, status: event.target.value })}><option value="">All statuses</option>{['Draft', 'Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled'].map((status) => <option key={status} value={status}>{formatActivityStatus(status)}</option>)}</select></label>
        </div>
        <div className="view-toggle" aria-label="View"><button type="button" aria-pressed={view === 'list'} onClick={() => setView('list')}><List size={16} /> List</button><button type="button" aria-pressed={view === 'calendar'} onClick={() => setView('calendar')}><CalendarDays size={16} /> Calendar</button></div>
      </section>

      {visibleActivities.length === 0 ? <section className="record-panel activity-empty"><ClipboardCheck size={28} /><h2>No activities match this view</h2><p>Record planned or unplanned work against an Active or Ready-for-harvest crop cycle.</p></section> : view === 'list' ? (
        <section className="activity-list" aria-label="Activity list">{visibleActivities.map((activity) => <ActivityRow key={activity.id} activity={activity} onOpen={openDetails} />)}</section>
      ) : <ActivityCalendar groups={grouped} onOpen={openDetails} />}

      <section className="unavailable-strip" aria-label="Evidence capabilities"><strong>Operational evidence</strong><span><Sheet size={15} /> Source references and confirmed labour evidence appear in the diary. Document upload, inventory, and cost posting remain deferred.</span></section>

      {showCreate && <ActivityDialog title="Record activity" onClose={() => setShowCreate(false)}>
        <CreateActivityForm fields={fields} types={activeTypes} supervisors={supervisors} onSaved={async (details) => { setShowCreate(false); setSelected(details); await reload(); }} onError={setError} />
      </ActivityDialog>}
      {showTypeForm && <ActivityDialog title="Activity types" onClose={() => setShowTypeForm(false)}>
        <ActivityTypeForm types={types} onSaved={(type) => setTypes((current) => [...current, type])} onError={setError} />
      </ActivityDialog>}
      {selected && <ActivityDialog title={selected.activity.activityTypeName} onClose={() => setSelected(null)}>
        <ActivityOverview details={selected} onChanged={async (details) => { setSelected(details); await reload(); }} onError={setError} />
      </ActivityDialog>}
    </div>
  );
}

/** @param {{ activity: import('../../web-api-client').ActivityListItemDto, onOpen: (id: string) => void }} props */
function ActivityRow({ activity, onOpen }) {
  return <button type="button" className="activity-row" onClick={() => onOpen(activity.id)}>
    <span className="activity-date"><small>{activity.actualAt ? 'Worked' : 'Planned'}</small><strong>{formatDate(activity.actualAt?.slice(0, 10) ?? activity.plannedDate)}</strong></span>
    <span className="activity-main"><span><strong>{activity.activityTypeName}</strong><em className={`status-pill status-${activity.status.toLowerCase()}`}>{formatActivityStatus(activity.status)}</em>{activity.isRetrospective && <em className="late-flag"><TriangleAlert size={13} /> {activity.entryDelayDays}d late</em>}</span><small>{activity.fieldCode} · {activity.supervisorName} · {activity.kind}</small></span>
    <span className="activity-coverage">{coverage(activity)}</span>
  </button>;
}

/** @param {{ groups: Record<string, import('../../web-api-client').ActivityListItemDto[]>, onOpen: (id: string) => void }} props */
function ActivityCalendar({ groups, onOpen }) {
  const datedKeys = Object.keys(groups).filter((date) => date !== 'Unscheduled').sort();
  const [cursor, setCursor] = useState(() => new Date(`${datedKeys[datedKeys.length - 1] ?? harareToday()}T00:00:00`));
  const dates = monthGridDates(cursor.getFullYear(), cursor.getMonth());
  const monthLabel = new Intl.DateTimeFormat('en-ZW', { month: 'long', year: 'numeric' }).format(cursor);
  /** @param {number} offset */
  const moveMonth = (offset) => setCursor((current) => new Date(current.getFullYear(), current.getMonth() + offset, 1));
  return <>
    <section className="desktop-month" aria-label={`Activity calendar for ${monthLabel}`}>
      <header className="activity-calendar-header"><h2>{monthLabel}</h2><div><button type="button" onClick={() => moveMonth(-1)} aria-label="Previous month"><ChevronLeft size={17} /></button><button type="button" onClick={() => moveMonth(1)} aria-label="Next month"><ChevronRight size={17} /></button></div></header>
      <div className="month-weekdays" aria-hidden="true">{['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'].map((day) => <span key={day}>{day}</span>)}</div>
      <div className="month-grid">{dates.map((date, index) => date ? <div className="month-day" key={date}><time dateTime={date}>{Number(date.slice(-2))}</time>{(groups[date] ?? []).map((activity) => <button type="button" key={activity.id} onClick={() => onOpen(activity.id)}><strong>{activity.activityTypeName}</strong><small>{activity.fieldCode} · {formatActivityStatus(activity.status)}</small></button>)}</div> : <div className="month-day is-outside" aria-hidden="true" key={`empty-${index}`} />)}</div>
      {groups.Unscheduled?.length > 0 && <div className="calendar-unscheduled"><strong>Unscheduled drafts</strong>{groups.Unscheduled.map((activity) => <button type="button" key={activity.id} onClick={() => onOpen(activity.id)}>{activity.activityTypeName} · {activity.fieldCode}</button>)}</div>}
    </section>
    <section className="diary-agenda mobile-agenda" aria-label="Activity agenda">{Object.entries(groups).map(([date, items]) => <div className="agenda-day" key={date}><time>{formatDate(date)}</time><div>{items.map((activity) => <ActivityRow key={activity.id} activity={activity} onOpen={onOpen} />)}</div></div>)}</section>
  </>;
}

/** @param {{ title: string, onClose: () => void, children: import('react').ReactNode }} props */
function ActivityDialog({ title, onClose, children }) {
  return <dialog open className="activity-dialog"><article><header><div><span className="eyebrow">Field diary</span><h2>{title}</h2></div><button type="button" className="dialog-close" onClick={onClose} aria-label="Close"><X /></button></header>{children}</article></dialog>;
}

/** @param {{ fields: import('../../web-api-client').FieldDto[], types: import('../../web-api-client').ActivityTypeDto[], supervisors: import('../../web-api-client').PersonDto[], onSaved: (details: import('../../web-api-client').ActivityDetailsDto) => void, onError: (message: string) => void }} props */
function CreateActivityForm({ fields, types, supervisors, onSaved, onError }) {
  const [fieldId, setFieldId] = useState('');
  const [kind, setKind] = useState('Planned');
  const [saving, setSaving] = useState(false);
  const field = fields.find((item) => item.id === fieldId);
  const validTypes = types.filter((type) => kind === 'Planned' ? type.supportsPlanned : type.supportsUnplanned);
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); onError('');
    try { onSaved(await createActivity({ fieldId, cropCycleId: field?.currentCropCycle?.id ?? '', activityTypeId: String(data.get('activityTypeId')), kind, plannedDate: kind === 'Planned' ? String(data.get('plannedDate')) : undefined, supervisorPersonId: String(data.get('supervisorPersonId')) })); }
    catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <form className="activity-form" onSubmit={save}><fieldset className="form-grid">
    <label>Work kind<select value={kind} onChange={(event) => setKind(event.target.value)}><option>Planned</option><option>Unplanned</option></select></label>
    <label>Field<select required value={fieldId} onChange={(event) => setFieldId(event.target.value)}><option value="">Select field</option>{fields.filter((item) => ['Active', 'ReadyForHarvest'].includes(item.currentCropCycle?.status ?? '')).map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label>
    <label>Activity type<select name="activityTypeId" required defaultValue=""><option value="">Select type</option>{validTypes.map((type) => <option key={type.id} value={type.id}>{type.name} · {type.quantityBasis}</option>)}</select></label>
    <label>Responsible supervisor<select name="supervisorPersonId" required defaultValue=""><option value="">Select supervisor</option>{supervisors.map((person) => <option key={person.id} value={person.id}>{person.displayName}</option>)}</select></label>
    {kind === 'Planned' && <label>Planned date<DatePicker name="plannedDate" defaultValue={harareToday()} required /></label>}
  </fieldset><p className="context-note">Unplanned work needs actual work details before it can move from Draft to Planned.</p><footer className="form-actions"><span /><button disabled={saving || !field?.currentCropCycle}>{saving ? 'Recording…' : 'Record activity'}</button></footer></form>;
}

/** @param {{ types: import('../../web-api-client').ActivityTypeDto[], onSaved: (type: import('../../web-api-client').ActivityTypeDto) => void, onError: (message: string) => void }} props */
function ActivityTypeForm({ types, onSaved, onError }) {
  const [saving, setSaving] = useState(false);
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setSaving(true); onError('');
    const supportsPlanned = data.get('supportsPlanned') === 'on';
    const supportsUnplanned = data.get('supportsUnplanned') === 'on';
    if (!supportsPlanned && !supportsUnplanned) {
      setSaving(false); onError('Select Planned, Unplanned, or both.'); return;
    }
    try {
      const type = await activityTypesClient.activityTypes(new CreateActivityTypeRequest({
        code: String(data.get('code')).trim(),
        name: String(data.get('name')).trim(),
        supportsPlanned,
        supportsUnplanned,
        quantityBasis: String(data.get('quantityBasis')),
      }));
      form.reset(); onSaved(type);
    } catch (requestError) { onError(getApiError(requestError)); } finally { setSaving(false); }
  };
  return <div className="activity-form"><form onSubmit={save}><div className="form-grid"><label>Code<input name="code" maxLength={24} pattern="[A-Za-z0-9][A-Za-z0-9_-]*" required /></label><label>Name<input name="name" maxLength={100} required /></label><label>Coverage basis<select name="quantityBasis"><option value="None">No quantity</option><option value="Hectares">Hectares</option><option value="StandardLines">Standard lines</option></select></label><div className="planning-modes"><label className="toggle-control"><input type="checkbox" name="supportsPlanned" /><span className="toggle-control-track" aria-hidden="true" /><span>Planned</span></label><label className="toggle-control"><input type="checkbox" name="supportsUnplanned" /><span className="toggle-control-track" aria-hidden="true" /><span>Unplanned</span></label></div></div><button disabled={saving}>{saving ? 'Adding…' : 'Add activity type'}</button></form><div className="type-register">{types.length === 0 ? <p>No activity types configured.</p> : types.map((type) => <span key={type.id}><strong>{type.code}</strong> {type.name}<small>{type.quantityBasis} · {type.status}</small></span>)}</div></div>;
}

/** @param {{ details: import('../../web-api-client').ActivityDetailsDto, onChanged: (details: import('../../web-api-client').ActivityDetailsDto) => void, onError: (message: string) => void }} props */
function ActivityOverview({ details, onChanged, onError }) {
  const activity = details.activity;
  const [saving, setSaving] = useState(false);
  const [actualAtValue, setActualAtValue] = useState(activity.actualAt?.slice(0, 16) || harareNow());
  /** @param {string} action @param {string | undefined} reason */
  const run = async (action, reason) => { setSaving(true); onError(''); try { onChanged(await transitionActivity(activity.id, action, activity.version, reason)); } catch (error) { onError(getApiError(error)); } finally { setSaving(false); } };
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveActual = async (event) => { event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); try { onChanged(await recordActual(activity.id, activity.version, String(data.get('actualAt')), activity.quantityBasis === 'None' ? undefined : Number(data.get('actualQuantity')), String(data.get('lateEntryReason')).trim() || undefined)); } catch (error) { onError(getApiError(error)); } finally { setSaving(false); } };
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveReference = async (event) => { event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setSaving(true); try { onChanged(await addSourceReference(activity.id, activity.version, String(data.get('reference')).trim(), String(data.get('capturedDate')))); form.reset(); } catch (error) { onError(getApiError(error)); } finally { setSaving(false); } };
  return <div className="activity-overview">
    <div className="overview-facts"><span><small>Status</small><strong>{formatActivityStatus(activity.status)}</strong></span><span><small>Field</small><strong>{activity.fieldCode} · {activity.fieldName}</strong></span><span><small>Supervisor</small><strong>{activity.supervisorName}</strong></span><span><small>Coverage</small><strong>{coverage(activity)}</strong></span></div>
    {activity.isRetrospective && <div className="late-callout"><TriangleAlert size={18} /><div><strong>Retrospective entry · {activity.entryDelayDays} calendar days</strong><span>{activity.lateEntryReason || 'Entered within the two-day reason-free window.'}</span></div></div>}
    {['Draft', 'Planned', 'InProgress'].includes(activity.status) && <form className="subrecord-form" onSubmit={saveActual}><h3>Actual work</h3><div className="form-grid"><label>When work happened<DatePicker name="actualAt" type="datetime-local" required value={actualAtValue} onChange={setActualAtValue} /></label>{activity.quantityBasis !== 'None' && <label>{quantityLabel(activity.quantityBasis)}<input name="actualQuantity" type="number" min="0.0001" step={activity.quantityBasis === 'StandardLines' ? '1' : '0.0001'} required defaultValue={activity.actualQuantity} /></label>}<label className="is-wide">Late-entry reason <small>Required after 2 days</small><textarea name="lateEntryReason" maxLength={500} defaultValue={activity.lateEntryReason} /></label></div><button disabled={saving}>Save actual work</button></form>}
    <section className="lifecycle-actions"><h3>Next action</h3>{orderedActions(details.allowedTransitions).map((action) => <button key={action} type="button" className={action === 'Cancelled' ? 'secondary outline' : 'secondary-action'} disabled={saving} onClick={() => run(action, action === 'Cancelled' ? window.prompt('Cancellation reason') || '' : undefined)}>{action === 'ManagerConfirmation' ? 'Supervisor verified' : formatActivityStatus(action)}</button>)}{Object.values(details.blockedTransitions).map((message) => <p className="context-note" key={message}>{message}</p>)}</section>
    {!['Closed', 'Cancelled'].includes(activity.status) && <form className="subrecord-form" onSubmit={saveReference}><h3>Source reference</h3><div className="form-grid"><label>Source-sheet reference<input name="reference" maxLength={160} required placeholder="e.g. Field sheet FS-204" /></label><label>Captured date<DatePicker name="capturedDate" defaultValue={harareToday()} required /></label></div><button disabled={saving}>Add reference</button><small>Metadata only. Document and photo upload is unavailable.</small></form>}
    <section className="diary-timeline"><h3>Chronological diary</h3>{details.timeline.map((event) => <article key={`${event.type}-${event.id}`}><span className="timeline-dot" /><div><small>{formatDateTime(event.eventAt)}</small><strong>{event.title}</strong><p>{event.detail}</p><span>Entered by {event.enteredBy}{event.operationalActor ? ` · operational actor ${event.operationalActor}` : ''}</span>{event.reason && <em>{event.reason}</em>}</div></article>)}</section>
  </div>;
}

/** @param {import('../../web-api-client').ActivityListItemDto} activity */
function coverage(activity) { if (activity.quantityBasis === 'None') return 'No quantity'; if (activity.actualQuantity == null) return activity.quantityBasis === 'Hectares' ? 'ha pending' : 'lines pending'; return activity.quantityBasis === 'Hectares' ? `${activity.actualQuantity.toLocaleString()} ha` : `${activity.actualQuantity.toLocaleString()} lines${activity.lineContextUnavailable ? ' · context unavailable' : ''}`; }
/** @param {string | undefined} value */
function formatDate(value) { if (!value || value === 'Unscheduled') return 'Unscheduled'; return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${value}T00:00:00`)); }
/** @param {string} value */
function formatDateTime(value) { return new Intl.DateTimeFormat('en-ZW', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value)); }
function harareToday() { return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date()); }
function harareNow() {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit',
    hour: '2-digit', minute: '2-digit', hourCycle: 'h23',
  }).formatToParts(new Date());
  const part = (/** @type {string} */ type) => parts.find((item) => item.type === type)?.value;
  return `${part('year')}-${part('month')}-${part('day')}T${part('hour')}:${part('minute')}`;
}
