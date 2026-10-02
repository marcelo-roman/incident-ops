import { Link } from 'react-router';
import { AckMissedBadge, SeverityBadge, SlaStateBadge, SourceBadge, StatusBadge } from './Badges';
import { formatRelative } from '../../../shared/format/duration';
import { formatIncidentNumber, type Incident } from '../domain/incident';
import { useNow } from '../../../shared/time/useNow';
import { useSlaClocks } from '../hooks/useSlaClocks';
import styles from './IncidentTable.module.css';

interface IncidentTableRowProps {
  incident: Incident;
  serviceName: string;
}

export function IncidentTableRow({ incident, serviceName }: IncidentTableRowProps) {
  const { state } = useSlaClocks(incident);
  const now = useNow();
  return (
    <tr>
      <td className={styles.number}>{formatIncidentNumber(incident.number)}</td>
      <td>
        <Link className={styles.title} to={`/incidents/${incident.id}`}>
          {incident.title}
        </Link>
      </td>
      <td>{serviceName}</td>
      <td>
        <SeverityBadge severity={incident.severity} />
      </td>
      <td>
        <StatusBadge status={incident.status} />
      </td>
      <td>
        <span className={styles.badges}>
          <SlaStateBadge state={state} />
          {incident.acknowledgementBreached && <AckMissedBadge />}
        </span>
      </td>
      <td>
        <SourceBadge source={incident.source} />
      </td>
      <td className={styles.muted}>
        <time dateTime={incident.createdAt}>{formatRelative(incident.createdAt, now)}</time>
      </td>
      <td className={styles.muted}>{incident.assignee ?? 'Unassigned'}</td>
    </tr>
  );
}
