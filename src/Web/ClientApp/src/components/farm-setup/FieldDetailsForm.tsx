import {type FormEvent, useState} from 'react';
import {UpdateFieldDetailsRequest, type FarmSetupDto, type FieldDto} from '../../web-api-client';
import {farmSetupClient, getApiError} from './farmSetupApi';
import {ValidationError} from '../ValidationError';

export function FieldDetailsForm({field, onSaved}: {field: FieldDto; onSaved: (result: FarmSetupDto) => void}) {
    const [editing, setEditing] = useState(false);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState('');
    if (!editing) return <button className="secondary-action" type="button" onClick={() => setEditing(true)}>Edit field details</button>;
    const save = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        setBusy(true);
        setError('');
        try {
            onSaved(await farmSetupClient.updateFieldDetails(field.id, new UpdateFieldDetailsRequest({
                name: String(data.get('name')).trim(), irrigationMethod: String(data.get('irrigationMethod')).trim(),
                soilNotes: String(data.get('soilNotes') ?? '').trim() || undefined,
            })));
            setEditing(false);
        } catch (requestError) {setError(getApiError(requestError));}
        finally {setBusy(false);}
    };
    return <form className="setup-form record-panel" onSubmit={save}>
        <h3>Field details · {field.code}</h3><ValidationError message={error}/>
        <fieldset className="form-grid" disabled={busy}>
            <label>Field name<input name="name" maxLength={120} defaultValue={field.name} required/></label>
            <label>Irrigation method<input name="irrigationMethod" maxLength={100} defaultValue={field.irrigationMethod} required/></label>
            <label className="is-wide">Soil notes <small>Optional</small><textarea name="soilNotes" maxLength={500} rows={2} defaultValue={field.soilNotes}/></label>
        </fieldset>
        <footer className="form-actions"><button type="button" className="secondary outline" disabled={busy} onClick={() => setEditing(false)}>Cancel</button>
            <button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save field details'}</button></footer>
    </form>;
}
