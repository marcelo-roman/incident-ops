import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { SlaClock } from '../domain/sla';
import { SlaFuse } from './SlaFuse';

function aClock(overrides: Partial<SlaClock>): SlaClock {
  return {
    target: 'Acknowledge',
    dueAt: 0,
    remainingMs: 300_000,
    fractionRemaining: 0.33,
    state: 'OnTrack',
    ...overrides,
  };
}

describe('SlaFuse', () => {
  it('shows the remaining time and describes it to assistive technology', () => {
    render(<SlaFuse clock={aClock({ remainingMs: 185_000 })} />);

    const timer = screen.getByRole('timer');
    expect(timer).toHaveTextContent('3m 05s');
    expect(timer).toHaveAccessibleName('Acknowledge within 3m 05s, On track');
  });

  it('shows how long ago a breached deadline passed', () => {
    render(
      <SlaFuse
        clock={aClock({ target: 'Resolve', remainingMs: -3_720_000, fractionRemaining: 0, state: 'Breached' })}
      />,
    );

    expect(screen.getByRole('timer')).toHaveAccessibleName('Resolve deadline passed 1h 02m ago, Breached');
  });
});
