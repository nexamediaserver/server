import { useCallback, useEffect, useRef, useState } from 'react'

export interface ContainerSize {
  height: number
  width: number
}

/**
 * Hook to measure and track the size of a container element.
 * Automatically updates on window resize and returns a ref to attach to the container.
 *
 * @returns Object containing:
 *   - `containerRef`: Ref to attach to the target container element
 *   - `containerSize`: Current dimensions or null if not yet measured
 */
export function useContainerSize<T extends HTMLElement = HTMLDivElement>(): {
  containerRef: React.RefObject<null | T>
  containerSize: ContainerSize | null
} {
  const containerRef = useRef<null | T>(null)
  const [containerSize, setContainerSize] = useState<ContainerSize | null>(null)

  const updateSize = useCallback(() => {
    if (containerRef.current) {
      const { height, width } = containerRef.current.getBoundingClientRect()

      // Only update if we have valid dimensions
      if (height > 0 && width > 0) {
        setContainerSize({
          height: Math.round(height),
          width: Math.round(width),
        })
      }
    }
  }, [])

  useEffect(() => {
    // Use requestAnimationFrame to ensure DOM is painted
    requestAnimationFrame(() => {
      updateSize()
    })

    globalThis.addEventListener('resize', updateSize)

    return () => {
      globalThis.removeEventListener('resize', updateSize)
    }
  }, [updateSize])

  return { containerRef, containerSize }
}
