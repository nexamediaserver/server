import { useIntersectionObserver } from '@uidotdev/usehooks'
import {
  type CSSProperties,
  forwardRef,
  type ImgHTMLAttributes,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react'
import { thumbHashToDataURL, thumbHashToRGBA } from 'thumbhash'

import { getImageTranscodeUrl } from '@/domain/utils'
import { cn } from '@/shared/lib/utils'

export interface ImageProps extends Omit<
  ImgHTMLAttributes<HTMLImageElement>,
  'height' | 'src' | 'srcSet' | 'width'
> {
  className?: string
  /** Numeric display height (px) */
  height: number
  /** Source image URI (e.g., metadata://...) */
  imageUri?: string
  /** Optional img className */
  imgClassName?: string
  /** Defer loading until visible (default: true) */
  lazy?: boolean
  /** Object-fit for the rendered img (default: cover) */
  objectFit?: CSSProperties['objectFit']
  /** Override device pixel ratio used for requests */
  pixelRatio?: number
  /** Request quality (0-100), defaults to 90 */
  quality?: number
  /** Optional ThumbHash placeholder (base64 or bytes) */
  thumbHash?: string | Uint8Array
  /** Numeric display width (px) */
  width: number
}

interface DecodedThumbHash {
  /** Data URL of the cropped ThumbHash image */
  dataUrl: string
  /** Native height of the ThumbHash before cropping */
  nativeHeight: number
  /** Native width of the ThumbHash before cropping */
  nativeWidth: number
}

interface LoadedImage {
  height: number
  uri: string
  url: string
  width: number
}

interface PendingImage {
  height: number
  uri?: string
  width: number
}

type TransitionKind = 'crossfade' | 'rotate'

const CROSSFADE_DURATION_MS = 260
const ROTATE_DURATION_MS = 440

// eslint-disable-next-line react-refresh/only-export-components
export function coerceThumbHashBytes(
  hash: string | Uint8Array | undefined,
): null | Uint8Array {
  if (!hash) {
    return null
  }

  if (hash instanceof Uint8Array) {
    return hash
  }

  try {
    const normalized = hash.replaceAll('-', '+').replaceAll('_', '/')
    const binary = globalThis.atob(normalized)
    const out = new Uint8Array(binary.length)
    for (let i = 0; i < binary.length; i += 1) {
      out[i] = binary.codePointAt(i) ?? 0
    }
    return out
  } catch {
    return null
  }
}

// eslint-disable-next-line react-refresh/only-export-components
export function selectTransitionKind(
  previous: LoadedImage | null,
  next: null | PendingImage,
  prefersReducedMotion: boolean,
): null | TransitionKind {
  if (prefersReducedMotion) {
    return previous ? 'crossfade' : null
  }

  if (!previous || !next?.uri) {
    return previous ? 'crossfade' : null
  }

  if (previous.uri === next.uri) {
    if (previous.width !== next.width || previous.height !== next.height) {
      return 'crossfade'
    }
    return null
  }

  return 'rotate'
}

/**
 * Decodes a ThumbHash and crops it to match a target aspect ratio using
 * "cover" fit from the center (matching the server's image cropping behavior).
 */
function decodeThumbHash(
  hash: Uint8Array,
  targetWidth: number,
  targetHeight: number,
): DecodedThumbHash | null {
  try {
    const { h: nativeHeight, rgba, w: nativeWidth } = thumbHashToRGBA(hash)

    if (typeof document === 'undefined') {
      return {
        dataUrl: thumbHashToDataURL(hash),
        nativeHeight,
        nativeWidth,
      }
    }

    // Create a temporary canvas with the full ThumbHash image
    const sourceCanvas = document.createElement('canvas')
    sourceCanvas.width = nativeWidth
    sourceCanvas.height = nativeHeight

    const sourceCtx = sourceCanvas.getContext('2d')
    if (!sourceCtx) {
      return {
        dataUrl: thumbHashToDataURL(hash),
        nativeHeight,
        nativeWidth,
      }
    }

    const imageData = sourceCtx.createImageData(nativeWidth, nativeHeight)
    imageData.data.set(rgba)
    sourceCtx.putImageData(imageData, 0, 0)

    // Calculate the crop region to simulate "cover" fit from center
    const targetAspect = targetWidth / targetHeight
    const sourceAspect = nativeWidth / nativeHeight

    let cropX = 0
    let cropY = 0
    let cropWidth = nativeWidth
    let cropHeight = nativeHeight

    if (sourceAspect > targetAspect) {
      // Source is wider than target - crop horizontally
      cropWidth = nativeHeight * targetAspect
      cropX = (nativeWidth - cropWidth) / 2
    } else if (sourceAspect < targetAspect) {
      // Source is taller than target - crop vertically
      cropHeight = nativeWidth / targetAspect
      cropY = (nativeHeight - cropHeight) / 2
    }

    // Create output canvas with the target dimensions
    // Use small dimensions since ThumbHash is already low-res
    const outputWidth = Math.min(32, targetWidth)
    const outputHeight = Math.round(outputWidth / targetAspect)

    const outputCanvas = document.createElement('canvas')
    outputCanvas.width = outputWidth
    outputCanvas.height = outputHeight

    const outputCtx = outputCanvas.getContext('2d')
    if (!outputCtx) {
      return {
        dataUrl: sourceCanvas.toDataURL('image/png'),
        nativeHeight,
        nativeWidth,
      }
    }

    // Draw the cropped region scaled to output size
    outputCtx.drawImage(
      sourceCanvas,
      cropX,
      cropY,
      cropWidth,
      cropHeight,
      0,
      0,
      outputWidth,
      outputHeight,
    )

    return {
      dataUrl: outputCanvas.toDataURL('image/png'),
      nativeHeight,
      nativeWidth,
    }
  } catch {
    return null
  }
}

function usePrefersReducedMotion() {
  const [prefers, setPrefers] = useState(() => {
    if (typeof globalThis.matchMedia !== 'function') {
      return false
    }
    const mediaQuery = globalThis.matchMedia('(prefers-reduced-motion: reduce)')
    return mediaQuery.matches
  })

  useEffect(() => {
    if (typeof globalThis.matchMedia !== 'function') {
      return
    }

    const mediaQuery = globalThis.matchMedia('(prefers-reduced-motion: reduce)')

    const listener = (event: MediaQueryListEvent) => {
      setPrefers(event.matches)
    }

    mediaQuery.addEventListener('change', listener)

    return () => {
      mediaQuery.removeEventListener('change', listener)
    }
  }, [])

  return prefers
}

export const Image = forwardRef<HTMLDivElement, ImageProps>(function Image(
  {
    alt,
    className,
    decoding,
    height,
    imageUri,
    imgClassName,
    lazy = true,
    loading,
    objectFit = 'cover',
    pixelRatio,
    quality = 90,
    sizes,
    thumbHash,
    width,
    ...imgProps
  },
  forwardedRef,
) {
  const prefersReducedMotion = usePrefersReducedMotion()
  const dpr = pixelRatio ?? globalThis.devicePixelRatio
  const displayWidth = Math.max(1, Math.round(width))
  const displayHeight = Math.max(1, Math.round(height))
  const requestWidth = Math.max(1, Math.round(displayWidth * dpr))
  const requestHeight = Math.max(1, Math.round(displayHeight * dpr))

  const [observeRef, entry] = useIntersectionObserver<HTMLDivElement>({
    root: null,
    rootMargin: '200px',
    threshold: 0.01,
  })

  const containerRef = useRef<HTMLDivElement | null>(null)
  const combinedRef = useCallback(
    (node: HTMLDivElement | null) => {
      containerRef.current = node
      observeRef(node)
      if (typeof forwardedRef === 'function') {
        forwardedRef(node)
      } else if (forwardedRef) {
        forwardedRef.current = node
      }
    },
    [observeRef, forwardedRef],
  )

  const isVisible = !lazy || entry?.isIntersecting === true

  const [current, setCurrent] = useState<LoadedImage | null>(null)
  const currentRef = useRef<LoadedImage | null>(null)
  const [previous, setPrevious] = useState<LoadedImage | null>(null)
  const [previousExiting, setPreviousExiting] = useState(false)
  const [transitionKind, setTransitionKind] = useState<null | TransitionKind>(
    null,
  )
  const [currentVisible, setCurrentVisible] = useState(false)
  const hideCurrent = useCallback(() => {
    requestAnimationFrame(() => {
      setCurrentVisible(false)
    })
  }, [])
  const triggerPreviousExit = useCallback(() => {
    requestAnimationFrame(() => {
      setPreviousExiting(true)
    })
  }, [])
  const revealCurrent = useCallback(() => {
    requestAnimationFrame(() => {
      setCurrentVisible(true)
    })
  }, [])

  useEffect(() => {
    currentRef.current = current
  }, [current])

  const placeholderUrl = useMemo(() => {
    const bytes = coerceThumbHashBytes(thumbHash)
    if (!bytes) {
      return null
    }
    const decoded = decodeThumbHash(bytes, displayWidth, displayHeight)
    return decoded?.dataUrl ?? null
  }, [thumbHash, displayWidth, displayHeight])

  useEffect(() => {
    if (!isVisible) {
      return
    }

    const nextPending: null | PendingImage = imageUri
      ? { height: requestHeight, uri: imageUri, width: requestWidth }
      : null

    hideCurrent()

    const transition = selectTransitionKind(
      currentRef.current,
      nextPending,
      prefersReducedMotion,
    )

    let cancelled = false

    if (!nextPending?.uri) {
      if (transition) {
        setPrevious(currentRef.current)
        setTransitionKind(transition)
        setPreviousExiting(false)
        requestAnimationFrame(() => {
          setPreviousExiting(true)
        })
      } else {
        setPrevious(null)
        setTransitionKind(null)
      }
      setCurrent(null)
      currentRef.current = null

      return () => {
        cancelled = true
      }
    }

    const loadImage = async () => {
      try {
        const {
          height: nextHeight,
          uri: nextUri,
          width: nextWidth,
        } = nextPending

        if (!nextUri) {
          return
        }

        const url = await getImageTranscodeUrl(nextUri, {
          height: nextHeight,
          quality,
          width: nextWidth,
        })

        if (cancelled) {
          return
        }

        const loader = new globalThis.Image()
        loader.decoding = decoding ?? 'async'
        loader.loading = 'eager'

        loader.onload = () => {
          if (cancelled) {
            return
          }

          const previousImage = currentRef.current
          const nextImage: LoadedImage = {
            height: nextHeight,
            uri: nextUri,
            url,
            width: nextWidth,
          }

          currentRef.current = nextImage
          setCurrent(nextImage)

          if (transition) {
            setPrevious(previousImage)
            setTransitionKind(transition)
            setPreviousExiting(false)
            triggerPreviousExit()
          } else {
            setPrevious(null)
            setTransitionKind(null)
          }

          revealCurrent()
        }

        loader.onerror = () => {
          if (cancelled) {
            return
          }
          if (!transition) {
            setPrevious(null)
          }
        }

        loader.src = url
      } catch {
        if (!cancelled && !transition) {
          setPrevious(null)
        }
      }
    }

    void loadImage()

    return () => {
      cancelled = true
    }
  }, [
    decoding,
    imageUri,
    isVisible,
    hideCurrent,
    prefersReducedMotion,
    quality,
    requestHeight,
    requestWidth,
    triggerPreviousExit,
    revealCurrent,
  ])

  useEffect(() => {
    if (!previous || !previousExiting) {
      return
    }

    let duration = CROSSFADE_DURATION_MS
    if (!prefersReducedMotion && transitionKind === 'rotate') {
      duration = ROTATE_DURATION_MS
    }

    const timer = globalThis.setTimeout(() => {
      setPrevious(null)
      setPreviousExiting(false)
    }, duration + 32)

    return () => {
      globalThis.clearTimeout(timer)
    }
  }, [previous, previousExiting, prefersReducedMotion, transitionKind])

  const previousStyles: CSSProperties = useMemo(() => {
    const base: CSSProperties = {
      opacity: previousExiting ? 0 : 1,
      transformOrigin: '50% 50%',
      willChange: 'opacity, transform',
    }

    if (prefersReducedMotion || transitionKind !== 'rotate') {
      base.transition = `opacity ${String(CROSSFADE_DURATION_MS)}ms ease`
      return base
    }

    base.transition = `opacity ${String(CROSSFADE_DURATION_MS)}ms ease, transform ${String(ROTATE_DURATION_MS)}ms ease`
    base.transform = previousExiting ? 'rotateX(-78deg)' : 'rotateX(0deg)'
    return base
  }, [prefersReducedMotion, previousExiting, transitionKind])

  const currentStyles: CSSProperties = useMemo(() => {
    let opacity = 0
    if (previous) {
      opacity = previousExiting ? 1 : 0
    } else if (current && currentVisible) {
      opacity = 1
    }
    return {
      objectFit,
      opacity,
      transition: `opacity ${String(CROSSFADE_DURATION_MS)}ms ease`,
    }
  }, [current, currentVisible, objectFit, previous, previousExiting])

  const wrapperStyles: CSSProperties = useMemo(() => {
    const styles: CSSProperties = {
      height: displayHeight,
      width: displayWidth,
    }

    return styles
  }, [displayHeight, displayWidth])

  const placeholderStyles: CSSProperties = useMemo(() => {
    const styles: CSSProperties = {}

    if (placeholderUrl) {
      styles.backgroundImage = `url(${placeholderUrl})`
      styles.backgroundPosition = '50% 50%'
      styles.backgroundSize = 'cover'
    }

    return styles
  }, [placeholderUrl])

  const placeholderOpacity = previous || (current && currentVisible) ? 0 : 1

  return (
    <div
      className={cn('relative overflow-hidden', className)}
      ref={combinedRef}
      style={wrapperStyles}
    >
      <div
        aria-hidden
        className={cn(
          'absolute inset-0 bg-stone-900 transition-opacity duration-300',
        )}
        style={{ ...placeholderStyles, opacity: placeholderOpacity }}
      />

      {previous ? (
        <img
          alt={alt}
          aria-hidden
          className={cn(
            'absolute inset-0 h-full w-full object-cover',
            imgClassName,
          )}
          decoding={decoding ?? 'async'}
          draggable={false}
          height={displayHeight}
          loading={loading ?? (lazy ? 'lazy' : 'eager')}
          sizes={sizes}
          src={previous.url}
          style={previousStyles}
          width={displayWidth}
          {...imgProps}
        />
      ) : null}

      {current ? (
        <img
          alt={alt}
          className={cn(
            'absolute inset-0 h-full w-full object-cover',
            imgClassName,
          )}
          decoding={decoding ?? 'async'}
          draggable={false}
          height={displayHeight}
          loading={loading ?? (lazy ? 'lazy' : 'eager')}
          sizes={sizes}
          src={current.url}
          style={currentStyles}
          width={displayWidth}
          {...imgProps}
        />
      ) : null}

      {!current && !previous ? (
        <div aria-hidden className="absolute inset-0" />
      ) : null}
    </div>
  )
})
