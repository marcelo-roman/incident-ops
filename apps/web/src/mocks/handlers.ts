import { delay, http, HttpResponse, type HttpResponseResolver } from 'msw';
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
import { acceptsMockCredentials, hasMockBearer, mockAccessToken, mockTokenLifetimeMs } from './auth';
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

function unauthorized() {
  return problem(401, 'Unauthorized', 'A valid bearer token is required.');
}

type Resolver = HttpResponseResolver;

function authenticated(resolver: Resolver): Resolver {
  return (info) => {
    if (!hasMockBearer(info.request)) {
      return unauthorized();
    }
    return resolver(info);
  };
}

function days(request: Request): number {
  return parseInsightWindow(new URL(request.url).searchParams.get('days'));
}

export function createHandlers(store: () => MockIncidentStore) {
  const { apiBaseUrl: api, insightsBaseUrl: insights } = getConfig();
  return [
    http.get(`${api}/health/ready`, () => new HttpResponse('Healthy', { headers: { 'content-type': 'text/plain' } })),
    http.post(`${api}/api/auth/token`, async ({ request }) => {
      if (!acceptsMockCredentials((await request.json()) as Record<string, unknown>)) {
        return problem(401, 'Unauthorized', 'The credentials were not accepted.');
      }
      return HttpResponse.json({
        accessToken: mockAccessToken,
        tokenType: 'Bearer',
        expiresAt: new Date(Date.now() + mockTokenLifetimeMs).toISOString(),
      });
    }),
    http.get(
      `${api}/api/services`,
      authenticated(() => HttpResponse.json(serviceFixtures)),
    ),
    http.get(
      `${api}/api/oncall/current`,
      authenticated(() => HttpResponse.json(rosterFixture(Date.now()))),
    ),
    http.get(
      `${api}/api/metrics/summary`,
      authenticated(() => HttpResponse.json(store().metricsSummary())),
    ),
    http.get(
      `${api}/api/incidents`,
      authenticated(({ request }) => HttpResponse.json(store().list(queryFrom(new URL(request.url))))),
    ),
    http.get(
      `${api}/api/incidents/:id`,
      authenticated(({ params }) => guarded(() => store().get(String(params.id)))),
    ),
    http.post(
      `${api}/api/incidents`,
      authenticated(async ({ request }) => {
        const input = (await request.json()) as DeclareIncidentInput;
        return HttpResponse.json(store().declare(input), { status: 201 });
      }),
    ),
    http.post(
      `${api}/api/incidents/:id/acknowledge`,
      authenticated(async ({ params, request }) => {
        const input = (await request.json()) as AcknowledgeInput;
        return guarded(() => store().acknowledge(String(params.id), input));
      }),
    ),
    http.post(
      `${api}/api/incidents/:id/mitigate`,
      authenticated(async ({ params, request }) => {
        const input = (await request.json()) as MitigateInput;
        return guarded(() => store().mitigate(String(params.id), input));
      }),
    ),
    http.post(
      `${api}/api/incidents/:id/resolve`,
      authenticated(async ({ params, request }) => {
        const input = (await request.json()) as ResolveInput;
        return guarded(() => store().resolve(String(params.id), input));
      }),
    ),
    http.post(`${api}/api/incidents/:id/escalate`, async ({ params, request }) => {
      if (request.headers.get('x-api-key') === null) {
        return problem(401, 'Unauthorized', 'The X-Api-Key header is required to escalate.');
      }
      const { reason } = (await request.json()) as { reason: string };
      return guarded(() => store().escalate(String(params.id), reason));
    }),
    http.post(
      `${api}/api/incidents/:id/notes`,
      authenticated(async ({ params, request }) => {
        const input = (await request.json()) as NoteInput;
        return guarded(() => store().addNote(String(params.id), input));
      }),
    ),
    http.get(
      `${insights}/api/kpis`,
      authenticated(({ request }) => HttpResponse.json(kpiReportFixture(days(request), Date.now()))),
    ),
    http.get(
      `${insights}/api/recurring`,
      authenticated(({ request }) => HttpResponse.json(recurringFixture(days(request), Date.now()))),
    ),
    http.get(
      `${insights}/api/anomalies`,
      authenticated(({ request }) => HttpResponse.json(anomaliesFixture(days(request), Date.now()))),
    ),
    http.post(
      `${insights}/api/rca/draft`,
      authenticated(async ({ request }) => {
        const { incidentId } = (await request.json()) as { incidentId: string };
        const incident = store().get(incidentId);
        if (incident === undefined) {
          return notFound();
        }
        await delay(600);
        return HttpResponse.json(rcaDraftFixture(incident));
      }),
    ),
  ];
}
