import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { ReactElement } from 'react';
import { createMemoryRouter, RouterProvider, type RouteObject } from 'react-router';

export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: { queries: { retry: false, gcTime: Infinity }, mutations: { retry: false } },
  });
}

interface RenderRouteOptions {
  path?: string;
  initialEntry?: string;
  extraRoutes?: RouteObject[];
}

export function renderRoute(element: ReactElement, options: RenderRouteOptions = {}) {
  const { path = '/', initialEntry = '/', extraRoutes = [] } = options;
  const queryClient = createTestQueryClient();
  const router = createMemoryRouter([{ path, element }, ...extraRoutes], { initialEntries: [initialEntry] });
  const user = userEvent.setup();
  const view = render(
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>,
  );
  return { ...view, user, router, queryClient };
}
