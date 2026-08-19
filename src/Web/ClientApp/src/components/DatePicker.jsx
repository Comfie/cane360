import { useEffect, useId, useMemo, useRef, useState } from 'react';
import { CalendarDays, ChevronLeft, ChevronRight, Clock3 } from 'lucide-react';

const weekdays = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

/**
 * @param {{
 *   name?: string,
 *   type?: 'date' | 'datetime-local',
 *   value?: string,
 *   defaultValue?: string,
 *   min?: string,
 *   max?: string,
 *   required?: boolean,
 *   disabled?: boolean,
 *   autoFocus?: boolean,
 *   onChange?: (value: string) => void,
 * }} props
 */
export function DatePicker({
  name,
  type = 'date',
  value,
  defaultValue = '',
  min,
  max,
  required = false,
  disabled = false,
  autoFocus = false,
  onChange,
}) {
  const dialogId = useId();
  const rootRef = useRef(/** @type {HTMLDivElement | null} */ (null));
  const triggerRef = useRef(/** @type {HTMLButtonElement | null} */ (null));
  const [internalValue, setInternalValue] = useState(defaultValue);
  const [isOpen, setIsOpen] = useState(false);
  const selectedValue = value ?? internalValue;
  const selectedDate = selectedValue.slice(0, 10);
  const [viewDate, setViewDate] = useState(() => parseIsoDate(selectedDate) ?? new Date());
  const isDateTime = type === 'datetime-local';

  useEffect(() => {
    if (autoFocus) triggerRef.current?.focus();
  }, [autoFocus]);

  useEffect(() => {
    if (!isOpen) return undefined;

    const closeFromOutside = (/** @type {PointerEvent} */ event) => {
      if (!rootRef.current?.contains(/** @type {Node} */ (event.target))) setIsOpen(false);
    };
    const closeFromKeyboard = (/** @type {KeyboardEvent} */ event) => {
      if (event.key === 'Escape') {
        setIsOpen(false);
        triggerRef.current?.focus();
      }
    };

    document.addEventListener('pointerdown', closeFromOutside);
    document.addEventListener('keydown', closeFromKeyboard);
    return () => {
      document.removeEventListener('pointerdown', closeFromOutside);
      document.removeEventListener('keydown', closeFromKeyboard);
    };
  }, [isOpen]);

  const days = useMemo(() => calendarDays(viewDate), [viewDate]);

  const commit = (/** @type {string} */ nextValue) => {
    if (value === undefined) setInternalValue(nextValue);
    onChange?.(nextValue);
  };

  const selectDate = (/** @type {string} */ date) => {
    if (isDateDisabled(date, min, max)) return;
    if (isDateTime) {
      commit(`${date}T${selectedValue.slice(11, 16) || '12:00'}`);
      return;
    }

    commit(date);
    setIsOpen(false);
    triggerRef.current?.focus();
  };

  const selectTime = (/** @type {string} */ time) => {
    commit(`${selectedDate || todayIso()}T${time}`);
  };

  return <div className={`date-picker${isOpen ? ' is-open' : ''}`} ref={rootRef}>
    <input
      className="date-picker-form-value"
      name={name}
      value={selectedValue}
      required={required}
      pattern={isDateTime ? '\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}' : '\\d{4}-\\d{2}-\\d{2}'}
      aria-hidden="true"
      tabIndex={-1}
      onChange={() => {}}
      onInvalid={(event) => {
        event.preventDefault();
        setViewDate(parseIsoDate(selectedDate) ?? new Date());
        setIsOpen(true);
        triggerRef.current?.focus();
      }}
    />
    <button
      ref={triggerRef}
      type="button"
      className="date-picker-trigger"
      aria-expanded={isOpen}
      aria-controls={dialogId}
      aria-haspopup="dialog"
      aria-required={required}
      disabled={disabled}
      onClick={() => {
        if (!isOpen) setViewDate(parseIsoDate(selectedDate) ?? new Date());
        setIsOpen((current) => !current);
      }}
    >
      <span className={selectedValue ? '' : 'is-placeholder'}>{formatPickerValue(selectedValue, isDateTime)}</span>
      <CalendarDays size={17} aria-hidden="true" />
    </button>
    {isOpen && <div className="date-picker-popover" id={dialogId} role="dialog" aria-label="Choose date">
      <header className="date-picker-header">
        <button type="button" className="date-picker-nav" aria-label="Previous month" onClick={() => setViewDate(shiftMonth(viewDate, -1))}><ChevronLeft size={17} /></button>
        <strong>{new Intl.DateTimeFormat('en-ZW', { month: 'long', year: 'numeric' }).format(viewDate)}</strong>
        <button type="button" className="date-picker-nav" aria-label="Next month" onClick={() => setViewDate(shiftMonth(viewDate, 1))}><ChevronRight size={17} /></button>
      </header>
      <div className="date-picker-weekdays" aria-hidden="true">{weekdays.map((day) => <span key={day}>{day}</span>)}</div>
      <div className="date-picker-grid">
        {days.map((day) => <button
          key={day.iso}
          type="button"
          className={[!day.inMonth && 'is-outside', day.iso === selectedDate && 'is-selected', day.iso === todayIso() && 'is-today'].filter(Boolean).join(' ')}
          aria-label={new Intl.DateTimeFormat('en-ZW', { dateStyle: 'full' }).format(day.date)}
          aria-pressed={day.iso === selectedDate}
          disabled={isDateDisabled(day.iso, min, max)}
          onClick={() => selectDate(day.iso)}
        >{day.date.getDate()}</button>)}
      </div>
      {isDateTime && <div className="date-picker-time">
        <Clock3 size={16} aria-hidden="true" />
        <label>Time<input type="text" inputMode="numeric" value={selectedValue.slice(11, 16) || '12:00'} pattern="[0-2]\\d:[0-5]\\d" onChange={(event) => selectTime(event.target.value)} /></label>
        <button type="button" className="date-picker-done" onClick={() => { setIsOpen(false); triggerRef.current?.focus(); }}>Done</button>
      </div>}
      <footer className="date-picker-footer">
        {!required && selectedValue && <button type="button" className="date-picker-clear" onClick={() => { commit(''); setIsOpen(false); }}>Clear</button>}
        <button type="button" onClick={() => { const today = todayIso(); setViewDate(new Date(`${today}T00:00:00`)); selectDate(today); }}>Today</button>
      </footer>
    </div>}
  </div>;
}

