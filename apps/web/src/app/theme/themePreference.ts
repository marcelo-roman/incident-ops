export const themePreferences = ['system', 'light', 'dark'] as const;
export type ThemePreference = (typeof themePreferences)[number];

export const themeStorageKey = 'incident-ops.theme';

const arrowSteps: Readonly<Record<string, number>> = { ArrowRight: 1, ArrowDown: 1, ArrowLeft: -1, ArrowUp: -1 };

export function isThemePreference(value: string | null): value is ThemePreference {
  return value !== null && (themePreferences as readonly string[]).includes(value);
}

export function arrowStep(key: string): number | undefined {
  return arrowSteps[key];
}

export function neighbourPreference(current: ThemePreference, step: number): ThemePreference {
  const index = themePreferences.indexOf(current);
  const next = (index + step + themePreferences.length) % themePreferences.length;
  return themePreferences[next] ?? current;
}
