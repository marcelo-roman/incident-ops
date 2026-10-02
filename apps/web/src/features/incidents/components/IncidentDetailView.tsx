import { AckMissedBadge, SeverityBadge, SourceBadge, StatusBadge } from './Badges';
import { PageHeader } from '../../../shared/ui/PageHeader';
import { Panel } from '../../../shared/ui/Panel';
import { formatIncidentNumber, type IncidentDetail } from '../domain/incident';
import { RcaDraftPanel } from '../../rca';
import { IncidentActions } from './actions/IncidentActions';
import styles from './IncidentDetail.module.css';
import { IncidentFacts } from './IncidentFacts';
import { SlaPanel } from './SlaPanel';
import { Timeline } from './Timeline';

export function IncidentDetailView({ incident }: { incident: IncidentDetail }) {
  return (
    <>
      <PageHeader
        title={
          <>
            <span className={styles.number}>{formatIncidentNumber(incident.number)}</span>
            {incident.title}
          </>
        }
        summary={
          <span className={styles.badges}>
            <SeverityBadge severity={incident.severity} />
            <StatusBadge status={incident.status} />
            <SourceBadge source={incident.source} />
            {incident.acknowledgementBreached && <AckMissedBadge />}
          </span>
        }
      />
      <div className={styles.layout}>
        <div className={styles.column}>
          <SlaPanel incident={incident} />
          <Panel title="Details">
            <IncidentFacts incident={incident} />
          </Panel>
          <Panel title="Description">
            <p className={styles.prose}>{incident.description}</p>
          </Panel>
          {incident.rootCause !== null && (
            <Panel title="Root cause">
              <p className={styles.prose}>{incident.rootCause}</p>
            </Panel>
          )}
          <Panel title="Timeline" meta={`${String(incident.timeline.length)} entries`}>
            <Timeline entries={incident.timeline} />
          </Panel>
        </div>
        <div className={styles.column}>
          <IncidentActions incident={incident} />
          {incident.status === 'Resolved' && (
            <Panel title="Root cause analysis">
              <RcaDraftPanel incidentId={incident.id} />
            </Panel>
          )}
        </div>
      </div>
    </>
  );
}
