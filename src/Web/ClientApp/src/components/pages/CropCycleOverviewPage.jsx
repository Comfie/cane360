import { createElement, useEffect, useState } from 'react';
import { ArrowLeft, CalendarDays, CircleDot, Coins, History, Sprout, Users, Warehouse } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { ConfirmationDialog } from '../ConfirmationDialog';
import { cropCyclesClient, localDate, transitionCycle } from '../crop-cycles/cropCycleApi';
import { cycleGroup, formatCycleStatus } from '../crop-cycles/cropCycleView';
import { getApiError } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';

const transitionCopy = {
  Activate: ['Activate crop cycle?', 'This makes the draft the field’s current crop and allows future operational entries.', 'Activate cycle'],
  Cancel: ['Cancel crop-cycle draft?', 'The draft will become read-only. This cannot be reversed in Phase 2.', 'Cancel draft'],
  ReadyForHarvest: ['Mark ready for harvest?', 'The field remains current, but the next valid action will be recording its harvest result.', 'Mark ready'],
  Harvest: ['Record harvest result?', 'The harvest date and actual tonnes will be saved permanently against this cycle.', 'Record harvest'],
  Close: ['Close crop cycle?', 'The harvested cycle will move to history and reject further operational entries.', 'Close cycle'],
};

export function CropCycleOverviewPage() {
  const { fieldId = '', cropCycleId = '' } = useParams();
  const [details, setDetails] = useState(/** @type {import('../../web-api-client').CropCycleDetailsDto | null} */ (null));
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [pendingAction, setPendingAction] = useState(/** @type {keyof typeof transitionCopy | null} */ (null));
  const [isSaving, setIsSaving] = useState(false);

  useEffect(() => {
    cropCyclesClient.cropCyclesGET2(fieldId, cropCycleId)
      .then(setDetails)
      .catch((requestError) => setError(getApiError(requestError)))
      .finally(() => setIsLoading(false));
  }, [fieldId, cropCycleId]);

  if (isLoading) return <LoadingState label="Opening crop-cycle logbook" />;
  if (!details) return <ValidationError title="Crop-cycle overview unavailable" message={error} />;

  const cycle = details.cropCycle;

  /** @param {FormData | undefined} data */
  const confirmTransition = async (data) => {
    if (!pendingAction) return;
    setError('');
    setIsSaving(true);
    try {
      const result = await transitionCycle(fieldId, cropCycleId, pendingAction, cycle.version, {
        reason: data ? String(data.get('reason') ?? '').trim() : undefined,
        harvestDate: data ? localDate(data.get('harvestDate')) : undefined,
        actualTonnes: data ? Number(data.get('actualTonnes')) : undefined,
      });
      setDetails(result);
      setPendingAction(null);
    } catch (requestError) {
      setError(getApiError(requestError));
      setPendingAction(null);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="page-stack cycle-overview-page">
      <Link className="back-link" to="/fields"><ArrowLeft size={15} /> Fields and crop cycles</Link>
      <PageHeader eyebrow={`${details.field.code} · ${details.field.name}`} title={`${cycle.variety} ${cycle.cycleType === 'Ratoon' ? `ratoon ${cycle.ratoonNumber}` : 'plant cane'}`} description="Lifecycle state, harvest result and real field-history events for this crop cycle.">
        <span className={`status-chip is-${cycleGroup(cycle.status)}`}>{formatCycleStatus(cycle.status)}</span>
      </PageHeader>
      <ValidationError title="Transition blocked" message={error} />

      <section className="cycle-overview-grid" aria-label="Crop-cycle overview">
        <article className="record-panel cycle-facts-panel">
          <header><div><span className="eyebrow">Cycle overview</span><h2>Recorded plan</h2></div><Sprout size={21} aria-hidden="true" /></header>
          <dl className="cycle-facts">
            <div><dt>Field</dt><dd>{details.field.code} · {details.field.name}</dd></div>
            <div><dt>Reporting area</dt><dd>{details.field.reportingHectares.toLocaleString()} ha</dd></div>
            <div><dt>Crop type</dt><dd>{cycle.cycleType === 'Ratoon' ? `Ratoon ${cycle.ratoonNumber}` : 'Plant cane'}</dd></div>
            <div><dt>Variety</dt><dd>{cycle.variety}</dd></div>
            <div><dt>Start date</dt><dd>{formatDate(cycle.startDate)}</dd></div>
            <div><dt>Harvest window</dt><dd>{formatDate(cycle.expectedHarvestStart)}–{formatDate(cycle.expectedHarvestEnd)}</dd></div>
            <div><dt>Expected yield</dt><dd>{cycle.expectedYieldTonnes.toLocaleString()} t</dd></div>
            <div><dt>Actual harvest</dt><dd>{cycle.harvestResult ? `${cycle.harvestResult.actualTonnes.toLocaleString()} t · ${formatDate(cycle.harvestResult.harvestDate)}` : 'Not recorded'}</dd></div>
          </dl>
        </article>

        <article className="record-panel transition-panel">
          <header><div><span className="eyebrow">Next lifecycle step</span><h2>{nextStepTitle(cycle.status)}</h2></div><CircleDot size={21} aria-hidden="true" /></header>
          {details.allowedTransitions.length > 0 ? <><p>{nextStepDescription(cycle.status)}</p><div className="transition-actions">{details.allowedTransitions.map((action) => <button key={action} type="button" className={action === 'Cancel' ? 'secondary outline' : ''} onClick={() => setPendingAction(/** @type {keyof typeof transitionCopy} */ (action))}>{actionLabel(action)}</button>)}</div></> : <p>This record is terminal and remains available as read-only field history.</p>}
          {Object.entries(details.blockedTransitions).map(([action, reason]) => <p className="blocked-reason" key={action}><strong>{actionLabel(action)} unavailable:</strong> {reason}</p>)}
        </article>
      </section>

      <section aria-labelledby="history-title">
        <div className="section-heading"><div><span className="eyebrow">Field logbook</span><h2 id="history-title">Chronological history</h2></div><p>Event dates and recorded timestamps stay distinct where both are available.</p></div>
        <ol className="field-timeline">
          {details.timeline.map((item) => <li key={item.id}><span className="timeline-marker" aria-hidden="true"><History size={15} /></span><article><header><strong>{item.title}</strong><time dateTime={item.eventDate}>{formatEventDate(item.eventDate)}</time></header>{item.detail && <p>{item.detail}</p>}{item.reason && <p><strong>Reason:</strong> {item.reason}</p>}<small>Recorded {formatTimestamp(item.recordedAt)}</small></article></li>)}
        </ol>
      </section>

      <section aria-labelledby="future-history-title">
        <div className="section-heading"><div><span className="eyebrow">Operational records</span><h2 id="future-history-title">Cycle-linked information</h2></div><p>Only implemented, persisted records appear in Cane360 history.</p></div>
        <div className="unavailable-grid">
          <UnavailableHistory icon={CalendarDays} title="Activities" />
          <UnavailableHistory icon={Users} title="Labour" />
          <UnavailableHistory icon={Warehouse} title="Inputs" />
          <UnavailableHistory icon={Coins} title="Costs" />
        </div>
      </section>

      {pendingAction && <TransitionConfirmation action={pendingAction} isBusy={isSaving} onCancel={() => setPendingAction(null)} onConfirm={confirmTransition} />}
    </div>
  );
}

/** @param {{ action: keyof typeof transitionCopy, isBusy: boolean, onCancel: () => void, onConfirm: (data?: FormData) => void }} props */
function TransitionConfirmation({ action, isBusy, onCancel, onConfirm }) {
  const [form, setForm] = useState(/** @type {HTMLFormElement | null} */ (null));
  const [clientError, setClientError] = useState('');
  const [title, description, confirmLabel] = transitionCopy[action];

  const submit = () => {
    if (form && !form.reportValidity()) return;
    const data = form ? new FormData(form) : undefined;
    if (action === 'Harvest' && Number(data?.get('actualTonnes')) <= 0) {
      setClientError('Actual tonnes must be greater than zero.');
      return;
    }
    onConfirm(data);
  };

  return <ConfirmationDialog title={title} description={description} confirmLabel={confirmLabel} isBusy={isBusy} onConfirm={submit} onCancel={onCancel}>
    {(action === 'Cancel' || action === 'Harvest') && <form ref={setForm} className="confirmation-form" onSubmit={(event) => event.preventDefault()}>
      {action === 'Cancel' && <label>Cancellation reason<textarea name="reason" rows={3} maxLength={500} required autoFocus /></label>}
      {action === 'Harvest' && <><label>Harvest date<input name="harvestDate" type="date" required autoFocus /></label><label>Actual tonnes<input name="actualTonnes" type="number" min="0.001" max="1000000" step="0.001" inputMode="decimal" required /></label></>}
      <ValidationError message={clientError} />
    </form>}
  </ConfirmationDialog>;
}

/** @param {{ icon: import('lucide-react').LucideIcon, title: string }} props */
function UnavailableHistory({ icon: Icon, title }) {
  return <article className="unavailable-card">{createElement(Icon, { size: 18, 'aria-hidden': true })}<div><strong>{title}</strong><span>Unavailable until this module is implemented.</span></div></article>;
}

/** @param {string} status */
function nextStepTitle(status) {
  return ({ Draft: 'Review and activate', Active: 'Prepare for harvest', ReadyForHarvest: 'Record harvest', Harvested: 'Close the cycle', Closed: 'Cycle complete', Cancelled: 'Draft cancelled' })[status] ?? status;
}

/** @param {string} status */
function nextStepDescription(status) {
  return ({ Draft: 'Activation makes this the field’s current crop. Cancel only if the draft will not proceed.', Active: 'Mark the crop ready only when harvest preparation should begin.', ReadyForHarvest: 'A valid harvest date and positive actual tonnes are required.', Harvested: 'Review the permanent harvest result before closing this cycle.' })[status] ?? '';
}

/** @param {string} action */
function actionLabel(action) {
  return ({ Activate: 'Activate cycle', Cancel: 'Cancel draft', ReadyForHarvest: 'Mark ready for harvest', Harvest: 'Record harvest', Close: 'Close cycle', Modify: 'Further changes' })[action] ?? action;
}

/** @param {string} value */
function formatDate(value) {
  return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(`${value}T00:00:00`));
}

/** @param {string} value */
function formatEventDate(value) {
  return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(value));
}

/** @param {string} value */
function formatTimestamp(value) {
  return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' }).format(new Date(value));
}
