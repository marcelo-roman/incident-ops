import { AsyncContent, EmptyState } from '../../../shared/ui/AsyncContent';
import { ButtonLink } from '../../../shared/ui/ButtonLink';
import { PageHeader } from '../../../shared/ui/PageHeader';
import { Panel } from '../../../shared/ui/Panel';
import { useIncidentFilters } from '../hooks/useIncidentFilters';
import { useIncidents } from '../api/useIncidents';
import { IncidentFilters } from '../components/IncidentFilters';
import { IncidentTable } from '../components/IncidentTable';

export function IncidentsPage() {
  const { filters } = useIncidentFilters();
  const query = useIncidents(filters);
  const count = query.data?.length ?? 0;
  return (
    <>
      <PageHeader
        title="Incidents"
        summary="Every incident on record, newest first. Filters are kept in the address so a view can be shared."
        actions={
          <ButtonLink to="/incidents/new" variant="primary">
            Declare incident
          </ButtonLink>
        }
      />
      <IncidentFilters />
      <Panel title="Results" meta={`${String(count)} incidents`} flush>
        <AsyncContent query={query} loadingLabel="Loading incidents">
          {(incidents) => {
            if (incidents.length === 0) {
              return <EmptyState>No incidents match these filters. Clear a filter to widen the search.</EmptyState>;
            }
            return <IncidentTable incidents={incidents} />;
          }}
        </AsyncContent>
      </Panel>
    </>
  );
}
