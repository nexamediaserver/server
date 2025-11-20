export function getStoredAccessToken(): null | string {
  try {
    const raw = localStorage.getItem('auth:accessToken')
    return raw ? (JSON.parse(raw) as null | string) : null
  } catch {
    return null
  }
}
