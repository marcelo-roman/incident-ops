import { QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from '../../../mocks/node';
import { getConfig } from '../../../shared/config/appConfig';
import { createTestQueryClient } from '../../../shared/test/render';
import { environmentKeys } from '../api/environmentApi';
import { EnvironmentGate } from './EnvironmentGate';

const pausedMessage = 'The demo environment is paused. It is started on request for evaluations.';
const retryWindow = { timeout: 4000 };

function readyUrl(): string {
  return `${getConfig().apiBaseUrl}/health/ready`;
}

function renderGate() {
  const queryClient = createTestQueryClient();
  render(
    <QueryClientProvider client={queryClient}>
      <EnvironmentGate>
        <p>Console content</p>
      </EnvironmentGate>
    </QueryClientProvider>,
  );
  return queryClient;
}

describe('EnvironmentGate', () => {
  it('renders the console while the API is ready', async () => {
    renderGate();

    expect(await screen.findByText('Console content')).toBeInTheDocument();
    expect(screen.queryByText(pausedMessage)).not.toBeInTheDocument();
  });

  it.each([502, 503, 504])('shows the paused notice instead of the console on %i', async (status) => {
    server.use(http.get(readyUrl(), () => new HttpResponse(null, { status })));

    renderGate();

    expect(await screen.findByText(pausedMessage, undefined, retryWindow)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Environment paused' })).toBeInTheDocument();
    expect(screen.queryByText('Console content')).not.toBeInTheDocument();
  });

  it('shows the paused notice when the API cannot be reached', async () => {
    server.use(http.get(readyUrl(), () => HttpResponse.error()));

    renderGate();

    expect(await screen.findByText(pausedMessage, undefined, retryWindow)).toBeInTheDocument();
  });

  it('keeps the console when the API answers with an application error', async () => {
    server.use(http.get(readyUrl(), () => new HttpResponse(null, { status: 500 })));

    const queryClient = renderGate();

    await waitFor(() => {
      expect(queryClient.getQueryState(environmentKeys.probe)?.status).toBe('error');
    }, retryWindow);
    expect(screen.getByText('Console content')).toBeInTheDocument();
    expect(screen.queryByText(pausedMessage)).not.toBeInTheDocument();
  });
});
