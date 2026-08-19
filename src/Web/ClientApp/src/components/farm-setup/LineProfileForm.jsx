import { useEffect, useState } from 'react';
import { Ruler } from 'lucide-react';
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
  return <section className="line-profile"><div><Ruler size={15} /><span>{profile ? `${profile.estimatedLineCount} lines · ${profile.standardLineLengthMetres} m standard length` : 'Standard-line context not configured'}</span></div><button type="button" className="text-action" onClick={() => setEditing(!editing)}>{profile ? 'Replace profile' : 'Set line profile'}</button>{editing && <form onSubmit={save}><label>Standard line length (m)<input name="length" type="number" min="0.01" step="0.01" required /></label><label>Estimated whole-line count<input name="count" type="number" min="1" step="1" required /></label><label>Numbering scheme<input name="scheme" maxLength={240} placeholder="e.g. North to south, 1–120" required /></label><label>Effective from<DatePicker name="effectiveFrom" required /></label><button>Save line profile</button>{error && <small className="form-error">{error}</small>}</form>}</section>;
}
