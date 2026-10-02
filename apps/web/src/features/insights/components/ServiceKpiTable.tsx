import { Panel } from '../../../shared/ui/Panel';
import { formatMinutes, formatPercent } from '../../../shared/format/duration';
import type { KpiGroup } from '../domain/insights';
import { useServiceName } from '../../incidents';
import styles from './Insights.module.css';

export function ServiceKpiTable({ groups }: Readonly<{ groups: KpiGroup[] }>) {
  const serviceName = useServiceName();
  return (
    <Panel title="By service" flush>
      <div className={styles.scroller}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th scope="col">Service</th>
              <th scope="col">Incidents</th>
              <th scope="col">Median ack</th>
              <th scope="col">Median resolve</th>
              <th scope="col">SLA</th>
            </tr>
          </thead>
          <tbody>
            {groups.map((group) => (
              <tr key={group.serviceId ?? 'unknown'}>
                <td>{serviceName(group.serviceId ?? '')}</td>
                <td>{group.incidents}</td>
                <td>{formatMinutes(group.mtta.medianMinutes)}</td>
                <td>{formatMinutes(group.mttr.medianMinutes)}</td>
                <td>{formatPercent(group.sla.overallPct)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </Panel>
  );
}
