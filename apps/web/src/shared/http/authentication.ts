export interface HttpAuthentication {
  accessToken: () => string | null;
  onUnauthorized: () => void;
}

export const anonymousAuthentication: HttpAuthentication = {
  accessToken: () => null,
  onUnauthorized: () => undefined,
};

let activeAuthentication: HttpAuthentication = anonymousAuthentication;

export function getHttpAuthentication(): HttpAuthentication {
  return activeAuthentication;
}

export function setHttpAuthentication(authentication: HttpAuthentication): void {
  activeAuthentication = authentication;
}
