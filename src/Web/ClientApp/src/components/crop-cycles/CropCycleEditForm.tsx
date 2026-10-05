import {type FormEvent, useState} from 'react';
import {UpdateActualYieldRequest, UpdateCropCyclePlanRequest, type CropCycleDetailsDto} from '../../web-api-client';
import {cropCyclesClient, localDate} from './cropCycleApi';
import {CropPlanFields} from './CropPlanFields';
import {getApiError} from '../farm-setup/farmSetupApi';
import {ValidationError} from '../ValidationError';

export function CropCycleEditForm({details, onSaved}: {details: CropCycleDetailsDto; onSaved: (details: CropCycleDetailsDto) => void}) {
    const [editing, setEditing] = useState(false);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    const cycle = details.cropCycle;
    if (cycle.status !== 'Draft' && cycle.status !== 'Harvested') return null;
    if (!editing) return <button className="secondary-action" type="button" onClick={() => setEditing(true)}>
        {cycle.status === 'Draft' ? 'Edit crop plan' : 'Edit actual yield'}</button>;

    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        setBusy(true);
        setError('');
        try {
            const result = cycle.status === 'Draft'
                ? await cropCyclesClient.updateCropCyclePlan(details.field.id, cycle.id, new UpdateCropCyclePlanRequest({
                    expectedVersion: cycle.version,
                    startDate: localDate(data.get('startDate')),
                    expectedHarvestStart: data.get('expectedHarvestStart') ? localDate(data.get('expectedHarvestStart')) : undefined,
                    expectedHarvestEnd: data.get('expectedHarvestEnd') ? localDate(data.get('expectedHarvestEnd')) : undefined,
                    expectedYieldTonnes: Number(data.get('expectedYieldTonnes')),
                }))
                : await cropCyclesClient.updateCropCycleActualYield(details.field.id, cycle.id, new UpdateActualYieldRequest({
                    expectedVersion: cycle.version, actualTonnes: Number(data.get('actualTonnes')),
                }));
            onSaved(result);
            setEditing(false);
        } catch (requestError) {setError(getApiError(requestError));}
        finally {setBusy(false);}
    };

    return <form className="record-panel setup-form" onSubmit={save}>
        <h2>{cycle.status === 'Draft' ? 'Edit crop plan' : 'Edit actual yield'}</h2>
        <ValidationError message={error}/>
        <fieldset className="form-grid" disabled={busy}>
            {cycle.status === 'Draft' ? <CropPlanFields fieldId={details.field.id} startDate={cycle.startDate}
                harvestStart={cycle.expectedHarvestStart} harvestEnd={cycle.expectedHarvestEnd} expectedYield={cycle.expectedYieldTonnes}/>
                : <label>Actual yield (tonnes)<input name="actualTonnes" type="number" min="0.001" max="1000000" step="0.001"
                    inputMode="decimal" defaultValue={cycle.harvestResult?.actualTonnes} required/></label>}
        </fieldset>
        {cycle.status === 'Harvested' && <p>Enter the field’s actual yield manually. Mill tickets and reconciled tonnage remain separate.</p>}
        <footer className="form-actions"><button type="button" className="secondary outline" disabled={busy} onClick={() => setEditing(false)}>Cancel</button>
            <button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save changes'}</button></footer>
    </form>;
}
