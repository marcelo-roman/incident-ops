import type { Incident, IncidentDetail, IncidentStatus, Severity, TimelineEntry } from '../domain/incident';
import type { HttpClient } from '../../../shared/http/httpClient';

export interface IncidentQuery {
  status?: IncidentStatus;
  severity?: Severity;
  serviceId?: string;
  open?: boolean;
}

export interface DeclareIncidentInput {
  title: string;
  description: string;
  serviceId: string;
  severity: Severity;
}

export interface AcknowledgeInput {
  actor: string;
}

export interface MitigateInput {
  actor: string;
  note: string;
}

export interface ResolveInput {
  actor: string;
  rootCause: string;
}

export interface NoteInput {
  actor: string;
  message: string;
}

export interface IncidentsApi {
  list: (query: IncidentQuery) => Promise<Incident[]>;
  get: (id: string) => Promise<IncidentDetail>;
  declare: (input: DeclareIncidentInput) => Promise<Incident>;
  acknowledge: (id: string, input: AcknowledgeInput) => Promise<Incident>;
  mitigate: (id: string, input: MitigateInput) => Promise<Incident>;
  resolve: (id: string, input: ResolveInput) => Promise<Incident>;
  addNote: (id: string, input: NoteInput) => Promise<TimelineEntry>;
}

function incidentPath(id: string, action = ''): string {
  return `/api/incidents/${encodeURIComponent(id)}${action}`;
}

export function createIncidentsApi(http: HttpClient): IncidentsApi {
  return {
    list: (query) => http.get<Incident[]>('/api/incidents', { ...query }),
    get: (id) => http.get<IncidentDetail>(incidentPath(id)),
    declare: (input) => http.post<Incident>('/api/incidents', input),
    acknowledge: (id, input) => http.post<Incident>(incidentPath(id, '/acknowledge'), input),
    mitigate: (id, input) => http.post<Incident>(incidentPath(id, '/mitigate'), input),
    resolve: (id, input) => http.post<Incident>(incidentPath(id, '/resolve'), input),
    addNote: (id, input) => http.post<TimelineEntry>(incidentPath(id, '/notes'), input),
  };
}
