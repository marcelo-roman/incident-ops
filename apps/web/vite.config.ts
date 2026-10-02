import react from '@vitejs/plugin-react';
import type { Plugin } from 'vite';
import { defineConfig } from 'vitest/config';
import { runtimeConfigDocument, type BuildEnvironment } from './src/shared/config/environment.ts';

function runtimeConfigFile(): Plugin {
  let environment: BuildEnvironment = {};
  return {
    name: 'runtime-config-file',
    configResolved(resolved) {
      environment = resolved.env;
    },
    configureServer(server) {
      server.middlewares.use('/config.json', (_request, response) => {
        response.setHeader('content-type', 'application/json');
        response.setHeader('cache-control', 'no-store');
        response.end(runtimeConfigDocument(environment));
      });
    },
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'config.json', source: runtimeConfigDocument(environment) });
    },
  };
}

export default defineConfig({
  plugins: [react(), runtimeConfigFile()],
  server: { port: 5173, strictPort: true },
  build: {
    sourcemap: true,
    rolldownOptions: {
      output: {
        codeSplitting: {
          groups: [
            { name: 'react', test: /node_modules[\\/](react|react-dom|react-router|scheduler)[\\/]/ },
            { name: 'data', test: /node_modules[\\/](@tanstack|@microsoft[\\/]signalr)[\\/]/ },
            { name: 'forms', test: /node_modules[\\/](react-hook-form|@hookform|zod)[\\/]/ },
          ],
        },
      },
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    css: false,
    restoreMocks: true,
    coverage: {
      provider: 'v8',
      reporter: ['text-summary', 'html', 'lcov'],
      include: ['src/**/*.{ts,tsx}'],
      exclude: [
        'src/**/*.test.{ts,tsx}',
        'src/**/index.ts',
        'src/**/testing.ts',
        'src/**/testing/**',
        'src/test/**',
        'src/shared/test/**',
        'src/mocks/**',
        'src/main.tsx',
        'src/vite-env.d.ts',
      ],
      thresholds: {
        lines: 70,
        statements: 70,
        functions: 70,
        branches: 70,
        'src/features/*/domain/**': { lines: 95, statements: 95, functions: 95, branches: 90 },
        'src/shared/{format,config,http}/**': { lines: 90, statements: 90, functions: 90, branches: 85 },
      },
    },
  },
});
