import { screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { renderRoute } from '../../../shared/test/render';
import { IncidentDetailPage } from '../../incidents';
import { DeclareIncidentPage } from './DeclareIncidentPage';

function renderDeclare() {
  return renderRoute(<DeclareIncidentPage />, {
    path: '/incidents/new',
    initialEntry: '/incidents/new',
    extraRoutes: [{ path: '/incidents/:incidentId', element: <IncidentDetailPage /> }],
  });
}

describe('DeclareIncidentPage', () => {
  it('validates every field before declaring', async () => {
    const { user } = renderDeclare();

    await user.click(screen.getByRole('button', { name: 'Declare incident' }));

    expect(await screen.findByText('Give the incident a title of at least 5 characters.')).toBeInTheDocument();
    expect(screen.getByText('Choose the affected service.')).toBeInTheDocument();
    expect(screen.getByText('Choose a severity.')).toBeInTheDocument();
    expect(screen.getByText('Describe the impact in at least 10 characters.')).toBeInTheDocument();
  });

  it('declares the incident and opens it', async () => {
    const { user } = renderDeclare();
    await screen.findByRole('option', { name: 'Search' });

    await user.type(screen.getByLabelText('Title'), 'Search results empty for all queries');
    await user.selectOptions(screen.getByLabelText('Affected service'), 'search');
    await user.click(screen.getByRole('radio', { name: /Sev2/ }));
    await user.type(screen.getByLabelText('Impact'), 'Every query returns zero results since 12:40 UTC.');
    await user.click(screen.getByRole('button', { name: 'Declare incident' }));

    expect(await screen.findByRole('heading', { name: /Search results empty for all queries/ })).toBeInTheDocument();
    expect(screen.getByText('INC-1057')).toBeInTheDocument();
  });
});
