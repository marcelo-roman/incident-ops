import { describe, expect, it } from 'vitest';
import { arrowStep, isThemePreference, neighbourPreference } from './themePreference';

describe('theme preference', () => {
  it('validates stored values', () => {
    expect(isThemePreference('dark')).toBe(true);
    expect(isThemePreference('sepia')).toBe(false);
    expect(isThemePreference(null)).toBe(false);
  });

  it('moves between options with arrow keys and wraps around', () => {
    expect(arrowStep('ArrowRight')).toBe(1);
    expect(arrowStep('ArrowUp')).toBe(-1);
    expect(arrowStep('Enter')).toBeUndefined();
    expect(neighbourPreference('dark', 1)).toBe('system');
    expect(neighbourPreference('system', -1)).toBe('dark');
  });
});
