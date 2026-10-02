import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { renderRoute } from '../../../shared/test/render';
import { InsightsPage } from './InsightsPage';

function renderInsights(initialEntry = '/insights') {
  return renderRoute(<InsightsPage />, { path: '/insights', initialEntry });
}

describe('InsightsPage', () => {
  it('shows indicators, recurring clusters and anomalies', async () => {
    renderInsights();

    expect(await screen.findByLabelText('Key indicators')).toBeInTheDocument();
    expect(await screen.findByText('certificate expiry handshake')).toBeInTheDocument();
    const anomalies = await screen.findByRole('region', { name: 'Volume anomalies' });
    expect(await within(anomalies).findByText('4.4')).toBeInTheDocument();
  });

  it('shows the ten largest clusters and expands to all of them', async () => {
    const { user } = renderInsights();

    const panel = await screen.findByRole('region', { name: 'Recurring incidents' });
    const toggle = await within(panel).findByRole('button', { name: 'Show all 12 clusters' });
    const list = document.getElementById(toggle.getAttribute('aria-controls') ?? '');
    expect(list?.children).toHaveLength(10);
    expect(toggle).toHaveAttribute('aria-expanded', 'false');
    expect(within(panel).queryByText('webhook retry backlog')).not.toBeInTheDocument();

    await user.click(toggle);

    expect(list?.children).toHaveLength(12);
    expect(toggle).toHaveAttribute('aria-expanded', 'true');
    expect(toggle).toHaveTextContent('Show fewer');
    expect(toggle).toHaveFocus();

    await user.click(toggle);

    expect(list?.children).toHaveLength(10);
    expect(toggle).toHaveTextContent('Show all 12 clusters');
  });

  it('switches the analysis window through the address', async () => {
    const { user, router } = renderInsights();

    await user.click(screen.getByRole('button', { name: '30 days' }));

    expect(router.state.location.search).toBe('?days=30');
    expect(screen.getByRole('button', { name: '30 days' })).toHaveAttribute('aria-pressed', 'true');
  });

  it('switches the trend metric', async () => {
    const { user } = renderInsights();
    await screen.findByLabelText('Key indicators');

    await user.click(screen.getByRole('button', { name: 'Time to resolve' }));

    expect(screen.getByRole('img', { name: 'Time to resolve per week' })).toBeInTheDocument();
  });

  it('lists recently resolved incidents with an RCA draft action', async () => {
    renderInsights();

    const queue = await screen.findByRole('region', { name: 'Root cause analysis drafts' });
    expect(await within(queue).findAllByRole('button', { name: 'Draft RCA with AI' })).toHaveLength(5);
  });
});
