import { screen, within } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { getConfig } from '../../../shared/config/appConfig';
import { server } from '../../../mocks/node';
import { renderRoute } from '../../../shared/test/render';
import { IncidentDetailPage } from './IncidentDetailPage';

const triggeredId = '00000000-0000-4000-8000-000000001056';
const resolvedId = '00000000-0000-4000-8000-000000001048';

function renderIncident(id: string) {
  return renderRoute(<IncidentDetailPage />, { path: '/incidents/:incidentId', initialEntry: `/incidents/${id}` });
}

describe('IncidentDetailPage', () => {
  it('offers only the actions allowed from the current status', async () => {
    renderIncident(triggeredId);

    const actions = await screen.findByRole('group', { name: 'Incident actions' });
    expect(
      within(actions)
        .getAllByRole('button')
        .map((button) => button.textContent),
    ).toEqual(['Acknowledge', 'Mitigate', 'Resolve', 'Add note']);
  });

  it('shows the alert source and renders alert entries in the timeline', async () => {
    renderIncident(triggeredId);

    const timeline = await screen.findByRole('list', { name: 'Incident timeline' });
    expect(within(timeline).getByText('Alert')).toBeInTheDocument();
    expect(screen.getAllByText('Alertmanager').length).toBeGreaterThan(0);
    expect(screen.getByText('a41f9c07e2b35d18')).toBeInTheDocument();
  });

  it('flags a missed acknowledgement SLA', async () => {
    renderIncident('00000000-0000-4000-8000-000000001055');

    expect(await screen.findByText('Ack SLA missed')).toBeInTheDocument();
  });

  it('does not flag incidents acknowledged in time', async () => {
    renderIncident(triggeredId);

    await screen.findByRole('group', { name: 'Incident actions' });
    expect(screen.queryByText('Ack SLA missed')).not.toBeInTheDocument();
  });

  it('requires a root cause before resolving', async () => {
    const { user } = renderIncident(triggeredId);

    await user.click(await screen.findByRole('button', { name: 'Resolve' }));
    const form = screen.getByRole('form', { name: 'Resolve incident' });
    await user.type(within(form).getByLabelText('Resolved by'), 'Ana Ribeiro');
    await user.click(within(form).getByRole('button', { name: 'Resolve' }));

    expect(await within(form).findByText('Describe the root cause in at least 10 characters.')).toBeInTheDocument();
    expect(within(form).getByLabelText('Root cause')).toHaveAttribute('aria-invalid', 'true');
  });

  it('resolves the incident and appends the resolution to the timeline', async () => {
    const { user } = renderIncident(triggeredId);

    await user.click(await screen.findByRole('button', { name: 'Resolve' }));
    const form = screen.getByRole('form', { name: 'Resolve incident' });
    await user.type(within(form).getByLabelText('Resolved by'), 'Ana Ribeiro');
    await user.type(within(form).getByLabelText('Root cause'), 'Bad deploy of the card tokenizer, rolled back.');
    await user.click(within(form).getByRole('button', { name: 'Resolve' }));

    expect(await screen.findByText('Incident resolved. The SLA outcome is now final.')).toBeInTheDocument();
    const timeline = await screen.findByRole('list', { name: 'Incident timeline' });
    expect(await within(timeline).findByText('Bad deploy of the card tokenizer, rolled back.')).toBeInTheDocument();
    expect(screen.queryByRole('form', { name: 'Resolve incident' })).not.toBeInTheDocument();
  });

  it('explains a conflict returned by the API', async () => {
    server.use(
      http.post(`${getConfig().apiBaseUrl}/api/incidents/:id/acknowledge`, () =>
        HttpResponse.json(
          { title: 'Conflict', status: 409, detail: 'Already acknowledged.' },
          { status: 409, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    );
    const { user } = renderIncident(triggeredId);

    await user.click(await screen.findByRole('button', { name: 'Acknowledge' }));
    const form = screen.getByRole('form', { name: 'Acknowledge incident' });
    await user.type(within(form).getByLabelText('Acknowledged by'), 'Ana Ribeiro');
    await user.click(within(form).getByRole('button', { name: 'Acknowledge' }));

    expect(await within(form).findByRole('alert')).toHaveTextContent(/changed while you were working/);
  });

  it('drafts an RCA for resolved incidents', async () => {
    const { user } = renderIncident(resolvedId);

    await user.click(await screen.findByRole('button', { name: 'Draft RCA with AI' }));

    const draft = await screen.findByRole('article', { name: 'RCA draft' }, { timeout: 3000 });
    expect(within(draft).getByText('Action items')).toBeInTheDocument();
  });
});
