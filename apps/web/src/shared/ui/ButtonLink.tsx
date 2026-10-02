import { Link, type LinkProps } from 'react-router';
import { cx } from '../lib/cx';
import styles from './Button.module.css';

interface ButtonLinkProps extends LinkProps {
  variant?: 'default' | 'primary';
}

export function ButtonLink({ variant = 'default', className, ...props }: Readonly<ButtonLinkProps>) {
  return <Link className={cx(styles.button, variant === 'primary' && styles.primary, className)} {...props} />;
}
