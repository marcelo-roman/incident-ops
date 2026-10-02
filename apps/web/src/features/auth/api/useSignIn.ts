import { useMutation } from '@tanstack/react-query';
import { adoptOperator } from '../../../shared/operator/useOperator';
import { UnusableTokenError } from '../domain/loginError';
import type { LoginValues } from '../domain/loginSchema';
import { sessionFromToken, type Session } from '../domain/session';
import { sessionStore } from '../session/activeSession';
import { authApi } from './authApi';

async function signIn(credentials: LoginValues): Promise<Session> {
  const token = await authApi.token(credentials);
  const session = sessionFromToken(credentials.username, token, Date.now());
  if (session === null) {
    throw new UnusableTokenError();
  }
  return session;
}

export function useSignIn() {
  return useMutation({
    mutationFn: signIn,
    onSuccess: (session) => {
      sessionStore.signIn(session);
      adoptOperator(session.username);
    },
  });
}
