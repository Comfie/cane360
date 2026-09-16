import { useCallback, useEffect, useState, type FormEvent } from 'react';
import {
  ActivityTypesClient, AdministrationClient, ArchiveDocumentCategoryRequest,
  CreateActivityTypeRequest, CreateDocumentCategoryRequest, CreateFarmSettingRequest,
  CreateInventoryApplicationRuleRequest, CreateManagerInvitationRequest,
  CreateUnitOfMeasureRequest, EndEffectiveRuleRequest, InputControlsClient,
  InventoryClient, VersionedInventoryRequest,
  RenameActivityTypeRequest, RenameUnitOfMeasureRequest, UpdateDocumentCategoryRequest,
  VersionedRequest, type ActivityTypeDto, type AdministrationAuditDto,
  type AdministrationAuditPageDto, type AdministrationOverviewDto,
  type AdministrationSessionDto, type AdministrationUserDto, type UnitOfMeasureDto,
  type AdministrationManagerAccessDto,
  type AdministrationRuleDto, type InventoryItemDto,
  type DocumentCategoryDto, type FarmSettingDto,
  type AdministrationCapabilityDto,
} from '../../web-api-client';
import { getApiError } from '../apiError';
import { DatePicker } from '../DatePicker';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';

const administration = new AdministrationClient();
const activities = new ActivityTypesClient();
const inventory = new InventoryClient();
const inputControls = new InputControlsClient();

type Section = 'overview' | 'users' | 'roles' | 'activities' | 'units' | 'rules' | 'categories' | 'audit';
const sections: { id: Section; label: string }[] = [
  { id: 'overview', label: 'Overview' },
  { id: 'users', label: 'Users & Access' },
  { id: 'roles', label: 'Roles & Permissions' },
  { id: 'activities', label: 'Activity Types' },
  { id: 'units', label: 'Units' },
  { id: 'rules', label: 'Rules & Tolerances' },
  { id: 'categories', label: 'Document Categories' },
  { id: 'audit', label: 'Audit' },
];

