/** @param {{ eyebrow: string, title: string, description: string, children?: import('react').ReactNode }} props */
export function PageHeader({ eyebrow, title, description, children }) {
  return (
    <header className="page-header">
      <div>
        <span className="eyebrow">{eyebrow}</span>
        <h1>{title}</h1>
        <p>{description}</p>
      </div>
      {children && <div className="page-header-actions">{children}</div>}
    </header>
  );
}
