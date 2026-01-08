import type { CodegenConfig } from '@graphql-codegen/cli'

const config: CodegenConfig = {
  documents: ['src/**/*.tsx', 'src/**/*.ts'],
  generates: {
    './schema.graphql': {
      config: {
        includeDirectives: true,
      },
      plugins: ['schema-ast'],
    },
    './src/shared/api/graphql/': {
      config: {
        scalars: {
          DateTime: 'Date',
          JSON: 'string',
          LocalDate: 'string',
          Long: 'BigInt',
          Upload: 'File',
          UUID: 'string',
        },
        useTypeImports: true,
      },
      preset: 'client',
    },
  },
  hooks: { afterAllFileWrite: ['prettier --write', 'eslint --fix'] },
  ignoreNoDocuments: true,
  schema: 'http://localhost:5000/graphql',
}

export default config
