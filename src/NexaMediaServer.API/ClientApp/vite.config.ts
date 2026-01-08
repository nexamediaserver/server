import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { resolve } from 'node:path'
import icons from 'unplugin-icons/vite'
import { defineConfig } from 'vite'
import { compression } from 'vite-plugin-compression2'
import graphqlLoader from 'vite-plugin-graphql-loader'

// https://vite.dev/config/
export default defineConfig({
  base: '/web/',
  build: {
    emptyOutDir: true,
    outDir: '../wwwroot/build',
  },
  optimizeDeps: {
    entries: ['./src/main.tsx'],
  },
  plugins: [
    react({
      babel: {
        plugins: ['babel-plugin-react-compiler'],
      },
    }),
    graphqlLoader(),
    tailwindcss(),
    icons({
      autoInstall: true,
      compiler: 'jsx',
      jsx: 'react',
    }),
    compression(),
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, './src'),
    },
  },
  server: {
    port: 3000,
    proxy: {
      '/api': 'http://localhost:5000',
      '/graphql': {
        changeOrigin: true,
        secure: false,
        target: 'http://localhost:5000',
        ws: true,
      },
      '/hangfire': {
        changeOrigin: true,
        secure: false,
        target: 'http://localhost:5000',
        ws: true,
      },
      '/openapi': {
        changeOrigin: true,
        secure: false,
        target: 'http://localhost:5000',
      },
      '/scalar': {
        changeOrigin: true,
        secure: false,
        target: 'http://localhost:5000',
      },
    },
    strictPort: false,
  },
})
