import { useEffect, useState, type FormEvent } from 'react';
import { Eye, LandPlot, Plus, Sprout } from 'lucide-react';
import { Link } from 'react-router-dom';
import { CreateFieldRequest, type CropCycleCollectionDto, type FarmSetupDto } from '../../web-api-client';
import { CropCycleForm } from '../crop-cycles/CropCycleForm';
import { cropCyclesClient } from '../crop-cycles/cropCycleApi';
import { CropCycleRegister } from '../crop-cycles/CropCycleRegister';
import { EmptyState } from '../EmptyState';
import { FieldRecord } from '../farm-setup/FieldRecord';
import { FarmSetupProgress } from '../farm-setup/FarmSetupProgress';
import { farmSetupClient, getApiError, useFarmSetup } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';
import { LineProfileForm } from '../farm-setup/LineProfileForm';

export function FieldsPage() {
  const { setup, setSetup, error, setError, isLoading } = useFarmSetup();
  const [isAddingField, setIsAddingField] = useState(false);
  const [activeCycleField, setActiveCycleField] = useState<string | null>(null);
  const [cycleCollections, setCycleCollections] = useState<CropCycleCollectionDto[]>([]);
  const [cycleFilter, setCycleFilter] = useState('all');
  const [loadedFieldKey, setLoadedFieldKey] = useState('');
  const fieldIds = (setup?.farm?.fields ?? []).map((field) => field.id);
  const fieldKey = fieldIds.join(',');
  const areCyclesLoading = Boolean(fieldKey) && loadedFieldKey !== fieldKey;

  useEffect(() => {
    let isCurrent = true;
    if (!fieldKey) {
      return () => { isCurrent = false; };
    }

    Promise.all(fieldKey.split(',').map((fieldId) => cropCyclesClient.cropCyclesGET(fieldId)))
      .then((collections) => { if (isCurrent) setCycleCollections(collections); })
      .catch((requestError) => { if (isCurrent) setError(getApiError(requestError)); })
      .finally(() => { if (isCurrent) setLoadedFieldKey(fieldKey); });

    return () => { isCurrent = false; };
  }, [fieldKey, setError]);

  if (isLoading) return <LoadingState label="Loading fields and crop cycles" />;
  if (!setup) return <ValidationError title="Field records unavailable" message={error} persistent />;

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

  const reloadFieldCycles = async (fieldId: string) => {
    try {
      const collection = await cropCyclesClient.cropCyclesGET(fieldId);
      setCycleCollections((current) => [...current.filter((item) => item.field.id !== fieldId), collection]);
    } catch (requestError) {
      setError(getApiError(requestError));
    }
  };

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Crop records" title="Fields and crop cycles" description={`Manage field plans, current crops and chronological history on ${setup.farm?.name}.`}>
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
            <p>Each field keeps its own current crop and full cycle register.</p>
          </div>
          <div className="field-record-list">
            {fields.map((field) => (
              <FieldRecord key={field.id} field={field}>
                <LineProfileForm fieldId={field.id} />
                <div className="field-cycle-actions">
                  {field.currentCropCycle && <Link className="secondary-action" to={`/fields/${field.id}/crop-cycles/${field.currentCropCycle.id}`}><Eye size={16} /> View current cycle</Link>}
                  {activeCycleField !== field.id && <button type="button" className="secondary-action" onClick={() => setActiveCycleField(field.id)}><Sprout size={17} /> Plan crop cycle</button>}
                </div>
                {activeCycleField === field.id && (
                  <CropCycleForm
                    field={field}
                    onSaved={async () => { setActiveCycleField(null); await reloadFieldCycles(field.id); }}
                    onCancel={() => setActiveCycleField(null)}
                  />
                )}
              </FieldRecord>
            ))}
          </div>
        </section>
      )}

      {fields.length > 0 && (areCyclesLoading
        ? <LoadingState label="Loading crop-cycle register" />
        : <CropCycleRegister collections={cycleCollections} filter={cycleFilter} onFilterChange={setCycleFilter} />)}
    </div>
  );
}

interface FieldFormProps {
  onSaved: (setup: FarmSetupDto, fieldId: string) => void;
  onCancel?: () => void;
  onError: (message: string) => void;
}

function FieldForm({ onSaved, onCancel, onError }: FieldFormProps) {
  const [isSaving, setIsSaving] = useState(false);
  const [reportingSource, setReportingSource] = useState('Declared');

  const saveField = async (event: FormEvent<HTMLFormElement>) => {
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
        <div><span className="eyebrow">Field setup</span><h2>Add a field</h2><p>Record both measured areas, then choose the source Cane360 should report.</p></div>
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

function optionalValue(value: string | File | null): string | undefined {
  const text = String(value ?? '').trim();
  return text || undefined;
}

function optionalNumber(value: string | File | null): number | undefined {
  const text = String(value ?? '').trim();
  return text ? Number(text) : undefined;
}
