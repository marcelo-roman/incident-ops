import { createBrowserRouter, type RouteObject } from 'react-router';
import { RequireSession } from '../features/auth';
import { DashboardPage } from '../features/dashboard';
import { DeclareIncidentPage } from '../features/declare-incident';
import { IncidentDetailPage, IncidentsPage } from '../features/incidents';
import { NotFoundPage } from './NotFoundPage';
import { RouteLoading } from './RouteLoading';
import { AppShell } from './AppShell';
import { LoginLayout } from './LoginLayout';

export const routes: RouteObject[] = [
  { path: 'login', element: <LoginLayout /> },
  {
    element: (
      <RequireSession>
        <AppShell />
      </RequireSession>
    ),
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
