import type { IncidentSource, IncidentStatus, Severity, SlaState } from '../domain/incident';
import { cx } from '../../../shared/lib/cx';
import styles from './Badge.module.css';
import { slaStateLabels, sourceLabels, statusLabels } from '../domain/labels';

export function SeverityBadge({ severity }: { severity: Severity }) {
  return (
    <span className={cx(styles.badge, styles[severity])}>
      <span className={styles.marker} aria-hidden="true" />
      {severity}
    </span>
  );
}

export function StatusBadge({ status }: { status: IncidentStatus }) {
  return <span className={cx(styles.badge, styles.status, styles[status])}>{statusLabels[status]}</span>;
}

export function SlaStateBadge({ state }: { state: SlaState }) {
  return (
    <span className={cx(styles.badge, styles[state])}>
      <span className={styles.marker} aria-hidden="true" />
      {slaStateLabels[state]}
    </span>
  );
}

export function SourceBadge({ source }: { source: IncidentSource }) {
  return <span className={cx(styles.badge, styles.source, styles[source])}>{sourceLabels[source]}</span>;
}

export function AckMissedBadge() {
  return (
    <span className={cx(styles.badge, styles.ackMissed)} title="Acknowledged late or escalated before acknowledgement">
      <span className={styles.marker} aria-hidden="true" />
      Ack SLA missed
    </span>
  );
}
