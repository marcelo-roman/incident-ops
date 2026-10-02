export const loginRoute = '/login';
export const returnToParameter = 'returnTo';

const homePath = '/';

function pointsAtLogin(path: string): boolean {
  return path === loginRoute || path.startsWith(`${loginRoute}?`) || path.startsWith(`${loginRoute}/`);
}

export function safeReturnPath(candidate: string | null): string {
  if (candidate === null || !candidate.startsWith('/') || candidate.startsWith('//') || candidate.startsWith('/\\')) {
    return homePath;
  }
  if (pointsAtLogin(candidate)) {
    return homePath;
  }
  return candidate;
}

export function loginPathFor(returnTo: string): string {
  const target = safeReturnPath(returnTo);
  if (target === homePath) {
    return loginRoute;
  }
  return `${loginRoute}?${new URLSearchParams({ [returnToParameter]: target }).toString()}`;
}
