import type { PlaybackState } from '../../store'
import type { PlayerStatsCategory, PlayerStatsProvider } from './types'

import { formatResolution } from './utils'

export interface ImagePlayerStatsProviderOptions {
  playbackState: PlaybackState
}

/**
 * Stats provider for image viewing.
 * Displays information relevant to image display:
 * - Image metadata (title, dimensions, file size)
 * - Display information
 */
export class ImagePlayerStatsProvider implements PlayerStatsProvider {
  private imageElement: HTMLImageElement | null = null
  private playbackState: PlaybackState

  constructor(options: ImagePlayerStatsProviderOptions) {
    this.playbackState = options.playbackState
    this.loadImageElement()
  }

  cleanup(): void {
    this.imageElement = null
  }

  async getStats(): Promise<PlayerStatsCategory[]> {
    const categories: PlayerStatsCategory[] = []

    // Image Info Category
    const imageInfo = this.getImageInfoCategory()
    if (imageInfo) {
      categories.push(imageInfo)
    }

    // Metadata Category
    const metadata = this.getMetadataCategory()
    if (metadata) {
      categories.push(metadata)
    }

    return categories
  }

  private getImageInfoCategory(): null | PlayerStatsCategory {
    const stats: PlayerStatsCategory = {
      name: 'Image Info',
      stats: [],
    }

    // Player type
    stats.stats.push({
      label: 'Viewer',
      value: 'Image Viewer',
    })

    // Try to get dimensions from the loaded image
    if (!this.imageElement) {
      this.loadImageElement()
    }

    if (this.imageElement) {
      const naturalWidth = this.imageElement.naturalWidth
      const naturalHeight = this.imageElement.naturalHeight

      if (naturalWidth > 0 && naturalHeight > 0) {
        stats.stats.push({
          label: 'Dimensions',
          value: formatResolution(naturalWidth, naturalHeight),
        })

        // Calculate aspect ratio
        const gcd = (a: number, b: number): number => {
          return b === 0 ? a : gcd(b, a % b)
        }
        const divisor = gcd(naturalWidth, naturalHeight)
        const ratioW = naturalWidth / divisor
        const ratioH = naturalHeight / divisor

        // Only show aspect ratio if it's a common one or interesting
        if (divisor > 1) {
          stats.stats.push({
            label: 'Aspect Ratio',
            value: `${String(ratioW)}:${String(ratioH)}`,
          })
        }
      }

      // Display size (if different from natural)
      const displayWidth = this.imageElement.width
      const displayHeight = this.imageElement.height

      if (
        displayWidth > 0 &&
        displayHeight > 0 &&
        (displayWidth !== naturalWidth || displayHeight !== naturalHeight)
      ) {
        stats.stats.push({
          label: 'Display Size',
          value: formatResolution(displayWidth, displayHeight),
        })
      }
    }

    // File format (from URL or stream plan)
    const streamPlan = this.parseStreamPlan()
    if (streamPlan?.Container) {
      stats.stats.push({
        label: 'Format',
        value: streamPlan.Container.toUpperCase(),
      })
    } else if (this.playbackState.playbackUrl) {
      // Try to extract format from URL
      const regex = /\.(\w+)(?:\?|$)/
      const match = regex.exec(this.playbackState.playbackUrl)
      if (match?.[1]) {
        stats.stats.push({
          label: 'Format',
          value: match[1].toUpperCase(),
        })
      }
    }

    return stats.stats.length > 0 ? stats : null
  }

  private getMetadataCategory(): null | PlayerStatsCategory {
    const originator = this.playbackState.originator
    if (!originator) return null

    const stats: PlayerStatsCategory = {
      name: 'Metadata',
      stats: [],
    }

    // Image title
    if (originator.title) {
      stats.stats.push({
        label: 'Title',
        value: originator.title,
      })
    }

    // Parent/Album title (for photos in albums)
    if (originator.parentTitle) {
      stats.stats.push({
        label: 'Album',
        value: originator.parentTitle,
      })
    }

    // Media part ID (for debugging)
    const streamPlan = this.parseStreamPlan()
    if (streamPlan?.MediaPartId) {
      stats.stats.push({
        label: 'Media Part ID',
        value: streamPlan.MediaPartId.toString(),
      })
    }

    // Session ID
    if (this.playbackState.playbackSessionId) {
      stats.stats.push({
        label: 'Session ID',
        value: this.playbackState.playbackSessionId.slice(0, 8),
      })
    }

    return stats.stats.length > 0 ? stats : null
  }

  private loadImageElement(): void {
    // Try to find the image element in the DOM
    const imageUrl = this.playbackState.playbackUrl
    if (!imageUrl) return

    // Find the image by src
    const images = Array.from(document.querySelectorAll('img'))
    this.imageElement = images.find((img) => img.src.includes(imageUrl)) ?? null
  }

  private parseStreamPlan(): null | {
    Container?: string
    MediaPartId?: number
    Mode?: number | string
  } {
    if (!this.playbackState.streamPlanJson) return null

    try {
      return JSON.parse(this.playbackState.streamPlanJson) as {
        Container?: string
        MediaPartId?: number
        Mode?: number | string
      }
    } catch {
      return null
    }
  }
}
