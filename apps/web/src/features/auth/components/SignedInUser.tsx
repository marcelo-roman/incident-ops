import { Button } from '../../../shared/ui/Button';
import { useSession } from '../hooks/useSession';
import { useSignOut } from '../hooks/useSignOut';
import styles from './SignedInUser.module.css';

export function SignedInUser() {
  const session = useSession();
  const signOut = useSignOut();
  if (session === null) {
    return null;
  }
  return (
    <div className={styles.user}>
      <span>
        Signed in as <strong className={styles.name}>{session.username}</strong>
      </span>
      <Button variant="quiet" onClick={signOut}>
        Sign out
      </Button>
    </div>
  );
}
