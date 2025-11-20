import jsConfigs from '@eslint/js'
import graphqlPlugin from '@graphql-eslint/eslint-plugin'
import pluginHtml from '@html-eslint/eslint-plugin'
import * as eslintParserHTML from '@html-eslint/parser'
import pluginTanstackRouter from '@tanstack/eslint-plugin-router'
import * as tsParser from '@typescript-eslint/parser'
import pluginTailwind from 'eslint-plugin-better-tailwindcss'
import { importX } from 'eslint-plugin-import-x'
import pluginJestDom from 'eslint-plugin-jest-dom'
import pluginNode from 'eslint-plugin-n'
import pluginPerfectionist from 'eslint-plugin-perfectionist'
import pluginPrettierConfigRecommended from 'eslint-plugin-prettier/recommended'
import pluginReact from 'eslint-plugin-react'
import * as pluginReactHooks from 'eslint-plugin-react-hooks'
import pluginReactRefresh from 'eslint-plugin-react-refresh'
import pluginSonar from 'eslint-plugin-sonarjs'
import pluginTestingLibrary from 'eslint-plugin-testing-library'
import pluginUnicorn from 'eslint-plugin-unicorn'
import { defineConfig } from 'eslint/config'
import globals from 'globals'
import { plugin as pluginTypescript } from 'typescript-eslint'

export default defineConfig([
  // Ignore build artifacts and dependencies
  {
    ignores: ['dist/**', 'node_modules/**'],
  },
  // HTML
  {
    extends: ['html/recommended'],
    files: ['**/*.html'],
    languageOptions: {
      parser: eslintParserHTML,
    },
    plugins: {
      html: pluginHtml,
    },
    rules: {
      'html/attrs-newline': 'off',
      'html/no-extra-spacing-attrs': [
        'error',
        { enforceBeforeSelfClose: true },
      ],
      'html/require-closing-tags': ['error', { selfClosing: 'always' }],
    },
  },
  // GraphQL
  {
    files: ['src/**/*.graphql'],
    languageOptions: {
      parser: graphqlPlugin.parser,
    },
    plugins: {
      //@ts-expect-error -- Something is wrong with the types here, but it works
      '@graphql-eslint': graphqlPlugin,
    },
    rules: {
      ...graphqlPlugin.configs['flat/schema-relay'].rules,
      ...graphqlPlugin.configs['flat/operations-recommended'].rules,
      '@graphql-eslint/strict-id-in-types': [
        'error',
        {
          acceptedIdNames: ['id', '_id'],
          acceptedIdTypes: ['ID'],
          exceptions: { suffixes: ['Payload', 'Event'], types: ['LogEntry'] },
        },
      ],
    },
  },
  pluginReactRefresh.configs.vite,
  // Global settings
  {
    files: ['**/*.{js,cjs,mjs,jsx,ts,tsx}'],
    ...jsConfigs.configs.recommended,
    ...pluginReact.configs.flat.recommended,
    ...pluginReact.configs.flat['jsx-runtime'],
    ...pluginReactHooks.configs.recommended,
    ...pluginUnicorn.configs.recommended,
    // eslint-disable-next-line import-x/no-named-as-default-member
    ...pluginSonar.configs.recommended,
    extends: [
      'typescript-eslint/strict-type-checked',
      'typescript-eslint/stylistic-type-checked',
      'import-x/flat/recommended',
      'import-x/flat/typescript',
      'import-x/flat/react',
    ],
    languageOptions: {
      ecmaVersion: 2022,
      globals: {
        ...globals.browser,
        ...globals.es2022,
        __DEV__: 'readonly',
      },
      parser: tsParser,
      parserOptions: {
        ecmaFeatures: { jsx: true },
        projectService: true,
        tsconfigRootDir: new URL('.', import.meta.url).pathname,
      },
      sourceType: 'module',
    },
    plugins: {
      //@ts-expect-error -- Something is wrong with the types here, but it works
      '@tanstack/router': pluginTanstackRouter,
      'better-tailwindcss': pluginTailwind,
      //@ts-expect-error -- Something is wrong with the types here, but it works
      'import-x': importX,
      perfectionist: pluginPerfectionist,
      'typescript-eslint': pluginTypescript,
    },
    processor: graphqlPlugin.processor,
    rules: {
      // TanStack Router
      '@tanstack/router/create-route-property-order': 'error',
      // Better TailwindCSS
      ...pluginTailwind.configs['recommended-error'].rules,
      // Import
      'import-x/no-unresolved': ['error', { ignore: ['^~icons/'] }],
      // Perfectionist
      ...pluginPerfectionist.configs['recommended-natural'].rules,
      // TypeScript
      '@typescript-eslint/only-throw-error': [
        'error',
        {
          allow: [
            {
              from: 'package',
              name: 'Redirect',
              package: '@tanstack/router-core',
            },
          ],
        },
      ],
      'perfectionist/sort-objects': [
        'error',
        {
          type: 'unsorted',
          useConfigurationIf: {
            // Ignore createRoute since that is handled by TanStack Router plugin
            callingFunctionNamePattern: '^createRoute$',
          },
        },
        {
          type: 'natural',
        },
      ],
    },
    settings: {
      'better-tailwindcss': {
        entryPoint: 'src/app/styles/main.css',
      },
      'import-x/resolver': {
        node: true,
        typescript: true,
      },
      react: { version: 'detect' },
    },
  },
  // Vitest + Testing Library
  {
    files: ['**/*.test.{ts,tsx}', '**/__tests__/**/*.{ts,tsx}'],
    plugins: {
      'jest-dom': pluginJestDom,
      'testing-library': pluginTestingLibrary,
    },
    rules: {
      'jest-dom/prefer-to-have-text-content': 'warn',
      'testing-library/no-unnecessary-act': 'warn',
      'testing-library/render-result-naming-convention': 'error',
    },
  },
  // Scripts (looser rules for config/migration scripts)
  {
    files: ['*.config.{js,cjs,mjs,ts}', 'scripts/**/*.{js,ts}'],
    languageOptions: {
      globals: {
        ...globals.node,
      },
    },
    plugins: {
      n: pluginNode,
    },
    rules: {
      ...pluginNode.configs['flat/recommended-module'].rules,
    },
  },
  pluginPrettierConfigRecommended,
])
