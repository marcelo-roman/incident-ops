import type { IncidentQuery } from './incidentsApi';

const root = ['incidents'] as const;

export const incidentKeys = {
  all: root,
  lists: () => [...root, 'list'] as const,
  list: (query: IncidentQuery) => [...root, 'list', query] as const,
  detail: (id: string) => [...root, 'detail', id] as const,
  derived: () => [...root, 'derived'] as const,
};

export const serviceKeys = {
  all: ['services'] as const,
};
