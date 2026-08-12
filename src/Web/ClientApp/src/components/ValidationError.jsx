import { CircleAlert } from 'lucide-react';

/** @param {{ title?: string, message: string }} props */
export function ValidationError({ title = 'Check the details below', message }) {
  if (!message) return null;

  return (
    <div className="validation-error" role="alert">
      <CircleAlert size={19} aria-hidden="true" />
      <div>
        <strong>{title}</strong>
        <span>{message}</span>
      </div>
    </div>
  );
}
