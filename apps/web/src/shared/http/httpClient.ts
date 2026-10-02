import { anonymousAuthentication, type HttpAuthentication } from './authentication';
import { ApiError, type ProblemDetails } from './problem';

export type QueryValue = string | number | boolean | undefined;
export type QueryParams = Readonly<Record<string, QueryValue>>;

export interface HttpClient {
  get: <T>(path: string, query?: QueryParams) => Promise<T>;
  post: <T>(path: string, body?: unknown, headers?: Readonly<Record<string, string>>) => Promise<T>;
}

export function buildQueryString(query: QueryParams = {}): string {
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === '') {
      continue;
    }
    params.set(key, String(value));
  }
  const serialized = params.toString();
  if (serialized === '') {
    return '';
  }
  return `?${serialized}`;
}

async function readProblem(response: Response): Promise<ProblemDetails | null> {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('json')) {
    return null;
  }
  try {
    return (await response.json()) as ProblemDetails;
  } catch {
    return null;
  }
}

async function readBody<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new ApiError(response.status, await readProblem(response));
  }
  if (response.status === 204) {
    return undefined as T;
  }
  return (await response.json()) as T;
}

type Headers = Readonly<Record<string, string>>;

const jsonAccept: Headers = { accept: 'application/json' };

function withAuthorization(headers: Headers, accessToken: string | null): Headers {
  if (accessToken === null || accessToken === '') {
    return headers;
  }
  return { ...headers, authorization: `Bearer ${accessToken}` };
}

export function createHttpClient(
  baseUrl: () => string,
  authentication: () => HttpAuthentication = () => anonymousAuthentication,
  fetchImpl: typeof fetch = (...args) => fetch(...args),
): HttpClient {
  async function send<T>(path: string, init: RequestInit & { headers: Headers }): Promise<T> {
    const { accessToken, onUnauthorized } = authentication();
    const response = await fetchImpl(`${baseUrl()}${path}`, {
      ...init,
      headers: withAuthorization(init.headers, accessToken()),
    });
    if (response.status === 401) {
      onUnauthorized();
    }
    return readBody<T>(response);
  }

  return {
    get: <T>(path: string, query?: QueryParams) =>
      send<T>(`${path}${buildQueryString(query)}`, { headers: jsonAccept }),
    post: <T>(path: string, body?: unknown, headers: Headers = {}) =>
      send<T>(path, {
        method: 'POST',
        headers: { ...jsonAccept, 'content-type': 'application/json', ...headers },
        body: JSON.stringify(body ?? {}),
      }),
  };
}
