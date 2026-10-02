import { QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { http, HttpResponse } from 'msw';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { useSession } from '../features/auth';
import { mockCredentials } from '../mocks/auth';
import { server } from '../mocks/node';
import { getConfig } from '../shared/config/appConfig';
import { createTestQueryClient } from '../shared/test/render';
import { connectSessionToHttp } from './connectSessionToHttp';
import { routes } from './router';

function SessionProbe() {
  return <p>{useSession() === null ? 'anonymous' : 'signed in'}</p>;
}

function renderApp(initialEntry: string) {
  const router = createMemoryRouter(routes, { initialEntries: [initialEntry] });
  render(
    <QueryClientProvider client={createTestQueryClient()}>
      <SessionProbe />
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return { router, user: userEvent.setup() };
}

describe('protected routes', () => {
  beforeEach(() => {
    connectSessionToHttp();
  });

  afterEach(() => {
    window.sessionStorage.clear();
  });

  it('signs in through the login page and opens the requested page with a bearer token', async () => {
    const { router, user } = renderApp('/incidents');

    expect(await screen.findByRole('heading', { name: 'Sign in to Incident Ops' })).toBeInTheDocument();
    expect(router.state.location.search).toBe('?returnTo=%2Fincidents');

    await user.type(screen.getByLabelText('Username'), mockCredentials.username);
    await user.type(screen.getByLabelText('Password'), mockCredentials.password);
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('heading', { name: 'Incidents', level: 1 })).toBeInTheDocument();
    expect(await screen.findAllByRole('row')).not.toHaveLength(0);
    expect(screen.getByText('Signed in as')).toBeInTheDocument();
    expect(screen.getByLabelText('On shift as')).toHaveValue('demo');

    server.use(
      http.get(`${getConfig().apiBaseUrl}/api/metrics/summary`, () =>
        HttpResponse.json({ title: 'Unauthorized', status: 401 }, { status: 401 }),
      ),
    );
    await user.click(
      within(screen.getByRole('navigation', { name: 'Primary' })).getByRole('link', { name: 'Dashboard' }),
    );

    expect(await screen.findByRole('heading', { name: 'Sign in to Incident Ops' })).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/login');
    expect(screen.getByText('anonymous')).toBeInTheDocument();
  });

  it('shows the paused notice on the login page while the environment is off', async () => {
    server.use(http.get(`${getConfig().apiBaseUrl}/health/ready`, () => HttpResponse.error()));

    renderApp('/login');

    expect(
      await screen.findByText('The demo environment is paused. It is started on request for evaluations.', undefined, {
        timeout: 4000,
      }),
    ).toBeInTheDocument();
    expect(screen.queryByRole('form', { name: 'Sign in' })).not.toBeInTheDocument();
  });
});
