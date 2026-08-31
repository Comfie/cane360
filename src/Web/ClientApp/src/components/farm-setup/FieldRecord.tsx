import { CalendarDays, Droplets, Sprout } from 'lucide-react';
import { formatCycleStatus } from '../crop-cycles/cropCycleView';
import type { ReactNode } from 'react';
import type { FieldDto } from '../../web-api-client';

export function FieldRecord({ field, children }: { field: FieldDto; children?: ReactNode }) {
  const cycle = field.currentCropCycle;

  return (
    <article className="field-record">
      <header>
        <div>
          <span className="record-code">{field.code}</span>
          <h3>{field.name}</h3>
        </div>
        <strong className="area-value">{field.reportingHectares.toLocaleString()} <small>ha</small></strong>
      </header>
      <div className="field-facts">
        <span><Droplets size={14} aria-hidden="true" /> {field.irrigationMethod}</span>
        <span>Reporting from {field.reportingAreaSource.toLowerCase()} area</span>
      </div>
      {cycle ? (
        <section className="cycle-summary" aria-label={`Current crop cycle for ${field.name}`}>
          <div className="cycle-icon" aria-hidden="true"><Sprout size={17} /></div>
          <div>
            <span className="record-status"><span aria-hidden="true" /> {formatCycleStatus(cycle.status)}</span>
            <strong>{cycle.variety} · {cycle.cycleType === 'Ratoon' ? `Ratoon ${cycle.ratoonNumber}` : 'Plant cane'}</strong>
            <small><CalendarDays size={13} aria-hidden="true" /> Harvest window {formatDate(cycle.expectedHarvestStart)}–{formatDate(cycle.expectedHarvestEnd)}</small>
          </div>
          <div className="yield-value"><span>Expected yield</span><strong>{cycle.expectedYieldTonnes.toLocaleString()} t</strong></div>
        </section>
      ) : (
        <div className="cycle-empty"><Sprout size={18} aria-hidden="true" /> No current crop cycle</div>
      )}
      {children}
    </article>
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' })
    .format(new Date(`${value}T00:00:00`));
}
