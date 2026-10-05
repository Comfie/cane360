import {CalendarDays, Droplets, Sprout} from 'lucide-react';
import {formatCycleStatus} from '../crop-cycles/cropCycleView';
import type {ReactNode} from 'react';
import type {CropCycleListItemDto, FieldDto} from '../../web-api-client';

interface FieldRecordProps {
    field: FieldDto;
    draftCycle?: CropCycleListItemDto;
    children?: ReactNode;
}

export function FieldRecord({field, draftCycle, children}: FieldRecordProps) {
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
                <span><Droplets size={14} aria-hidden="true"/> {field.irrigationMethod}</span>
                <span>Reporting from {field.reportingAreaSource.toLowerCase()} area</span>
            </div>
            {cycle ? (
                <section className="cycle-summary" aria-label={`Current crop cycle for ${field.name}`}>
                    <div className="cycle-icon" aria-hidden="true"><Sprout size={17}/></div>
                    <div>
                        <span className="record-status"><span
                            aria-hidden="true"/> {formatCycleStatus(cycle.status)}</span>
                        <strong>{cycle.variety} · {cycle.cycleType === 'Ratoon' ? `Ratoon ${cycle.ratoonNumber}` : 'Plant cane'}</strong>
                        <small><CalendarDays size={13} aria-hidden="true"/> Harvest
                            window {formatDate(cycle.expectedHarvestStart)}–{formatDate(cycle.expectedHarvestEnd)}
                        </small>
                    </div>
                    <div className="yield-value">
                        <span>Expected yield</span><strong>{cycle.expectedYieldTonnes.toLocaleString()} t</strong></div>
                </section>
            ) : !draftCycle && (
                <div className="cycle-empty">
                    <span className="cycle-empty-icon" aria-hidden="true"><Sprout size={18}/></span>
                    <div><strong>Field ready</strong><span>No crop cycle configured. Set up the crop growing in this field.</span>
                    </div>
                </div>
            )}
            {draftCycle && (
                <section className="draft-cycle-summary" aria-label={`Draft crop cycle for ${field.name}`}>
                    <span className="draft-cycle-icon" aria-hidden="true"><Sprout size={17}/></span>
                    <div>
                        <span className="record-status is-draft"><span aria-hidden="true"/> Draft crop plan</span>
                        <strong>{draftCycle.variety} · {draftCycle.cycleType === 'Ratoon' ? `Ratoon ${draftCycle.ratoonNumber}` : 'Plant cane'}</strong>
                        <small><CalendarDays size={13} aria-hidden="true"/> Starts {formatDate(draftCycle.startDate)} ·
                            harvest {formatDate(draftCycle.expectedHarvestStart)}–{formatDate(draftCycle.expectedHarvestEnd)}
                        </small>
                    </div>
                </section>
            )}
            {children}
        </article>
    );
}

function formatDate(value: string): string {
    return new Intl.DateTimeFormat('en-ZW', {day: 'numeric', month: 'short', year: 'numeric'})
        .format(new Date(`${value}T00:00:00`));
}
