import { formatShortDate } from '../../../shared/format/dates';
import styles from './Insights.module.css';
import type { TrendMetricDefinition, TrendPoint } from '../domain/trendMetrics';

export function TrendDataTable({ points, metric }: { points: TrendPoint[]; metric: TrendMetricDefinition }) {
  return (
    <details className={styles.dataToggle}>
      <summary>Show weekly values</summary>
      <div className={styles.scroller}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th scope="col">Week of</th>
              <th scope="col">{metric.label}</th>
            </tr>
          </thead>
          <tbody>
            {points.map((point) => (
              <tr key={point.weekStart}>
                <td>{formatShortDate(point.weekStart)}</td>
                <td>{metric.format(point.value)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}
