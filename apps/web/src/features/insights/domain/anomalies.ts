import type { VolumeAnomaly } from './insights';

export const strongAnomalyZScore = 3;

export function isStrongAnomaly(anomaly: Pick<VolumeAnomaly, 'zScore'>): boolean {
  return anomaly.zScore >= strongAnomalyZScore;
}

export function anomalyKey(anomaly: Pick<VolumeAnomaly, 'serviceId' | 'weekStart'>): string {
  return `${anomaly.serviceId}-${anomaly.weekStart}`;
}
