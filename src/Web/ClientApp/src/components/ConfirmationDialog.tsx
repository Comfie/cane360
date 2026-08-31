import { AlertTriangle } from 'lucide-react';
import type { MouseEvent, ReactNode } from 'react';

interface ConfirmationDialogProps {
  title: string;
  description: string;
  confirmLabel: string;
  isBusy?: boolean;
  children?: ReactNode;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmationDialog({ title, description, confirmLabel, isBusy = false, children, onConfirm, onCancel }: ConfirmationDialogProps) {
  return (
    <div className="dialog-backdrop" role="presentation" onMouseDown={(event: MouseEvent<HTMLDivElement>) => {
      if (event.target === event.currentTarget && !isBusy) onCancel();
    }}>
      <section className="confirmation-dialog" role="alertdialog" aria-modal="true" aria-labelledby="confirmation-title" aria-describedby="confirmation-description">
        <span className="confirmation-icon" aria-hidden="true"><AlertTriangle size={20} /></span>
        <div>
          <h2 id="confirmation-title">{title}</h2>
          <p id="confirmation-description">{description}</p>
        </div>
        {children}
        <footer>
          <button type="button" className="secondary outline" onClick={onCancel} disabled={isBusy}>Keep unchanged</button>
          <button type="button" onClick={onConfirm} disabled={isBusy}>{isBusy ? 'Saving…' : confirmLabel}</button>
        </footer>
      </section>
    </div>
  );
}
