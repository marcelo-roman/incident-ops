import type { IncidentDetail } from '../../features/incidents';
import type {
  AnomalyReport,
  KpiGroup,
  KpiReport,
  RecurringCluster,
  RecurringReport,
  WeeklyKpi,
} from '../../features/insights';
import type { RcaDraft } from '../../features/rca';
import { serviceFixtures } from './services';

const dayMs = 86_400_000;
const weekMs = 7 * dayMs;

function window(days: number, now: number) {
  return { start: new Date(now - days * dayMs).toISOString(), end: new Date(now).toISOString(), days };
}

function wave(seed: number, amplitude: number, base: number): number {
  return Math.round((base + amplitude * Math.sin(seed * 1.7) + (amplitude / 2) * Math.cos(seed * 0.6)) * 10) / 10;
}

function weekly(days: number, now: number): WeeklyKpi[] {
  const weeks = Math.ceil(days / 7);
  return Array.from({ length: weeks }, (_, index) => ({
    weekStart: new Date(now - (weeks - index) * weekMs).toISOString().slice(0, 10),
    incidents: Math.max(1, Math.round(wave(index, 4, 9))),
    mttaMedianMinutes: wave(index + 3, 4, 12),
    mttrMedianMinutes: wave(index + 5, 60, 220),
    slaCompliancePct: Math.min(100, wave(index + 2, 4, 93)),
  }));
}

function serviceGroup(serviceId: string, index: number, days: number): KpiGroup {
  const incidents = Math.round((days / 30) * (6 + ((index * 5) % 9)));
  const breached = Math.round(incidents * (0.04 + (index % 3) * 0.03));
  return {
    serviceId,
    incidents,
    mtta: { samples: incidents, medianMinutes: wave(index, 3, 11), p90Minutes: wave(index, 8, 34) },
    mttr: { samples: incidents, medianMinutes: wave(index, 70, 210), p90Minutes: wave(index, 180, 690) },
    sla: {
      evaluated: incidents,
      breached,
      acknowledgePct: wave(index, 2, 96),
      resolvePct: wave(index, 4, 91),
      overallPct: Math.round((100 - (breached / Math.max(incidents, 1)) * 100) * 10) / 10,
    },
  };
}

export function kpiReportFixture(days: number, now: number): KpiReport {
  const byService = serviceFixtures.map((service, index) => serviceGroup(service.id, index, days));
  const incidents = byService.reduce((sum, group) => sum + group.incidents, 0);
  const breached = byService.reduce((sum, group) => sum + group.sla.breached, 0);
  return {
    window: window(days, now),
    overall: {
      incidents,
      mtta: { samples: incidents, medianMinutes: 11.4, p90Minutes: 38.2 },
      mttr: { samples: incidents, medianMinutes: 214, p90Minutes: 702 },
      sla: {
        evaluated: incidents,
        breached,
        acknowledgePct: 95.1,
        resolvePct: 90.3,
        overallPct: Math.round((100 - (breached / incidents) * 100) * 10) / 10,
      },
    },
    byService,
    bySeverity: [],
    weekly: weekly(days, now),
  };
}

const minorClusterTopics = [
  ['dns resolution timeout', 'platform'],
  ['cache eviction storm', 'checkout'],
  ['token refresh failure', 'identity'],
  ['webhook retry backlog', 'payments-gateway'],
  ['email bounce spike', 'notifications'],
  ['report export timeout', 'reporting'],
  ['autocomplete latency', 'search'],
  ['pod restart loop', 'platform'],
  ['rate limit exhaustion', 'payments-gateway'],
] as const;

function minorClusters(now: number): RecurringCluster[] {
  return minorClusterTopics.map(([label, serviceId], index) => {
    const count = 3 - (index % 2);
    return {
      label,
      terms: label.split(' '),
      count,
      cohesion: 0.4,
      services: [{ serviceId, count }],
      sampleTitles: [`${label.charAt(0).toUpperCase()}${label.slice(1)} on ${serviceId}`],
      firstSeen: new Date(now - (60 + index) * dayMs).toISOString(),
      lastSeen: new Date(now - (index + 3) * dayMs).toISOString(),
    };
  });
}

