import { describe, expect, it } from 'vitest';
import { declareIncidentSchema } from './declareSchema';

const valid = {
  title: 'Checkout returns 502',
  serviceId: 'checkout',
  severity: 'Sev1',
  description: 'Card payments fail for a third of customers.',
};

describe('declareIncidentSchema', () => {
  it('accepts a complete declaration', () => {
    expect(declareIncidentSchema.parse(valid)).toEqual(valid);
  });

  it('rejects severities outside the contract', () => {
    const result = declareIncidentSchema.safeParse({ ...valid, severity: 'P1' });

    expect(result.error?.issues[0]?.message).toBe('Choose a severity.');
  });

  it('requires a service and enough detail', () => {
    const result = declareIncidentSchema.safeParse({ ...valid, serviceId: '', title: 'x', description: 'short' });

    expect(result.error?.issues.map((issue) => issue.path[0])).toEqual(['title', 'serviceId', 'description']);
  });
});
