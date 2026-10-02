import { AsyncContent, EmptyState } from '../../../shared/ui/AsyncContent';
import { Panel } from '../../../shared/ui/Panel';
import { useOpenIncidentsByUrgency } from '../../incidents';
import { OpenIncidentRow } from './OpenIncidentRow';
import styles from './OpenIncidentsBoard.module.css';

export function OpenIncidentsBoard() {
  const query = useOpenIncidentsByUrgency();
  return (
    <Panel title="Open incidents" meta={`${String(query.incidents.length)} open`} flush>
      <AsyncContent query={query} loadingLabel="Loading open incidents">
        {() => {
          if (query.incidents.length === 0) {
            return <EmptyState>No open incidents. New declarations appear here as they arrive.</EmptyState>;
          }
          return (
            <ol className={styles.list}>
              {query.incidents.map((incident) => (
                <OpenIncidentRow key={incident.id} incident={incident} />
              ))}
            </ol>
          );
        }}
      </AsyncContent>
    </Panel>
  );
}