export function AdministrationPage() {
  const [section, setSection] = useState<Section>('overview');
  const [session, setSession] = useState<AdministrationSessionDto | null>(null);
  const [overview, setOverview] = useState<AdministrationOverviewDto | null>(null);
  const [users, setUsers] = useState<AdministrationUserDto[]>([]);
  const [types, setTypes] = useState<ActivityTypeDto[]>([]);
  const [units, setUnits] = useState<UnitOfMeasureDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const reload = useCallback(async () => {
    const nextSession = await administration.session();
    const [nextOverview, nextUsers, nextTypes, nextUnits] = await Promise.all([
      administration.overview(), nextSession.role === 'Grower' ? administration.users() : Promise.resolve([]),
      activities.activityTypesAll(), inventory.unitsAll(),
    ]);
    setSession(nextSession);
    setOverview(nextOverview);
    setUsers(nextUsers);
    setTypes(nextTypes);
    setUnits(nextUnits);
  }, []);

  useEffect(() => {
    let active = true;
    administration.session().then((currentSession) => Promise.all([Promise.resolve(currentSession),
      administration.overview(), currentSession.role === 'Grower' ? administration.users() : Promise.resolve([]),
      activities.activityTypesAll(), inventory.unitsAll()]))
      .then(([nextSession, nextOverview, nextUsers, nextTypes, nextUnits]) => {
        if (!active) return;
        setSession(nextSession);
        setOverview(nextOverview);
        setUsers(nextUsers);
        setTypes(nextTypes);
        setUnits(nextUnits);
      })
      .catch((cause) => { if (active) setError(getApiError(cause)); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, []);

  const change = async (action: () => Promise<unknown>) => {
    try { setError(''); await action(); await reload(); }
    catch (cause) { setError(getApiError(cause)); }
  };

  if (loading) return <LoadingState label="Opening Administration" />;
  if (!session || !overview) return <ValidationError message={error || 'Administration could not be loaded.'} />;

  return <div className="page-stack administration-page">
    <PageHeader eyebrow="Farm configuration" title="Administration"
      description="Users, access, farm configuration and audit" />
    <ValidationError message={error} />
    <nav className="administration-tabs" aria-label="Administration sections">
      {sections.filter((item) => session.role === 'Grower' || !['audit', 'users'].includes(item.id)).map((item) =>
        <button type="button" key={item.id} aria-current={section === item.id}
          onClick={() => setSection(item.id)}>{item.label}</button>)}
    </nav>
    {section === 'overview' && <Overview overview={overview} role={session.role} onNavigate={setSection} />}
    {section === 'users' && session.role === 'Grower' && <Users users={users} session={session} onDisable={(id) =>
      change(() => administration.disable(id))} />}
    {section === 'roles' && <Roles session={session} />}
    {section === 'activities' && <ActivityTypes types={types} onChange={change} />}
    {section === 'units' && <Units units={units} onChange={change} />}
    {section === 'rules' && <Rules types={types} onChange={change} />}
    {section === 'categories' && <DocumentCategories onChange={change} />}
    {section === 'audit' && session.role === 'Grower' && <Audit />}
  </div>;
}

function Overview({ overview, role, onNavigate }: { overview: AdministrationOverviewDto;
  role: string; onNavigate: (section: Section) => void }) {
  const metrics: { label: string; value: string | number; section: Section }[] = [
    { label: 'Active users', value: overview.activeUsers, section: 'users' },
    { label: 'Pending invitations', value: overview.pendingInvitations, section: 'users' },
    { label: 'Active manager', value: overview.activeFarmManager || 'None', section: 'users' },
    { label: 'Activity types', value: overview.activeActivityTypes, section: 'activities' },
    { label: 'Units', value: overview.activeUnits, section: 'units' },
    { label: 'Application rules', value: overview.ruleCount, section: 'rules' },
  ];
  return <>
    <div className="administration-summary">{metrics.map((metric) =>
      <button type="button" key={metric.label} onClick={() => onNavigate(metric.section)}>
        <span>{metric.label}</span><strong>{metric.value}</strong>
      </button>)}</div>
    {overview.attentionCount > 0 && <section className="record-panel">
      <h2>Configuration requiring attention</h2>
      <p>No active FarmManager application membership is linked to this farm.</p>
      <button type="button" onClick={() => onNavigate('users')}>Review access</button>
    </section>}
    <section className="record-panel"><h2>Recent administration and audit events</h2>
      {overview.recentEvents.length ? <ul className="administration-events">
        {overview.recentEvents.map((event) => <li key={event.id}>
          <time>{event.occurredAt.toLocaleString()}</time><strong>{event.action}</strong>
          <span>{event.safeSummary}</span></li>)}</ul>
        : <p>No recent events are available for this role.</p>}
      {role === 'Grower' && <button type="button" onClick={() => onNavigate('audit')}>Review audit</button>}
    </section>
  </>;
}

function Users({ users, session, onDisable }: { users: AdministrationUserDto[];
  session: AdministrationSessionDto; onDisable: (id: string) => void }) {
  return <section className="record-panel"><header className="ledger-title"><div>
    <span className="eyebrow">Login membership</span><h2>Users & Access</h2></div></header>
    <p>Operational people remain in Farm personnel. Only linked application accounts appear here.</p>
    <div className="administration-list">{users.map((member) => <article key={member.membershipId}>
      <div><strong>{member.personName || member.email || member.userId}</strong>
        <small>{member.email} · {member.role}</small></div>
      <div><span>{member.personName ? `Linked person: ${member.personName}` : 'No operational person'}</span>
        <span>{member.status === 'Archived' ? 'Disabled' : member.status}</span></div>
      {session.role === 'Grower' && member.role === 'FarmManager' && member.status === 'Active' &&
        <button type="button" onClick={() => onDisable(member.membershipId)}>Disable manager</button>}
    </article>)}</div>
    {session.role === 'Grower' && <ManagerInvitations />}
  </section>;
}

function ManagerInvitations() {
  const [access, setAccess] = useState<AdministrationManagerAccessDto | null>(null);
  const [personId, setPersonId] = useState('');
  const [token, setToken] = useState('');
  const [error, setError] = useState('');
  const reload = useCallback(async () => {
    const result = await administration.managerAccess();
    setAccess(result);
    setPersonId((current) => current || result.candidates[0]?.personId || '');
  }, []);
  useEffect(() => {
    let active = true;
    administration.managerAccess().then((result) => {
      if (active) { setAccess(result); setPersonId(result.candidates[0]?.personId || ''); }
    }).catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, []);
  const invite = async (event: FormEvent) => {
    event.preventDefault();
    try {
      setError('');
      const created = await inputControls.managerInvitations(
        new CreateManagerInvitationRequest({ personId, expiresInHours: 48 }));
      setToken(created.token);
      await reload();
    } catch (cause) { setError(getApiError(cause)); }
  };
  if (!access) return <ValidationError message={error} />;
  return <div className="administration-access">
    <h3>FarmManager invitation</h3><ValidationError message={error} />
    <p>Only the active primary FarmManager person can receive a single-use invitation.</p>
    <form className="administration-form" onSubmit={invite}>
      <label>Operational person<select required value={personId} onChange={(event) => setPersonId(event.target.value)}>
        {access.candidates.map((candidate) => <option key={candidate.personId} value={candidate.personId}>{candidate.name}</option>)}
      </select></label>
      <button type="submit" disabled={!personId}>Create invitation</button>
    </form>
    {token && <div className="invitation-token"><strong>Copy this token now</strong><code>{token}</code>
      <small>It is shown once. Only its hash is stored.</small></div>}
    <div className="administration-list">{access.invitations.map((invitation) => <article key={invitation.id}>
      <div><strong>{invitation.redeemedAt ? 'Redeemed' : invitation.revokedAt ? 'Revoked' :
        invitation.expiresAt < new Date() ? 'Expired' : 'Open invitation'}</strong>
        <small>Expires {invitation.expiresAt.toLocaleString()}</small></div>
      {!invitation.redeemedAt && !invitation.revokedAt && invitation.expiresAt > new Date() &&
        <button type="button" onClick={async () => {
          try { setError(''); await inputControls.revoke(invitation.id,
            new VersionedInventoryRequest({ expectedVersion: invitation.version })); await reload(); }
          catch (cause) { setError(getApiError(cause)); }
        }}>Revoke</button>}
    </article>)}</div>
  </div>;
}

function Roles({ session }: { session: AdministrationSessionDto }) {
  const [matrix, setMatrix] = useState<AdministrationCapabilityDto[]>([]);
  const [error, setError] = useState('');
  useEffect(() => {
    let active = true;
    administration.roles().then((result) => { if (active) setMatrix(result); })
      .catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, []);
  return <section className="record-panel"><h2>Roles & Permissions</h2>
    <p>Your current role: <strong>{session.role}</strong>. Security checks run on the server.</p>
    <ValidationError message={error} />
    <div className="administration-matrix" role="table" aria-label="Fixed role capabilities">
      <div role="row" className="administration-matrix-head"><strong role="columnheader">Capability</strong>
        <strong role="columnheader">Grower</strong><strong role="columnheader">FarmManager</strong>
        <strong role="columnheader">Platform Administrator</strong></div>
      {matrix.map((item) => <div role="row" key={item.capability}>
        <span role="cell">{item.capability}</span>
        <span role="cell">{item.grower ? 'Allowed' : '—'}</span>
        <span role="cell">{item.farmManager ? 'Allowed' : '—'}</span>
        <span role="cell">{item.platformAdministrator ? 'Allowed' : '—'}</span>
      </div>)}
    </div>
    <p>Platform Administrator uses audited support access and cannot perform routine tenant operations.</p>
  </section>;
}

function ActivityTypes({ types, onChange }: { types: ActivityTypeDto[]; onChange: (action: () => Promise<unknown>) => Promise<void> }) {
  const [code, setCode] = useState(''); const [name, setName] = useState('');
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [planned, setPlanned] = useState(true); const [unplanned, setUnplanned] = useState(true);
  const [basis, setBasis] = useState('None');
  const create = async (event: FormEvent) => {
    event.preventDefault();
    await onChange(() => activities.activityTypesPOST(new CreateActivityTypeRequest({
      code, name, supportsPlanned: planned, supportsUnplanned: unplanned, quantityBasis: basis,
    })));
    setCode(''); setName('');
  };
  return <section className="record-panel"><h2>Activity Types</h2>
    <form className="administration-form" onSubmit={create}>
      <label>Code<input required maxLength={24} value={code} onChange={(event) => setCode(event.target.value)} /></label>
      <label>Name<input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
      <label>Quantity basis<select value={basis} onChange={(event) => setBasis(event.target.value)}>
        <option>None</option><option>Hectares</option><option>StandardLines</option></select></label>
      <label><input type="checkbox" checked={planned} onChange={(event) => setPlanned(event.target.checked)} /> Planned allowed</label>
      <label><input type="checkbox" checked={unplanned} onChange={(event) => setUnplanned(event.target.checked)} /> Unplanned allowed</label>
      <button type="submit">Add activity type</button>
    </form>
    <div className="administration-list">{types.map((type) => <article key={type.id}>
      <div><strong>{type.code} · {type.name}</strong><small>{type.quantityBasis}</small></div>
      <div><span>Planned: {type.supportsPlanned ? 'Yes' : 'No'}</span><span>Unplanned: {type.supportsUnplanned ? 'Yes' : 'No'}</span><span>{type.status}</span></div>
      {editing === type.id && <form className="administration-form" onSubmit={async (event) => {
        event.preventDefault();
        await onChange(() => activities.activityTypesPUT(type.id,
          new RenameActivityTypeRequest({ name: editName, expectedVersion: type.version })));
        setEditing(null);
      }}><label>Name<input required maxLength={100} value={editName}
        onChange={(event) => setEditName(event.target.value)} /></label>
        <button type="submit">Save name</button><button type="button" onClick={() => setEditing(null)}>Cancel</button></form>}
      {type.status === 'Active' && editing !== type.id && <button type="button"
        onClick={() => { setEditing(type.id); setEditName(type.name); }}>Edit name</button>}
      {type.status === 'Active' && <button type="button" onClick={() => onChange(() =>
        activities.archive(type.id, new VersionedRequest({ expectedVersion: type.version })))}>Archive</button>}
    </article>)}</div>
  </section>;
}

function Units({ units, onChange }: { units: UnitOfMeasureDto[]; onChange: (action: () => Promise<unknown>) => Promise<void> }) {
  const [code, setCode] = useState(''); const [name, setName] = useState('');
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [dimension, setDimension] = useState(''); const [places, setPlaces] = useState(2);
  const create = async (event: FormEvent) => {
    event.preventDefault();
    await onChange(() => inventory.unitsPOST(new CreateUnitOfMeasureRequest({ code, name, dimension, decimalPlaces: places })));
    setCode(''); setName(''); setDimension('');
  };
  return <section className="record-panel"><h2>Units of Measure</h2>
    <form className="administration-form" onSubmit={create}>
      <label>Code<input required maxLength={20} value={code} onChange={(event) => setCode(event.target.value)} /></label>
      <label>Name<input required maxLength={80} value={name} onChange={(event) => setName(event.target.value)} /></label>
      <label>Dimension<input required maxLength={40} value={dimension} onChange={(event) => setDimension(event.target.value)} /></label>
      <label>Decimal precision<input type="number" min="0" max="6" required value={places} onChange={(event) => setPlaces(Number(event.target.value))} /></label>
      <button type="submit">Add unit</button>
    </form>
    <div className="administration-list">{units.map((unit) => <article key={unit.id}>
      <div><strong>{unit.code} · {unit.name}</strong><small>{unit.dimension} · {unit.decimalPlaces} decimals</small></div>
      <span>{unit.status}</span>
      {editing === unit.id && <form className="administration-form" onSubmit={async (event) => {
        event.preventDefault();
        await onChange(() => inventory.unitsPUT(unit.id,
          new RenameUnitOfMeasureRequest({ name: editName, expectedVersion: unit.version })));
        setEditing(null);
      }}><label>Name<input required maxLength={80} value={editName}
        onChange={(event) => setEditName(event.target.value)} /></label>
        <button type="submit">Save name</button><button type="button" onClick={() => setEditing(null)}>Cancel</button></form>}
      {unit.status === 'Active' && editing !== unit.id && <button type="button"
        onClick={() => { setEditing(unit.id); setEditName(unit.name); }}>Edit name</button>}
      {unit.status === 'Active' && <button type="button" onClick={() => onChange(() =>
        inventory.archive3(unit.id, new VersionedInventoryRequest({ expectedVersion: unit.version })))}>Archive</button>}
    </article>)}</div>
  </section>;
}

function Rules({ types, onChange }: { types: ActivityTypeDto[];
  onChange: (action: () => Promise<unknown>) => Promise<void> }) {
  const [rules, setRules] = useState<AdministrationRuleDto[]>([]);
  const [items, setItems] = useState<InventoryItemDto[]>([]);
  const [itemId, setItemId] = useState('');
  const [typeId, setTypeId] = useState('');
  const [basis, setBasis] = useState('FieldReportingHectares');
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [rate, setRate] = useState(0);
  const [lower, setLower] = useState(0);
  const [upper, setUpper] = useState(0);
  const [error, setError] = useState('');
  const load = useCallback(async () => {
    const [nextRules, nextItems] = await Promise.all([administration.rulesAll(), administration.ruleItems()]);
    setRules(nextRules); setItems(nextItems);
    setItemId((current) => current || nextItems[0]?.id || '');
  }, []);
  useEffect(() => {
    let active = true;
    Promise.all([administration.rulesAll(), administration.ruleItems()])
      .then(([nextRules, nextItems]) => {
        if (!active) return;
        setRules(nextRules); setItems(nextItems); setItemId(nextItems[0]?.id || '');
        setTypeId(types.find((type) => type.status === 'Active')?.id || '');
      }).catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, [types]);
  const create = async (event: FormEvent) => {
    event.preventDefault();
    try {
      setError('');
      await onChange(() => inputControls.rules(new CreateInventoryApplicationRuleRequest({
        inventoryItemId: itemId, activityTypeId: typeId, effectiveFrom: new Date(`${from}T00:00:00Z`),
        effectiveTo: to ? new Date(`${to}T00:00:00Z`) : undefined,
        coverageBasis: basis, ratePerCoverageUnit: rate,
        lowerTolerancePercent: lower, upperTolerancePercent: upper,
      })));
      await load();
    } catch (cause) { setError(getApiError(cause)); }
  };
  return <section className="record-panel"><h2>Rules & Tolerances</h2>
    <p>Inventory application rates are versioned by effective date. Worker rates remain in Labour & Payroll.</p>
    <ValidationError message={error} />
    <form className="administration-form" onSubmit={create}>
      <label>Item<select required value={itemId} onChange={(event) => setItemId(event.target.value)}>
        {items.map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label>
      <label>Activity type<select required value={typeId} onChange={(event) => setTypeId(event.target.value)}>
        {types.filter((type) => type.status === 'Active').map((type) =>
          <option key={type.id} value={type.id}>{type.code} · {type.name}</option>)}</select></label>
      <label>Coverage basis<select value={basis} onChange={(event) => setBasis(event.target.value)}>
        <option value="FieldReportingHectares">Field hectares</option>
        <option value="ActivityActualQuantity">Activity actual quantity</option></select></label>
      <label>Rate per coverage unit<input type="number" required min="0.000001" step="any" value={rate} onChange={(event) => setRate(Number(event.target.value))} /></label>
      <label>Lower tolerance %<input type="number" required min="0" step="any" value={lower} onChange={(event) => setLower(Number(event.target.value))} /></label>
      <label>Upper tolerance %<input type="number" required min="0" step="any" value={upper} onChange={(event) => setUpper(Number(event.target.value))} /></label>
      <label>Effective from<DatePicker required value={from} onChange={setFrom} /></label>
      <label>Effective to<DatePicker min={from} value={to} onChange={setTo} /></label>
      <button type="submit" disabled={!itemId || !typeId}>Add effective rule</button>
    </form>
    <div className="administration-list">{rules.map((rule) => <article key={rule.id}>
      <div><strong>{rule.itemName} · {rule.activityTypeName}</strong>
        <small>{rule.ratePerCoverageUnit} {rule.unitCode} per {rule.coverageBasis}</small></div>
      <div><span>−{rule.lowerTolerancePercent}% / +{rule.upperTolerancePercent}%</span>
        <span>{rule.effectiveFrom.toLocaleDateString()} – {rule.effectiveTo?.toLocaleDateString() || 'open'}</span></div>
      {!rule.effectiveTo && <EndDateAction label="End rule" onEnd={async (date) => {
        await onChange(() => administration.end(rule.id,
          new EndEffectiveRuleRequest({ effectiveTo: date, expectedVersion: rule.version })));
        await load();
      }} />}
    </article>)}</div>
    <FarmSettings onChange={onChange} />
  </section>;
}

function FarmSettings({ onChange }: { onChange: (action: () => Promise<unknown>) => Promise<void> }) {
  const [settings, setSettings] = useState<FarmSettingDto[]>([]);
  const [days, setDays] = useState(2);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [error, setError] = useState('');
  const load = useCallback(async () => setSettings(await administration.settingsAll()), []);
  useEffect(() => {
    let active = true;
    administration.settingsAll().then((items) => { if (active) setSettings(items); })
      .catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, []);
  const create = async (event: FormEvent) => {
    event.preventDefault();
    await onChange(() => administration.settings(new CreateFarmSettingRequest({
      key: 'ActivityLateEntryReasonDays', value: days,
      effectiveFrom: new Date(`${from}T00:00:00Z`),
      effectiveTo: to ? new Date(`${to}T00:00:00Z`) : undefined,
    })));
    try { await load(); } catch (cause) { setError(getApiError(cause)); }
  };
  return <div className="administration-settings"><h3>Farm settings</h3>
    <p>Activity late-entry reason threshold. The default is two calendar days until an effective version applies.</p>
    <ValidationError message={error} />
    <form className="administration-form" onSubmit={create}>
      <label>Reason required after days<input type="number" min="0" max="30" required value={days}
        onChange={(event) => setDays(Number(event.target.value))} /></label>
      <label>Effective from<DatePicker required value={from} onChange={setFrom} /></label>
      <label>Effective to<DatePicker min={from} value={to} onChange={setTo} /></label>
      <button type="submit">Add setting version</button>
    </form>
    <div className="administration-list">{settings.map((setting) => <article key={setting.id}>
      <strong>{setting.label}: {setting.value} days</strong>
      <span>{setting.effectiveFrom.toLocaleDateString()} – {setting.effectiveTo?.toLocaleDateString() || 'open'}</span>
      {!setting.effectiveTo && <EndDateAction label="End setting" onEnd={async (date) => {
        await onChange(() => administration.end2(setting.id,
          new EndEffectiveRuleRequest({ effectiveTo: date, expectedVersion: setting.version })));
        await load();
      }} />}
    </article>)}</div>
  </div>;
}

function EndDateAction({ label, onEnd }: { label: string; onEnd: (date: Date) => Promise<void> }) {
  const [date, setDate] = useState('');
  return <form className="administration-end-action" onSubmit={(event) => {
    event.preventDefault();
    if (date) void onEnd(new Date(`${date}T00:00:00Z`));
  }}>
    <label>Last effective date<DatePicker required value={date} onChange={setDate} /></label>
    <button type="submit">{label}</button>
  </form>;
}

function DocumentCategories({ onChange }: { onChange: (action: () => Promise<unknown>) => Promise<void> }) {
  const [categories, setCategories] = useState<DocumentCategoryDto[]>([]);
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [editing, setEditing] = useState<string | null>(null);
  const [editName, setEditName] = useState('');
  const [editDescription, setEditDescription] = useState('');
  const [error, setError] = useState('');
  const load = useCallback(async () => setCategories(await administration.documentCategoriesAll()), []);
  useEffect(() => {
    let active = true;
    administration.documentCategoriesAll().then((items) => { if (active) setCategories(items); })
      .catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, []);
  const create = async (event: FormEvent) => {
    event.preventDefault();
    await onChange(() => administration.documentCategoriesPOST(new CreateDocumentCategoryRequest({
      code, name, description: description || undefined,
    })));
    await load();
    setCode(''); setName(''); setDescription('');
  };
  return <section className="record-panel"><h2>Document Categories</h2>
    <p>Categories classify mill evidence. Archived categories remain attached to historical evidence.</p>
    <ValidationError message={error} />
    <form className="administration-form" onSubmit={create}>
      <label>Code<input required maxLength={24} value={code} onChange={(event) => setCode(event.target.value)} /></label>
      <label>Name<input required maxLength={100} value={name} onChange={(event) => setName(event.target.value)} /></label>
      <label>Description<input maxLength={300} value={description} onChange={(event) => setDescription(event.target.value)} /></label>
      <button type="submit">Add category</button>
    </form>
    <div className="administration-list">{categories.map((category) => <article key={category.id}>
      <div><strong>{category.code} · {category.name}</strong><small>{category.description}</small></div>
      <span>{category.active ? 'Active' : 'Archived'}</span>
      {editing === category.id && <form className="administration-form" onSubmit={async (event) => {
        event.preventDefault();
        await onChange(() => administration.documentCategoriesPUT(category.id,
          new UpdateDocumentCategoryRequest({ name: editName,
            description: editDescription || undefined, expectedVersion: category.version })));
        await load(); setEditing(null);
      }}><label>Name<input required maxLength={100} value={editName}
        onChange={(event) => setEditName(event.target.value)} /></label>
        <label>Description<input maxLength={300} value={editDescription}
          onChange={(event) => setEditDescription(event.target.value)} /></label>
        <button type="submit">Save category</button><button type="button" onClick={() => setEditing(null)}>Cancel</button></form>}
      {category.active && editing !== category.id && <button type="button" onClick={() => {
        setEditing(category.id); setEditName(category.name);
        setEditDescription(category.description || '');
      }}>Edit</button>}
      {category.active && <button type="button" onClick={async () => {
        await onChange(() => administration.archive2(category.id,
          new ArchiveDocumentCategoryRequest({ expectedVersion: category.version })));
        await load();
      }}>Archive</button>}
    </article>)}</div>
  </section>;
}

function Audit() {
  const [page, setPage] = useState(1);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [action, setAction] = useState('');
  const [subject, setSubject] = useState('');
  const [userId, setUserId] = useState('');
  const [personId, setPersonId] = useState('');
  const [correlation, setCorrelation] = useState('');
  const [result, setResult] = useState<AdministrationAuditPageDto | null>(null);
  const [selected, setSelected] = useState<AdministrationAuditDto | null>(null);
  const [error, setError] = useState('');
  const [exporting, setExporting] = useState(false);
  const exportCsv = async () => {
    let download: Response | null = null;
    const client = new AdministrationClient('', { fetch: async (url, options) => {
      const response = await window.fetch(url, options);
      if (response.ok) download = response.clone();
      return response;
    } });
    try {
      setExporting(true); setError('');
      await client.audit_csv(from ? new Date(`${from}T00:00:00Z`) : undefined,
        to ? new Date(`${to}T23:59:59Z`) : undefined,
        action || undefined, subject || undefined, userId || undefined,
        personId || undefined, correlation || undefined, undefined, undefined);
      if (!download) throw new Error('Audit export returned no file.');
      const response: Response = download;
      const href = URL.createObjectURL(await response.blob());
      const anchor = document.createElement('a');
      anchor.href = href;
      anchor.download = `cane360-audit-${new Date().toISOString().slice(0, 10)}.csv`;
      anchor.click();
      URL.revokeObjectURL(href);
    } catch (cause) { setError(getApiError(cause)); }
    finally { setExporting(false); }
  };
  useEffect(() => {
    let active = true;
    administration.audit(from ? new Date(`${from}T00:00:00Z`) : undefined,
      to ? new Date(`${to}T23:59:59Z`) : undefined,
      action || undefined, subject || undefined, userId || undefined,
      personId || undefined, correlation || undefined, page, 25)
      .then((data) => { if (active) setResult(data); })
      .catch((cause) => { if (active) setError(getApiError(cause)); });
    return () => { active = false; };
  }, [from, to, action, subject, userId, personId, correlation, page]);
  return <section className="record-panel"><header className="ledger-title"><h2>Audit</h2>
    <button type="button" disabled={exporting} onClick={exportCsv}>{exporting ? 'Exporting…' : 'Export CSV'}</button>
    </header><ValidationError message={error} />
    <div className="administration-filters">
      <label>From<DatePicker value={from} onChange={(value) => { setPage(1); setFrom(value); }} /></label>
      <label>To<DatePicker min={from} value={to} onChange={(value) => { setPage(1); setTo(value); }} /></label>
      <label>Action<input value={action} onChange={(event) => { setPage(1); setAction(event.target.value); }} /></label>
      <label>Subject<input value={subject} onChange={(event) => { setPage(1); setSubject(event.target.value); }} /></label>
      <label>Authenticated user ID<input value={userId} onChange={(event) => { setPage(1); setUserId(event.target.value); }} /></label>
      <label>Operational person ID<input value={personId} onChange={(event) => { setPage(1); setPersonId(event.target.value); }} /></label>
      <label>Correlation ID<input value={correlation} onChange={(event) => { setPage(1); setCorrelation(event.target.value); }} /></label>
    </div>
    {result && <><div className="administration-list">{result.items.map((event) =>
      <button className="administration-audit-card" type="button" key={event.id} onClick={() => setSelected(event)}>
        <time>{event.occurredAt.toLocaleString()}</time><strong>{event.action}</strong>
        <span>{event.subjectType} · {event.safeSummary}</span>
      </button>)}</div>
      <div className="administration-pagination"><button type="button" disabled={page === 1}
        onClick={() => setPage(page - 1)}>Previous</button><span>Page {page} · {result.totalCount} events</span>
        <button type="button" disabled={page * 25 >= result.totalCount} onClick={() => setPage(page + 1)}>Next</button></div></>}
    {selected && <div className="administration-audit-detail"><button type="button" onClick={() => setSelected(null)}>Close detail</button>
      <h3>{selected.action}</h3><p>{selected.safeSummary}</p>
      <dl><dt>Occurred</dt><dd>{selected.occurredAt.toLocaleString()}</dd>
        <dt>Subject</dt><dd>{selected.subjectType} · {selected.subjectId}</dd>
        <dt>Authenticated user</dt><dd>{selected.authenticatedUserEmail || selected.authenticatedUserId}</dd>
        <dt>Operational person</dt><dd>{selected.operationalPersonName || '—'}</dd>
        <dt>Reason</dt><dd>{selected.reason || '—'}</dd>
        <dt>Correlation</dt><dd>{selected.correlationId}</dd></dl></div>}
  </section>;
}
