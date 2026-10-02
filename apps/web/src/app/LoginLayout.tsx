import { LoginPage } from '../features/auth';
import { EnvironmentGate } from '../features/environment';
import styles from './LoginLayout.module.css';

export function LoginLayout() {
  return (
    <main id="main" className={styles.page}>
      <div className={styles.content}>
        <EnvironmentGate>
          <LoginPage />
        </EnvironmentGate>
      </div>
    </main>
  );
}
