import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from '../../../mocks/node';
import { getConfig } from '../../../shared/config/appConfig';
import { renderRoute } from '../../../shared/test/render';
import { OnCallPanel } from './OnCallPanel';

describe('OnCallPanel', () => {
  it('shows who is paged at each escalation level', async () => {
    renderRoute(<OnCallPanel />);

    expect(await screen.findByText('Ana Ribeiro')).toBeInTheDocument();
    expect(screen.getByLabelText('Level 3')).toHaveTextContent('3');
    expect(screen.getByText(/^Week of /)).toBeInTheDocument();
  });

  it('offers a retry when the rotation cannot be loaded', async () => {
    server.use(http.get(`${getConfig().apiBaseUrl}/api/oncall/current`, () => new HttpResponse(null, { status: 503 })));
    renderRoute(<OnCallPanel />);

    expect(await screen.findByRole('alert')).toHaveTextContent('Try again');
  });
});
