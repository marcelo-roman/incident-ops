import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ConnectionIndicator } from './ConnectionIndicator';

describe('ConnectionIndicator', () => {
  it.each([
    ['connected', 'Live'],
    ['reconnecting', 'Reconnecting'],
    ['offline', 'Offline, retrying'],
    ['disabled', 'Live updates off'],
  ] as const)('shows %s as "%s"', (status, text) => {
    render(<ConnectionIndicator status={status} />);

    expect(screen.getByRole('status')).toHaveTextContent(text);
  });
});
