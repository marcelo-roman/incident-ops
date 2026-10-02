import { QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { RouterProvider } from 'react-router';
import { currentAccessToken, useSession } from '../features/auth';
import { RealtimeProvider } from '../features/realtime';
import type { AppConfig } from '../shared/config/appConfig';
import { createQueryClient } from './queryClient';
import { createAppRouter } from './router';

export function App({ config }: Readonly<{ config: AppConfig }>) {
  const [queryClient] = useState(createQueryClient);
  const [router] = useState(createAppRouter);
  const session = useSession();
  return (
    <QueryClientProvider client={queryClient}>
      <RealtimeProvider
        apiBaseUrl={config.apiBaseUrl}
        enabled={!config.useMocks && session !== null}
        accessTokenFactory={currentAccessToken}
      >
        <RouterProvider router={router} />
      </RealtimeProvider>
    </QueryClientProvider>
  );
}
