import { describe, expect, it } from 'vitest';
import { timeToAcknowledgeMs, timeToResolveMs } from './incidentDurations';

const createdAt = '2026-10-02T12:00:00Z';

describe('incident durations', () => {
  it('measures from creation to each milestone', () => {
    expect(timeToAcknowledgeMs({ createdAt, acknowledgedAt: '2026-10-02T12:04:00Z' })).toBe(240_000);
    expect(timeToResolveMs({ createdAt, resolvedAt: '2026-10-02T15:00:00Z' })).toBe(10_800_000);
  });

  it('is null until the milestone happens', () => {
    expect(timeToAcknowledgeMs({ createdAt, acknowledgedAt: null })).toBeNull();
    expect(timeToResolveMs({ createdAt, resolvedAt: null })).toBeNull();
  });
});
