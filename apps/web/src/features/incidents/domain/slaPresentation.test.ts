import { describe, expect, it } from 'vitest';
import type { SlaClock } from './sla';
import { burnPercent, clockReading, describeClock } from './slaPresentation';

function aClock(overrides: Partial<SlaClock>): SlaClock {
  return { target: 'Resolve', dueAt: 0, remainingMs: 600_000, fractionRemaining: 0.5, state: 'OnTrack', ...overrides };
}

describe('SLA presentation', () => {
  it('describes running and breached clocks', () => {
    expect(describeClock(aClock({}))).toBe('Resolve within 10m 00s, On track');
    expect(describeClock(aClock({ remainingMs: -60_000, state: 'Breached' }))).toBe(
      'Resolve deadline passed 1m 00s ago, Breached',
    );
  });

  it('burns the bar down with the remaining window and fills it once breached', () => {
    expect(burnPercent(aClock({ fractionRemaining: 0.42 }))).toBe(42);
    expect(burnPercent(aClock({ fractionRemaining: 0, state: 'AtRisk' }))).toBe(2);
    expect(burnPercent(aClock({ state: 'Breached', fractionRemaining: 0 }))).toBe(100);
  });

  it('reads the absolute time and flags overdue clocks', () => {
    expect(clockReading(aClock({ remainingMs: -125_000 }))).toEqual({ time: '2m 05s', overdue: true });
    expect(clockReading(aClock({}))).toEqual({ time: '10m 00s', overdue: false });
  });
});
