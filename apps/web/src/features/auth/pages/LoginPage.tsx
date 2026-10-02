import { Navigate } from 'react-router';
import { LoginForm } from '../components/LoginForm';
import { useReturnPath } from '../hooks/useReturnPath';
import { useSession } from '../hooks/useSession';
import styles from './LoginPage.module.css';

export function LoginPage() {
  const session = useSession();
  const returnTo = useReturnPath();
  if (session !== null) {
    return <Navigate to={returnTo} replace />;
  }
  return (
    <section className={styles.card} aria-labelledby="login-title">
      <h1 id="login-title" className={styles.title}>
        Sign in to Incident Ops
      </h1>
      <p className={styles.summary}>The operations console is a demo. Credentials are shared on request.</p>
      <LoginForm returnTo={returnTo} />
    </section>
  );
}
