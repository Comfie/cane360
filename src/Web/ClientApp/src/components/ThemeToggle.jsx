import { Laptop, Moon, Sun } from 'lucide-react';
import { useTheme } from './ThemeContext';

const icons = {
  auto: Laptop,
  light: Sun,
  dark: Moon,
};

const labels = {
  auto: 'Use system colour theme',
  light: 'Use light colour theme',
  dark: 'Use dark colour theme',
};

const nextTheme = {
  auto: /** @type {const} */ ('light'),
  light: /** @type {const} */ ('dark'),
  dark: /** @type {const} */ ('auto'),
};

export function ThemeToggle() {
  const { theme, setTheme } = useTheme();
  const Icon = icons[theme];

  return (
    <button
      className="quiet-icon-button"
      type="button"
      onClick={() => setTheme(nextTheme[theme])}
      aria-label={labels[theme]}
      title={labels[theme]}
    >
      <Icon size={17} aria-hidden="true" />
    </button>
  );
}
