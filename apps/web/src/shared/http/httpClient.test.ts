import { http, HttpResponse } from 'msw';
import { describe, expect, it, vi } from 'vitest';
import { server } from '../../mocks/node';
import { anonymousAuthentication, type HttpAuthentication } from './authentication';
import { buildQueryString, createHttpClient } from './httpClient';
import { ApiError, describeError } from './problem';

const baseUrl = 'https://api.test';

describe('buildQueryString', () => {
  it('skips empty values', () => {
    expect(buildQueryString({ status: 'Triggered', severity: undefined, serviceId: '', open: true })).toBe(
      '?status=Triggered&open=true',
    );
    expect(buildQueryString({})).toBe('');
  });
});

describe('createHttpClient', () => {
  const client = createHttpClient(() => baseUrl);

  it('returns parsed JSON for successful responses', async () => {
    server.use(http.get(`${baseUrl}/api/things`, () => HttpResponse.json([{ id: 1 }])));

    await expect(client.get('/api/things')).resolves.toEqual([{ id: 1 }]);
  });

  it('sends JSON bodies on POST', async () => {
    server.use(
      http.post(`${baseUrl}/api/echo`, async ({ request }) => HttpResponse.json(await request.json(), { status: 201 })),
    );

    await expect(client.post('/api/echo', { actor: 'Ana' })).resolves.toEqual({ actor: 'Ana' });
  });

  it('turns problem details into an ApiError', async () => {
    server.use(
      http.post(`${baseUrl}/api/incidents/1/mitigate`, () =>
        HttpResponse.json(
          { title: 'Conflict', status: 409, detail: 'Cannot move an incident from Triggered to Mitigated.' },
          { status: 409, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    );

    const error = await client.post('/api/incidents/1/mitigate', {}).catch((caught: unknown) => caught);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 409, problem: { title: 'Conflict' } });
    expect(describeError(error)).toMatch(/changed while you were working/);
  });

  it('handles error responses without a body', async () => {
    server.use(http.get(`${baseUrl}/api/broken`, () => new HttpResponse(null, { status: 503 })));

    await expect(client.get('/api/broken')).rejects.toMatchObject({ status: 503, problem: null });
  });

  it('describes network failures', () => {
    expect(describeError(new TypeError('Failed to fetch'))).toMatch(/could not be reached/);
  });
});

describe('createHttpClient with authentication', () => {
  function authenticated(accessToken: string | null): HttpAuthentication & { onUnauthorized: () => void } {
    return { accessToken: () => accessToken, onUnauthorized: vi.fn() };
  }

  it('sends the access token as a bearer credential', async () => {
    server.use(
      http.get(`${baseUrl}/api/whoami`, ({ request }) =>
        HttpResponse.json({ authorization: request.headers.get('authorization') }),
      ),
    );
    const client = createHttpClient(
      () => baseUrl,
      () => authenticated('token-1'),
    );

    await expect(client.get('/api/whoami')).resolves.toEqual({ authorization: 'Bearer token-1' });
  });

  it('sends no authorization header without a token', async () => {
    server.use(
      http.post(`${baseUrl}/api/whoami`, ({ request }) =>
        HttpResponse.json({ authorization: request.headers.get('authorization') }),
      ),
    );
    const client = createHttpClient(
      () => baseUrl,
      () => authenticated(null),
    );

    await expect(client.post('/api/whoami')).resolves.toEqual({ authorization: null });
  });

  it('reports an unauthorized response before rejecting', async () => {
    server.use(http.get(`${baseUrl}/api/private`, () => new HttpResponse(null, { status: 401 })));
    const authentication = authenticated('expired');
    const client = createHttpClient(
      () => baseUrl,
      () => authentication,
    );

    await expect(client.get('/api/private')).rejects.toMatchObject({ status: 401 });
    expect(authentication.onUnauthorized).toHaveBeenCalledTimes(1);
  });

  it('is anonymous by default', () => {
    expect(anonymousAuthentication.accessToken()).toBeNull();
    expect(() => {
      anonymousAuthentication.onUnauthorized();
    }).not.toThrow();
  });
});
