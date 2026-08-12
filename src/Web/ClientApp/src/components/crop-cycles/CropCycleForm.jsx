import { useEffect, useState } from 'react';
import { Sprout } from 'lucide-react';
import { createCycle, createVariety, cropVarietiesClient, localDate } from './cropCycleApi';
import { getApiError } from '../farm-setup/farmSetupApi';
import { ValidationError } from '../ValidationError';

/** @param {{ field: import('../../web-api-client').FieldDto, onSaved: (details: import('../../web-api-client').CropCycleDetailsDto) => void, onCancel: () => void }} props */
export function CropCycleForm({ field, onSaved, onCancel }) {
  const [isSaving, setIsSaving] = useState(false);
  const [cycleType, setCycleType] = useState('PlantCane');
  const [varieties, setVarieties] = useState(/** @type {import('../../web-api-client').CropVarietyDto[]} */ ([]));
  const [isAddingVariety, setIsAddingVariety] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    cropVarietiesClient.cropVarietiesAll()
      .then(setVarieties)
      .catch((requestError) => setError(getApiError(requestError)));
  }, []);

  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const saveCycle = async (event) => {
    event.preventDefault();
    const data = new FormData(event.currentTarget);
    setError('');
    setIsSaving(true);

    try {
      let cropVarietyId = String(data.get('cropVarietyId') ?? '');
      if (isAddingVariety) {
        const code = String(data.get('varietyCode')).trim();
        const variety = await createVariety(code, String(data.get('varietyName')).trim());
        cropVarietyId = variety.id;
      }

      const details = await createCycle(field.id, {
        cycleType,
        ratoonNumber: cycleType === 'Ratoon' ? Number(data.get('ratoonNumber')) : undefined,
        cropVarietyId,
        startDate: localDate(data.get('startDate')),
        expectedHarvestStart: localDate(data.get('expectedHarvestStart')),
        expectedHarvestEnd: localDate(data.get('expectedHarvestEnd')),
        expectedYieldTonnes: Number(data.get('expectedYieldTonnes')),
      });
      onSaved(details);
    } catch (requestError) {
      setError(getApiError(requestError));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <form className="cycle-form record-panel" onSubmit={saveCycle}>
      <header className="form-section-heading">
        <span className="form-section-icon" aria-hidden="true"><Sprout size={19} /></span>
        <div><span className="eyebrow">Crop-cycle draft</span><h2>Plan {field.name}&apos;s next crop</h2><p>Save the agronomic plan first, then activate it when the field is ready.</p></div>
      </header>
      <ValidationError message={error} />
      <fieldset className="form-grid">
        <label>Crop type<select name="cycleType" value={cycleType} onChange={(event) => setCycleType(event.target.value)}><option value="PlantCane">Plant cane</option><option value="Ratoon">Ratoon</option></select></label>
        {cycleType === 'Ratoon' && <label>Ratoon number<input name="ratoonNumber" type="number" min="1" max="20" step="1" inputMode="numeric" required /></label>}
        {!isAddingVariety && varieties.length > 0 && <label>Variety<select name="cropVarietyId" required defaultValue=""><option value="" disabled>Select a variety</option>{varieties.map((variety) => <option key={variety.id} value={variety.id}>{variety.code} · {variety.name}</option>)}</select></label>}
        {isAddingVariety && <><label>Variety code<input name="varietyCode" maxLength={20} placeholder="e.g. N14" required /></label><label>Variety name<input name="varietyName" maxLength={80} placeholder="e.g. N14" required /></label></>}
        <label className="inline-choice"><input type="checkbox" checked={isAddingVariety} onChange={(event) => setIsAddingVariety(event.target.checked)} /> Add a new variety</label>
        <label>Cycle start date<input name="startDate" type="date" required /></label>
        <label>Expected harvest from<input name="expectedHarvestStart" type="date" required /></label>
        <label>Expected harvest to<input name="expectedHarvestEnd" type="date" required /></label>
        <label>Expected yield (tonnes)<input name="expectedYieldTonnes" type="number" min="0.01" max="1000000" step="0.001" inputMode="decimal" required /></label>
      </fieldset>
      {varieties.length === 0 && !isAddingVariety && <p className="form-guidance">No crop varieties exist yet. Choose “Add a new variety” to create the first one.</p>}
      <footer className="form-actions"><p>A Draft does not accept operational entries and does not become the field&apos;s current crop until activated.</p><div><button type="button" className="secondary outline" onClick={onCancel}>Cancel</button><button type="submit" disabled={isSaving || (varieties.length === 0 && !isAddingVariety)}>{isSaving ? 'Saving draft…' : 'Save draft'}</button></div></footer>
    </form>
  );
}
