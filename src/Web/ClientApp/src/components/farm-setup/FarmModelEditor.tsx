import {useEffect, useState} from 'react';
import type {FarmModelDto} from '../../web-api-client';
import {SaveFarmModelCommand} from '../../web-api-client';
import {farmSetupClient, getApiError} from './farmSetupApi';

export function FarmModelEditor({selected, onSelected}: {selected: string; onSelected: (value: string) => void}) {
    const [models, setModels] = useState<FarmModelDto[]>([]);
    const [error, setError] = useState('');
    const [busy, setBusy] = useState(false);
    useEffect(() => {
        let current = true;
        farmSetupClient.listFarmModels().then(items => {if (current) setModels(items);})
            .catch(error => {if (current) setError(getApiError(error));});
        return () => {current = false;};
    }, []);
    const save = async (code: string, name: string, model?: FarmModelDto) => {
        if (busy) return;
        setBusy(true); setError('');
        try {
            await farmSetupClient.saveFarmModel(new SaveFarmModelCommand({code, name, active: model ? !model.active : true,
                id: model?.id, expectedVersion: model?.version}));
            setModels(await farmSetupClient.listFarmModels());
        } catch (error) {setError(getApiError(error));} finally {setBusy(false);}
    };
    return <fieldset className="form-grid"><legend>Farm Model</legend>
        <label>Category<select value={selected} onChange={event => onSelected(event.target.value)}>
            <option value="">Not assigned</option>
            {models.filter(model => model.active || model.id === selected).map(model =>
                <option key={model.id} value={model.id}>{model.name}{model.active ? '' : ' (inactive)'}</option>)}
        </select></label>
        <details className="is-wide"><summary>Manage Farm Model categories</summary>
            <div className="form-grid">
                <label>New category code<input name="newModelCode" maxLength={24} pattern="[A-Za-z0-9][A-Za-z0-9_\-]*"/></label>
                <label>New category name<input name="newModelName" maxLength={100}/></label>
                <button type="button" disabled={busy} onClick={event => {
                    const data = new FormData(event.currentTarget.form!);
                    void save(String(data.get('newModelCode') ?? '').trim(), String(data.get('newModelName') ?? '').trim());
                }}>Add category</button>
            </div>
            {models.map(model => <div className="farm-model-row" key={model.id}>
                <span>{model.code} · {model.name} · {model.active ? 'Active' : 'Inactive'}</span>
                <button type="button" className="secondary" disabled={busy} onClick={() => void save(model.code, model.name, model)}>
                    {model.active ? 'Deactivate' : 'Activate'}</button>
            </div>)}
        </details>
        {error && <p className="form-error is-wide" role="alert">{error}</p>}
    </fieldset>;
}
