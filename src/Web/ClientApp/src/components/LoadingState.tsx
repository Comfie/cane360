interface LoadingStateProps {
  label?: string;
  fullPage?: boolean;
}

export function LoadingState({ label = 'Loading Cane360', fullPage = false }: LoadingStateProps) {
  return (
    <div className={`loading-state${fullPage ? ' is-full-page' : ''}`} role="status" aria-live="polite">
      <span className="loading-mark" aria-hidden="true"><span /><span /><span /></span>
      <span>{label}</span>
    </div>
  );
}
