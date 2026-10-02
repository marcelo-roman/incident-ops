import { cx } from '../lib/cx';
import styles from './KpiStrip.module.css';

export interface Kpi {
  label: string;
  value: string;
  alert?: boolean;
}

export function KpiStrip({ items, label }: { items: Kpi[]; label: string }) {
  return (
    <dl className={styles.strip} aria-label={label}>
      {items.map((item) => (
        <div key={item.label} className={cx(styles.tile, item.alert === true && styles.alert)}>
          <dt className={styles.label}>{item.label}</dt>
          <dd className={styles.value}>{item.value}</dd>
        </div>
      ))}
    </dl>
  );
}
