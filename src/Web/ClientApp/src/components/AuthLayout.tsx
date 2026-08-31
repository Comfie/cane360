import { Check, Leaf } from 'lucide-react';
import type { ReactNode } from 'react';

interface AuthLayoutProps {
  children: ReactNode;
  title: string;
  description: string;
}

export function AuthLayout({ children, title, description }: AuthLayoutProps) {
  return (
    <main className="auth-layout">
      <section className="auth-story" aria-label="About Cane360">
        <a className="brand auth-brand" href="/" aria-label="Cane360 home">
          <span className="brand-mark" aria-hidden="true"><Leaf size={22} /></span>
          <span><strong>Cane360</strong><small>Grower operations</small></span>
        </a>
        <div className="auth-story-copy">
          <span className="eyebrow">Built for Zimbabwean sugarcane growers</span>
          <h1>Every field record, connected.</h1>
          <p>Capture what happened, who performed the work, what inputs were used, and what each crop cycle is costing.</p>
          <ul>
            <li><Check size={17} aria-hidden="true" /> Reliable crop-cycle records</li>
            <li><Check size={17} aria-hidden="true" /> Clear input accountability</li>
            <li><Check size={17} aria-hidden="true" /> Verifiable operational payroll</li>
          </ul>
        </div>
        <small className="auth-story-footnote">Online-first · Responsive from field to office</small>
      </section>
      <section className="auth-panel">
        <div className="auth-mobile-brand"><Leaf size={20} aria-hidden="true" /> Cane360</div>
        <article className="auth-card">
          <header>
            <span className="eyebrow">Secure account access</span>
            <h2>{title}</h2>
            <p>{description}</p>
          </header>
          {children}
        </article>
      </section>
    </main>
  );
}
