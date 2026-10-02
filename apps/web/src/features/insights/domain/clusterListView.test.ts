import { describe, expect, it } from 'vitest';
import { clusterListView } from './clusters';
import type { RecurringCluster } from './insights';

function aCluster(label: string, count: number): RecurringCluster {
  return {
    label,
    terms: [],
    count,
    cohesion: 0.5,
    services: [],
    sampleTitles: [],
    firstSeen: '2026-06-01T00:00:00Z',
    lastSeen: '2026-09-30T00:00:00Z',
  };
}

const clusters = Array.from({ length: 12 }, (_, index) => aCluster(`cluster ${String(index)}`, index + 1));

describe('clusterListView', () => {
  it('shows the ten largest clusters when collapsed', () => {
    const view = clusterListView(clusters, false);

    expect(view.visible).toHaveLength(10);
    expect(view.visible[0]?.count).toBe(12);
    expect(view.visible.at(-1)?.count).toBe(3);
    expect(view).toMatchObject({ total: 12, canToggle: true, toggleLabel: 'Show all 12 clusters' });
  });

  it('shows every cluster, largest first, when expanded', () => {
    const view = clusterListView(clusters, true);

    expect(view.visible.map((cluster) => cluster.count)).toEqual([12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1]);
    expect(view.toggleLabel).toBe('Show fewer');
  });

  it('offers no toggle when everything fits', () => {
    const view = clusterListView(clusters.slice(0, 4), false);

    expect(view.visible).toHaveLength(4);
    expect(view.canToggle).toBe(false);
  });

  it('breaks count ties by label', () => {
    const view = clusterListView([aCluster('beta', 2), aCluster('alpha', 2)], false);

    expect(view.visible.map((cluster) => cluster.label)).toEqual(['alpha', 'beta']);
  });
});
