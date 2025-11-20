import * as UAParser from 'ua-parser-js'

import type { LoginResponse, PublicUser, RefreshResponse } from '../types'
export interface MeResponse {
  email: string
  isEmailConfirmed: boolean
}

interface DeviceRegistrationPayload {
  identifier: string
  name: string
  platform: string
  version?: null | string
}

const API_BASE = '/api/v1'
const JSON_HEADERS = {
  Accept: 'application/json',
  'Content-Type': 'application/json',
}
const DEVICE_ID_STORAGE_KEY = 'nexa:deviceId'

export class UnauthorizedError extends Error {
  readonly wwwAuthenticate: string
  constructor(message: string, wwwAuthenticate: string) {
    super(message)
    this.name = 'UnauthorizedError'
    this.wwwAuthenticate = wwwAuthenticate
  }
}

export function clearStoredDeviceIdentifier(): void {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.removeItem(DEVICE_ID_STORAGE_KEY)
}

export function decodeJwt(token: string): {
  [k: string]: unknown
  exp?: number
} {
  try {
    const [, payload] = token.split('.')
    if (!payload) return {}
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(json) as { [k: string]: unknown; exp?: number }
  } catch {
    return {}
  }
}

export async function login(
  username: string,
  password: string,
  rememberMe = true,
  clientVersion?: null | string,
): Promise<LoginResponse> {
  const device = getDeviceMetadata(clientVersion ?? undefined)
  const url = `${API_BASE}/login`
  return request<LoginResponse>(url, {
    body: JSON.stringify({
      device,
      email: username,
      password,
      rememberMe,
    }),
    method: 'POST',
  })
}

export async function logout(): Promise<void> {
  try {
    await request<unknown>(`${API_BASE}/logout`, { method: 'POST' })
  } finally {
    clearStoredDeviceIdentifier()
  }
}

export async function me(): Promise<MeResponse> {
  // Using manage/info as a surrogate for user profile for cookie sessions
  return request<MeResponse>(`${API_BASE}/manage/info`, { method: 'GET' })
}

// Best-effort mapping of common JWT claim names to our PublicUser shape
// Supports typical ASP.NET Identity/JWT claim keys.
export function parseUserFromToken(token: string): null | PublicUser {
  const claims = decodeJwt(token) as Record<string, unknown>
  const get = (k: string) => claims[k] as string | undefined

  // Common claim keys we might see
  const id =
    get('sub') ??
    get('nameid') ??
    get('http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier')

  const username =
    get('unique_name') ??
    get('name') ??
    get('http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name')

  const email =
    get('email') ??
    get('http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress')

  if (!id && !username && !email) return null
  return {
    email,
    id: id ?? username ?? email ?? 'unknown',
    username: username ?? email ?? id ?? 'unknown',
  }
}

export async function refresh(
  refreshToken?: null | string,
): Promise<RefreshResponse> {
  const payload: Record<string, unknown> = {}
  if (refreshToken) {
    payload.refreshToken = refreshToken
  }

  return request<RefreshResponse>(`${API_BASE}/refresh`, {
    body: JSON.stringify(payload),
    method: 'POST',
  })
}

function generateUuid(): string {
  if (
    typeof crypto !== 'undefined' &&
    typeof crypto.randomUUID === 'function'
  ) {
    return crypto.randomUUID()
  }

  return Math.random().toString(36).slice(2) + Date.now().toString(36)
}

function getDeviceMetadata(clientVersion?: string): DeviceRegistrationPayload {
  const identifier = getOrCreateDeviceIdentifier()
  const uaString =
    typeof navigator === 'undefined' ? undefined : navigator.userAgent
  const parser = new UAParser.UAParser(uaString)
  const browser = parser.getBrowser()
  const os = parser.getOS()
  const name = (browser.name ?? 'Unknown Browser').slice(0, 256)
  const platform = (os.name ?? 'Unknown OS').slice(0, 128)

  return {
    identifier,
    name,
    platform,
    version: clientVersion ?? null,
  }
}

function getOrCreateDeviceIdentifier(): string {
  if (typeof window === 'undefined') {
    return generateUuid()
  }

  const existing = window.localStorage.getItem(DEVICE_ID_STORAGE_KEY)
  if (existing) return existing

  const newId = generateUuid()
  window.localStorage.setItem(DEVICE_ID_STORAGE_KEY, newId)
  return newId
}

async function request<T>(
  input: RequestInfo | URL,
  init: RequestInit = {},
): Promise<T> {
  const headerInit = (init.headers ?? {}) as Record<string, string>
  const res = await fetch(input, {
    credentials: 'include',
    ...init,
    headers: {
      ...JSON_HEADERS,
      ...headerInit,
    },
  })
  if (!res.ok) {
    // Prefer WWW-Authenticate details if present (RFC 6750)
    if (res.status === 401) {
      const wa =
        res.headers.get('WWW-Authenticate') ??
        res.headers.get('Www-Authenticate') ??
        ''
      if (wa) {
        // Surface the server-provided error and description if present
        // Example: Bearer error="invalid_token", error_description="user 123 not found"
        throw new UnauthorizedError('Unauthorized', wa)
      }
    }

    let message = res.statusText
    try {
      const data = (await res.json()) as { error?: string; message?: string }
      message = data.error ?? data.message ?? message
    } catch {
      /* ignore parse error */
    }
    throw new Error(message)
  }
  if (res.status === 204) return {} as T
  // Some endpoints may return 200 with an empty body when using cookies-only auth
  const text = await res.text()
  if (!text) return {} as T
  try {
    return JSON.parse(text) as T
  } catch {
    // Fallback: if content-type is json but parsing failed, throw; else return as-is
    throw new Error('Unexpected non-JSON response')
  }
}
