import type { QueryClient } from '@tanstack/react-query';
import { incidentKeys } from './incidentKeys';
import type { Incident, IncidentDetail, TimelineEntry } from '../domain/incident';

function mergeIncident(current: IncidentDetail | undefined, incident: Incident): IncidentDetail | undefined {
  if (current === undefined) {
    return current;
  }
  return { ...current, ...incident, timeline: current.timeline };
}

function replaceIncident(item: Incident, incident: Incident): Incident {
  if (item.id !== incident.id) {
    return item;
  }
  return incident;
}

function replaceInList(list: Incident[] | undefined, incident: Incident): Incident[] | undefined {
  if (list === undefined) {
    return list;
  }
  return list.map((item) => replaceIncident(item, incident));
}

function byTime(left: TimelineEntry, right: TimelineEntry): number {
  return Date.parse(left.at) - Date.parse(right.at);
}

function appendEntry(current: IncidentDetail | undefined, entry: TimelineEntry): IncidentDetail | undefined {
  if (current === undefined) {
    return current;
  }
  if (current.timeline.some((existing) => existing.id === entry.id)) {
    return current;
  }
  return { ...current, timeline: [...current.timeline, entry].sort(byTime) };
}

export function invalidateIncidentViews(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: incidentKeys.lists() });
  void queryClient.invalidateQueries({ queryKey: incidentKeys.derived() });
}

export function applyIncidentChanged(queryClient: QueryClient, incident: Incident): void {
  queryClient.setQueryData<IncidentDetail>(incidentKeys.detail(incident.id), (current) =>
    mergeIncident(current, incident),
  );
  queryClient.setQueriesData<Incident[]>({ queryKey: incidentKeys.lists() }, (list) => replaceInList(list, incident));
  invalidateIncidentViews(queryClient);
}

export function applyTimelineAppended(queryClient: QueryClient, entry: TimelineEntry): void {
  queryClient.setQueryData<IncidentDetail>(incidentKeys.detail(entry.incidentId), (current) =>
    appendEntry(current, entry),
  );
}

export function refreshAfterReconnect(queryClient: QueryClient): void {
  void queryClient.invalidateQueries({ queryKey: incidentKeys.all });
}
