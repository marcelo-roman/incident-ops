import { useQuery } from '@tanstack/react-query';
import { insightsApi, insightsKeys } from './insightsApi';

const insightsStaleMs = 5 * 60_000;

export function useKpis(days: number) {
  return useQuery({
    queryKey: insightsKeys.kpis(days),
    queryFn: () => insightsApi.kpis(days),
    staleTime: insightsStaleMs,
  });
}

export function useRecurringClusters(days: number) {
  return useQuery({
    queryKey: insightsKeys.recurring(days),
    queryFn: () => insightsApi.recurring(days),
    staleTime: insightsStaleMs,
  });
}

export function useAnomalies(days: number) {
  return useQuery({
    queryKey: insightsKeys.anomalies(days),
    queryFn: () => insightsApi.anomalies(days),
    staleTime: insightsStaleMs,
  });
}
