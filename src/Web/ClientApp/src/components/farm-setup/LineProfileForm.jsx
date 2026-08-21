import { useEffect, useState } from 'react';
import { Ruler, X } from 'lucide-react';
import { FieldLineProfilesClient, ReplaceFieldLineProfileRequest } from '../../web-api-client';
import { DatePicker } from '../DatePicker';
import { getApiError } from './farmSetupApi';

const lineProfilesClient = new FieldLineProfilesClient();

/** @param {{ fieldId: string }} props */
export function LineProfileForm({ fieldId }) {
  const [profile, setProfile] = useState(/** @type {import('../../web-api-client').FieldLineProfileDto | null} */ (null));
  const [editing, setEditing] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => {
    let current = true;
    lineProfilesClient.lineProfileGET(fieldId).then((result) => { if (current) setProfile(result); }).catch(() => {});
    return () => { current = false; };
  }, [fieldId]);
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const data = new FormData(event.currentTarget); setError('');
    try { setProfile(await lineProfilesClient.lineProfilePUT(fieldId, new ReplaceFieldLineProfileRequest({ standardLineLengthMetres: Number(data.get('length')), estimatedLineCount: Number(data.get('count')), numberingScheme: String(data.get('scheme')).trim(), effectiveFrom: new Date(`${String(data.get('effectiveFrom'))}T00:00:00`), expectedVersion: profile?.version }))); setEditing(false); }
    catch (requestError) { setError(getApiError(requestError)); }
  };
  return <>
    <section className="line-profile">
      <div><Ruler size={15} /><span>{profile ? `${profile.estimatedLineCount} lines · ${profile.standardLineLengthMetres} m standard length` : 'Standard-line context not configured'}</span></div>
      <button type="button" className="text-action" onClick={() => setEditing(true)}>{profile ? 'Replace profile' : 'Set line profile'}</button>
    </section>
    {editing && <dialog open className="line-profile-dialog" aria-labelledby="line-profile-title" onCancel={(event) => { event.preventDefault(); setEditing(false); }}>
      <article>
        <header>
          <div><span className="eyebrow">Field setup</span><h2 id="line-profile-title">{profile ? 'Replace line profile' : 'Set line profile'}</h2></div>
          <button type="button" className="dialog-close" onClick={() => setEditing(false)} aria-label="Close"><X /></button>
        </header>
        <form className="line-profile-form" onSubmit={save}>
          <div className="line-profile-form-fields">
            <label>Standard line length (m)<input name="length" type="number" min="0.01" step="0.01" required /></label>
            <label>Estimated whole-line count<input name="count" type="number" min="1" step="1" required /></label>
            <label>Numbering scheme<input name="scheme" maxLength={240} placeholder="e.g. North to south, 1–120" required /></label>
            <label>Effective from<DatePicker name="effectiveFrom" required /></label>
          </div>
          {error && <small className="form-error">{error}</small>}
          <footer className="line-profile-form-actions">
            <button type="button" className="secondary" onClick={() => setEditing(false)}>Cancel</button>
            <button>Save line profile</button>
          </footer>
        </form>
      </article>
    </dialog>}
  </>;
}
