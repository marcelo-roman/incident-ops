import { AsyncContent } from '../../../shared/ui/AsyncContent';
import { SeverityBadge } from '../../incidents';
import { Panel } from '../../../shared/ui/Panel';
import { severityShares } from '../domain/metrics';
import { useMetricsSummary } from '../api/useMetricsSummary';
import { cx } from '../../../shared/lib/cx';
import styles from './SeverityMix.module.css';

export function SeverityMix() {
  const query = useMetricsSummary();
  return (
    <Panel title="Open by severity">
      <AsyncContent query={query} loadingLabel="Loading severity mix">
        {(summary) => {
          const shares = severityShares(summary);
          return (
            <>
              <div className={styles.bar} aria-hidden="true">
                {shares
                  .filter((share) => share.count > 0)
                  .map((share) => (
                    <span
                      key={share.severity}
                      className={cx(styles.segment, styles[share.severity])}
                      style={{ flexGrow: share.share }}
                    />
                  ))}
              </div>
              <ul className={styles.legend}>
                {shares.map((share) => (
                  <li key={share.severity} className={styles.item}>
                    <SeverityBadge severity={share.severity} />
                    <span className={styles.count}>{share.count}</span>
                  </li>
                ))}
              </ul>
            </>
          );
        }}
      </AsyncContent>
    </Panel>
  );
}
