import { Button } from '../../../shared/ui/Button';
import { sourceLabels, statusLabels } from '../domain/labels';
import { incidentSources, incidentStatuses, severities } from '../domain/incident';
import { useIncidentFilters } from '../hooks/useIncidentFilters';
import { useServices } from '../api/useServices';
import { FilterSelect } from './FilterSelect';
import styles from './IncidentFilters.module.css';

const statusOptions = incidentStatuses.map((status) => ({ value: status, label: statusLabels[status] }));
const severityOptions = severities.map((severity) => ({ value: severity, label: severity }));
const sourceOptions = incidentSources.map((source) => ({ value: source, label: sourceLabels[source] }));

export function IncidentFilters() {
  const { filters, setFilter, setOpenOnly, clearFilters } = useIncidentFilters();
  const { data: services = [] } = useServices();
  const serviceOptions = services.map((service) => ({ value: service.id, label: service.name }));
  return (
    <div className={styles.filters} role="search" aria-label="Filter incidents">
      <FilterSelect
        label="Status"
        anyLabel="Any status"
        value={filters.status}
        options={statusOptions}
        onChange={(value) => {
          setFilter('status', value);
        }}
      />
      <FilterSelect
        label="Severity"
        anyLabel="Any severity"
        value={filters.severity}
        options={severityOptions}
        onChange={(value) => {
          setFilter('severity', value);
        }}
      />
      <FilterSelect
        label="Service"
        anyLabel="Any service"
        value={filters.serviceId}
        options={serviceOptions}
        onChange={(value) => {
          setFilter('serviceId', value);
        }}
      />
      <FilterSelect
        label="Source"
        anyLabel="Any source"
        value={filters.source}
        options={sourceOptions}
        onChange={(value) => {
          setFilter('source', value);
        }}
      />
      <label className={styles.toggle}>
        <input
          type="checkbox"
          checked={filters.open === true}
          onChange={(event) => {
            setOpenOnly(event.target.checked);
          }}
        />
        Open only
      </label>
      <Button variant="quiet" onClick={clearFilters}>
        Clear filters
      </Button>
    </div>
  );
}
