import { describeError } from '../../../shared/http/problem';
import { Button } from '../../../shared/ui/Button';
import { useRcaDraft } from '../api/useRcaDraft';
import { draftButtonLabel } from '../domain/draftButton';
import { rcaGuidance } from '../domain/draftSource';
import styles from './RcaDraft.module.css';
import { RcaDraftView } from './RcaDraftView';

interface RcaDraftPanelProps {
  incidentId: string;
  showGuidance?: boolean;
}

export function RcaDraftPanel({ incidentId, showGuidance = true }: RcaDraftPanelProps) {
  const mutation = useRcaDraft();
  return (
    <div>
      <div className={styles.intro}>
        {showGuidance && <p className={styles.note}>{rcaGuidance}</p>}
        <Button
          variant="primary"
          disabled={mutation.isPending}
          onClick={() => {
            mutation.mutate(incidentId);
          }}
        >
          {draftButtonLabel({ isPending: mutation.isPending, hasDraft: mutation.data !== undefined })}
        </Button>
      </div>
      <div aria-live="polite">
        {mutation.error !== null && (
          <p className={styles.error} role="alert">
            {describeError(mutation.error)}
          </p>
        )}
        {mutation.data !== undefined && <RcaDraftView draft={mutation.data} />}
      </div>
    </div>
  );
}
