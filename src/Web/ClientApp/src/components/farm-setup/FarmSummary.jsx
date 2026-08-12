import { Droplets, LandPlot, MapPin, UserRound } from 'lucide-react';

/** @param {{ setup: import('../../web-api-client').FarmSetupDto, compact?: boolean }} props */
export function FarmSummary({ setup, compact = false }) {
  const farm = setup.farm;
  if (!farm) return null;

  return (
    <section className={`record-panel farm-summary${compact ? ' is-compact' : ''}`} aria-labelledby="farm-summary-title">
      <header>
        <div>
          <span className="record-code">{farm.code}</span>
          <h2 id="farm-summary-title">{farm.name}</h2>
        </div>
        <span className="record-status"><span aria-hidden="true" /> Active farm</span>
      </header>
      <dl className="record-details">
        <div><dt><UserRound size={16} aria-hidden="true" /> Grower</dt><dd>{setup.grower?.displayName}</dd></div>
        <div><dt><MapPin size={16} aria-hidden="true" /> Location</dt><dd>{farm.location}</dd></div>
        <div><dt><LandPlot size={16} aria-hidden="true" /> Declared area</dt><dd>{farm.declaredHectares.toLocaleString()} ha</dd></div>
        <div><dt><Droplets size={16} aria-hidden="true" /> Irrigation</dt><dd>{farm.irrigationContext}</dd></div>
      </dl>
      {!compact && (
        <div className="record-notes">
          <div><span>Address</span><strong>{farm.address}</strong></div>
          <div><span>Tenure</span><strong>{farm.tenure}</strong></div>
          {setup.grower?.phone && <div><span>Grower phone</span><strong>{setup.grower.phone}</strong></div>}
        </div>
      )}
    </section>
  );
}
