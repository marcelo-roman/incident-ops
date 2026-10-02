import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router';
import { loginPathFor } from '../domain/returnPath';
import { useSession } from '../hooks/useSession';

export function RequireSession({ children }: Readonly<{ children: ReactNode }>) {
  const session = useSession();
  const { pathname, search, hash } = useLocation();
  if (session === null) {
    return <Navigate to={loginPathFor(`${pathname}${search}${hash}`)} replace />;
  }
  return <>{children}</>;
}
