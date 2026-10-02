import { describe, expect, it } from 'vitest';
import { EscalationRejected, MockIncidentStore } from './store';

const triggeredSev1 = '00000000-0000-4000-8000-000000001056';
const minute = 60_000;

describe('MockIncidentStore', () => {
  it('flags a late acknowledgement and keeps the flag through resolution', () => {
    const store = new MockIncidentStore(Date.now() - 30 * minute);

    store.acknowledge(triggeredSev1, { actor: 'Ana' });
    const resolved = store.resolve(triggeredSev1, { actor: 'Ana', rootCause: 'Bad deploy rolled back.' });

    expect(resolved?.acknowledgementBreached).toBe(true);
    expect(resolved?.slaState).toBe('Breached');
  });

  it('meets the SLA when acknowledged and resolved in time', () => {
    const store = new MockIncidentStore(Date.now());

    store.acknowledge(triggeredSev1, { actor: 'Ana' });
    const resolved = store.resolve(triggeredSev1, { actor: 'Ana', rootCause: 'Bad deploy rolled back.' });

    expect(resolved?.acknowledgementBreached).toBe(false);
    expect(resolved?.slaState).toBe('Met');
  });

  it('escalates triggered incidents, moving ackDueAt and setting the breach flag', () => {
    const store = new MockIncidentStore(Date.now());
    const before = store.get(triggeredSev1);

    const escalated = store.escalate(triggeredSev1, 'Not acknowledged in time.');

    expect(escalated?.escalationLevel).toBe(2);
    expect(escalated?.acknowledgementBreached).toBe(true);
    expect(Date.parse(escalated?.ackDueAt ?? '')).toBeGreaterThan(Date.parse(before?.ackDueAt ?? ''));
  });

  it('stops escalating at level 3', () => {
    const store = new MockIncidentStore(Date.now());
    store.escalate(triggeredSev1, 'first');
    store.escalate(triggeredSev1, 'second');

    expect(() => store.escalate(triggeredSev1, 'third')).toThrow(EscalationRejected);
  });
});
