import { useEffect, useState } from 'react';
import { readStored, writeStored } from '../../shared/lib/storage';
import { isThemePreference, themeStorageKey, type ThemePreference } from './themePreference';

function initialPreference(): ThemePreference {
  const stored = readStored(themeStorageKey);
  if (isThemePreference(stored)) {
    return stored;
  }
  return 'system';
}

function applyPreference(preference: ThemePreference): void {
  const root = document.documentElement;
  if (preference === 'system') {
    root.removeAttribute('data-theme');
    return;
  }
  root.setAttribute('data-theme', preference);
}

export function useTheme() {
  const [preference, setPreference] = useState<ThemePreference>(initialPreference);

  useEffect(() => {
    applyPreference(preference);
    writeStored(themeStorageKey, preference);
  }, [preference]);

  return { preference, setPreference };
}
