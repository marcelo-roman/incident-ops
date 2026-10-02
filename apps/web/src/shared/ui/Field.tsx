import {
  useId,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react';
import { cx } from '../lib/cx';
import styles from './Field.module.css';

interface FieldChrome {
  label: string;
  hint?: string;
  error?: string | undefined;
}

interface ControlProps {
  id: string;
  className: string;
  'aria-invalid': boolean;
  'aria-describedby': string | undefined;
}

interface FieldProps extends FieldChrome {
  className?: string;
  children: (control: ControlProps) => ReactNode;
}

function describedBy(hintId: string, errorId: string, hint: string | undefined, error: string | undefined) {
  const ids = [hint !== undefined && hintId, error !== undefined && errorId].filter(Boolean);
  if (ids.length === 0) {
    return undefined;
  }
  return ids.join(' ');
}

export function Field({ label, hint, error, className, children }: Readonly<FieldProps>) {
  const id = useId();
  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;
  return (
    <div className={cx(styles.field, className)}>
      <label className={styles.label} htmlFor={id}>
        {label}
      </label>
      {children({
        id,
        className: cx(styles.control),
        'aria-invalid': error !== undefined,
        'aria-describedby': describedBy(hintId, errorId, hint, error),
      })}
      {hint !== undefined && (
        <span id={hintId} className={styles.hint}>
          {hint}
        </span>
      )}
      {error !== undefined && (
        <span id={errorId} className={styles.error}>
          {error}
        </span>
      )}
    </div>
  );
}

type TextFieldProps = FieldChrome & InputHTMLAttributes<HTMLInputElement>;

export function TextField({ label, hint, error, ...inputProps }: TextFieldProps) {
  return (
    <Field label={label} {...(hint !== undefined && { hint })} error={error}>
      {(control) => <input {...inputProps} {...control} />}
    </Field>
  );
}

type TextAreaFieldProps = FieldChrome & TextareaHTMLAttributes<HTMLTextAreaElement>;

export function TextAreaField({ label, hint, error, ...textareaProps }: TextAreaFieldProps) {
  return (
    <Field label={label} {...(hint !== undefined && { hint })} error={error}>
      {(control) => <textarea {...textareaProps} {...control} />}
    </Field>
  );
}

type SelectFieldProps = FieldChrome & SelectHTMLAttributes<HTMLSelectElement>;

export function SelectField({ label, hint, error, children, ...selectProps }: SelectFieldProps) {
  return (
    <Field label={label} {...(hint !== undefined && { hint })} error={error}>
      {(control) => (
        <select {...selectProps} {...control}>
          {children}
        </select>
      )}
    </Field>
  );
}
