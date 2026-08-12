import { createElement } from 'react';
import { ArrowRight, Sprout } from 'lucide-react';

/** @param {{ title: string, description: string, nextStep?: string, icon?: import('lucide-react').LucideIcon }} props */
export function EmptyState({ title, description, nextStep, icon = Sprout }) {
  const titleId = `empty-${title.replace(/\s+/g, '-').toLowerCase()}`;

  return (
    <section className="empty-state" aria-labelledby={titleId}>
      <span className="empty-state-icon" aria-hidden="true">{createElement(icon, { size: 28 })}</span>
      <div>
        <span className="status-label"><span aria-hidden="true" /> Foundation ready</span>
        <h2 id={titleId}>{title}</h2>
        <p>{description}</p>
        {nextStep && (
          <p className="empty-state-next"><ArrowRight size={16} aria-hidden="true" /> {nextStep}</p>
        )}
      </div>
    </section>
  );
}
