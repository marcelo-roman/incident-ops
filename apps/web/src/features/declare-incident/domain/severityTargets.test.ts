import { describe, expect, it } from 'vitest';
import { severityTargets } from './severityTargets';

describe('severityTargets', () => {
  it('states the SLA policy for each severity', () => {
    expect(severityTargets('Sev1')).toBe('Acknowledge in 15m 00s, resolve in 4h 00m');
    expect(severityTargets('Sev4')).toBe('Acknowledge in 1d 00h, resolve in 10d 00h');
  });
});
