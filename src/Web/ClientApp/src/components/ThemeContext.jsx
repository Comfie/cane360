import { createContext, useContext, useEffect, useState } from 'react';

const STORAGE_KEY = 'cane360ColorScheme';

/** @typedef {'auto' | 'light' | 'dark'} Theme */
/** @typedef {{ theme: Theme, setTheme: import('react').Dispatch<import('react').SetStateAction<Theme>> }} ThemeContextValue */

const ThemeContext = createContext(/** @type {ThemeContextValue | null} */ (null));

export function useTheme() {
  const context = useContext(ThemeContext);

  if (!context) {
    throw new Error('useTheme must be used inside ThemeProvider.');
  }

  return context;
}

/** @param {{ children: import('react').ReactNode }} props */
export function ThemeProvider({ children }) {
  const [theme, setTheme] = useState(
    /** @returns {Theme} */
    () => {
      const storedTheme = localStorage.getItem(STORAGE_KEY);
      return storedTheme === 'light' || storedTheme === 'dark' ? storedTheme : 'auto';
    }
  );

  useEffect(() => {
    if (theme === 'auto') {
      document.documentElement.removeAttribute('data-theme');
    } else {
      document.documentElement.setAttribute('data-theme', theme);
    }
    localStorage.setItem(STORAGE_KEY, theme);
  }, [theme]);

  return <ThemeContext.Provider value={{ theme, setTheme }}>{children}</ThemeContext.Provider>;
}
