import type { ReactNode } from 'react'

import { forwardRef, useEffect, useImperativeHandle, useRef } from 'react'
import {
  TransformComponent,
  TransformWrapper,
  useControls,
} from 'react-zoom-pan-pinch'

import { cn } from '@/shared/lib/utils'

import type { PlaybackState } from '../store'

/**
 * Handle for controlling the ImageViewer component externally.
 * Provides methods for zoom and pan control.
 */
export interface ImageViewerHandle {
  /** Reset zoom and pan to initial state */
  resetTransform: () => void
  /** Zoom in to the next zoom level */
  zoomIn: () => void
  /** Zoom out to the previous zoom level */
  zoomOut: () => void
}

interface ImageViewerProps {
  /**
   * Additional class name for the container
   */
  className?: string

  /**
   * Current playback state containing image URL and metadata
   */
  playback: PlaybackState
}

/**
 * Resolves the image URL for photo playback.
 * Uses the playbackUrl from the server which points to the transcoded image.
 */
function resolveImageUrl(playbackUrl?: string): string | undefined {
  return playbackUrl ?? undefined
}

/**
 * Zoomable and pannable image viewer component for photo playback.
 * - Supports mouse drag, scroll-wheel zoom, and touch gestures
 * - Double-click to reset zoom
 * - Uses react-zoom-pan-pinch for smooth transformations
 * - Exposes zoom controls via ref for external control
 */
export const ImageViewer = forwardRef<ImageViewerHandle, ImageViewerProps>(
  function ImageViewer({ className, playback }, ref) {
    const controlsRef = useRef<null | ReturnType<typeof useControls>>(null)

    // Expose zoom controls to parent
    useImperativeHandle(
      ref,
      () => ({
        resetTransform: () => controlsRef.current?.resetTransform(),
        zoomIn: () => controlsRef.current?.zoomIn(),
        zoomOut: () => controlsRef.current?.zoomOut(),
      }),
      [],
    )

    const imageUrl = resolveImageUrl(playback.playbackUrl)
    const title = playback.originator?.title ?? 'Image'

    if (!imageUrl) {
      return (
        <div
          className={cn(
            'flex h-full w-full items-center justify-center',
            className,
          )}
        >
          <p className="text-muted-foreground">No image available</p>
        </div>
      )
    }

    return (
      <div className={cn('h-full w-full', className)}>
        <TransformWrapper
          centerOnInit
          doubleClick={{ mode: 'reset' }}
          initialScale={1}
          key={imageUrl}
          maxScale={8}
          minScale={0.5}
          wheel={{ step: 0.1 }}
        >
          <ImageViewerContent
            controlsRef={controlsRef}
            imageUrl={imageUrl}
            title={title}
          />
        </TransformWrapper>
      </div>
    )
  },
)

interface ImageViewerContentProps {
  controlsRef: React.RefObject<null | ReturnType<typeof useControls>>
  imageUrl: string
  title: string
}

/**
 * Inner content component that has access to TransformWrapper context.
 */
function ImageViewerContent({
  controlsRef,
  imageUrl,
  title,
}: ImageViewerContentProps): ReactNode {
  const controls = useControls()

  // Store controls reference for external access
  useEffect(() => {
    controlsRef.current = controls
  }, [controls, controlsRef])

  return (
    <TransformComponent
      contentStyle={{
        display: 'flex',
        height: '100%',
        justifyContent: 'center',
        width: '100%',
      }}
      wrapperStyle={{
        height: '100%',
        width: '100%',
      }}
    >
      <img
        alt={title}
        className="max-h-full max-w-full object-contain"
        draggable={false}
        src={imageUrl}
      />
    </TransformComponent>
  )
}
