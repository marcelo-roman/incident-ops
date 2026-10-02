import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { afterEach, describe, expect, it } from 'vitest';
import { mockCredentials } from '../../../mocks/auth';
import { server } from '../../../mocks/node';
import { getConfig } from '../../../shared/config/appConfig';
import { renderRoute } from '../../../shared/test/render';
import { invalidCredentialsMessage, tooManyAttemptsMessage } from '../domain/loginError';
import { sessionStorageKey, sessionStore } from '../session/activeSession';
import { LoginPage } from './LoginPage';

function renderLogin(initialEntry = '/login?returnTo=%2Fincidents') {
  return renderRoute(<LoginPage />, {
    path: '/login',
    initialEntry,
    extraRoutes: [
      { path: '/incidents', element: <p>Incidents page</p> },
      { path: '/', element: <p>Dashboard page</p> },
    ],
  });
}

async function signIn(user: ReturnType<typeof renderLogin>['user'], username: string, password: string) {
  await user.type(screen.getByLabelText('Username'), username);
  await user.type(screen.getByLabelText('Password'), password);
  await user.click(screen.getByRole('button', { name: 'Sign in' }));
}

describe('LoginPage', () => {
  afterEach(() => {
    sessionStore.signOut();
  });

  it('asks for both credentials before calling the API', async () => {
    const { user } = renderLogin();

    expect(screen.getByRole('heading', { name: 'Sign in to Incident Ops' })).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('Enter your username.')).toBeInTheDocument();
    expect(screen.getByText('Enter your password.')).toBeInTheDocument();
    expect(screen.getByLabelText('Username')).toHaveAttribute('aria-invalid', 'true');
  });

  it('signs in, keeps the session for the tab and returns to the requested page', async () => {
    const { user } = renderLogin();

    await signIn(user, mockCredentials.username, mockCredentials.password);

    expect(await screen.findByText('Incidents page')).toBeInTheDocument();
    expect(sessionStore.read()?.username).toBe('demo');
    expect(window.sessionStorage.getItem(sessionStorageKey)).toContain('mock-access-token');
    expect(window.localStorage.getItem('incident-ops.operator')).toBe('demo');
  });

  it('rejects wrong credentials without saying which one was wrong', async () => {
    const { user } = renderLogin();

    await signIn(user, mockCredentials.username, 'not-the-password');

    expect(await screen.findByRole('alert')).toHaveTextContent(invalidCredentialsMessage);
    expect(screen.getByLabelText('Password')).toHaveValue('');
    expect(sessionStore.read()).toBeNull();
  });

  it('asks to wait after too many attempts', async () => {
    server.use(
      http.post(`${getConfig().apiBaseUrl}/api/auth/token`, () =>
        HttpResponse.json({ title: 'Too many requests', status: 429 }, { status: 429 }),
      ),
    );
    const { user } = renderLogin();

    await signIn(user, mockCredentials.username, mockCredentials.password);

    expect(await screen.findByRole('alert')).toHaveTextContent(tooManyAttemptsMessage);
  });

  it('refuses a token response it cannot use', async () => {
    server.use(
      http.post(`${getConfig().apiBaseUrl}/api/auth/token`, () =>
        HttpResponse.json({ accessToken: 'jwt', tokenType: 'Bearer', expiresAt: '2000-01-01T00:00:00Z' }),
      ),
    );
    const { user } = renderLogin();

    await signIn(user, mockCredentials.username, mockCredentials.password);

    expect(await screen.findByRole('alert')).toHaveTextContent('The sign-in response could not be used.');
    expect(sessionStore.read()).toBeNull();
  });

  it('sends a signed-in user straight to the requested page', async () => {
    sessionStore.signIn({ username: 'demo', accessToken: 'jwt', expiresAt: Date.now() + 60_000 });

    renderLogin('/login?returnTo=https%3A%2F%2Fevil.test');

    expect(await screen.findByText('Dashboard page')).toBeInTheDocument();
  });
});
