import { useSearchParams } from 'react-router';
import { returnToParameter, safeReturnPath } from '../domain/returnPath';

export function useReturnPath(): string {
  const [params] = useSearchParams();
  return safeReturnPath(params.get(returnToParameter));
}
