import type { UseFormRegisterReturn } from 'react-hook-form';
import { severities, SeverityBadge } from '../../incidents';
import { severityTargets } from '../domain/severityTargets';
import styles from './SeverityPicker.module.css';

interface SeverityPickerProps {
  registration: UseFormRegisterReturn;
  error: string | undefined;
}

export function SeverityPicker({ registration, error }: Readonly<SeverityPickerProps>) {
  return (
    <fieldset className={styles.fieldset} aria-invalid={error !== undefined}>
      <legend className={styles.legend}>Severity</legend>
      <div className={styles.options}>
        {severities.map((severity) => (
          <label key={severity} className={styles.option}>
            <input type="radio" value={severity} {...registration} />
            <SeverityBadge severity={severity} />
            <span className={styles.targets}>{severityTargets(severity)}</span>
          </label>
        ))}
      </div>
      {error !== undefined && (
        <p className={styles.error} role="alert">
          {error}
        </p>
      )}
    </fieldset>
  );
}
