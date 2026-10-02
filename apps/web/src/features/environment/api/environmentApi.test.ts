import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from '../../../mocks/node';
import { ApiError } from '../../../shared/http/problem';
import { createEnvironmentApi } from './environmentApi';

const baseUrl = 'https://api.test';

describe('createEnvironmentApi', () => {
  const api = createEnvironmentApi(() => baseUrl);

  it('resolves with the status of a ready API', async () => {
    server.use(http.get(`${baseUrl}/health/ready`, () => new HttpResponse('Healthy', { status: 200 })));

    await expect(api.probe()).resolves.toBe(200);
  });

  it('rejects with the status code when the API is not ready', async () => {
    server.use(http.get(`${baseUrl}/health/ready`, () => new HttpResponse('Unhealthy', { status: 503 })));

    const probe = api.probe();

    await expect(probe).rejects.toBeInstanceOf(ApiError);
    await expect(probe).rejects.toMatchObject({ status: 503 });
  });

  it('rejects when the API cannot be reached', async () => {
    server.use(http.get(`${baseUrl}/health/ready`, () => HttpResponse.error()));

    await expect(api.probe()).rejects.toBeInstanceOf(TypeError);
  });
});
