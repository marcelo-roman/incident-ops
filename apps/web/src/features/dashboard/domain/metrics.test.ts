import { describe, expect, it } from 'vitest';
import { severityShares, totalOpen } from './metrics';

describe('severity shares', () => {
  it('splits open incidents by severity', () => {
    const summary = { openBySeverity: { Sev1: 1, Sev2: 3, Sev3: 0, Sev4: 4 } };

    expect(totalOpen(summary)).toBe(8);
    expect(severityShares(summary).map((share) => share.share)).toEqual([0.125, 0.375, 0, 0.5]);
  });

  it('returns zero shares when nothing is open', () => {
    const summary = { openBySeverity: { Sev1: 0, Sev2: 0, Sev3: 0, Sev4: 0 } };

    expect(severityShares(summary).every((share) => share.share === 0)).toBe(true);
  });
});
