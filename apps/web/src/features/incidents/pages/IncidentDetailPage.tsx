import { Link, useParams } from 'react-router';
import { AsyncContent } from '../../../shared/ui/AsyncContent';
import { useIncident } from '../api/useIncidents';
import styles from '../components/IncidentDetail.module.css';
import { IncidentDetailView } from '../components/IncidentDetailView';

export function IncidentDetailPage() {
  const { incidentId = '' } = useParams();
  const query = useIncident(incidentId);
  return (
    <>
      <Link className={styles.back} to="/incidents">
        All incidents
      </Link>
      <AsyncContent query={query} loadingLabel="Loading incident">
        {(incident) => <IncidentDetailView incident={incident} />}
      </AsyncContent>
    </>
  );
}
