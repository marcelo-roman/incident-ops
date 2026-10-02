export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails | null;

  constructor(status: number, problem: ProblemDetails | null) {
    super(problem?.detail ?? problem?.title ?? `Request failed with status ${String(status)}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

const statusMessages: Readonly<Record<number, string>> = {
  409: 'The incident changed while you were working on it. Review its current state and try again.',
  429: 'Too many changes in a short time. Wait a moment and try again.',
};

export function describeError(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return 'The service could not be reached. Check your connection and try again.';
  }
  return statusMessages[error.status] ?? error.message;
}
