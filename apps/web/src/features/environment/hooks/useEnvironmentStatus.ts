import { useQuery } from '@tanstack/react-query';
import { environmentApi, environmentKeys } from '../api/environmentApi';
import { environmentStatus, recheckInterval, type EnvironmentStatus } from '../domain/environmentStatus';

const probeRetries = 1;

export function useEnvironmentStatus(): EnvironmentStatus {
  const probe = useQuery({
    queryKey: environmentKeys.probe,
    queryFn: () => environmentApi.probe(),
    retry: probeRetries,
    refetchInterval: (query) =>
      recheckInterval(environmentStatus({ isPending: query.state.status === 'pending', error: query.state.error })),
  });
  return environmentStatus({ isPending: probe.isPending, error: probe.error });
}
