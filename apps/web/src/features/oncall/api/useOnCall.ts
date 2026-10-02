import { useQuery } from '@tanstack/react-query';
import { onCallApi, onCallKeys } from './onCallApi';

const onCallStaleMs = 5 * 60_000;

export function useOnCall() {
  return useQuery({
    queryKey: onCallKeys.current,
    queryFn: () => onCallApi.current(),
    staleTime: onCallStaleMs,
  });
}
