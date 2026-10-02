import { describe, expect, it } from 'vitest';
import { formatCountdown, formatMinutes, formatPercent, formatRelative } from './duration';

describe('formatCountdown', () => {
  it.each([
    [65_000, '1m 05s'],
    [3_600_000 + 12 * 60_000, '1h 12m'],
    [2 * 86_400_000 + 4 * 3_600_000, '2d 04h'],
    [-90_000, '-1m 30s'],
    [0, '0m 00s'],
  ])('formats %d ms as %s', (ms, expected) => {
    expect(formatCountdown(ms)).toBe(expected);
  });
});

describe('formatMinutes', () => {
  it('picks a readable unit', () => {
    expect(formatMinutes(42.4)).toBe('42m');
    expect(formatMinutes(150)).toBe('2.5h');
    expect(formatMinutes(2880)).toBe('2.0d');
    expect(formatMinutes(null)).toBe('—');
  });
});

describe('formatPercent', () => {
  it('formats a 0 to 100 percentage', () => {
    expect(formatPercent(92.44)).toBe('92.4%');
    expect(formatPercent(null)).toBe('—');
  });
});

describe('formatRelative', () => {
  it('describes elapsed time', () => {
    const now = Date.parse('2026-10-02T12:00:00Z');

    expect(formatRelative('2026-10-02T11:59:30Z', now)).toBe('just now');
    expect(formatRelative('2026-10-02T11:15:00Z', now)).toBe('45m ago');
    expect(formatRelative('2026-10-01T09:00:00Z', now)).toBe('1d ago');
  });
});
