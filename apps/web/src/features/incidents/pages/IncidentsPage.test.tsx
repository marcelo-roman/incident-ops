import { screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { renderRoute } from '../../../shared/test/render';
import { IncidentsPage } from './IncidentsPage';

async function rowTitles() {
  const table = await screen.findByRole('table');
  return within(table)
    .getAllByRole('link')
    .map((link) => link.textContent);
}

describe('IncidentsPage', () => {
  it('lists every incident newest first', async () => {
    renderRoute(<IncidentsPage />, { path: '/incidents', initialEntry: '/incidents' });

    const titles = await rowTitles();
    expect(titles).toHaveLength(14);
    expect(titles[0]).toBe('Card payments failing with 502 at checkout');
  });

  it('filters by severity and keeps the filter in the address', async () => {
    const { user, router } = renderRoute(<IncidentsPage />, { path: '/incidents', initialEntry: '/incidents' });
    await rowTitles();

    await user.selectOptions(screen.getByLabelText('Severity'), 'Sev1');

    await screen.findByText('3 incidents');
    expect(await rowTitles()).toEqual([
      'Card payments failing with 502 at checkout',
      'API error rate above 5% on incident endpoints',
      'Payment authorizations timing out for one acquirer',
    ]);
    expect(router.state.location.search).toBe('?severity=Sev1');
  });

  it('filters by alert source', async () => {
    renderRoute(<IncidentsPage />, { path: '/incidents', initialEntry: '/incidents?source=Alertmanager' });

    expect(await rowTitles()).toEqual([
      'Card payments failing with 502 at checkout',
      'Search API error rate above threshold',
    ]);
  });

  it('marks incidents whose acknowledgement SLA was missed', async () => {
    renderRoute(<IncidentsPage />, { path: '/incidents', initialEntry: '/incidents' });
    await rowTitles();

    expect(screen.getAllByText('Ack SLA missed')).toHaveLength(2);
  });

  it('explains when nothing matches', async () => {
    renderRoute(<IncidentsPage />, {
      path: '/incidents',
      initialEntry: '/incidents?serviceId=reporting&severity=Sev1',
    });

    expect(await screen.findByText(/No incidents match these filters/)).toBeInTheDocument();
  });
});
