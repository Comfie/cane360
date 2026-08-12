import { useEffect, useState } from 'react';
import { BadgeCheck, UserPlus, Users } from 'lucide-react';
import { CreatePersonRequest, FarmPersonnelClient } from '../../web-api-client';
import { getApiError } from './farmSetupApi';

const personnelClient = new FarmPersonnelClient();

export function PersonnelRegister() {
  const [register, setRegister] = useState(/** @type {import('../../web-api-client').PersonnelRegisterDto | null} */ (null));
  const [error, setError] = useState('');
  const [adding, setAdding] = useState(false);
  const [saving, setSaving] = useState(false);
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
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); setSaving(true); setError('');
    const role = String(data.get('role'));
    try {
      setRegister(await personnelClient.farmPersonnelPOST(new CreatePersonRequest({
        displayName: String(data.get('displayName')).trim(),
        phone: String(data.get('phone')).trim() || undefined,
        activeFrom: new Date(`${String(data.get('activeFrom'))}T00:00:00`),
        roles: [role],
        isPrimaryManager: role === 'FarmManager' && data.get('isPrimary') === 'on',
      })));
      setAdding(false);
    } catch (requestError) { setError(getApiError(requestError)); } finally { setSaving(false); }
  };

  return <section className="record-panel personnel-register">
    <header className="section-heading"><div><span className="eyebrow">People and roles</span><h2>Personnel register</h2></div><button type="button" className="secondary-action" onClick={() => setAdding(!adding)}><UserPlus size={16} /> Add person</button></header>
    {!register.primaryManagerAssigned && <div className="manager-gap"><Users size={18} /><div><strong>Primary manager not assigned</strong><span>Add a named person with the primary Farm manager role when ready. The grower has not been assumed to be the manager.</span></div></div>}
    {error && <p className="form-error">{error}</p>}
    {adding && <form className="subrecord-form" onSubmit={save}><div className="form-grid"><label>Display name<input name="displayName" maxLength={120} required /></label><label>Phone <small>Optional</small><input name="phone" type="tel" maxLength={30} /></label><label>Active from<input name="activeFrom" type="date" defaultValue={today} required /></label><label>Operational role<select name="role"><option value="Supervisor">Supervisor</option><option value="FarmManager">Farm manager</option><option value="Storekeeper">Storekeeper</option></select></label><label className="checkbox-label"><input name="isPrimary" type="checkbox" /> Primary manager <small>Applies only to Farm manager</small></label></div><button disabled={saving}>{saving ? 'Adding…' : 'Add person'}</button></form>}
    <div className="person-list">{register.persons.length === 0 ? <p>No named operational personnel recorded.</p> : register.persons.map((person) => <article key={person.id}><span className="person-avatar">{person.displayName.slice(0, 1).toUpperCase()}</span><div><strong>{person.displayName}</strong><small>{person.phone || 'No phone'} · Active from {person.activeFrom}</small></div><div className="person-roles">{person.roles.filter((role) => !role.effectiveTo).map((role) => <span key={role.id}>{role.isPrimary && <BadgeCheck size={13} />} {role.role === 'FarmManager' ? 'Farm manager' : role.role}</span>)}</div></article>)}</div>
  </section>;
}
