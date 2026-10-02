import { Link } from 'react-router';
import { AsyncContent, EmptyState } from '../../../shared/ui/AsyncContent';
import { formatIncidentNumber, SeverityBadge, useIncidents } from '../../incidents';
import { Panel } from '../../../shared/ui/Panel';
import { RcaDraftPanel, rcaGuidance } from '../../rca';
import styles from './Insights.module.css';

const resolvedFilters = { status: 'Resolved' } as const;
const queueSize = 5;

export function RcaQueuePanel() {
  const query = useIncidents(resolvedFilters);
  return (
    <Panel title="Root cause analysis drafts" meta={rcaGuidance} flush>
      <AsyncContent query={query} loadingLabel="Loading resolved incidents">
        {(incidents) => {
          if (incidents.length === 0) {
            return <EmptyState>Resolved incidents appear here, ready for an RCA draft.</EmptyState>;
          }
          return (
            <ul className={styles.resolvedList}>
              {incidents.slice(0, queueSize).map((incident) => (
                <li key={incident.id} className={styles.resolvedItem}>
                  <div className={styles.resolvedHead}>
                    <Link to={`/incidents/${incident.id}`}>
                      {formatIncidentNumber(incident.number)} {incident.title}
                    </Link>
                    <SeverityBadge severity={incident.severity} />
                  </div>
                  <RcaDraftPanel incidentId={incident.id} showGuidance={false} />
                </li>
              ))}
            </ul>
          );
        }}
      </AsyncContent>
    </Panel>
  );
}
