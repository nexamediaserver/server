const computedStyles = getComputedStyle(document.documentElement)

// Derive the pixel value of the breakpoints from CSS variables (defined in rem)
function parseBreakpoint(value: string) {
  if (value.endsWith('rem')) {
    const num = Number(value.replace('rem', ''))
    return num * 16
  }
  if (value.endsWith('px')) {
    return Number(value.replace('px', ''))
  }

  return 0
}

/* eslint-disable perfectionist/sort-objects */
export const breakpoints = {
  sm: parseBreakpoint(computedStyles.getPropertyValue('--breakpoint-sm')),
  md: parseBreakpoint(computedStyles.getPropertyValue('--breakpoint-md')),
  lg: parseBreakpoint(computedStyles.getPropertyValue('--breakpoint-lg')),
  xl: parseBreakpoint(computedStyles.getPropertyValue('--breakpoint-xl')),
  '2xl': parseBreakpoint(computedStyles.getPropertyValue('--breakpoint-2xl')),
}
/* eslint-enable perfectionist/sort-objects */
