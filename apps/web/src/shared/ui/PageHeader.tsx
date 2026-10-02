import type { ReactNode } from 'react';
import styles from './PageHeader.module.css';

interface PageHeaderProps {
  title: ReactNode;
  summary?: ReactNode;
  actions?: ReactNode;
}

export function PageHeader({ title, summary, actions }: Readonly<PageHeaderProps>) {
  return (
    <div className={styles.header}>
      <div>
        <h1 className={styles.title}>{title}</h1>
        {summary !== undefined && <p className={styles.summary}>{summary}</p>}
      </div>
      {actions !== undefined && <div className={styles.actions}>{actions}</div>}
    </div>
  );
}
