import type { ReactNode, SubmitEventHandler } from 'react';
import { describeError } from '../../../../shared/http/problem';
import { Button } from '../../../../shared/ui/Button';
import styles from './ActionForm.module.css';

interface ActionFormProps {
  label: string;
  submitLabel: string;
  pendingLabel: string;
  isPending: boolean;
  error: unknown;
  onSubmit: SubmitEventHandler<HTMLFormElement>;
  onCancel: () => void;
  children: ReactNode;
}

export function ActionForm(props: Readonly<ActionFormProps>) {
  return (
    <form className={styles.form} aria-label={props.label} noValidate onSubmit={props.onSubmit}>
      {props.children}
      {props.error !== null && (
        <p className={styles.error} role="alert">
          {describeError(props.error)}
        </p>
      )}
      <div className={styles.footer}>
        <Button type="submit" variant="primary" disabled={props.isPending}>
          {props.isPending && props.pendingLabel}
          {!props.isPending && props.submitLabel}
        </Button>
        <Button variant="quiet" onClick={props.onCancel}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
