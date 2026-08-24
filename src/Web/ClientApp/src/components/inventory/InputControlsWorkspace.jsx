import { useEffect, useState } from 'react';
import { BadgeCheck, CircleDollarSign, PackageMinus, ShieldAlert } from 'lucide-react';
import { getApiError } from '../apiError';
import { DatePicker } from '../DatePicker';
import {
  createApplicationRule,
  createManagerInvitation,
  createStockIssue,
  decideInputRequest,
  loadInputControls,
  postStockIssue,
  requestIssueCorrection,
  reverseStockIssue,
} from './inputControlApi';
import { harareToday, quantity } from './inventoryView';
import { approvalLane, estimatedCostLabel } from './inputControlView';

/** @param {{onError: (message: string) => void}} props */
export function InputControlsWorkspace({ onError }) {
  const [workspace, setWorkspace] = useState(/** @type {import('../../web-api-client').InputControlWorkspaceDto | null} */ (null));
  const [mode, setMode] = useState('queue');
  const [working, setWorking] = useState('');
  const [issuedRequest, setIssuedRequest] = useState(/** @type {import('../../web-api-client').InputRequestDto | null} */ (null));
  const [shownToken, setShownToken] = useState('');
  const reload = async () => setWorkspace(await loadInputControls(undefined));
  useEffect(() => { let current = true; loadInputControls(undefined).then((value) => { if (current) setWorkspace(value); }).catch((error) => onError(getApiError(error))); return () => { current = false; }; }, [onError]);
  if (!workspace) return <section className="record-panel input-controls-loading">Loading input controls…</section>;
  const pending = workspace.requests.filter((request) => request.status === 'PendingApproval');
  const approved = workspace.requests.filter((request) => ['Approved', 'PartiallyIssued'].includes(request.status));
  /** @param {import('../../web-api-client').InputRequestDto} request @param {'Approved' | 'Rejected'} outcome */
  const decide = async (request, outcome) => {
    const reason = outcome === 'Rejected' ? globalThis.prompt('Rejection reason:')?.trim() : undefined;
    if (outcome === 'Rejected' && !reason) return;
    setWorking(request.id); onError('');
    try { await decideInputRequest(request.id, request.version, outcome, reason); await reload(); }
    catch (error) { onError(getApiError(error)); } finally { setWorking(''); }
  };
  /** @param {import('../../web-api-client').StockIssueDto} issue @param {'post' | 'correct' | 'reverse'} action */
  const issueAction = async (issue, action) => {
    setWorking(issue.id); onError('');
    try {
      if (action === 'post') await postStockIssue(issue.id, issue.version);
      if (action === 'correct') { const reason = globalThis.prompt('Correction reason:')?.trim(); if (!reason) return; await requestIssueCorrection(issue.id, issue.version, reason); }
      if (action === 'reverse') { const reason = globalThis.prompt('Grower-authorised reversal reason:')?.trim(); if (!reason) return; await reverseStockIssue(issue.id, issue.version, reason); }
      await reload();
    } catch (error) { onError(getApiError(error)); } finally { setWorking(''); }
  };
  return <div className="input-controls-workspace">
    <section className="inventory-commandbar record-panel"><div className="store-identity"><span><PackageMinus size={17} /></span><div><small>Authenticated tenant context</small><strong>{workspace.session.role}{workspace.session.personName ? ` · ${workspace.session.personName}` : ''}</strong></div></div><nav className="inventory-tabs"><button aria-current={mode === 'queue'} onClick={() => setMode('queue')}>Approval queue <span>{pending.length}</span></button><button aria-current={mode === 'issues'} onClick={() => setMode('issues')}>Issues <span>{workspace.issues.length}</span></button><button aria-current={mode === 'rules'} onClick={() => setMode('rules')}>Application rules <span>{workspace.rules.length}</span></button><button aria-current={mode === 'access'} onClick={() => setMode('access')}>Manager access</button></nav></section>
    {mode === 'queue' && <section className="record-panel input-approval-queue"><header className="ledger-title"><div><span className="eyebrow">Exact-version decisions</span><h2>Approval queue</h2></div><small>Manager eligible and Grower required remain distinct</small></header>{pending.length === 0 ? <p className="context-note">No requests await approval.</p> : pending.map((request) => { const lane = approvalLane(request, workspace.session.role); return <article key={request.id}><div><strong>{request.activityTypeName} · {request.fieldName}</strong><small>{lane.label} · version {request.version}</small></div><div className="request-metrics">{request.lines.map((line) => <span key={line.id}><b>{line.itemCode}</b><small>plan {quantity(line.plannedQuantity, line.unitCode)} · request {quantity(line.requestedQuantity, line.unitCode)}</small><small>range {line.minimumQuantity}–{line.maximumQuantity} · live {quantity(line.liveAvailableQuantity, line.unitCode)}</small><small>{estimatedCostLabel(line.estimatedValueUsdSnapshot)}</small></span>)}</div><div className="row-actions"><button disabled={working === request.id || !lane.canApprove} onClick={() => decide(request, 'Approved')}><BadgeCheck size={14} /> Approve</button><button className="secondary" disabled={working === request.id} onClick={() => decide(request, 'Rejected')}>Reject</button></div></article>; })}</section>}
    {mode === 'issues' && <><section className="record-panel input-issue-register"><header className="ledger-title"><div><span className="eyebrow">Issue is not consumption</span><h2>Partial stock issues</h2></div><small>{approved.length} approved request{approved.length === 1 ? '' : 's'} available</small></header>{approved.map((request) => <article key={`request-${request.id}`}><span><strong>{request.activityTypeName} · {request.fieldName}</strong><small>{request.lines.map((line) => `${line.itemCode} ${quantity(line.remainingQuantity, line.unitCode)} remaining`).join(' · ')}</small></span><button onClick={() => setIssuedRequest(request)}>Issue remaining</button></article>)}{workspace.issues.length === 0 ? <p className="context-note">No issues recorded.</p> : workspace.issues.map((issue) => <article key={issue.id}><span><strong>{issue.status} · {issue.issueDate.toISOString().slice(0, 10)}</strong><small>{issue.lines.map((line) => `${line.itemCode} ${quantity(line.quantity, line.unitCode)}`).join(' · ')}</small></span><span className="row-actions">{issue.status === 'Draft' && <button disabled={working === issue.id} onClick={() => issueAction(issue, 'post')}>Post</button>}{issue.status === 'Posted' && <button className="secondary" disabled={working === issue.id} onClick={() => issueAction(issue, 'correct')}>Initiate correction</button>}{['Posted', 'CorrectionRequested'].includes(issue.status) && workspace.session.role === 'Grower' && <button className="text-action" disabled={working === issue.id} onClick={() => issueAction(issue, 'reverse')}>Reverse</button>}</span></article>)}</section>{issuedRequest && <IssueForm workspace={workspace} request={issuedRequest} onClose={() => setIssuedRequest(null)} onSaved={async () => { setIssuedRequest(null); await reload(); }} onError={onError} />}</>}
    {mode === 'rules' && <RuleRegister workspace={workspace} onSaved={reload} onError={onError} />}
    {mode === 'access' && <ManagerAccess workspace={workspace} token={shownToken} onToken={setShownToken} onSaved={reload} onError={onError} />}
  </div>;
}

