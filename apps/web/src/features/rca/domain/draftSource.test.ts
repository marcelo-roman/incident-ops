import { describe, expect, it } from 'vitest';
import { describeDraftSource } from './draftSource';

describe('describeDraftSource', () => {
  it('names the Azure OpenAI deployment when present', () => {
    expect(describeDraftSource('azure-openai:rca-drafts')).toEqual({
      kind: 'azure-openai',
      label: 'Drafted by Azure OpenAI (rca-drafts)',
    });
  });

  it('omits the deployment when the service does not report one', () => {
    expect(describeDraftSource('azure-openai').label).toBe('Drafted by Azure OpenAI');
    expect(describeDraftSource('azure-openai:').label).toBe('Drafted by Azure OpenAI');
  });

  it('says plainly when the rule-based fallback wrote the draft', () => {
    expect(describeDraftSource('deterministic-fallback')).toEqual({
      kind: 'fallback',
      label: 'Drafted by the rule-based fallback (Azure OpenAI not configured)',
    });
  });

  it('passes unknown generators through without claiming Azure OpenAI', () => {
    expect(describeDraftSource('local-llm')).toEqual({ kind: 'other', label: 'Drafted by local-llm' });
    expect(describeDraftSource(' ').label).toBe('Drafted by an unidentified generator');
    expect(describeDraftSource('azure-openai-proxy').kind).toBe('other');
  });
});
