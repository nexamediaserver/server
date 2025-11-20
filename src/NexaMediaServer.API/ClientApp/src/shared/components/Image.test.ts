import { describe, expect, it } from 'vitest'

import { coerceThumbHashBytes, selectTransitionKind } from './Image'

describe('coerceThumbHashBytes', () => {
  it('decodes base64 strings into bytes', () => {
    const bytes = coerceThumbHashBytes('AQID')

    expect(bytes).toBeInstanceOf(Uint8Array)
    expect(bytes).toStrictEqual(new Uint8Array([1, 2, 3]))
  })

  it('returns null for invalid input', () => {
    expect(coerceThumbHashBytes(undefined)).toBeNull()
    expect(coerceThumbHashBytes('@@bad@@')).toBeNull()
  })
})

describe('selectTransitionKind', () => {
  const previous = { height: 100, uri: 'a', url: '/a', width: 80 }

  it('rotates when URI changes', () => {
    const kind = selectTransitionKind(
      previous,
      { height: 100, uri: 'b', width: 80 },
      false,
    )

    expect(kind).toBe('rotate')
  })

  it('crossfades when only dimensions change', () => {
    const kind = selectTransitionKind(
      previous,
      { height: 120, uri: 'a', width: 90 },
      false,
    )

    expect(kind).toBe('crossfade')
  })

  it('crossfades when reduced motion is preferred', () => {
    const kind = selectTransitionKind(
      previous,
      { height: 100, uri: 'b', width: 80 },
      true,
    )

    expect(kind).toBe('crossfade')
  })
})
