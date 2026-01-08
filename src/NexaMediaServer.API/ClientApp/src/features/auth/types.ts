export type AuthStatus =
  | 'authenticated'
  | 'idle'
  | 'loading'
  | 'unauthenticated'

export interface LoginResponse {
  accessToken?: string
  expiresIn?: number
  refreshToken?: string
  tokenType?: null | string
}

export interface PublicUser {
  email?: string
  id: string
  roles?: string[]
  username: string
}

export interface RefreshResponse {
  accessToken: string
  expiresIn: number
  refreshToken: string
  tokenType: null | string
}
