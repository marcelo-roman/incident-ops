import { timelineKindLabels } from '../domain/labels';
import type { TimelineEntry } from '../domain/incident';
import { cx } from '../../../shared/lib/cx';
import { formatShortDate, formatTime } from '../../../shared/format/dates';
import styles from './Timeline.module.css';

export function Timeline({ entries }: { entries: TimelineEntry[] }) {
  return (
    <ol className={styles.timeline} aria-label="Incident timeline">
      {entries.map((entry) => (
        <li key={entry.id} className={cx(styles.entry, styles[entry.kind])}>
          <time className={styles.when} dateTime={entry.at}>
            {formatTime(entry.at)}
            <span className={styles.date}>{formatShortDate(entry.at)}</span>
          </time>
          <div className={styles.body}>
            <div className={styles.heading}>
              <span className={styles.kind}>{timelineKindLabels[entry.kind]}</span>
              <span className={styles.actor}>by {entry.actor}</span>
            </div>
            <p className={styles.message}>{entry.message}</p>
          </div>
        </li>
      ))}
    </ol>
  );
}
