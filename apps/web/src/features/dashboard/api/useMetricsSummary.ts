import { useQuery } from '@tanstack/react-query';
import { metricsApi, metricsKeys } from './metricsApi';

export function useMetricsSummary() {
  return useQuery({
    queryKey: metricsKeys.summary,
    queryFn: () => metricsApi.summary(),
  });
}
