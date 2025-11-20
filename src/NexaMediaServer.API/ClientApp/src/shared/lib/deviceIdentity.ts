import * as UAParser from 'ua-parser-js'

const DEVICE_ID_STORAGE_KEY = 'nexa:deviceId'

export function buildDeviceMetadata(clientVersion?: string): {
  identifier: string
  name: string
  platform: string
  version: null | string
} {
  return {
    identifier: getOrCreateDeviceIdentifier(),
    name: getDeviceName(),
    platform: getDevicePlatform(),
    version: clientVersion ?? null,
  }
}

export function clearStoredDeviceIdentifier(): void {
  globalThis.localStorage.removeItem(DEVICE_ID_STORAGE_KEY)
}

export function getDeviceName(): string {
  const uaString = globalThis.navigator.userAgent
  const parser = new UAParser.UAParser(uaString)
  const browser = parser.getBrowser()
  const device = parser.getDevice()

  let name = (browser.name ?? 'Web Browser').slice(0, 256)

  if (device.vendor) {
    name = `${device.vendor} ${device.model ?? name}`.trim()
  } else if (device.model) {
    name = `${name} ${device.model}`.trim()
  }

  return name || 'Web Browser'
}

export function getDevicePlatform(): string {
  const uaString = globalThis.navigator.userAgent
  const parser = new UAParser.UAParser(uaString)
  const os = parser.getOS()

  return (os.name ?? 'Unknown OS').slice(0, 128)
}

export function getOrCreateDeviceIdentifier(): string {
  const existing = globalThis.localStorage.getItem(DEVICE_ID_STORAGE_KEY)
  if (existing) return existing

  const identifier = generateUuid()
  globalThis.globalThis.localStorage.setItem(DEVICE_ID_STORAGE_KEY, identifier)
  return identifier
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

export { DEVICE_ID_STORAGE_KEY }
