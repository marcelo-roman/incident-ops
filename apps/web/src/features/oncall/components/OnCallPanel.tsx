import { AsyncContent } from '../../../shared/ui/AsyncContent';
import { Panel } from '../../../shared/ui/Panel';
import { escalationTiers } from '../domain/oncall';
import { useOnCall } from '../api/useOnCall';
import { formatDate } from '../../../shared/format/dates';
import styles from './OnCallPanel.module.css';

export function OnCallPanel() {
  const query = useOnCall();
  const weekStart = query.data?.weekStart;
  return (
    <Panel title="On call" meta={weekStart !== undefined && `Week of ${formatDate(weekStart)}`} flush>
      <AsyncContent query={query} loadingLabel="Loading on-call rotation">
        {(roster) => (
          <ol className={styles.tiers} aria-label="Escalation order">
            {escalationTiers(roster).map((tier) => (
              <li key={tier.level} className={styles.tier}>
                <span className={styles.level} aria-label={`Level ${String(tier.level)}`}>
                  {tier.level}
                </span>
                <span>
                  <span className={styles.engineer}>{tier.engineer}</span>
                  <span className={styles.role}>{tier.role}</span>
                </span>
              </li>
            ))}
          </ol>
        )}
      </AsyncContent>
    </Panel>
  );
}
