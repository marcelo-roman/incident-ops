import { QueryClientProvider } from '@tanstack/react-query';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { createMemoryRouter, RouterProvider } from 'react-router';
import { describe, expect, it } from 'vitest';
import { createTestQueryClient } from '../shared/test/render';
import { AppShell } from './AppShell';

function renderShell(initialEntry = '/incidents') {
  const router = createMemoryRouter(
    [{ element: <AppShell />, children: [{ path: '*', element: <p>Page content</p> }] }],
    { initialEntries: [initialEntry] },
  );
  render(
    <QueryClientProvider client={createTestQueryClient()}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return userEvent.setup();
}

describe('AppShell', () => {
  it('marks the current page in the primary navigation', () => {
    renderShell('/incidents');

    const navigation = screen.getByRole('navigation', { name: 'Primary' });
    expect(navigation.querySelector('[aria-current="page"]')).toHaveTextContent('Incidents');
    expect(screen.getByRole('link', { name: 'Skip to content' })).toHaveAttribute('href', '#main');
  });

  it('switches theme from the keyboard and remembers it', async () => {
    const user = renderShell();

    screen.getByRole('radio', { name: 'Auto' }).focus();
    await user.keyboard('{ArrowRight}');

    expect(screen.getByRole('radio', { name: 'Light' })).toHaveAttribute('aria-checked', 'true');
    expect(screen.getByRole('radio', { name: 'Light' })).toHaveFocus();
    expect(document.documentElement).toHaveAttribute('data-theme', 'light');
    expect(window.localStorage.getItem('incident-ops.theme')).toBe('light');
  });

  it('shows that live updates are off outside a realtime provider', () => {
    renderShell();

    expect(screen.getByRole('status')).toHaveTextContent('Live updates off');
  });

  it('remembers the operator name', async () => {
    const user = renderShell();

    await user.type(screen.getByLabelText('On shift as'), 'Ana');

    expect(window.localStorage.getItem('incident-ops.operator')).toBe('Ana');
  });
});
