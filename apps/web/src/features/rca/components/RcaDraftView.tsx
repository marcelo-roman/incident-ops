import { cx } from '../../../shared/lib/cx';
import { formatDateTime, formatTime } from '../../../shared/format/dates';
import { describeDraftSource } from '../domain/draftSource';
import type { RcaActionItem, RcaDraft } from '../domain/rcaDraft';
import styles from './RcaDraft.module.css';

function BulletSection({ title, items }: Readonly<{ title: string; items: string[] }>) {
  if (items.length === 0) {
    return null;
  }
  return (
    <section className={styles.section}>
      <h3 className={styles.heading}>{title}</h3>
      <ul className={styles.bullets}>
        {items.map((item) => (
          <li key={item}>{item}</li>
        ))}
      </ul>
    </section>
  );
}

function ActionItems({ items }: Readonly<{ items: RcaActionItem[] }>) {
  if (items.length === 0) {
    return null;
  }
  return (
    <section className={styles.section}>
      <h3 className={styles.heading}>Action items</h3>
      <ul className={styles.actions}>
        {items.map((item) => (
          <li key={item.title} className={styles.action}>
            <span className={styles.priority}>{item.priority}</span>
            <span>{item.title}</span>
            <span className={styles.owner}>{item.owner}</span>
          </li>
        ))}
      </ul>
    </section>
  );
}

export function RcaDraftView({ draft }: Readonly<{ draft: RcaDraft }>) {
  const source = describeDraftSource(draft.generatedBy);
  return (
    <article className={styles.draft} aria-label="RCA draft">
      <p className={cx(styles.source, styles[source.kind])}>
        {source.label}, {formatDateTime(draft.generatedAt)}
      </p>
      <section className={styles.section}>
        <h3 className={styles.heading}>Summary</h3>
        <p className={styles.prose}>{draft.summary}</p>
      </section>
      <section className={styles.section}>
        <h3 className={styles.heading}>Impact</h3>
        <p className={styles.prose}>{draft.impact}</p>
      </section>
      {draft.timeline.length > 0 && (
        <section className={styles.section}>
          <h3 className={styles.heading}>Timeline</h3>
          <ol className={styles.steps}>
            {draft.timeline.map((item) => (
              <li key={`${item.at}-${item.event}`} className={styles.step}>
                <time className={styles.stepTime} dateTime={item.at}>
                  {formatTime(item.at)}
                </time>
                <span>{item.event}</span>
              </li>
            ))}
          </ol>
        </section>
      )}
      <BulletSection title="Contributing factors" items={draft.contributingFactors} />
      <ActionItems items={draft.actionItems} />
    </article>
  );
}
