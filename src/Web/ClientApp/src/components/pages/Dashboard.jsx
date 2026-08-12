import { BookOpen, MapPin, PackageCheck, ShieldCheck, Users } from 'lucide-react';
import { DashboardCard } from '../DashboardCard';
import { EmptyState } from '../EmptyState';
import { PageHeader } from '../PageHeader';

const readinessCards = [
  {
    label: 'Farm record',
    title: 'One grower, one farm',
    description: 'Grower, farm, personnel, and field setup arrives in the next vertical slice.',
    icon: MapPin,
  },
  {
    label: 'Field diary',
    title: 'Work with evidence',
    description: 'Activities will retain event dates, entry dates, responsible people, and source records.',
    icon: BookOpen,
  },
  {
    label: 'Input control',
    title: 'Account for every unit',
    description: 'Issues, applications, returns, and approved losses will remain traceable end to end.',
    icon: PackageCheck,
  },
  {
    label: 'Operational payroll',
    title: 'Pay from verified work',
    description: 'Attendance and confirmed work will support each future payroll amount.',
    icon: Users,
  },
];

export function Dashboard() {
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Farm overview"
        title="Your Cane360 workspace"
        description="The secure application foundation is ready. Farm records will appear here as each working capability is delivered."
      />

      <section className="field-context" aria-label="Current farm context">
        <div className="field-context-copy">
          <span className="eyebrow">Current working context</span>
          <strong>Farm and crop cycle not configured</strong>
          <p>Once setup begins, this strip will keep the selected farm, field, crop cycle, and reporting area visible.</p>
        </div>
        <div className="cane-lines" aria-hidden="true"><span /><span /><span /><span /><span /></div>
        <span className="secure-context"><ShieldCheck size={17} aria-hidden="true" /> Authenticated workspace</span>
      </section>

      <section aria-labelledby="readiness-title">
        <div className="section-heading">
          <div>
            <span className="eyebrow">MVP outcomes</span>
            <h2 id="readiness-title">Operational foundation</h2>
          </div>
          <p>Each capability will be added as a runnable vertical slice—without placeholder business records.</p>
        </div>
        <div className="dashboard-grid">
          {readinessCards.map((card) => <DashboardCard key={card.label} {...card} />)}
        </div>
      </section>

      <EmptyState
        title="Your action queue is clear"
        description="Approvals, late records, input exceptions, and payroll checks will appear here when those workflows are implemented."
        nextStep="Phase 1 begins with grower, farm, field, and current crop-cycle setup."
      />
    </div>
  );
}
