import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { renderRoute } from '../../../shared/test/render';
import { DashboardPage } from './DashboardPage';

describe('DashboardPage', () => {
  it('shows service metrics from the summary endpoint', async () => {
    renderRoute(<DashboardPage />);

    const metrics = await screen.findByLabelText('Service metrics');
    expect(within(metrics).getByText('Open incidents').nextSibling).toHaveTextContent('8');
    expect(within(metrics).getByText('Open past SLA').nextSibling).toHaveTextContent('2');
    expect(within(metrics).getByText('92.4%')).toBeInTheDocument();
  });

  it('lists the escalation order of the current on-call rotation', async () => {
    renderRoute(<DashboardPage />);

    const tiers = await screen.findByRole('list', { name: 'Escalation order' });
    expect(
      within(tiers)
        .getAllByRole('listitem')
        .map((item) => item.textContent),
    ).toEqual(['1Ana RibeiroPrimary', '2Daniel OkaforSecondary', '3Priya NatarajanEngineering lead']);
  });

  it('orders open incidents by the deadline that runs out first', async () => {
    renderRoute(<DashboardPage />);

    const board = await screen.findByRole('region', { name: 'Open incidents' });
    const links = await within(board).findAllByRole('link');
    expect(links).toHaveLength(8);
    expect(links[0]).toHaveTextContent('API error rate above 5% on incident endpoints');
    expect(within(board).getAllByRole('timer').length).toBeGreaterThan(0);
  });
});
