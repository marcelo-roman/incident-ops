import type { ConnectionStatus } from '../connection/connectionStatus';
import { cx } from '../../../shared/lib/cx';
import styles from './ConnectionIndicator.module.css';

const statusText: Readonly<Record<ConnectionStatus, string>> = {
  connected: 'Live',
  connecting: 'Connecting',
  reconnecting: 'Reconnecting',
  offline: 'Offline, retrying',
  disabled: 'Live updates off',
};

const statusDescription: Readonly<Record<ConnectionStatus, string>> = {
  connected: 'Live updates connected',
  connecting: 'Connecting to live updates',
  reconnecting: 'Live updates interrupted, reconnecting',
  offline: 'Live updates offline, retrying with backoff',
  disabled: 'Live updates are disabled in mock mode',
};

export function ConnectionIndicator({ status }: { status: ConnectionStatus }) {
  return (
    <span className={cx(styles.indicator, styles[status])} role="status" title={statusDescription[status]}>
      <span className={styles.dot} aria-hidden="true" />
      <span className={styles.text}>{statusText[status]}</span>
    </span>
  );
}
