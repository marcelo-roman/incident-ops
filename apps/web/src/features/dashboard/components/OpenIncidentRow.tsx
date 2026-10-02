import { Link } from 'react-router';
import {
  formatIncidentNumber,
  type Incident,
  SeverityBadge,
  SlaFuse,
  StatusBadge,
  useServiceName,
  useSlaClocks,
} from '../../incidents';
import { cx } from '../../../shared/lib/cx';
import styles from './OpenIncidentsBoard.module.css';

export function OpenIncidentRow({ incident }: { incident: Incident }) {
  const clocks = useSlaClocks(incident);
  const serviceName = useServiceName();
  return (
    <li className={cx(styles.row, styles.rowLink, styles[clocks.state])}>
      <SeverityBadge severity={incident.severity} />
      <div className={styles.identity}>
        <Link className={styles.title} to={`/incidents/${incident.id}`}>
          {incident.title}
        </Link>
        <span className={styles.context}>
          <span>{formatIncidentNumber(incident.number)}</span>
          <span>{serviceName(incident.serviceId)}</span>
          <StatusBadge status={incident.status} />
        </span>
      </div>
      {clocks.acknowledge !== null && <SlaFuse clock={clocks.acknowledge} />}
      {clocks.acknowledge === null && <span className={styles.waiting}>{incident.assignee ?? 'Unassigned'}</span>}
      {clocks.resolve !== null && <SlaFuse clock={clocks.resolve} />}
    </li>
  );
}