export function recurringFixture(days: number, now: number): RecurringReport {
  return {
    window: window(days, now),
    incidentsAnalyzed: Math.round(days * 1.4),
    clusters: [
      ...minorClusters(now),
      {
        label: 'certificate expiry handshake',
        terms: ['certificate', 'expired', 'tls', 'handshake'],
        count: 7,
        cohesion: 0.62,
        services: [
          { serviceId: 'payments-gateway', count: 4 },
          { serviceId: 'identity', count: 3 },
        ],
        sampleTitles: [
          'Payment authorizations timing out for one acquirer',
          'SSO login fails with TLS handshake error',
          'Webhook delivery failing after certificate rotation',
        ],
        firstSeen: new Date(now - 160 * dayMs).toISOString(),
        lastSeen: new Date(now - 5 * dayMs).toISOString(),
      },
      {
        label: 'queue backlog delayed delivery',
        terms: ['queue', 'backlog', 'delayed', 'consumer'],
        count: 6,
        cohesion: 0.55,
        services: [
          { serviceId: 'notifications', count: 5 },
          { serviceId: 'reporting', count: 1 },
        ],
        sampleTitles: ['Push notifications delayed over 10 minutes', 'Order emails delayed during campaign send'],
        firstSeen: new Date(now - 120 * dayMs).toISOString(),
        lastSeen: new Date(now - 1 * dayMs).toISOString(),
      },
      {
        label: 'index lag stale results',
        terms: ['index', 'stale', 'lag', 'search'],
        count: 4,
        cohesion: 0.49,
        services: [{ serviceId: 'search', count: 4 }],
        sampleTitles: ['Product search returns stale prices', 'New products missing from search for an hour'],
        firstSeen: new Date(now - 90 * dayMs).toISOString(),
        lastSeen: new Date(now - 2 * dayMs).toISOString(),
      },
    ],
  };
}

export function anomaliesFixture(days: number, now: number): AnomalyReport {
  const weekStart = (weeksAgo: number) => new Date(now - weeksAgo * weekMs).toISOString().slice(0, 10);
  return {
    window: window(days, now),
    method: 'Rolling z-score',
    threshold: 2.5,
    anomalies: [
      {
        serviceId: 'notifications',
        weekStart: weekStart(1),
        count: 9,
        baselineMean: 2.8,
        baselineStd: 1.4,
        zScore: 4.4,
      },
      { serviceId: 'checkout', weekStart: weekStart(3), count: 7, baselineMean: 3.1, baselineStd: 1.3, zScore: 3.0 },
      { serviceId: 'search', weekStart: weekStart(6), count: 6, baselineMean: 2.4, baselineStd: 1.2, zScore: 2.6 },
    ],
  };
}

const openAiDrafter = 'azure-openai:rca-drafts';
const fallbackDrafter = 'deterministic-fallback';

export function mockDrafterFor(incidentNumber: number): string {
  if (incidentNumber % 2 === 0) {
    return openAiDrafter;
  }
  return fallbackDrafter;
}

export function rcaDraftFixture(incident: IncidentDetail): RcaDraft {
  return {
    incidentId: incident.id,
    incidentNumber: incident.number,
    summary: `${incident.title}. ${incident.rootCause ?? 'The root cause is still being confirmed.'}`,
    impact: `${incident.severity} impact on ${incident.serviceId} for the duration of the incident.`,
    timeline: incident.timeline.map((entry) => ({ at: entry.at, event: `${entry.kind}: ${entry.message}` })),
    contributingFactors: [
      'No alert covered the failing dependency, so detection relied on customer reports.',
      'The runbook for this service did not describe the rollback path.',
    ],
    actionItems: [
      {
        title: 'Add a synthetic check for the dependency with a paging alert.',
        owner: 'Site Reliability',
        priority: 'P1',
      },
      { title: 'Document the rollback procedure in the service runbook.', owner: 'Service owner', priority: 'P2' },
      { title: 'Review similar configurations across Tier 1 services.', owner: 'Engineering lead', priority: 'P3' },
    ],
    generatedBy: mockDrafterFor(incident.number),
    generatedAt: new Date().toISOString(),
  };
}
