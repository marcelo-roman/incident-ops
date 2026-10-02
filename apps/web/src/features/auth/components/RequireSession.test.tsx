import { screen } from '@testing-library/react';
import { afterEach, describe, expect, it } from 'vitest';
import { renderRoute } from '../../../shared/test/render';
import { sessionStore } from '../session/activeSession';
import { RequireSession } from './RequireSession';
import { SignedInUser } from './SignedInUser';

function renderProtected() {
  return renderRoute(
    <RequireSession>
      <SignedInUser />
      <p>Protected content</p>
    </RequireSession>,
    {
      path: '/incidents',
      initialEntry: '/incidents?status=Triggered',
      extraRoutes: [{ path: '/login', element: <p>Login page</p> }],
    },
  );
}

describe('RequireSession', () => {
  afterEach(() => {
    sessionStore.signOut();
  });

  it('sends an anonymous visitor to the login page with the return path', async () => {
    const { router } = renderProtected();

    expect(await screen.findByText('Login page')).toBeInTheDocument();
    expect(router.state.location.search).toBe('?returnTo=%2Fincidents%3Fstatus%3DTriggered');
  });

  it('shows the page and the signed-in user, and signs out to the login page', async () => {
    sessionStore.signIn({ username: 'demo', accessToken: 'jwt', expiresAt: Date.now() + 60_000 });
    const { user, router } = renderProtected();

    expect(screen.getByText('Protected content')).toBeInTheDocument();
    expect(screen.getByText('demo')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Sign out' }));

    expect(await screen.findByText('Login page')).toBeInTheDocument();
    expect(router.state.location.pathname).toBe('/login');
    expect(sessionStore.read()).toBeNull();
  });
});
