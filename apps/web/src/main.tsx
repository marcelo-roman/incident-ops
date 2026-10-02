import '@fontsource-variable/public-sans';
import './app/styles/tokens.css';
import './app/styles/base.css';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './app/App';
import { buildTimeConfig, setConfig, type AppConfig } from './shared/config/appConfig';
import { loadRuntimeConfig } from './shared/config/runtimeConfig';

async function startMockServiceWorker(config: AppConfig): Promise<void> {
  if (!config.useMocks) {
    return;
  }
  const { worker } = await import('./mocks/browser');
  await worker.start({ onUnhandledRequest: 'bypass', quiet: true });
}

async function bootstrap(): Promise<void> {
  const config = await loadRuntimeConfig(buildTimeConfig);
  setConfig(config);
  await startMockServiceWorker(config);
  const container = document.getElementById('root');
  if (container === null) {
    throw new Error('Root element #root is missing from index.html');
  }
  createRoot(container).render(
    <StrictMode>
      <App config={config} />
    </StrictMode>,
  );
}

void bootstrap();
