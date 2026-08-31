import { createElement } from 'react';
import type { LucideIcon } from 'lucide-react';

interface DashboardCardProps {
  label: string;
  title: string;
  description: string;
  icon: LucideIcon;
  footer?: string;
}

export function DashboardCard({ label, title, description, icon, footer = 'Live farm record' }: DashboardCardProps) {
  return (
    <article className="dashboard-card">
      <header>
        <span>{label}</span>
        {createElement(icon, { size: 18, 'aria-hidden': true })}
      </header>
      <strong>{title}</strong>
      <p>{description}</p>
      <footer><span aria-hidden="true" /> {footer}</footer>
    </article>
  );
}
