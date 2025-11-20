import type { ReactNode } from 'react'

export function AuthProvider({ children }: { children: ReactNode }) {
  // No-op provider for future extensibility
  return <>{children}</>
}
