import { useSearchParams } from 'react-router';
import { isIncidentSource, isIncidentStatus, isSeverity } from '../domain/incident';
import type { IncidentFilters } from '../domain/incidentFilters';

export type FilterField = 'status' | 'severity' | 'serviceId' | 'source';

export function parseIncidentFilters(params: URLSearchParams): IncidentFilters {
  const filters: IncidentFilters = {};
  const status = params.get('status') ?? '';
  const severity = params.get('severity') ?? '';
  const serviceId = params.get('serviceId') ?? '';
  const source = params.get('source') ?? '';
  if (isIncidentStatus(status)) {
    filters.status = status;
  }
  if (isSeverity(severity)) {
    filters.severity = severity;
  }
  if (isIncidentSource(source)) {
    filters.source = source;
  }
  if (serviceId !== '') {
    filters.serviceId = serviceId;
  }
  if (params.get('open') === 'true') {
    filters.open = true;
  }
  return filters;
}

export function useIncidentFilters() {
  const [params, setParams] = useSearchParams();
  const filters = parseIncidentFilters(params);

  const setFilter = (field: FilterField | 'open', value: string) => {
    setParams(
      (current) => {
        const next = new URLSearchParams(current);
        if (value === '') {
          next.delete(field);
          return next;
        }
        next.set(field, value);
        return next;
      },
      { replace: true },
    );
  };

  const setOpenOnly = (openOnly: boolean) => {
    if (openOnly) {
      setFilter('open', 'true');
      return;
    }
    setFilter('open', '');
  };

  const clearFilters = () => {
    setParams(new URLSearchParams(), { replace: true });
  };

  return { filters, setFilter, setOpenOnly, clearFilters };
}
