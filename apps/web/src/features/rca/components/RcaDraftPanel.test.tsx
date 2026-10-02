import { screen } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { describe, expect, it } from 'vitest';
import { server } from '../../../mocks/node';
import { getConfig } from '../../../shared/config/appConfig';
import { renderRoute } from '../../../shared/test/render';
import { RcaDraftPanel } from './RcaDraftPanel';

const resolvedId = '00000000-0000-4000-8000-000000001047';
const openAiDraftId = '00000000-0000-4000-8000-000000001048';

describe('RcaDraftPanel', () => {
  it('labels a draft written by Azure OpenAI with its deployment', async () => {
    const { user } = renderRoute(<RcaDraftPanel incidentId={openAiDraftId} />);

    await user.click(screen.getByRole('button', { name: 'Draft RCA with AI' }));

    expect(
      await screen.findByText(/^Drafted by Azure OpenAI \(rca-drafts\)/, undefined, { timeout: 3000 }),
    ).toBeInTheDocument();
  });

  it('never claims Azure OpenAI before a draft exists', () => {
    renderRoute(<RcaDraftPanel incidentId={resolvedId} />);

    expect(screen.getByText(/Review and edit before sharing/)).toBeInTheDocument();
    expect(screen.queryByText(/Azure OpenAI/)).not.toBeInTheDocument();
  });

  it('requests a draft and renders its sections', async () => {
    const { user } = renderRoute(<RcaDraftPanel incidentId={resolvedId} />);

    await user.click(screen.getByRole('button', { name: 'Draft RCA with AI' }));

    expect(await screen.findByText('Contributing factors', undefined, { timeout: 3000 })).toBeInTheDocument();
    expect(screen.getAllByText(/expired intermediate certificate/).length).toBeGreaterThan(0);
    expect(screen.getByText(/^Drafted by the rule-based fallback \(Azure OpenAI not configured\)/)).toBeInTheDocument();
    expect(screen.getByText('Site Reliability')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Draft again' })).toBeEnabled();
  });

  it('explains when the drafting service fails', async () => {
    server.use(
      http.post(`${getConfig().insightsBaseUrl}/api/rca/draft`, () =>
        HttpResponse.json(
          { title: 'Service Unavailable', status: 503, detail: 'Azure OpenAI is not configured.' },
          { status: 503, headers: { 'content-type': 'application/problem+json' } },
        ),
      ),
    );
    const { user } = renderRoute(<RcaDraftPanel incidentId={resolvedId} showGuidance={false} />);

    await user.click(screen.getByRole('button', { name: 'Draft RCA with AI' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Azure OpenAI is not configured.');
  });
});
