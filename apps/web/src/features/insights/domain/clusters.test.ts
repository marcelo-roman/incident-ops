import { describe, expect, it } from 'vitest';
import { describeClusterServices } from './clusters';
import type { RecurringCluster } from './insights';

describe('describeClusterServices', () => {
  it('lists services with their incident counts', () => {
    const cluster: RecurringCluster = {
      label: 'certificate expiry',
      terms: [],
      count: 5,
      cohesion: 0.6,
      services: [
        { serviceId: 'identity', count: 3 },
        { serviceId: 'payments-gateway', count: 2 },
      ],
      sampleTitles: [],
      firstSeen: '2026-06-01T00:00:00Z',
      lastSeen: '2026-09-30T00:00:00Z',
    };

    expect(describeClusterServices(cluster, (id) => id.toUpperCase())).toBe('IDENTITY (3), PAYMENTS-GATEWAY (2)');
  });
});
