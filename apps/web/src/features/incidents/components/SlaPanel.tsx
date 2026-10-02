import { SlaStateBadge } from './Badges';
import { Panel } from '../../../shared/ui/Panel';
import { SlaFuse } from './SlaFuse';
import { formatCountdown } from '../../../shared/format/duration';
import type { Incident } from '../domain/incident';
import { timeToAcknowledgeMs, timeToResolveMs } from '../domain/incidentDurations';
import { useSlaClocks } from '../hooks/useSlaClocks';
import styles from './SlaPanel.module.css';

function Outcome({ label, elapsedMs }: { label: string; elapsedMs: number | null }) {
  if (elapsedMs === null) {
    return null;
  }
  return (
    <div className={styles.outcome}>
      <span className={styles.figure}>{formatCountdown(elapsedMs)}</span>
      <span className={styles.caption}>{label}</span>
    </div>
  );
}

export function SlaPanel({ incident }: { incident: Incident }) {
  const clocks = useSlaClocks(incident);
  return (
    <Panel title="Service level" actions={<SlaStateBadge state={clocks.state} />}>
      <div className={styles.clocks}>
        {clocks.acknowledge !== null && <SlaFuse clock={clocks.acknowledge} size="large" />}
        {clocks.acknowledge === null && (
          <Outcome label="Time to acknowledge" elapsedMs={timeToAcknowledgeMs(incident)} />
        )}
        {clocks.resolve !== null && <SlaFuse clock={clocks.resolve} size="large" />}
        {clocks.resolve === null && <Outcome label="Time to resolve" elapsedMs={timeToResolveMs(incident)} />}
      </div>
    </Panel>
  );
}
