import {type FormEvent, useState} from 'react';
import {LandPlot} from 'lucide-react';
import {CreateFieldRequest, type FarmSetupDto} from '../../web-api-client';
import {farmSetupClient, getApiError} from './farmSetupApi';

interface FieldFormProps {
    onSaved: (setup: FarmSetupDto) => void;
    onCancel?: () => void;
    onError: (message: string) => void;
}

export function FieldForm({onSaved, onCancel, onError}: FieldFormProps) {
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
            onSaved(result);
        } catch (requestError) {
            onError(getApiError(requestError));
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <form className="setup-form record-panel" onSubmit={saveField}>
            <header className="form-section-heading">
                <span className="form-section-icon" aria-hidden="true"><LandPlot size={19}/></span>
                <div><span className="eyebrow">Field setup</span><h2>Add a field</h2><p>Record the physical field. Set up its crop separately after creation.</p></div>
            </header>
            <fieldset className="form-grid">
                <label>Field code<input name="code" maxLength={20} pattern="[A-Za-z0-9][A-Za-z0-9_-]*"
                                        placeholder="e.g. A-01" required/></label>
                <label>Field name<input name="name" maxLength={120} placeholder="e.g. North block" required/></label>
                <label>Declared area (ha)<input name="declaredHectares" type="number" min="0.01" max="100000"
                                                step="0.01" inputMode="decimal" required/></label>
                <label>Mapped area (ha) <small>{reportingSource === 'Mapped' ? 'Required' : 'Optional'}</small><input
                    name="mappedHectares" type="number" min="0.01" max="100000" step="0.01" inputMode="decimal"
                    required={reportingSource === 'Mapped'}/></label>
                <label>Reporting area source<select name="reportingAreaSource" value={reportingSource}
                                                    onChange={(event) => setReportingSource(event.target.value)}>
                    <option value="Declared">Declared area</option>
                    <option value="Mapped">Mapped area</option>
                </select></label>
                <label>Irrigation method<input name="irrigationMethod" maxLength={100} placeholder="e.g. Furrow"
                                               required/></label>
                <label className="is-wide">Soil notes <small>Optional</small><textarea name="soilNotes" maxLength={500}
                                                                                       rows={2}/></label>
            </fieldset>
            <footer className="form-actions"><p>Field codes must be unique within this farm.</p>
                <div>{onCancel &&
                    <button type="button" className="secondary outline" onClick={onCancel}>Cancel</button>}
                    <button type="submit" disabled={isSaving}>{isSaving ? 'Adding field…' : 'Add field'}</button>
                </div>
            </footer>
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
