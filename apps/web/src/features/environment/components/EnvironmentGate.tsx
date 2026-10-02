import type { ReactNode } from 'react';
import { useEnvironmentStatus } from '../hooks/useEnvironmentStatus';
import { EnvironmentPausedNotice } from './EnvironmentPausedNotice';

export function EnvironmentGate({ children }: { children: ReactNode }) {
  const status = useEnvironmentStatus();
  if (status === 'paused') {
    return <EnvironmentPausedNotice />;
  }
  return children;
}
