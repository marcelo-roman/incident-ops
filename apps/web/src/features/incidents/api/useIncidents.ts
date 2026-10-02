import { useQuery } from '@tanstack/react-query';
import { useCallback } from 'react';
import { incidentsApi } from './client';
import { incidentKeys } from './incidentKeys';
import type { Incident } from '../domain/incident';
import { filterBySource, serverFilters, type IncidentFilters } from '../domain/incidentFilters';

export function useIncidents(filters: IncidentFilters) {
  const query = serverFilters(filters);
  const { source } = filters;
  const select = useCallback((incidents: Incident[]) => filterBySource(incidents, source), [source]);
  return useQuery({
    queryKey: incidentKeys.list(query),
    queryFn: () => incidentsApi.list(query),
    select,
  });
}

export function useIncident(id: string) {
  return useQuery({
    queryKey: incidentKeys.detail(id),
    queryFn: () => incidentsApi.get(id),
  });
}
