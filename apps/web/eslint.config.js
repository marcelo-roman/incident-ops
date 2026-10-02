import js from '@eslint/js';
import prettier from 'eslint-config-prettier';
import boundaries from 'eslint-plugin-boundaries';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import sonarjs from 'eslint-plugin-sonarjs';
import globals from 'globals';
import tseslint from 'typescript-eslint';

const publicApi = { fileInternalPath: ['index.ts', 'testing.ts'] };

const architecturePolicies = [
  { allow: { to: { module: { origin: ['external', 'core'] } } } },
  { allow: { dependency: { relationship: { to: 'internal' } } } },
  { from: { element: { type: 'app' } }, allow: { to: { element: { types: ['shared'] } } } },
  { from: { element: { type: 'app' } }, allow: { to: { element: { type: 'feature', ...publicApi } } } },
  { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'shared' } } } },
  { from: { element: { type: 'feature' } }, allow: { to: { element: { type: 'feature', ...publicApi } } } },
  { from: { element: { types: ['mocks', 'test-setup'] } }, allow: { to: { element: { types: ['shared', 'mocks'] } } } },
  { from: { file: { categories: 'test' } }, allow: { to: { element: { types: ['mocks', 'test-setup'] } } } },
  { from: { element: { type: 'mocks' } }, allow: { to: { element: { type: 'feature', ...publicApi } } } },
];

export default tseslint.config(
  { ignores: ['dist', 'coverage', 'playwright-report', 'test-results', 'public/mockServiceWorker.js'] },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [js.configs.recommended, ...tseslint.configs.strictTypeChecked, ...tseslint.configs.stylisticTypeChecked],
    languageOptions: {
      ecmaVersion: 2023,
      globals: globals.browser,
      parserOptions: {
        projectService: true,
        tsconfigRootDir: import.meta.dirname,
      },
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
      boundaries,
    },
    settings: {
      'import/resolver': { typescript: { alwaysTryTypes: true, project: './tsconfig.app.json' } },
      'boundaries/elements': [
        { type: 'app', pattern: 'src/app' },
        { type: 'feature', pattern: 'src/features/*', capture: ['feature'] },
        { type: 'shared', pattern: 'src/shared' },
        { type: 'mocks', pattern: 'src/mocks' },
        { type: 'test-setup', pattern: 'src/test' },
      ],
      'boundaries/files': [{ category: 'test', pattern: '**/*.test.{ts,tsx}' }],
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': ['error', { allowConstantExport: true }],
      '@typescript-eslint/no-unused-vars': ['error', { ignoreRestSiblings: true, argsIgnorePattern: '^_' }],
      '@typescript-eslint/consistent-type-imports': ['error', { fixStyle: 'inline-type-imports' }],
      '@typescript-eslint/restrict-template-expressions': ['error', { allowNumber: false }],
      curly: ['error', 'all'],
      eqeqeq: ['error', 'always'],
      'no-else-return': ['error', { allowElseIf: false }],
      'no-nested-ternary': 'error',
      'no-console': 'error',
      'max-lines-per-function': ['error', { max: 80, skipBlankLines: true, skipComments: true }],
      'boundaries/dependencies': ['error', { default: 'disallow', policies: architecturePolicies }],
    },
  },
  {
    files: ['**/*.{ts,tsx}'],
    extends: [sonarjs.configs.recommended],
  },
  {
    files: ['src/**/*.test.{ts,tsx}', 'src/test/**', 'src/mocks/**', 'e2e/**'],
    rules: {
      'max-lines-per-function': 'off',
      '@typescript-eslint/no-non-null-assertion': 'off',
    },
  },
  {
    files: ['*.config.{js,ts}', 'e2e/**'],
    languageOptions: { globals: globals.node },
  },
  prettier,
);
