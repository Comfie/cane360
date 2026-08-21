import { useState } from 'react';
import { ArrowRight, Tractor } from 'lucide-react';
import { Link } from 'react-router-dom';
import { CreateGrowerFarmRequest } from '../../web-api-client';
import { FarmSetupProgress } from '../farm-setup/FarmSetupProgress';
import { FarmSummary } from '../farm-setup/FarmSummary';
import { farmSetupClient, getApiError, useFarmSetup } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { PersonnelRegister } from '../farm-setup/PersonnelRegister';

export function FarmPage() {
  const { setup, setSetup, error, setError, isLoading } = useFarmSetup();
  const [isSaving, setIsSaving] = useState(false);

  if (isLoading) return <LoadingState label="Loading your farm record" />;
  if (!setup) return <ValidationError title="Farm record unavailable" message={error} persistent />;

  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const createFarm = async (event) => {
    event.preventDefault();
    const form = event.currentTarget;
    const data = new FormData(form);
    setError('');
    setIsSaving(true);

    try {
      const result = await farmSetupClient.farm(new CreateGrowerFarmRequest({
        growerDisplayName: String(data.get('growerDisplayName')).trim(),
        growerPhone: optionalValue(data.get('growerPhone')),
        farmCode: String(data.get('farmCode')).trim(),
        farmName: String(data.get('farmName')).trim(),
        address: String(data.get('address')).trim(),
        location: String(data.get('location')).trim(),
        tenure: String(data.get('tenure')).trim(),
        declaredHectares: Number(data.get('declaredHectares')),
        irrigationContext: String(data.get('irrigationContext')).trim(),
      }));
      setSetup(result);
    } catch (requestError) {
      setError(getApiError(requestError));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Farm setup"
        title={setup.isConfigured ? 'Your farm record' : 'Create your farm'}
        description={setup.isConfigured
          ? 'The grower and farm details below define your Cane360 working boundary.'
          : 'Start with the grower and operating details used across field records, work, and reporting.'}
      >
        {setup.isConfigured && <Link className="primary-action" to="/fields">Add a field <ArrowRight size={17} /></Link>}
      </PageHeader>

      <FarmSetupProgress setup={setup} />
      <ValidationError message={error} />

      {setup.isConfigured ? <><FarmSummary setup={setup} /><PersonnelRegister /></> : (
        <form className="setup-form record-panel" onSubmit={createFarm}>
          <header className="form-section-heading">
            <span className="form-section-icon" aria-hidden="true"><Tractor size={19} /></span>
            <div><span className="eyebrow">Step 1 of 3</span><h2>Grower and farm details</h2></div>
          </header>

          <fieldset className="form-grid">
            <label>Grower name<input name="growerDisplayName" autoComplete="name" maxLength={120} required /></label>
            <label>Phone number <small>Optional</small><input name="growerPhone" type="tel" autoComplete="tel" maxLength={30} placeholder="+263 77 123 4567" /></label>
            <label>Farm code<input name="farmCode" maxLength={20} pattern="[A-Za-z0-9][A-Za-z0-9_-]*" placeholder="e.g. GREEN-01" required /></label>
            <label>Farm name<input name="farmName" maxLength={120} placeholder="e.g. Green Valley Farm" required /></label>
            <label>Location<input name="location" maxLength={120} placeholder="e.g. Triangle" required /></label>
            <label>Tenure<select name="tenure" required defaultValue=""><option value="" disabled>Select tenure</option><option>Owned</option><option>Leasehold</option><option>Outgrower agreement</option><option>Communal land</option><option>Other</option></select></label>
            <label className="is-wide">Physical address<textarea name="address" maxLength={240} rows={3} required /></label>
            <label>Declared farm area (ha)<input name="declaredHectares" type="number" min="0.01" max="100000" step="0.01" inputMode="decimal" required /></label>
            <label className="is-wide">Irrigation context<textarea name="irrigationContext" maxLength={160} rows={3} placeholder="Describe the main source and delivery method" required /></label>
          </fieldset>

          <footer className="form-actions">
            <p>This creates one grower workspace, active farm, membership, and default store together.</p>
            <button type="submit" disabled={isSaving}>{isSaving ? 'Creating farm…' : 'Create farm'}</button>
          </footer>
        </form>
      )}
    </div>
  );
}

/** @param {FormDataEntryValue | null} value */
function optionalValue(value) {
  const text = String(value ?? '').trim();
  return text || undefined;
}
