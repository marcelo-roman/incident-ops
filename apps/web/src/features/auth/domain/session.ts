import { z } from 'zod';

export interface Session {
  username: string;
  accessToken: string;
  expiresAt: number;
}

export interface TokenResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
}

export const maxTimerDelayMs = 2_147_483_647;

const storedSessionSchema = z.object({
  username: z.string().min(1),
  accessToken: z.string().min(1),
  expiresAt: z.number(),
});

export function isActive(session: Session | null, now: number): session is Session {
  return session !== null && session.expiresAt > now;
}

export function sessionFromToken(username: string, token: TokenResponse, now: number): Session | null {
  if (token.tokenType.toLowerCase() !== 'bearer' || token.accessToken === '') {
    return null;
  }
  const session = { username, accessToken: token.accessToken, expiresAt: Date.parse(token.expiresAt) };
  if (Number.isNaN(session.expiresAt) || !isActive(session, now)) {
    return null;
  }
  return session;
}

export function expiryDelayMs(session: Session, now: number): number {
  return Math.min(Math.max(0, session.expiresAt - now), maxTimerDelayMs);
}

export function serializeSession(session: Session): string {
  return JSON.stringify(session);
}

function parseJson(raw: string): unknown {
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}

export function restoreSession(raw: string | null, now: number): Session | null {
  if (raw === null) {
    return null;
  }
  const parsed = storedSessionSchema.safeParse(parseJson(raw));
  if (!parsed.success || !isActive(parsed.data, now)) {
    return null;
  }
  return parsed.data;
}
