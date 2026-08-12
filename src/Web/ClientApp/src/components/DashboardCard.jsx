import { createElement } from 'react';

/** @param {{ label: string, title: string, description: string, icon: import('lucide-react').LucideIcon, footer?: string }} props */
export function DashboardCard({ label, title, description, icon, footer = 'Live farm record' }) {
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
