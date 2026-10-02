import { describe, expect, it } from 'vitest';
import { anomalyKey, isStrongAnomaly } from './anomalies';

describe('anomalies', () => {
  it('flags z-scores of three or more as strong', () => {
    expect(isStrongAnomaly({ zScore: 3 })).toBe(true);
    expect(isStrongAnomaly({ zScore: 2.6 })).toBe(false);
  });

  it('keys an anomaly by service and week', () => {
    expect(anomalyKey({ serviceId: 'search', weekStart: '2026-09-21' })).toBe('search-2026-09-21');
  });
});
