import { describe, expect, it } from 'vitest';
import { formatIncidentNumber, isIncidentSource, isIncidentStatus, isOpen, isSeverity } from './incident';

describe('incident', () => {
  it('formats the human friendly number', () => {
    expect(formatIncidentNumber(1042)).toBe('INC-1042');
  });

  it('treats everything but Resolved as open', () => {
    expect(isOpen({ status: 'Triggered' })).toBe(true);
    expect(isOpen({ status: 'Mitigated' })).toBe(true);
    expect(isOpen({ status: 'Resolved' })).toBe(false);
  });

  it('recognises contract enum values only', () => {
    expect(isSeverity('Sev2')).toBe(true);
    expect(isSeverity('P1')).toBe(false);
    expect(isIncidentStatus('Acknowledged')).toBe(true);
    expect(isIncidentStatus('Closed')).toBe(false);
    expect(isIncidentSource('AzureMonitor')).toBe(true);
    expect(isIncidentSource('PagerDuty')).toBe(false);
  });
});
