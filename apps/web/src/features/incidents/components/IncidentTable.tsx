import type { Incident } from '../domain/incident';
import { useServiceName } from '../api/useServices';
import styles from './IncidentTable.module.css';
import { IncidentTableRow } from './IncidentTableRow';

const columns = ['Incident', 'Title', 'Service', 'Severity', 'Status', 'SLA', 'Source', 'Opened', 'Assignee'] as const;

export function IncidentTable({ incidents }: { incidents: Incident[] }) {
  const serviceName = useServiceName();
  return (
    <div className={styles.scroller}>
      <table className={styles.table}>
        <caption className="visually-hidden">Incidents matching the current filters, newest first</caption>
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column} scope="col">
                {column}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {incidents.map((incident) => (
            <IncidentTableRow key={incident.id} incident={incident} serviceName={serviceName(incident.serviceId)} />
          ))}
        </tbody>
      </table>
    </div>
  );
}
