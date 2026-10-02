import type { RecurringCluster } from './insights';

export function describeClusterServices(cluster: RecurringCluster, serviceName: (serviceId: string) => string): string {
  return cluster.services.map((entry) => `${serviceName(entry.serviceId)} (${String(entry.count)})`).join(', ');
}

export const clusterPreviewSize = 10;

export interface ClusterListView {
  visible: RecurringCluster[];
  total: number;
  canToggle: boolean;
  toggleLabel: string;
}

function byCountDescending(left: RecurringCluster, right: RecurringCluster): number {
  return right.count - left.count || left.label.localeCompare(right.label);
}

function toggleLabelFor(expanded: boolean, total: number): string {
  if (expanded) {
    return 'Show fewer';
  }
  return `Show all ${String(total)} clusters`;
}

function visibleClusters(ranked: RecurringCluster[], expanded: boolean, limit: number): RecurringCluster[] {
  if (expanded) {
    return ranked;
  }
  return ranked.slice(0, limit);
}

export function clusterListView(
  clusters: RecurringCluster[],
  expanded: boolean,
  limit: number = clusterPreviewSize,
): ClusterListView {
  const ranked = [...clusters].sort(byCountDescending);
  const canToggle = ranked.length > limit;
  return {
    visible: visibleClusters(ranked, expanded, limit),
    total: ranked.length,
    canToggle,
    toggleLabel: toggleLabelFor(expanded, ranked.length),
  };
}
