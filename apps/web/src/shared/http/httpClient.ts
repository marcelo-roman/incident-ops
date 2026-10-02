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

export function createHttpClient(
  baseUrl: () => string,
  fetchImpl: typeof fetch = (...args) => fetch(...args),
): HttpClient {
  return {
    async get<T>(path: string, query?: QueryParams): Promise<T> {
      const response = await fetchImpl(`${baseUrl()}${path}${buildQueryString(query)}`, {
        headers: { accept: 'application/json' },
      });
      return readBody<T>(response);
    },
    async post<T>(path: string, body?: unknown, headers: Readonly<Record<string, string>> = {}): Promise<T> {
      const response = await fetchImpl(`${baseUrl()}${path}`, {
        method: 'POST',
        headers: { accept: 'application/json', 'content-type': 'application/json', ...headers },
        body: JSON.stringify(body ?? {}),
      });
      return readBody<T>(response);
    },
  };
}
