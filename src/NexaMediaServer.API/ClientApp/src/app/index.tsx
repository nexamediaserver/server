import { StrictMode } from 'react'

import { ApolloProvider, AuthProvider } from '@/app/providers'
import { Provider as RouterProvider } from '@/app/router'
import '@/app/styles/main.css'
import { GlobalKeyboardShortcuts } from '@/shared/components/GlobalKeyboardShortcuts'

export function App() {
  return (
    <StrictMode>
      <ApolloProvider>
        <AuthProvider>
          <GlobalKeyboardShortcuts />
          <RouterProvider />
        </AuthProvider>
      </ApolloProvider>
    </StrictMode>
  )
}
