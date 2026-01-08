import jsConfigs from '@eslint/js'
import graphqlPlugin from '@graphql-eslint/eslint-plugin'
import pluginHtml from '@html-eslint/eslint-plugin'
import * as eslintParserHTML from '@html-eslint/parser'
import pluginTanstackRouter from '@tanstack/eslint-plugin-router'
import * as tsParser from '@typescript-eslint/parser'
import pluginTailwind from 'eslint-plugin-better-tailwindcss'
import importXPlugin, {
  flatConfigs as importXFlatConfigs,
} from 'eslint-plugin-import-x'
import pluginJestDom from 'eslint-plugin-jest-dom'
import pluginNode from 'eslint-plugin-n'
import pluginPerfectionist, {
  configs as perfectionistConfigs,
} from 'eslint-plugin-perfectionist'
import pluginPrettierConfigRecommended from 'eslint-plugin-prettier/recommended'
import pluginReact from 'eslint-plugin-react'
import pluginReactHooks from 'eslint-plugin-react-hooks'
import pluginReactRefresh from 'eslint-plugin-react-refresh'
import pluginSonar from 'eslint-plugin-sonarjs'
import pluginTestingLibrary from 'eslint-plugin-testing-library'
import pluginUnicorn from 'eslint-plugin-unicorn'
import { defineConfig } from 'eslint/config'
import globals from 'globals'
import { configs as tsConfigs, plugin as tsPlugin } from 'typescript-eslint'

/* eslint-disable @typescript-eslint/no-unsafe-assignment */
/* eslint-disable @typescript-eslint/no-explicit-any */

