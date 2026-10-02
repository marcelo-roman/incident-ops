import { defineConfig, devices } from '@playwright/test';

const port = 4173;

export default defineConfig({
  testDir: './e2e',
  timeout: 30_000,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: `http://127.0.0.1:${String(port)}`,
    trace: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: `pnpm exec vite --port ${String(port)} --strictPort --host 127.0.0.1`,
    env: { VITE_USE_MOCKS: 'true' },
    url: `http://127.0.0.1:${String(port)}`,
    reuseExistingServer: false,
    timeout: 60_000,
  },
});
