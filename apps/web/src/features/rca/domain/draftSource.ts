export type DraftSourceKind = 'azure-openai' | 'fallback' | 'other';

export interface DraftSource {
  kind: DraftSourceKind;
  label: string;
}

const azureOpenAiPrefix = 'azure-openai';
const fallbackName = 'deterministic-fallback';

export const rcaGuidance =
  'Drafts a root cause analysis from the incident record and timeline. Review and edit before sharing.';

function azureOpenAiSource(generatedBy: string): DraftSource {
  const deployment = generatedBy.slice(azureOpenAiPrefix.length).replace(/^:/, '').trim();
  if (deployment === '') {
    return { kind: 'azure-openai', label: 'Drafted by Azure OpenAI' };
  }
  return { kind: 'azure-openai', label: `Drafted by Azure OpenAI (${deployment})` };
}

export function describeDraftSource(generatedBy: string): DraftSource {
  const name = generatedBy.trim();
  if (name === fallbackName) {
    return { kind: 'fallback', label: 'Drafted by the rule-based fallback (Azure OpenAI not configured)' };
  }
  if (name === azureOpenAiPrefix || name.startsWith(`${azureOpenAiPrefix}:`)) {
    return azureOpenAiSource(name);
  }
  if (name === '') {
    return { kind: 'other', label: 'Drafted by an unidentified generator' };
  }
  return { kind: 'other', label: `Drafted by ${name}` };
}
