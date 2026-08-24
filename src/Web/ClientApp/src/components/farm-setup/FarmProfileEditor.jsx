import { useState } from 'react';
import { X } from 'lucide-react';
import { UpdateFarmInformationRequest } from '../../web-api-client';
import { farmSetupClient, getApiError } from './farmSetupApi';

const tenureOptions = ['Owned', 'Leasehold', 'Outgrower agreement', 'Communal land', 'Other'];

/** @param {{ setup: import('../../web-api-client').FarmSetupDto, onClose: () => void, onSaved: (setup: import('../../web-api-client').FarmSetupDto) => void }} props */
export function FarmProfileEditor({ setup, onClose, onSaved }) {
  const [error, setError] = useState('');
  const [saving, setSaving] = useState(false);
  const farm = setup.farm;

  if (!farm) return null;
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setError('');
    setSaving(true);
    try {
      onSaved(await farmSetupClient.farmPUT(new UpdateFarmInformationRequest({
        growerDisplayName: String(data.get('growerDisplayName')).trim(),
        growerPhone: optionalValue(data.get('growerPhone')),
        farmCode: String(data.get('farmCode')).trim(),
        farmName: String(data.get('farmName')).trim(),
        address: String(data.get('address')).trim(),
        location: String(data.get('location')).trim(),
        tenure: String(data.get('tenure')).trim(),
        declaredHectares: Number(data.get('declaredHectares')),
        irrigationContext: String(data.get('irrigationContext')).trim(),
      })));
    } catch (requestError) {
      setError(getApiError(requestError));
    } finally {
      setSaving(false);
    }
  };

  return <dialog open className="activity-dialog farm-profile-dialog" aria-labelledby="edit-farm-title" onCancel={(event) => { event.preventDefault(); onClose(); }}>
    <article>
      <header>
        <div><span className="eyebrow">Farm setup</span><h2 id="edit-farm-title">Edit farm information</h2></div>
        <button type="button" className="dialog-close" onClick={onClose} aria-label="Close"><X /></button>
      </header>
      <form className="farm-profile-form" onSubmit={save}>
        <fieldset className="form-grid">
          <label>Grower name<input name="growerDisplayName" autoComplete="name" maxLength={120} defaultValue={setup.grower?.displayName} required autoFocus /></label>
          <label>Phone number <small>Optional</small><input name="growerPhone" type="tel" autoComplete="tel" maxLength={30} defaultValue={setup.grower?.phone} /></label>
          <label>Farm code<input name="farmCode" maxLength={20} pattern="[A-Za-z0-9][A-Za-z0-9_-]*" defaultValue={farm.code} required /></label>
          <label>Farm name<input name="farmName" maxLength={120} defaultValue={farm.name} required /></label>
          <label>Location<input name="location" maxLength={120} defaultValue={farm.location} required /></label>
          <label>Tenure<select name="tenure" required defaultValue={farm.tenure}>{!tenureOptions.includes(farm.tenure) && <option value={farm.tenure}>{farm.tenure}</option>}{tenureOptions.map((tenure) => <option key={tenure}>{tenure}</option>)}</select></label>
          <label className="is-wide">Physical address<textarea name="address" maxLength={240} rows={3} defaultValue={farm.address} required /></label>
          <label>Declared farm area (ha)<input name="declaredHectares" type="number" min="0.01" max="100000" step="0.01" inputMode="decimal" defaultValue={farm.declaredHectares} required /></label>
          <label className="is-wide">Irrigation context<textarea name="irrigationContext" maxLength={160} rows={3} defaultValue={farm.irrigationContext} required /></label>
        </fieldset>
        {error && <p className="form-error">{error}</p>}
        <footer className="farm-profile-actions">
          <button type="button" className="secondary" onClick={onClose} disabled={saving}>Cancel</button>
          <button disabled={saving}>{saving ? 'Saving…' : 'Save changes'}</button>
        </footer>
      </form>
    </article>
  </dialog>;
}

/** @param {FormDataEntryValue | null} value */
function optionalValue(value) {
  const text = String(value ?? '').trim();
  return text || undefined;
}
