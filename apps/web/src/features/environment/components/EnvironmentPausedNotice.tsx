import { pausedRecheckMs } from '../domain/environmentStatus';
import styles from './EnvironmentPausedNotice.module.css';

const recheckSeconds = String(pausedRecheckMs / 1000);

export function EnvironmentPausedNotice() {
  return (
    <section className={styles.notice} aria-labelledby="environment-paused-title">
      <h1 id="environment-paused-title" className={styles.title}>
        Environment paused
      </h1>
      <p className={styles.message}>The demo environment is paused. It is started on request for evaluations.</p>
      <p className={styles.hint}>
        This page checks again every {recheckSeconds} seconds and opens the console once the environment is running.
      </p>
    </section>
  );
}
