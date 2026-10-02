import { createBrowserRouter, type RouteObject } from 'react-router';
import { DashboardPage } from '../features/dashboard';
import { DeclareIncidentPage } from '../features/declare-incident';
import { IncidentDetailPage, IncidentsPage } from '../features/incidents';
import { NotFoundPage } from './NotFoundPage';
import { RouteLoading } from './RouteLoading';
import { AppShell } from './AppShell';

export const routes: RouteObject[] = [
  {
    element: <AppShell />,
    children: [
      { index: true, element: <DashboardPage /> },
      { path: 'incidents', element: <IncidentsPage /> },
      { path: 'incidents/new', element: <DeclareIncidentPage /> },
      { path: 'incidents/:incidentId', element: <IncidentDetailPage /> },
      {
        path: 'insights',
        hydrateFallbackElement: <RouteLoading />,
        lazy: async () => {
          const { InsightsPage } = await import('../features/insights');
          return { Component: InsightsPage };
        },
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
];

export function createAppRouter() {
  return createBrowserRouter(routes);
}
