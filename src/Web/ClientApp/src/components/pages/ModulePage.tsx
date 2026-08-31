import {
  BarChart3,
  ClipboardList,
  DollarSign,
  MapPin,
  Package,
  PanelsTopLeft,
  Settings,
  Users,
} from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import type { NavigationId, NavigationItem } from '../../navigation';
import { EmptyState } from '../EmptyState';
import { PageHeader } from '../PageHeader';

const icons: Record<Exclude<NavigationId, 'dashboard'>, LucideIcon> = {
  farm: MapPin,
  fields: PanelsTopLeft,
  activities: ClipboardList,
  labour: Users,
  inventory: Package,
  finance: DollarSign,
  reports: BarChart3,
  administration: Settings,
};

const nextSteps: Record<NavigationId, string> = {
  dashboard: 'Use the navigation to continue setting up Cane360.',
  farm: 'Grower and farm setup is planned for Phase 1.',
  fields: 'Fields and the current crop-cycle workflow are planned for Phase 1.',
  activities: 'Activities follow the farm core vertical slice.',
  labour: 'Labour evidence and payroll will be delivered as later vertical slices.',
  inventory: 'Input accountability follows the activity and farm foundations.',
  finance: 'Operational cost views will be built from confirmed source records.',
  reports: 'Reports will be introduced only when traceable source records exist.',
  administration: 'Role and reference-data management will arrive with the workflows that require them.',
};

interface ModulePageProps {
  item: NavigationItem;
}

export function ModulePage({ item }: ModulePageProps) {
  const Icon = item.id === 'dashboard' ? MapPin : icons[item.id];

  return (
    <div className="page-stack">
      <PageHeader eyebrow={item.eyebrow} title={item.label} description={item.description} />
      <EmptyState
        icon={Icon}
        title={`${item.label} is ready for its first workflow`}
        description="This page is intentionally empty until its backend capability, validation, persistence, and tests are delivered together."
        nextStep={nextSteps[item.id]}
      />
    </div>
  );
}
