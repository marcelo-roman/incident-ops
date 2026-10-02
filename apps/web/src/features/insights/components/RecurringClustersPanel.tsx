import { useId } from 'react';
import { AsyncContent, EmptyState } from '../../../shared/ui/AsyncContent';
import { Button } from '../../../shared/ui/Button';
import { Panel } from '../../../shared/ui/Panel';
import { describeClusterServices } from '../domain/clusters';
import type { RecurringCluster } from '../domain/insights';
import { useRecurringClusters } from '../api/useInsights';
import { useServiceName } from '../../incidents';
import { useClusterList } from '../hooks/useClusterList';
import styles from './Insights.module.css';

function ClusterItem({ cluster }: Readonly<{ cluster: RecurringCluster }>) {
  const serviceName = useServiceName();
  return (
    <li className={styles.cluster}>
      <div className={styles.clusterHead}>
        <span className={styles.clusterLabel}>{cluster.label}</span>
        <span className={styles.clusterCount}>{cluster.count} incidents</span>
      </div>
      <ul className={styles.terms} aria-label="Top terms">
        {cluster.terms.map((term) => (
          <li key={term} className={styles.term}>
            {term}
          </li>
        ))}
      </ul>
      <p className={styles.meta}>{describeClusterServices(cluster, serviceName)}</p>
      <ul className={styles.samples} aria-label="Sample titles">
        {cluster.sampleTitles.map((title) => (
          <li key={title}>{title}</li>
        ))}
      </ul>
    </li>
  );
}

function ClusterList({ clusters }: Readonly<{ clusters: RecurringCluster[] }>) {
  const list = useClusterList(clusters);
  const listId = useId();
  return (
    <>
      <ul id={listId} className={styles.clusters}>
        {list.visible.map((cluster) => (
          <ClusterItem key={cluster.label} cluster={cluster} />
        ))}
      </ul>
      {list.canToggle && (
        <div className={styles.clusterToggle}>
          <Button variant="quiet" aria-expanded={list.expanded} aria-controls={listId} onClick={list.toggle}>
            {list.toggleLabel}
          </Button>
        </div>
      )}
    </>
  );
}

export function RecurringClustersPanel({ days }: Readonly<{ days: number }>) {
  const query = useRecurringClusters(days);
  return (
    <Panel title="Recurring incidents" meta={query.data && `${String(query.data.incidentsAnalyzed)} analyzed`} flush>
      <AsyncContent query={query} loadingLabel="Loading recurring clusters">
        {(report) => {
          if (report.clusters.length === 0) {
            return <EmptyState>No recurring patterns in this window.</EmptyState>;
          }
          return <ClusterList clusters={report.clusters} />;
        }}
      </AsyncContent>
    </Panel>
  );
}
