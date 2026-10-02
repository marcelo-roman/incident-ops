import type { Severity } from './incident';

const minute = 60_000;
const hour = 60 * minute;
const day = 24 * hour;

export interface SlaWindows {
  acknowledgeMs: number;
  resolveMs: number;
}

export const slaPolicy: Readonly<Record<Severity, SlaWindows>> = {
  Sev1: { acknowledgeMs: 15 * minute, resolveMs: 4 * hour },
  Sev2: { acknowledgeMs: 30 * minute, resolveMs: 8 * hour },
  Sev3: { acknowledgeMs: 4 * hour, resolveMs: 3 * day },
  Sev4: { acknowledgeMs: 24 * hour, resolveMs: 10 * day },
};

export const atRiskThreshold = 0.25;
