import { useId, type ReactNode } from 'react';
import { cx } from '../lib/cx';
import styles from './Panel.module.css';

interface PanelProps {
  title: string;
  meta?: ReactNode;
  actions?: ReactNode;
  flush?: boolean;
  className?: string;
  children: ReactNode;
}

export function Panel({ title, meta, actions, flush = false, className, children }: Readonly<PanelProps>) {
  const headingId = useId();
  return (
    <section className={cx(styles.panel, className)} aria-labelledby={headingId}>
      <header className={styles.header}>
        <h2 id={headingId} className={styles.title}>
          {title}
        </h2>
        {Boolean(meta) && <span className={styles.meta}>{meta}</span>}
        {actions}
      </header>
      <div className={cx(styles.body, flush && styles.flush)}>{children}</div>
    </section>
  );
}
