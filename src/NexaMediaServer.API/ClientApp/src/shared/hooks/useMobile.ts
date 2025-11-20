import { useMediaQuery } from '@uidotdev/usehooks'

const MOBILE_BREAKPOINT = 768

export function useIsMobile() {
  return useMediaQuery(`(max-width: ${String(MOBILE_BREAKPOINT - 1)}px)`)
}
