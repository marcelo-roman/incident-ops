import { useServiceName } from '../api/useServices';
import type { Incident } from '../domain/incident';
import { incidentFacts } from '../domain/incidentFacts';
import styles from './IncidentFacts.module.css';

export function IncidentFacts({ incident }: { incident: Incident }) {
  const serviceName = useServiceName();
  return (
    <dl className={styles.facts}>
      {incidentFacts(incident, serviceName(incident.serviceId)).map((fact) => (
        <div key={fact.term}>
          <dt className={styles.term}>{fact.term}</dt>
          <dd className={styles.detail}>{fact.detail}</dd>
        </div>
      ))}
    </dl>
  );
}