/** @param {string} value @param {boolean} isDateTime */
function formatPickerValue(value, isDateTime) {
  if (!value) return isDateTime ? 'Select date and time' : 'Select date';
  const date = parseIsoDate(value.slice(0, 10));
  if (!date) return value;
  const formatted = new Intl.DateTimeFormat('en-ZW', { day: '2-digit', month: 'short', year: 'numeric' }).format(date);
  return isDateTime && value.length >= 16 ? `${formatted} · ${value.slice(11, 16)}` : formatted;
}

/** @param {Date} viewDate */
function calendarDays(viewDate) {
  const first = new Date(viewDate.getFullYear(), viewDate.getMonth(), 1);
  const mondayOffset = (first.getDay() + 6) % 7;
  const start = new Date(first.getFullYear(), first.getMonth(), 1 - mondayOffset);
  return Array.from({ length: 42 }, (_, index) => {
    const date = new Date(start.getFullYear(), start.getMonth(), start.getDate() + index);
    return { date, iso: toIsoDate(date), inMonth: date.getMonth() === viewDate.getMonth() };
  });
}

/** @param {Date} value @param {number} amount */
function shiftMonth(value, amount) {
  return new Date(value.getFullYear(), value.getMonth() + amount, 1);
}

/** @param {string} value */
function parseIsoDate(value) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return null;
  const [year, month, day] = value.split('-').map(Number);
  const parsed = new Date(year, month - 1, day);
  return parsed.getFullYear() === year && parsed.getMonth() === month - 1 && parsed.getDate() === day ? parsed : null;
}

/** @param {Date} value */
function toIsoDate(value) {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
}

function todayIso() {
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Harare', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date());
}

/** @param {string} value @param {string | undefined} min @param {string | undefined} max */
function isDateDisabled(value, min, max) {
  return Boolean((min && value < min.slice(0, 10)) || (max && value > max.slice(0, 10)));
}
