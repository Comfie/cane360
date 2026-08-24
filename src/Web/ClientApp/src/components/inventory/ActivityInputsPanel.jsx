import { useEffect, useState } from 'react';
import { PackagePlus, ShieldAlert } from 'lucide-react';
import { getApiError } from '../apiError';
import { createInputRequest, loadInputControls, submitInputRequest } from './inputControlApi';
import { quantity, usd } from './inventoryView';

/** @param {{activityId: string, activityStatus: string, onError: (message: string) => void}} props */
export function ActivityInputsPanel({ activityId, activityStatus, onError }) {
  const [workspace, setWorkspace] = useState(/** @type {import('../../web-api-client').InputControlWorkspaceDto | null} */ (null));
  const [showRequest, setShowRequest] = useState(false);
  const [saving, setSaving] = useState(false);
  const reload = () => loadInputControls(activityId).then(setWorkspace);
  useEffect(() => { let current = true; loadInputControls(activityId).then((value) => { if (current) setWorkspace(value); }).catch((error) => onError(getApiError(error))); return () => { current = false; }; }, [activityId, onError]);
  if (!workspace) return <section className="activity-inputs-panel"><span>Loading input controls…</span></section>;
  const terminal = ['Completed', 'Closed', 'Cancelled'].includes(activityStatus);
  const requests = workspace.requests;
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => {
    event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); setSaving(true); onError('');
    try {
      const requestId = await createInputRequest(activityId, [{ inventoryItemId: String(data.get('inventoryItemId')), requestedQuantity: Number(data.get('requestedQuantity')) }]);
      await reload();
      const created = (await loadInputControls(activityId)).requests.find((request) => request.id === requestId);
      if (created) await submitInputRequest(created.id, created.version);
      setShowRequest(false); await reload();
    } catch (error) { onError(getApiError(error)); } finally { setSaving(false); }
  };
  return <section className="activity-inputs-panel">
    <header><div><span className="eyebrow">Controlled inputs</span><h3>Inputs</h3></div><button type="button" className="text-action" disabled={terminal} onClick={() => setShowRequest((value) => !value)}><PackagePlus size={15} /> Request input</button></header>
    {terminal && <p className="context-note"><ShieldAlert size={14} /> Terminal work cannot accept new input requests or issues.</p>}
    {showRequest && <form className="input-request-inline" onSubmit={save}><label>Inventory item<select name="inventoryItemId" required defaultValue=""><option value="">Select item with a rule</option>{workspace.items.filter((item) => item.status === 'Active').map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label><label>Requested quantity<input name="requestedQuantity" type="number" min="0.000001" step="0.000001" required /></label><button disabled={saving}>{saving ? 'Submitting…' : 'Create and submit'}</button></form>}
    {requests.length === 0 ? <p className="context-note">No inputs requested for this activity.</p> : <div className="activity-input-list">{requests.map((request) => <article key={request.id}><span><strong>{request.status}</strong><small>{request.fieldName} · {request.activityTypeName}</small></span>{request.lines.map((line) => <span key={line.id}><b>{line.itemCode}</b><small>{quantity(line.requestedQuantity, line.unitCode)} requested · {quantity(line.remainingQuantity, line.unitCode)} remaining · {line.estimatedValueUsdSnapshot == null ? 'Cost not available' : usd(line.estimatedValueUsdSnapshot)}</small></span>)}</article>)}</div>}
  </section>;
}
