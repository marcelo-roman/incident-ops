import { AsyncContent, EmptyState } from '../../../shared/ui/AsyncContent';
import { Panel } from '../../../shared/ui/Panel';
import { anomalyKey, isStrongAnomaly } from '../domain/anomalies';
import type { VolumeAnomaly } from '../domain/insights';
import { useAnomalies } from '../api/useInsights';
import { useServiceName } from '../../incidents';
import { cx } from '../../../shared/lib/cx';
import { formatShortDate } from '../../../shared/format/dates';
import styles from './Insights.module.css';

function AnomalyRow({ anomaly }: { anomaly: VolumeAnomaly }) {
  const serviceName = useServiceName();
  return (
    <tr>
      <td>{serviceName(anomaly.serviceId)}</td>
      <td>{formatShortDate(anomaly.weekStart)}</td>
      <td>{anomaly.count}</td>
      <td>{anomaly.baselineMean.toFixed(1)}</td>
      <td className={cx(isStrongAnomaly(anomaly) && styles.zHigh)}>{anomaly.zScore.toFixed(1)}</td>
    </tr>
  );
}

export function AnomaliesPanel({ days }: { days: number }) {
  const query = useAnomalies(days);
  return (
    <Panel title="Volume anomalies" meta={query.data?.method} flush>
      <AsyncContent query={query} loadingLabel="Loading anomalies">
        {(report) => {
          if (report.anomalies.length === 0) {
            return <EmptyState>No week stands out from its service baseline in this window.</EmptyState>;
          }
          return (
            <div className={styles.scroller}>
              <table className={styles.table}>
                <thead>
                  <tr>
                    <th scope="col">Service</th>
                    <th scope="col">Week of</th>
                    <th scope="col">Incidents</th>
                    <th scope="col">Baseline</th>
                    <th scope="col">Z-score</th>
                  </tr>
                </thead>
                <tbody>
                  {report.anomalies.map((anomaly) => (
                    <AnomalyRow key={anomalyKey(anomaly)} anomaly={anomaly} />
                  ))}
                </tbody>
              </table>
            </div>
          );
        }}
      </AsyncContent>
    </Panel>
  );
}