/** @param {{workspace: import('../../web-api-client').InputControlWorkspaceDto, onSaved: () => Promise<void>, onError: (message: string) => void}} props */
function RuleRegister({ workspace, onSaved, onError }) {
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => { event.preventDefault(); const form = event.currentTarget; const data = new FormData(form); const effectiveTo = String(data.get('effectiveTo')); onError(''); try { await createApplicationRule({ inventoryItemId: String(data.get('inventoryItemId')), activityTypeId: String(data.get('activityTypeId')), effectiveFrom: new Date(`${String(data.get('effectiveFrom'))}T00:00:00`), effectiveTo: effectiveTo ? new Date(`${effectiveTo}T00:00:00`) : undefined, coverageBasis: String(data.get('coverageBasis')), ratePerCoverageUnit: Number(data.get('rate')), lowerTolerancePercent: Number(data.get('lowerTolerance')), upperTolerancePercent: Number(data.get('upperTolerance')) }); form.reset(); await onSaved(); } catch (error) { onError(getApiError(error)); } };
  return <section className="record-panel rule-register"><header className="ledger-title"><div><span className="eyebrow">Effective dated</span><h2>Application-rate rules</h2></div><small>Item stock unit only · no conversions</small></header><form className="input-rule-form" onSubmit={save}><label>Item<select name="inventoryItemId" required defaultValue=""><option value="">Select</option>{workspace.items.filter((item) => item.status === 'Active').map((item) => <option key={item.id} value={item.id}>{item.code} · {item.name}</option>)}</select></label><label>Activity type<select name="activityTypeId" required defaultValue=""><option value="">Select</option>{workspace.activityTypes.filter((type) => type.status === 'Active').map((type) => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label><label>Coverage<select name="coverageBasis"><option value="FieldReportingHectares">Field hectares</option><option value="ActivityActualQuantity">Actual activity quantity</option></select></label><label>Effective from<DatePicker name="effectiveFrom" defaultValue={harareToday()} required /></label><label>Effective to<DatePicker name="effectiveTo" /></label><label>Rate<input name="rate" type="number" min="0.000001" step="0.000001" required /></label><label>Lower tolerance %<input name="lowerTolerance" type="number" min="0" step="0.000001" defaultValue="0" required /></label><label>Upper tolerance %<input name="upperTolerance" type="number" min="0" step="0.000001" defaultValue="0" required /></label><button>Add rule</button></form><div className="catalogue-list">{workspace.rules.map((rule) => { const item = workspace.items.find((candidate) => candidate.id === rule.inventoryItemId); const activity = workspace.activityTypes.find((candidate) => candidate.id === rule.activityTypeId); return <article key={rule.id}><span><strong>{item?.code} · {activity?.name}</strong><small>{rule.ratePerCoverageUnit} {rule.unitCode} · −{rule.lowerTolerancePercent}% / +{rule.upperTolerancePercent}%</small></span><em>{rule.effectiveFrom.toISOString().slice(0, 10)} → {rule.effectiveTo?.toISOString().slice(0, 10) || 'open'}</em></article>; })}</div></section>;
}

/** @param {{workspace: import('../../web-api-client').InputControlWorkspaceDto, request: import('../../web-api-client').InputRequestDto, onClose: () => void, onSaved: () => Promise<void>, onError: (message: string) => void}} props */
function IssueForm({ workspace, request, onClose, onSaved, onError }) {
  const storekeepers = workspace.people.filter((person) => person.status === 'Active' && person.roles.some((role) => role.role === 'Storekeeper' && !role.effectiveTo));
  const people = workspace.people.filter((person) => person.status === 'Active');
  /** @param {import('react').FormEvent<HTMLFormElement>} event */
  const save = async (event) => { event.preventDefault(); const data = new FormData(event.currentTarget); onError(''); try { await createStockIssue({ inputRequestId: request.id, issueDate: new Date(`${String(data.get('issueDate'))}T00:00:00`), issuerPersonId: String(data.get('issuerPersonId')), recipientPersonId: String(data.get('recipientPersonId')), lateEntryReason: String(data.get('lateEntryReason')) || undefined, lines: request.lines.filter((line) => Number(data.get(`quantity-${line.id}`)) > 0).map((line) => ({ inputRequestLineId: line.id, inventoryLotId: String(data.get(`lot-${line.id}`)) || undefined, quantity: Number(data.get(`quantity-${line.id}`)) })) }); await onSaved(); } catch (error) { onError(getApiError(error)); } };
  return <dialog open className="activity-dialog inventory-dialog"><article><header><div><span className="eyebrow">Approved request</span><h2>Record partial issue</h2></div><button className="dialog-close" onClick={onClose}>×</button></header><form className="inventory-form" onSubmit={save}><div className="form-grid"><label>Issue date<DatePicker name="issueDate" defaultValue={harareToday()} required /></label><label>Named Storekeeper<select name="issuerPersonId" required defaultValue=""><option value="">Select issuer</option>{storekeepers.map((person) => <option key={person.id} value={person.id}>{person.displayName}</option>)}</select></label><label>Field recipient<select name="recipientPersonId" required defaultValue=""><option value="">Select recipient</option>{people.map((person) => <option key={person.id} value={person.id}>{person.displayName}</option>)}</select></label><label>Late-entry reason<input name="lateEntryReason" maxLength={500} /></label></div><fieldset className="receipt-lines"><legend>Approved / issued / remaining</legend>{request.lines.map((line) => <div className="receipt-line" key={line.id}><span><strong>{line.itemCode}</strong><small>{line.requestedQuantity} / {line.alreadyIssuedQuantity} / {line.remainingQuantity} {line.unitCode}</small></span><label>Lot<select name={`lot-${line.id}`} defaultValue=""><option value="">Unbatched</option>{workspace.lots.filter((lot) => lot.inventoryItemId === line.inventoryItemId).map((lot) => <option key={lot.id} value={lot.id}>{lot.code}</option>)}</select></label><label>Issue quantity<input name={`quantity-${line.id}`} type="number" min={0} max={line.remainingQuantity} step="0.000001" defaultValue="0" /></label></div>)}</fieldset><footer className="form-actions"><span>No crop-cycle cost is posted at issue.</span><button>Create draft issue</button></footer></form></article></dialog>;
}

/** @param {{workspace: import('../../web-api-client').InputControlWorkspaceDto, token: string, onToken: (token: string) => void, onSaved: () => Promise<void>, onError: (message: string) => void}} props */
function ManagerAccess({ workspace, token, onToken, onSaved, onError }) {
  const managers = workspace.people.filter((person) => person.status === 'Active' && person.roles.some((role) => role.role === 'FarmManager' && role.isPrimary && !role.effectiveTo));
  const invite = async () => { if (!managers[0]) return; try { const result = await createManagerInvitation(managers[0].id, 48); onToken(result.token); await onSaved(); } catch (error) { onError(getApiError(error)); } };
  return <section className="record-panel manager-access"><header className="ledger-title"><div><span className="eyebrow">Cookie-authenticated membership</span><h2>FarmManager access</h2></div>{workspace.session.role === 'Grower' && <button disabled={!managers.length} onClick={invite}>Create invitation</button>}</header>{token && <div className="invitation-token"><ShieldAlert size={18} /><span><strong>Copy this token now</strong><code>{token}</code><small>It is displayed once; only a secure hash is stored.</small></span></div>}<div className="catalogue-list">{workspace.invitations.map((item) => <article key={item.id}><span><strong>{item.redeemedAt ? 'Redeemed' : item.revokedAt ? 'Revoked' : 'Open invitation'}</strong><small>Expires {item.expiresAt.toLocaleString('en-ZW')}</small></span></article>)}</div><p className="context-note"><CircleDollarSign size={14} /> No email infrastructure is added; the intended manager signs in normally and redeems the single-use token.</p></section>;
}
