import type { Incident } from '../domain/incident';
import { mostUrgentFirst } from '../domain/sla';
import { useIncidents } from '../api/useIncidents';
import { useNow } from '../../../shared/time/useNow';

const openFilters = { open: true } as const;

export function useOpenIncidentsByUrgency() {
  const query = useIncidents(openFilters);
  const now = useNow();
  const incidents: Incident[] = [...(query.data ?? [])].sort((left, right) => mostUrgentFirst(left, right, now));
  return { ...query, incidents };
}
