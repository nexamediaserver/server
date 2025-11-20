import { atom } from 'jotai'
import { atomWithStorage } from 'jotai/utils'

import type { AuthStatus, PublicUser } from '@/features/auth/types'

// Example: sidebar collapsed state
export const sidebarCollapsedAtom = atom(false)

// Example: global notification message
export const notificationAtom = atom<null | string>(null)

// Authentication atoms
export const authStatusAtom = atom<AuthStatus>('idle')
export const authUserAtom = atomWithStorage<null | PublicUser>(
  'auth:user',
  null,
)
export const authAccessTokenAtom = atomWithStorage<null | string>(
  'auth:accessToken',
  null,
)
export const authRefreshTokenAtom = atomWithStorage<null | string>(
  'auth:refreshToken',
  null,
)
export const authAccessTokenExpiresAtAtom = atomWithStorage<null | number>(
  'auth:accessTokenExpiresAt',
  null,
)

// Global UI: ItemCard width (Tailwind spacing token between 32 and 52)
// Store the numeric Tailwind spacing key; default matches current UI (w-52)
export const itemCardWidthAtom = atomWithStorage<number>('ui:itemCardWidth', 40)
