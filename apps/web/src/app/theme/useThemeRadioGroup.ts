import type { KeyboardEvent } from 'react';
import { arrowStep, neighbourPreference, type ThemePreference } from './themePreference';

export function useThemeRadioGroup(value: ThemePreference, onChange: (preference: ThemePreference) => void) {
  const onKeyDown = (event: KeyboardEvent<HTMLDivElement>) => {
    const step = arrowStep(event.key);
    if (step === undefined) {
      return;
    }
    event.preventDefault();
    const next = neighbourPreference(value, step);
    onChange(next);
    event.currentTarget.querySelector<HTMLButtonElement>(`[data-value='${next}']`)?.focus();
  };
  const tabIndexFor = (preference: ThemePreference) => {
    if (preference === value) {
      return 0;
    }
    return -1;
  };
  return { onKeyDown, tabIndexFor };
}
