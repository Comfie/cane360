import { CalendarDays, ChevronRight, History, Sprout, Wheat } from 'lucide-react';
import { Link } from 'react-router-dom';
import { EmptyState } from '../EmptyState';
import { cycleGroup, filterCycles, flattenCycleCollections, formatCycleStatus } from './cropCycleView';

const filters = [
  ['all', 'All'],
  ['current', 'Current'],
  ['drafts', 'Drafts'],
  ['awaiting-close', 'Awaiting close'],
  ['history', 'History'],
];

/** @param {{ collections: import('../../web-api-client').CropCycleCollectionDto[], filter: string, onFilterChange: (filter: string) => void }} props */
export function CropCycleRegister({ collections, filter, onFilterChange }) {
  const allCycles = flattenCycleCollections(collections);
  const visibleCycles = filterCycles(allCycles.map((entry) => entry.cropCycle), filter);
  const visibleIds = new Set(visibleCycles.map((cycle) => cycle.id));
  const entries = allCycles.filter((entry) => visibleIds.has(entry.cropCycle.id));

  return (
    <section aria-labelledby="cycle-register-title">
      <div className="section-heading">
        <div><span className="eyebrow">Field logbook</span><h2 id="cycle-register-title">Crop-cycle register</h2></div>
        <p>Current and historical cycles remain attached to their field in start-date order.</p>
      </div>
      <div className="segmented-filter" role="group" aria-label="Filter crop cycles">
        {filters.map(([value, label]) => <button key={value} type="button" className={filter === value ? 'is-active' : ''} aria-pressed={filter === value} onClick={() => onFilterChange(value)}>{label}</button>)}
      </div>
      {entries.length === 0 ? (
        <EmptyState icon={History} title={`No ${filter === 'all' ? '' : `${filters.find(([value]) => value === filter)?.[1].toLowerCase()} `}crop cycles`} description="Crop-cycle records will appear here after a draft is saved for a field." nextStep="Use a field card above to plan its next crop." />
      ) : (
        <div className="cycle-register" role="list">
          {entries.map(({ field, cropCycle }) => (
            <Link className="cycle-register-row" role="listitem" key={cropCycle.id} to={`/fields/${field.id}/crop-cycles/${cropCycle.id}`}>
              <span className={`cycle-status-dot is-${cycleGroup(cropCycle.status)}`} aria-hidden="true"><Sprout size={16} /></span>
              <span className="cycle-register-primary"><strong>{field.code} · {field.name}</strong><small>{cropCycle.variety} · {cropCycle.cycleType === 'Ratoon' ? `Ratoon ${cropCycle.ratoonNumber}` : 'Plant cane'}</small></span>
              <span className={`status-chip is-${cycleGroup(cropCycle.status)}`}>{formatCycleStatus(cropCycle.status)}</span>
              <span className="cycle-register-date"><CalendarDays size={14} aria-hidden="true" /> {formatDate(cropCycle.startDate)}</span>
              <span className="cycle-register-yield"><Wheat size={14} aria-hidden="true" /> {cropCycle.harvestResult ? `${cropCycle.harvestResult.actualTonnes.toLocaleString()} t actual` : `${cropCycle.expectedYieldTonnes.toLocaleString()} t expected`}</span>
              <ChevronRight className="cycle-register-arrow" size={17} aria-hidden="true" />
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}

/** @param {string} value */
function formatDate(value) {
  return new Intl.DateTimeFormat('en-ZW', { day: 'numeric', month: 'short', year: 'numeric' })
    .format(new Date(`${value}T00:00:00`));
}
