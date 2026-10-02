import { useState } from 'react';
import { clusterListView } from '../domain/clusters';
import type { RecurringCluster } from '../domain/insights';

export function useClusterList(clusters: RecurringCluster[]) {
  const [expanded, setExpanded] = useState(false);
  return {
    ...clusterListView(clusters, expanded),
    expanded,
    toggle: () => {
      setExpanded((current) => !current);
    },
  };
}
