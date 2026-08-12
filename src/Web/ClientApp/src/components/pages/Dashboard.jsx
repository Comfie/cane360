import { LandPlot, MapPin, Sprout, Target } from 'lucide-react';
import { Link } from 'react-router-dom';
import { DashboardCard } from '../DashboardCard';
import { EmptyState } from '../EmptyState';
import { FieldRecord } from '../farm-setup/FieldRecord';
import { FarmSummary } from '../farm-setup/FarmSummary';
import { useFarmSetup } from '../farm-setup/farmSetupApi';
import { LoadingState } from '../LoadingState';
import { PageHeader } from '../PageHeader';
import { ValidationError } from '../ValidationError';

export function Dashboard() {
  const { setup, error, isLoading } = useFarmSetup();

  if (isLoading) return <LoadingState label="Preparing your farm overview" />;
  if (!setup) return <ValidationError title="Dashboard unavailable" message={error} />;

  const farm = setup.farm;
  const fields = farm?.fields ?? [];
  const cycles = fields.filter((field) => field.currentCropCycle);
  const reportingHectares = fields.reduce((total, field) => total + field.reportingHectares, 0);
  const expectedYield = cycles.reduce((total, field) => total + (field.currentCropCycle?.expectedYieldTonnes ?? 0), 0);

  if (!setup.isConfigured || !farm) {
    return (
      <div className="page-stack">
        <PageHeader eyebrow="Farm overview" title="Welcome to Cane360" description="Create your farm record to begin a secure, traceable view of fields and current crops." />
        <section className="field-context" aria-label="Current farm context">
          <div className="field-context-copy"><span className="eyebrow">Current working context</span><strong>Ready for your farm</strong><p>Your active farm, fields, reporting area, and crop cycles will stay visible here.</p></div>
          <div className="cane-lines" aria-hidden="true"><span /><span /><span /><span /><span /></div>
        </section>
        <EmptyState title="Set up your grower workspace" description="One short setup creates your grower profile, active farm, secure membership, and default store together." nextStep="Start with your farm, then add a field and its current crop cycle." action={<Link className="primary-action" to="/farm">Create farm</Link>} />
      </div>
    );
  }

  return (
    <div className="page-stack">
      <PageHeader eyebrow="Farm overview" title={`Good day, ${firstName(setup.grower?.displayName)}`} description="Your current farm, field area, and growing crop are shown from persisted Cane360 records." />

      <section className="field-context has-farm" aria-label="Current farm context">
        <div className="field-context-copy"><span className="eyebrow">Current working context</span><strong>{farm.name} · {farm.code}</strong><p>{farm.location} · {farm.declaredHectares.toLocaleString()} declared hectares · {fields.length} {fields.length === 1 ? 'field' : 'fields'}</p></div>
        <div className="cane-lines" aria-hidden="true"><span /><span /><span /><span /><span /></div>
        <span className="secure-context"><MapPin size={17} aria-hidden="true" /> Active grower farm</span>
      </section>

      <section aria-labelledby="snapshot-title">
        <div className="section-heading"><div><span className="eyebrow">Live snapshot</span><h2 id="snapshot-title">Farm at a glance</h2></div><p>These totals come directly from the fields and current crop cycles below.</p></div>
        <div className="dashboard-grid">
          <DashboardCard label="Farm" title={farm.name} description={`${farm.location} · ${farm.tenure}`} icon={MapPin} />
          <DashboardCard label="Fields" title={String(fields.length)} description={`${reportingHectares.toLocaleString()} reporting hectares`} icon={LandPlot} footer={fields.length > 0 ? 'Field records current' : 'Add the first field'} />
          <DashboardCard label="Current crops" title={String(cycles.length)} description={cycles.length > 0 ? `${cycles.map((field) => field.currentCropCycle?.variety).join(', ')} in production` : 'No crop cycle opened yet'} icon={Sprout} footer={cycles.length > 0 ? 'Current cycles open' : 'Crop setup required'} />
          <DashboardCard label="Expected yield" title={`${expectedYield.toLocaleString()} t`} description="Across all current crop cycles" icon={Target} footer={cycles.length > 0 ? 'Planning estimate' : 'Awaiting crop setup'} />
        </div>
      </section>

      <FarmSummary setup={setup} compact />

      <section aria-labelledby="dashboard-fields-title">
        <div className="section-heading"><div><span className="eyebrow">Field context</span><h2 id="dashboard-fields-title">Fields and current crops</h2></div>{fields.length > 0 && <Link className="text-action" to="/fields">Manage fields</Link>}</div>
        {fields.length === 0 ? (
          <EmptyState icon={LandPlot} title="Add your first field" description="A field supplies the reporting area and boundary for its current crop cycle." nextStep="Record the field before opening its crop cycle." action={<Link className="primary-action" to="/fields">Add field</Link>} />
        ) : (
          <div className="field-record-list is-dashboard">{fields.map((field) => <FieldRecord key={field.id} field={field} />)}</div>
        )}
      </section>
    </div>
  );
}

/** @param {string | undefined} displayName */
function firstName(displayName) {
  return displayName?.trim().split(/\s+/)[0] || 'grower';
}
