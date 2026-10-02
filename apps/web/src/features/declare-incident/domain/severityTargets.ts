import { slaPolicy, type Severity } from '../../incidents';
import { formatCountdown } from '../../../shared/format/duration';

export function severityTargets(severity: Severity): string {
  const windows = slaPolicy[severity];
  return `Acknowledge in ${formatCountdown(windows.acknowledgeMs)}, resolve in ${formatCountdown(windows.resolveMs)}`;
}
