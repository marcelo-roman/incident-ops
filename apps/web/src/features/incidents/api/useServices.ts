import { useQuery } from '@tanstack/react-query';
import { useMemo } from 'react';
import { servicesApi } from './client';
import { serviceKeys } from './incidentKeys';
import type { Service } from '../domain/service';

const servicesStaleMs = 10 * 60_000;

export function useServices() {
  return useQuery({
    queryKey: serviceKeys.all,
    queryFn: () => servicesApi.list(),
    staleTime: servicesStaleMs,
  });
}

export function useServiceName(): (serviceId: string) => string {
  const { data } = useServices();
  const byId = useMemo(() => new Map((data ?? []).map((service: Service) => [service.id, service.name])), [data]);
  return (serviceId) => byId.get(serviceId) ?? serviceId;
}
