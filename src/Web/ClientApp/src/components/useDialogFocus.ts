import { useEffect, useRef } from 'react';

/** Keeps keyboard focus in the active dialog and returns it to its opener. */
export function useDialogFocus<T extends HTMLElement>(onClose: () => void, active = true) {
  const ref = useRef<T>(null);
  const close = useRef(onClose);
  useEffect(() => { close.current = onClose; }, [onClose]);
  useEffect(() => {
    const dialog = ref.current;
    if (!active || !dialog) return;
    const opener = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const focusable = () => Array.from(dialog.querySelectorAll<HTMLElement>(
      'button:not(:disabled), a[href], input:not(:disabled):not([type="hidden"]), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex="-1"])',
    )).filter((element) => element.getClientRects().length > 0 && getComputedStyle(element).visibility !== 'hidden');
    dialog.tabIndex = -1;
    (focusable()[0] ?? dialog).focus();
    const onKey = (event: KeyboardEvent) => {
      // A date picker or nested dialog owns its own key handling.
      const target = event.target instanceof Element ? event.target.closest('[role="dialog"], [role="alertdialog"], dialog') : null;
      if (target && target !== dialog) return;
      if (event.key === 'Escape') {
        event.preventDefault();
        event.stopPropagation();
        close.current();
      } else if (event.key === 'Tab') {
        const controls = focusable();
        const first = controls[0] ?? dialog;
        const last = controls[controls.length - 1] ?? dialog;
        if (event.shiftKey && (document.activeElement === first || document.activeElement === dialog)) {
          event.preventDefault();
          last.focus();
        } else if (!event.shiftKey && (document.activeElement === last || document.activeElement === dialog)) {
          event.preventDefault();
          first.focus();
        }
      }
    };
    dialog.addEventListener('keydown', onKey);
    return () => {
      dialog.removeEventListener('keydown', onKey);
      if (opener?.isConnected) opener.focus();
    };
  }, [active]);
  return ref;
}
