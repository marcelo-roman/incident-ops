import { describe, expect, it } from 'vitest';
import { anIncident } from '../testing/factories';
import { incidentFacts } from './incidentFacts';

describe('incidentFacts', () => {
  it('lists the record fields in reading order', () => {
    const facts = incidentFacts(anIncident({ escalationLevel: 2, assignee: null }), 'Checkout');

    expect(facts.map((fact) => fact.term)).toEqual([
      'Service',
      'Assignee',
      'Escalation',
      'Opened',
      'Acknowledged',
      'Mitigated',
      'Resolved',
    ]);
    expect(facts[0]?.detail).toBe('Checkout');
    expect(facts[1]?.detail).toBe('Unassigned');
    expect(facts[2]?.detail).toBe('Level 2, Secondary on call');
    expect(facts[4]?.detail).toBe('—');
  });

  it('adds the alert fingerprint for alert-sourced incidents', () => {
    const facts = incidentFacts(anIncident({ source: 'Alertmanager', alertFingerprint: 'abc123' }), 'Checkout');

    expect(facts.at(-1)).toEqual({ term: 'Alert fingerprint', detail: 'abc123' });
  });
});
