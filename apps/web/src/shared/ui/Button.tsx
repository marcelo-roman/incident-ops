import type { ButtonHTMLAttributes } from 'react';
import { cx } from '../lib/cx';
import styles from './Button.module.css';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'default' | 'primary' | 'quiet';
}

export function Button({ variant = 'default', className, type = 'button', ...props }: ButtonProps) {
  return (
    <button
      type={type}
      className={cx(
        styles.button,
        variant === 'primary' && styles.primary,
        variant === 'quiet' && styles.quiet,
        props['aria-pressed'] === true && styles.pressed,
        className,
      )}
      {...props}
    />
  );
}
