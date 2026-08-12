import { createElement } from 'react';

/** @param {{ label: string, title: string, description: string, icon: import('lucide-react').LucideIcon }} props */
export function DashboardCard({ label, title, description, icon }) {
  return (
    <article className="dashboard-card">
      <header>
        <span>{label}</span>
        {createElement(icon, { size: 21, 'aria-hidden': true })}
      </header>
      <strong>{title}</strong>
      <p>{description}</p>
      <footer><span aria-hidden="true" /> Awaiting setup</footer>
    </article>
  );
}
