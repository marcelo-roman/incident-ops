import type { ReactNode } from 'react';
import { describeError } from '../http/problem';
import { cx } from '../lib/cx';
import styles from './AsyncContent.module.css';
import { Button } from './Button';

interface QueryLike<T> {
  data: T | undefined;
  isPending: boolean;
  error: unknown;
  refetch: () => unknown;
}

interface AsyncContentProps<T> {
  query: QueryLike<T>;
  loadingLabel: string;
  children: (data: T) => ReactNode;
}

export function AsyncContent<T>({ query, loadingLabel, children }: Readonly<AsyncContentProps<T>>) {
  if (query.data !== undefined) {
    return <>{children(query.data)}</>;
  }
  if (query.isPending) {
    return (
      <div className={cx(styles.state, styles.loading)} role="status">
        {loadingLabel}
      </div>
    );
  }
  return (
    <div className={cx(styles.state, styles.error)} role="alert">
      <span>{describeError(query.error)}</span>
      <Button variant="quiet" onClick={() => void query.refetch()}>
        Try again
      </Button>
    </div>
  );
}

export function EmptyState({ children }: Readonly<{ children: ReactNode }>) {
  return <p className={styles.state}>{children}</p>;
}
