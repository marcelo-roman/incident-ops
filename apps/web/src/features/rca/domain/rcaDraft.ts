export interface RcaTimelineItem {
  at: string;
  event: string;
}

export const actionItemPriorities = ['P1', 'P2', 'P3'] as const;
export type ActionItemPriority = (typeof actionItemPriorities)[number];

export interface RcaActionItem {
  title: string;
  owner: string;
  priority: ActionItemPriority;
}

export interface RcaDraft {
  incidentId: string;
  incidentNumber: number;
  summary: string;
  impact: string;
  timeline: RcaTimelineItem[];
  contributingFactors: string[];
  actionItems: RcaActionItem[];
  generatedBy: string;
  generatedAt: string;
}
