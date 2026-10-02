import { cx } from '../../../shared/lib/cx';
import type { SlaClock } from '../domain/sla';
import { slaTargetLabels } from '../domain/labels';
import { burnPercent, clockReading, describeClock } from '../domain/slaPresentation';
import styles from './SlaFuse.module.css';

interface SlaFuseProps {
  clock: SlaClock;
  size?: 'regular' | 'large';
}

export function SlaFuse({ clock, size = 'regular' }: Readonly<SlaFuseProps>) {
  const reading = clockReading(clock);
  return (
    <div
      className={cx(styles.fuse, styles[clock.state], size === 'large' && styles.large)}
      role="timer"
      aria-label={describeClock(clock)}
    >
      <span className={styles.label}>{slaTargetLabels[clock.target]}</span>
      <span className={cx(styles.time, reading.overdue && styles.overdue)}>{reading.time}</span>
      <span className={styles.track} aria-hidden="true">
        <span className={styles.burn} style={{ width: `${String(burnPercent(clock))}%` }} />
      </span>
    </div>
  );
}
