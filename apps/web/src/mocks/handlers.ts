import { delay, http, HttpResponse } from 'msw';
import {
  type AcknowledgeInput,
  type DeclareIncidentInput,
  type IncidentQuery,
  isIncidentStatus,
  isSeverity,
  type MitigateInput,
  type NoteInput,
  type ResolveInput,
} from '../features/incidents';
import { getConfig } from '../shared/config/appConfig';
import { parseInsightWindow } from '../features/insights';
import { anomaliesFixture, kpiReportFixture, rcaDraftFixture, recurringFixture } from './fixtures/insights';
import { rosterFixture } from './fixtures/roster';
import { serviceFixtures } from './fixtures/services';
import { EscalationRejected, TransitionRejected, type MockIncidentStore } from './store';

function problem(status: number, title: string, detail: string) {
  return HttpResponse.json(
    { type: 'about:blank', title, status, detail },
    {
      status,
      headers: { 'content-type': 'application/problem+json' },
    },
  );
}

function notFound() {
  return problem(404, 'Not Found', 'No incident exists with this id.');
}

function queryFrom(url: URL): IncidentQuery {
  const query: IncidentQuery = {};
  const status = url.searchParams.get('status') ?? '';
  const severity = url.searchParams.get('severity') ?? '';
  const serviceId = url.searchParams.get('serviceId') ?? '';
  if (isIncidentStatus(status)) {
    query.status = status;
  }
  if (isSeverity(severity)) {
    query.severity = severity;
  }
  if (serviceId !== '') {
    query.serviceId = serviceId;
  }
  if (url.searchParams.get('open') === 'true') {
    query.open = true;
  }
  return query;
}

function guarded(run: () => unknown) {
  try {
    const result = run();
    if (result === undefined) {
      return notFound();
    }
    return HttpResponse.json(result);
  } catch (error) {
    if (error instanceof TransitionRejected || error instanceof EscalationRejected) {
      return problem(409, 'Conflict', error.message);
    }
    throw error;
  }
}

function days(request: Request): number {
  return parseInsightWindow(new URL(request.url).searchParams.get('days'));
}

export function createHandlers(store: () => MockIncidentStore) {
  const { apiBaseUrl: api, insightsBaseUrl: insights } = getConfig();
  return [
    http.get(`${api}/health/ready`, () => new HttpResponse('Healthy', { headers: { 'content-type': 'text/plain' } })),
    http.get(`${api}/api/services`, () => HttpResponse.json(serviceFixtures)),
    http.get(`${api}/api/oncall/current`, () => HttpResponse.json(rosterFixture(Date.now()))),
    http.get(`${api}/api/metrics/summary`, () => HttpResponse.json(store().metricsSummary())),
    http.get(`${api}/api/incidents`, ({ request }) => HttpResponse.json(store().list(queryFrom(new URL(request.url))))),
    http.get(`${api}/api/incidents/:id`, ({ params }) => guarded(() => store().get(String(params.id)))),
    http.post(`${api}/api/incidents`, async ({ request }) => {
      const input = (await request.json()) as DeclareIncidentInput;
      return HttpResponse.json(store().declare(input), { status: 201 });
    }),
    http.post(`${api}/api/incidents/:id/acknowledge`, async ({ params, request }) => {
      const input = (await request.json()) as AcknowledgeInput;
      return guarded(() => store().acknowledge(String(params.id), input));
    }),
    http.post(`${api}/api/incidents/:id/mitigate`, async ({ params, request }) => {
      const input = (await request.json()) as MitigateInput;
      return guarded(() => store().mitigate(String(params.id), input));
    }),
    http.post(`${api}/api/incidents/:id/resolve`, async ({ params, request }) => {
      const input = (await request.json()) as ResolveInput;
      return guarded(() => store().resolve(String(params.id), input));
    }),
    http.post(`${api}/api/incidents/:id/escalate`, async ({ params, request }) => {
      if (request.headers.get('x-api-key') === null) {
        return problem(401, 'Unauthorized', 'The X-Api-Key header is required to escalate.');
      }
      const { reason } = (await request.json()) as { reason: string };
      return guarded(() => store().escalate(String(params.id), reason));
    }),
    http.post(`${api}/api/incidents/:id/notes`, async ({ params, request }) => {
      const input = (await request.json()) as NoteInput;
      return guarded(() => store().addNote(String(params.id), input));
    }),
    http.get(`${insights}/api/kpis`, ({ request }) => HttpResponse.json(kpiReportFixture(days(request), Date.now()))),
    http.get(`${insights}/api/recurring`, ({ request }) =>
      HttpResponse.json(recurringFixture(days(request), Date.now())),
    ),
    http.get(`${insights}/api/anomalies`, ({ request }) =>
      HttpResponse.json(anomaliesFixture(days(request), Date.now())),
    ),
    http.post(`${insights}/api/rca/draft`, async ({ request }) => {
      const { incidentId } = (await request.json()) as { incidentId: string };
      const incident = store().get(incidentId);
      if (incident === undefined) {
        return notFound();
      }
      await delay(600);
      return HttpResponse.json(rcaDraftFixture(incident));
    }),
  ];
}
