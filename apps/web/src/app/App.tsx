import { QueryClientProvider } from '@tanstack/react-query';
import { useState } from 'react';
import { RouterProvider } from 'react-router';
import { RealtimeProvider } from '../features/realtime';
import type { AppConfig } from '../shared/config/appConfig';
import { createQueryClient } from './queryClient';
import { createAppRouter } from './router';

export function App({ config }: { config: AppConfig }) {
  const [queryClient] = useState(createQueryClient);
  const [router] = useState(createAppRouter);
  return (
    <QueryClientProvider client={queryClient}>
      <RealtimeProvider apiBaseUrl={config.apiBaseUrl} enabled={!config.useMocks}>
        <RouterProvider router={router} />
      </RealtimeProvider>
    </QueryClientProvider>
  );
}
