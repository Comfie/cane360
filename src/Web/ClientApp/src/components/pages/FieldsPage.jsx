import { useState } from 'react';
import { LandPlot, Plus, Sprout } from 'lucide-react';
import { Link } from 'react-router-dom';
import { CreateFieldRequest, OpenCropCycleRequest } from '../../web-api-client';
import { EmptyState } from '../EmptyState';
import { FieldRecord } from '../farm-setup/FieldRecord';
import { FarmSetupProgress } from '../farm-setup/FarmSetupProgress';
import { farmSetupClient, getApiError, useFarmSetup } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';

export function FieldsPage() {
  const { setup, setSetup, error, setError, isLoading } = useFarmSetup();
  const [isAddingField, setIsAddingField] = useState(false);
  const [activeCycleField, setActiveCycleField] = useState(/** @type {string | null} */ (null));

  if (isLoading) return <LoadingState label="Loading fields and crop cycles" />;
  if (!setup) return <ValidationError title="Field records unavailable" message={error} />;

  if (!setup.isConfigured) {
    return (
      <div className="page-stack">
        <PageHeader eyebrow="Crop records" title="Fields and crop cycles" description="Create your farm before adding its fields." />
        <FarmSetupProgress setup={setup} />
        <EmptyState title="Your farm record comes first" description="Fields belong to your active farm and use its grower workspace for secure data isolation." nextStep="Create the farm, then return here to add its first field." action={<Link className="primary-action" to="/farm">Create farm</Link>} />
      </div>
    );
  }

  const fields = setup.farm?.fields ?? [];
  const showFieldForm = fields.length === 0 || isAddingField;

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Crop records" title="Fields and crop cycles" description={`Manage the reporting fields and current crop on ${setup.farm?.name}.`}>
        {!showFieldForm && <button type="button" className="primary-action" onClick={() => setIsAddingField(true)}><Plus size={17} /> Add field</button>}
      </PageHeader>

      <FarmSetupProgress setup={setup} />
      <ValidationError message={error} />

      {showFieldForm && (
        <FieldForm
          onSaved={(result, fieldId) => {
            setSetup(result);
            setIsAddingField(false);
            setActiveCycleField(fieldId);
          }}
          onCancel={fields.length > 0 ? () => setIsAddingField(false) : undefined}
          onError={setError}
        />
      )}

      {fields.length > 0 && (
        <section aria-labelledby="field-list-title">
          <div className="section-heading">
            <div><span className="eyebrow">Farm fields</span><h2 id="field-list-title">{fields.length} {fields.length === 1 ? 'field' : 'fields'} recorded</h2></div>
            <p>Reporting hectares remain distinct from declared and mapped measurements.</p>
          </div>
          <div className="field-record-list">
            {fields.map((field) => (
              <FieldRecord key={field.id} field={field}>
                {!field.currentCropCycle && activeCycleField !== field.id && (
                  <button type="button" className="secondary-action" onClick={() => setActiveCycleField(field.id)}><Sprout size={17} /> Open current crop cycle</button>
                )}
                {!field.currentCropCycle && activeCycleField === field.id && (
                  <CropCycleForm
                    field={field}
                    onSaved={(result) => { setSetup(result); setActiveCycleField(null); }}
                    onCancel={() => setActiveCycleField(null)}
                    onError={setError}
                  />
                )}
              </FieldRecord>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

/** @param {{ onSaved: (setup: import('../../web-api-client').FarmSetupDto, fieldId: string) => void, onCancel?: () => void, onError: (message: string) => void }} props */
function FieldForm({ onSaved, onCancel, onError }) {
  const [isSaving, setIsSaving] = useState(false);
  const [reportingSource, setReportingSource] = useState('Declared');

  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveField = async (event) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    const fieldCode = String(data.get('code')).trim();
    onError('');
    setIsSaving(true);

    try {
      const result = await farmSetupClient.fields(new CreateFieldRequest({
        code: fieldCode,
        name: String(data.get('name')).trim(),
        declaredHectares: Number(data.get('declaredHectares')),
        mappedHectares: reportingSource === 'Mapped' ? Number(data.get('mappedHectares')) : optionalNumber(data.get('mappedHectares')),
        reportingAreaSource: reportingSource,
        irrigationMethod: String(data.get('irrigationMethod')).trim(),
        soilNotes: optionalValue(data.get('soilNotes')),
      }));
      const resultFields = result.farm?.fields ?? [];
      const newField = resultFields.find((field) => field.code.toUpperCase() === fieldCode.toUpperCase());
      onSaved(result, newField?.id ?? '');
    } catch (requestError) {
      onError(getApiError(requestError));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="setup-form record-panel" onSubmit={saveField}>
      <header className="form-section-heading">
        <span className="form-section-icon" aria-hidden="true"><LandPlot size={19} /></span>
        <div><span className="eyebrow">Step 2 of 3</span><h2>Add a field</h2><p>Record both measured areas, then choose the source Cane360 should report.</p></div>
      </header>
      <fieldset className="form-grid">
        <label>Field code<input name="code" maxLength={20} pattern="[A-Za-z0-9][A-Za-z0-9_-]*" placeholder="e.g. A-01" required /></label>
        <label>Field name<input name="name" maxLength={120} placeholder="e.g. North block" required /></label>
        <label>Declared area (ha)<input name="declaredHectares" type="number" min="0.01" max="100000" step="0.01" inputMode="decimal" required /></label>
        <label>Mapped area (ha) <small>{reportingSource === 'Mapped' ? 'Required' : 'Optional'}</small><input name="mappedHectares" type="number" min="0.01" max="100000" step="0.01" inputMode="decimal" required={reportingSource === 'Mapped'} /></label>
        <label>Reporting area source<select name="reportingAreaSource" value={reportingSource} onChange={(event) => setReportingSource(event.target.value)}><option value="Declared">Declared area</option><option value="Mapped">Mapped area</option></select></label>
        <label>Irrigation method<input name="irrigationMethod" maxLength={100} placeholder="e.g. Furrow" required /></label>
        <label className="is-wide">Soil notes <small>Optional</small><textarea name="soilNotes" maxLength={500} rows={3} /></label>
      </fieldset>
      <footer className="form-actions"><p>Field codes must be unique within this farm.</p><div>{onCancel && <button type="button" className="secondary outline" onClick={onCancel}>Cancel</button>}<button type="submit" disabled={isSaving}>{isSaving ? 'Adding field…' : 'Add field'}</button></div></footer>
    </form>
  );
}

/** @param {{ field: import('../../web-api-client').FieldDto, onSaved: (setup: import('../../web-api-client').FarmSetupDto) => void, onCancel: () => void, onError: (message: string) => void }} props */
function CropCycleForm({ field, onSaved, onCancel, onError }) {
  const [isSaving, setIsSaving] = useState(false);
  const [cycleType, setCycleType] = useState('PlantCane');

  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveCycle = async (event) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    onError('');
    setIsSaving(true);

    try {
      const result = await farmSetupClient.cropCycles(field.id, new OpenCropCycleRequest({
        cycleType,
        ratoonNumber: cycleType === 'Ratoon' ? Number(data.get('ratoonNumber')) : undefined,
        variety: String(data.get('variety')).trim(),
        startDate: localDate(data.get('startDate')),
        expectedHarvestStart: localDate(data.get('expectedHarvestStart')),
        expectedHarvestEnd: localDate(data.get('expectedHarvestEnd')),
        expectedYieldTonnes: Number(data.get('expectedYieldTonnes')),
      }));
      onSaved(result);
    } catch (requestError) {
      onError(getApiError(requestError));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="cycle-form" onSubmit={saveCycle}>
      <header><span className="eyebrow">Step 3 of 3</span><h4>Open {field.name}&apos;s current crop cycle</h4></header>
      <fieldset className="form-grid">
        <label>Crop type<select name="cycleType" value={cycleType} onChange={(event) => setCycleType(event.target.value)}><option value="PlantCane">Plant cane</option><option value="Ratoon">Ratoon</option></select></label>
        {cycleType === 'Ratoon' && <label>Ratoon number<input name="ratoonNumber" type="number" min="1" max="20" step="1" inputMode="numeric" required /></label>}
        <label>Variety<input name="variety" maxLength={80} placeholder="e.g. N14" required /></label>
        <label>Cycle start date<input name="startDate" type="date" required /></label>
        <label>Expected harvest from<input name="expectedHarvestStart" type="date" required /></label>
        <label>Expected harvest to<input name="expectedHarvestEnd" type="date" required /></label>
        <label>Expected yield (tonnes)<input name="expectedYieldTonnes" type="number" min="0.01" max="10000000" step="0.01" inputMode="decimal" required /></label>
      </fieldset>
      <footer className="form-actions"><p>Only one current crop cycle can be open for a field.</p><div><button type="button" className="secondary outline" onClick={onCancel}>Cancel</button><button type="submit" disabled={isSaving}>{isSaving ? 'Opening cycle…' : 'Open crop cycle'}</button></div></footer>
    </form>
  );
}

/** @param {FormDataEntryValue | null} value */
function optionalValue(value) {
  const text = String(value ?? '').trim();
  return text || undefined;
}

/** @param {FormDataEntryValue | null} value */
function optionalNumber(value) {
  const text = String(value ?? '').trim();
  return text ? Number(text) : undefined;
}

/** @param {FormDataEntryValue | null} value */
function localDate(value) {
  return new Date(`${String(value)}T00:00:00`);
}
