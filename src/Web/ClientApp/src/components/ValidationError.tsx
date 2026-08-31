import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import { CircleAlert, X } from 'lucide-react';

interface ValidationErrorProps {
  title?: string;
  message: string;
  persistent?: boolean;
}

interface ToastProps {
  title: string;
  message: string;
  persistent: boolean;
}

export function ValidationError({ title = 'Unable to complete that action', message, persistent = false }: ValidationErrorProps) {
  if (!message) return null;

  return createPortal(
    <Toast key={`${title}:${message}`} title={title} message={message} persistent={persistent} />,
    document.body,
  );
}

function Toast({ title, message, persistent }: ToastProps) {
  const [visible, setVisible] = useState(true);

  useEffect(() => {
    if (persistent) return undefined;

    const timeout = window.setTimeout(() => setVisible(false), 9000);
    return () => window.clearTimeout(timeout);
  }, [persistent]);

  if (!visible) return null;

  return (
    <div className="toast-region" aria-live="assertive" aria-atomic="true">
      <aside className="app-toast app-toast-error" role="alert">
        <span className="app-toast-icon"><CircleAlert size={19} aria-hidden="true" /></span>
        <div className="app-toast-copy">
          <strong>{title}</strong>
          <span>{message}</span>
        </div>
        <button type="button" className="app-toast-dismiss" aria-label="Dismiss error" onClick={() => setVisible(false)}>
          <X size={18} aria-hidden="true" />
        </button>
      </aside>
    </div>
  );
}
