import { useEffect, useState } from 'react';
import { BadgeCheck, Pencil, UserPlus, Users, X } from 'lucide-react';
import { CreatePersonRequest, FarmPersonnelClient, UpdatePersonRequest } from '../../web-api-client';
import { DatePicker } from '../DatePicker';
import { getApiError } from './farmSetupApi';

const personnelClient = new FarmPersonnelClient();

export function PersonnelRegister() {
  const [register, setRegister] = useState(/** @type {import('../../web-api-client').PersonnelRegisterDto | null} */ (null));
  const [error, setError] = useState('');
  const [adding, setAdding] = useState(false);
  const [editingPerson, setEditingPerson] = useState(/** @type {import('../../web-api-client').PersonDto | null} */ (null));
  const [saving, setSaving] = useState(false);
  const [role, setRole] = useState('Supervisor');
  const [isPrimaryManager, setIsPrimaryManager] = useState(false);
  const [roleEffectiveFrom, setRoleEffectiveFrom] = useState('');
  const [roleEffectiveFromMinimum, setRoleEffectiveFromMinimum] = useState('');
  const today = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Africa/Harare',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date());

  useEffect(() => {
    let current = true;
    personnelClient.farmPersonnelGET().then((result) => { if (current) setRegister(result); })
      .catch((requestError) => { if (current) setError(getApiError(requestError)); });
    return () => { current = false; };
  }, []);

  if (!register) return null;
  const openAddPerson = () => {
    setError('');
    setRole('Supervisor');
    setIsPrimaryManager(false);
    setRoleEffectiveFrom(today);
    setRoleEffectiveFromMinimum('');
    setEditingPerson(null);
    setAdding(true);
  };
  /** @param {import('../../web-api-client').PersonDto} person */
  const openEditPerson = (person) => {
    const currentRoles = person.roles.filter((assignment) => !assignment.effectiveTo);
    const currentRole = currentRoles[0];
    const latestRoleStart = currentRoles.reduce((latest, assignment) => latest > assignment.effectiveFrom ? latest : assignment.effectiveFrom, person.activeFrom);
    setError('');
    setAdding(false);
    setEditingPerson(person);
    setRole(currentRole?.role ?? 'Supervisor');
    setIsPrimaryManager(Boolean(currentRole?.isPrimary));
    const firstAvailableRoleDate = latestRoleStart >= today ? nextIsoDate(latestRoleStart) : today;
    setRoleEffectiveFromMinimum(firstAvailableRoleDate);
    setRoleEffectiveFrom(firstAvailableRoleDate);
  };
  const closeAddPerson = () => {
    setAdding(false);
    setEditingPerson(null);
    setRole('Supervisor');
    setIsPrimaryManager(false);
    setRoleEffectiveFrom('');
    setRoleEffectiveFromMinimum('');
    setError('');
  };
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); setError('');
    try {
      const request = {
        displayName: String(data.get('displayName')).trim(),
        phone: String(data.get('phone')).trim() || undefined,
        role,
        isPrimaryManager: role === 'FarmManager' && isPrimaryManager,
      };
      setRegister(editingPerson
        ? await personnelClient.farmPersonnelPUT(editingPerson.id, new UpdatePersonRequest({
          ...request,
          roleEffectiveFrom: new Date(`${roleEffectiveFrom}T00:00:00`),
          expectedVersion: editingPerson.version,
        }))
        : await personnelClient.farmPersonnelPOST(new CreatePersonRequest({
          ...request,
          activeFrom: new Date(`${String(data.get('activeFrom'))}T00:00:00`),
          roles: [role],
        })));
      closeAddPerson();
    } catch (requestError) { setError(getApiError(requestError)); } finally { setSaving(false); }
  };

  return <section className="record-panel personnel-register">
    <header className="section-heading"><div><span className="eyebrow">People and roles</span><h2>Personnel register</h2></div><button type="button" className="secondary-action" onClick={openAddPerson}><UserPlus size={16} /> Add person</button></header>
    {!register.primaryManagerAssigned && <div className="manager-gap"><Users size={18} /><div><strong>Primary manager not assigned</strong><span>Add a named person with the primary Farm manager role when ready. The grower has not been assumed to be the manager.</span></div></div>}
    {error && !adding && !editingPerson && <p className="form-error">{error}</p>}
    {(adding || editingPerson) && <dialog open className="activity-dialog personnel-dialog" aria-labelledby="person-editor-title" onCancel={(event) => { event.preventDefault(); closeAddPerson(); }}>
      <article>
        <header>
          <div><span className="eyebrow">People and roles</span><h2 id="person-editor-title">{editingPerson ? 'Edit person' : 'Add person'}</h2></div>
          <button type="button" className="dialog-close" onClick={closeAddPerson} aria-label="Close"><X /></button>
        </header>
        <form className="personnel-form" onSubmit={save}>
          <div className="form-grid">
            <label>Display name<input name="displayName" maxLength={120} required autoFocus defaultValue={editingPerson?.displayName} /></label>
            <label>Phone <small>Optional</small><input name="phone" type="tel" maxLength={30} defaultValue={editingPerson?.phone} /></label>
            {editingPerson
              ? <label>Role effective from<DatePicker name="roleEffectiveFrom" value={roleEffectiveFrom} min={roleEffectiveFromMinimum} onChange={setRoleEffectiveFrom} required /><small>Current roles end on the day before this date.</small></label>
              : <label>Active from<DatePicker name="activeFrom" defaultValue={today} required /></label>}
            <label>Operational role<select name="role" value={role} onChange={(event) => { setRole(event.target.value); setIsPrimaryManager(false); }}><option value="Supervisor">Supervisor</option><option value="FarmManager">Farm manager</option><option value="Storekeeper">Storekeeper</option></select></label>
            {role === 'FarmManager' && <label className="toggle-control personnel-primary-toggle">
              <input name="isPrimary" type="checkbox" checked={isPrimaryManager} onChange={(event) => setIsPrimaryManager(event.target.checked)} />
              <span className="toggle-control-track" aria-hidden="true" />
              <span className="personnel-primary-toggle-copy"><strong>Primary manager</strong><small>Assign this Farm manager as the primary manager.</small></span>
            </label>}
          </div>
          {error && <p className="form-error">{error}</p>}
          <footer className="personnel-form-actions">
            <button type="button" className="secondary" onClick={closeAddPerson} disabled={saving}>Cancel</button>
            <button disabled={saving}>{saving ? (editingPerson ? 'Saving…' : 'Adding…') : (editingPerson ? 'Save changes' : 'Add person')}</button>
          </footer>
        </form>
      </article>
    </dialog>}
    <div className="person-list">{register.persons.length === 0 ? <p>No named operational personnel recorded.</p> : register.persons.map((person) => <article key={person.id}><span className="person-avatar">{person.displayName.slice(0, 1).toUpperCase()}</span><div><strong>{person.displayName}</strong><small>{person.phone || 'No phone'} · Active from {person.activeFrom}</small></div><div className="person-roles">{person.roles.filter((role) => !role.effectiveTo).map((role) => <span key={role.id}>{role.isPrimary && <BadgeCheck size={13} />} {role.role === 'FarmManager' ? 'Farm manager' : role.role}</span>)}</div>{person.status === 'Active' && <button type="button" className="personnel-edit" onClick={() => openEditPerson(person)} aria-label={`Edit ${person.displayName}`} title={`Edit ${person.displayName}`}><Pencil size={15} /></button>}</article>)}</div>
  </section>;
}

/** @param {string} date */
function nextIsoDate(date) {
  const [year, month, day] = date.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day + 1)).toISOString().slice(0, 10);
}
