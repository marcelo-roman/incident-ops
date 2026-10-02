import { describe, expect, it } from 'vitest';
import { escalationTiers } from './oncall';

describe('escalationTiers', () => {
  it('orders the roster by escalation level', () => {
    const tiers = escalationTiers({
      weekStart: '2026-09-28T14:30:00Z',
      primary: 'Ana',
      secondary: 'Dan',
      lead: 'Priya',
    });

    expect(tiers).toEqual([
      { level: 1, role: 'Primary', engineer: 'Ana' },
      { level: 2, role: 'Secondary', engineer: 'Dan' },
      { level: 3, role: 'Engineering lead', engineer: 'Priya' },
    ]);
  });
});