export default defineConfig([
  // Ignore build artifacts and dependencies
  {
    ignores: ['dist/**', 'node_modules/**'],
  },
  pluginReactRefresh.configs.vite,
  importXFlatConfigs.recommended,
  importXFlatConfigs.typescript,
  importXFlatConfigs.react,
  ...tsConfigs.strictTypeChecked.map((config) => ({
    ...config,
    files: ['**/*.{js,cjs,mjs,jsx,ts,tsx}'],
  })),
  ...tsConfigs.stylisticTypeChecked.map((config) => ({
    ...config,
    files: ['**/*.{js,cjs,mjs,jsx,ts,tsx}'],
  })),
  // HTML
  {
    extends: ['@html-eslint/recommended'],
    files: ['**/*.html'],
    languageOptions: {
      parser: eslintParserHTML,
    },
    plugins: {
      '@html-eslint': pluginHtml,
    },
    rules: {
      '@html-eslint/attrs-newline': 'off',
      '@html-eslint/no-extra-spacing-attrs': [
        'error',
        { enforceBeforeSelfClose: true },
      ],
      '@html-eslint/require-closing-tags': ['error', { selfClosing: 'always' }],
    },
  },
  // Global settings
  {
    files: ['**/*.{js,cjs,mjs,jsx,ts,tsx}'],
    ...jsConfigs.configs.recommended,
    ...pluginReact.configs.flat.recommended,
    ...pluginReact.configs.flat['jsx-runtime'],
    ...pluginUnicorn.configs.recommended,
    // eslint-disable-next-line import-x/no-named-as-default-member
    ...pluginSonar.configs.recommended,
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
      '@tanstack/router': pluginTanstackRouter,
      '@typescript-eslint': tsPlugin,
      'better-tailwindcss': pluginTailwind,
      'import-x': importXPlugin as any,
      perfectionist: pluginPerfectionist,
      'react-hooks': pluginReactHooks as any,
    },
    processor: graphqlPlugin.processor,
    rules: {
      // TanStack Router
      '@tanstack/router/create-route-property-order': 'error',
      // Better TailwindCSS
      ...pluginTailwind.configs['recommended-error'].rules,
      /**
       * Domain Boundary Enforcement
       *
       * These rules enforce the architectural boundaries between layers:
       * - Features should not import from other features (use @/domain instead)
       * - Shared layer should not import from features or domain
       * - App layer can import from anywhere
       *
       * Note: Same-feature internal imports (relative like '../' or '@/features/X' within X)
       * are allowed. Only cross-feature imports trigger errors.
       *
       * @see src/domain/README.md for architecture documentation
       */
      'import-x/no-restricted-paths': [
        'error',
        {
          zones: [
            // Shared layer should not import from features
            {
              from: './src/features/**',
              message:
                'Shared utilities should not depend on feature implementations.',
              target: './src/shared/**/*',
            },
            // Shared layer should not import from domain
            {
              from: './src/domain/**',
              message:
                'Shared utilities should not depend on domain layer. Domain can import from shared.',
              target: './src/shared/**/*',
            },
            // Domain layer should not import from features
            {
              from: './src/features/**',
              message:
                'Domain layer should not depend on feature implementations.',
              target: './src/domain/**/*',
            },
          ],
        },
      ],
      // Import
      'import-x/no-unresolved': ['error', { ignore: ['^~icons/'] }],
      // Perfectionist
      ...(
        perfectionistConfigs?.['recommended-natural'] as
          | undefined
          | { rules?: Record<string, unknown> }
      )?.rules,
      // React Hooks
      ...pluginReactHooks.configs.recommended.rules,
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
        typescript: {
          alwaysTryTypes: true,
        },
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
  // GraphQL in .graphql files
  {
    files: ['src/**/*.graphql'],
    languageOptions: {
      parser: graphqlPlugin.parser,
    },
    plugins: { '@graphql-eslint': graphqlPlugin },
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
      // Disable TypeScript type-checked rules for GraphQL files
      '@typescript-eslint/await-thenable': 'off',
      '@typescript-eslint/dot-notation': 'off',
      '@typescript-eslint/no-array-delete': 'off',
      '@typescript-eslint/no-base-to-string': 'off',
      '@typescript-eslint/no-confusing-void-expression': 'off',
      '@typescript-eslint/no-deprecated': 'off',
      '@typescript-eslint/no-duplicate-type-constituents': 'off',
      '@typescript-eslint/no-floating-promises': 'off',
      '@typescript-eslint/no-for-in-array': 'off',
      '@typescript-eslint/no-implied-eval': 'off',
      '@typescript-eslint/no-meaningless-void-operator': 'off',
      '@typescript-eslint/no-misused-promises': 'off',
      '@typescript-eslint/no-misused-spread': 'off',
      '@typescript-eslint/no-mixed-enums': 'off',
      '@typescript-eslint/no-redundant-type-constituents': 'off',
      '@typescript-eslint/no-unnecessary-boolean-literal-compare': 'off',
      '@typescript-eslint/no-unnecessary-condition': 'off',
      '@typescript-eslint/no-unnecessary-template-expression': 'off',
      '@typescript-eslint/no-unnecessary-type-arguments': 'off',
      '@typescript-eslint/no-unnecessary-type-assertion': 'off',
      '@typescript-eslint/no-unnecessary-type-conversion': 'off',
      '@typescript-eslint/no-unnecessary-type-parameters': 'off',
      '@typescript-eslint/no-unsafe-argument': 'off',
      '@typescript-eslint/no-unsafe-assignment': 'off',
      '@typescript-eslint/no-unsafe-call': 'off',
      '@typescript-eslint/no-unsafe-enum-comparison': 'off',
      '@typescript-eslint/no-unsafe-member-access': 'off',
      '@typescript-eslint/no-unsafe-return': 'off',
      '@typescript-eslint/no-unsafe-unary-minus': 'off',
      '@typescript-eslint/no-useless-default-assignment': 'off',
      '@typescript-eslint/non-nullable-type-assertion-style': 'off',
      '@typescript-eslint/only-throw-error': 'off',
      '@typescript-eslint/prefer-find': 'off',
      '@typescript-eslint/prefer-includes': 'off',
      '@typescript-eslint/prefer-nullish-coalescing': 'off',
      '@typescript-eslint/prefer-optional-chain': 'off',
      '@typescript-eslint/prefer-promise-reject-errors': 'off',
      '@typescript-eslint/prefer-readonly': 'off',
      '@typescript-eslint/prefer-readonly-parameter-types': 'off',
      '@typescript-eslint/prefer-reduce-type-parameter': 'off',
      '@typescript-eslint/prefer-regexp-exec': 'off',
      '@typescript-eslint/prefer-return-this-type': 'off',
      '@typescript-eslint/prefer-string-starts-ends-with': 'off',
      '@typescript-eslint/promise-function-async': 'off',
      '@typescript-eslint/related-getter-setter-pairs': 'off',
      '@typescript-eslint/require-array-sort-compare': 'off',
      '@typescript-eslint/require-await': 'off',
      '@typescript-eslint/restrict-plus-operands': 'off',
      '@typescript-eslint/restrict-template-expressions': 'off',
      '@typescript-eslint/return-await': 'off',
      '@typescript-eslint/strict-boolean-expressions': 'off',
      '@typescript-eslint/switch-exhaustiveness-check': 'off',
      '@typescript-eslint/unbound-method': 'off',
      '@typescript-eslint/use-unknown-in-catch-callback-variable': 'off',
    },
  },
  pluginPrettierConfigRecommended as any,
])
