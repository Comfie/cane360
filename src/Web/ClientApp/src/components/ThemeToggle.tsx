import { Laptop, Moon, Sun } from 'lucide-react';
import type { LucideIcon } from 'lucide-react';
import { useTheme } from './ThemeContext';
import type { Theme } from './ThemeContext';

const icons: Record<Theme, LucideIcon> = {
  auto: Laptop,
  light: Sun,
  dark: Moon,
};

const labels: Record<Theme, string> = {
  auto: 'Use system colour theme',
  light: 'Use light colour theme',
  dark: 'Use dark colour theme',
};

const nextTheme: Record<Theme, Theme> = {
  auto: 'light',
  light: 'dark',
  dark: 'auto',
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
