import type shaka from 'shaka-player'

import type { PlaybackState } from '../../store'
import type { PlayerStatsCategory, PlayerStatsProvider } from './types'

import {
  checkHardwareDecodeSupport,
  detectHardwareAcceleration,
  formatBitrate,
  formatFramerate,
  formatPlayMethod,
  formatResolution,
  formatTranscodeReasons,
} from './utils'

export interface VideoPlayerStatsProviderOptions {
  getMediaElement: () => HTMLMediaElement | null
  getPlayer: () => null | shaka.Player
  playbackState: PlaybackState
}

/**
 * Parsed stream plan information from the server.
 */
interface StreamPlanInfo {
  Container?: string
  MediaPartId?: number
  Mode?: number | string
  TranscodeReasons?: number
  UseHardwareAcceleration?: boolean
}

/**
 * Stats provider for video playback using Shaka Player.
 * Collects comprehensive information about:
 * - Playback method and player info
 * - Stream quality (resolution, codecs, bitrates)
 * - Container and file info
 * - Network statistics
 */
export class VideoPlayerStatsProvider implements PlayerStatsProvider {
  private getMediaElement: () => HTMLMediaElement | null
  private getPlayer: () => null | shaka.Player
  private playbackState: PlaybackState

  constructor(options: VideoPlayerStatsProviderOptions) {
    this.getMediaElement = options.getMediaElement
    this.getPlayer = options.getPlayer
    this.playbackState = options.playbackState
  }

  cleanup(): void {
    // No cleanup needed for this provider
  }

  async getStats(): Promise<PlayerStatsCategory[]> {
    const categories: PlayerStatsCategory[] = []
    const player = this.getPlayer()
    const media = this.getMediaElement()

    if (!player || !media) {
      return categories
    }

    // Parse stream plan if available
    const streamPlan = this.parseStreamPlan()

    // Get Shaka stats
    const shakaStats = this.getShakaStats(player)

    // Playback Info Category (includes hardware acceleration info)
    const playbackInfo = await this.getPlaybackInfoCategory(
      player,
      media as HTMLVideoElement,
      streamPlan,
      shakaStats,
    )
    categories.push(playbackInfo)

    // Stream Info Category (current quality)
    const streamInfo = this.getStreamInfoCategory(player, shakaStats)
    if (streamInfo) {
      categories.push(streamInfo)
    }

    // Original Media Info Category
    const mediaInfo = this.getMediaInfoCategory(streamPlan)
    if (mediaInfo) {
      categories.push(mediaInfo)
    }

    // Network Stats Category
    const networkStats = this.getNetworkStatsCategory(shakaStats)
    if (networkStats) {
      categories.push(networkStats)
    }

    return categories
  }

  private async getHardwareAccelerationInfo(
    player: shaka.Player,
    video: HTMLVideoElement,
  ): Promise<{
    hardwareDecoding: string
    powerEfficient?: boolean
  }> {
    // First, try heuristic detection based on playback quality
    const heuristicInfo = detectHardwareAcceleration(video)

    // Get active track for Media Capabilities API check
    const tracks = player.getVariantTracks()
    const activeTrack = tracks.find((track) => track.active)

    if (
      activeTrack?.videoCodec &&
      activeTrack.width &&
      activeTrack.height &&
      activeTrack.frameRate
    ) {
      const capabilities = await checkHardwareDecodeSupport(
        activeTrack.videoCodec,
        activeTrack.width,
        activeTrack.height,
        activeTrack.frameRate,
        activeTrack.videoBandwidth ?? 5_000_000,
      )

      if (capabilities.supported) {
        // Media Capabilities API gives us definitive info about power efficiency
        // which is a strong indicator of hardware acceleration
        return {
          hardwareDecoding: capabilities.powerEfficient
            ? 'Yes'
            : heuristicInfo.hardwareDecoding,
          powerEfficient: capabilities.powerEfficient,
        }
      }
    }

    return { hardwareDecoding: heuristicInfo.hardwareDecoding }
  }

  private getMediaInfoCategory(
    streamPlan: null | StreamPlanInfo,
  ): null | PlayerStatsCategory {
    const stats: PlayerStatsCategory = {
      name: 'Original Media Info',
      stats: [],
    }

    // Container format
    if (streamPlan?.Container) {
      stats.stats.push({
        label: 'Container',
        value: streamPlan.Container.toUpperCase(),
      })
    }

    // Media part ID (for debugging)
    if (streamPlan?.MediaPartId) {
      stats.stats.push({
        label: 'Media Part ID',
        value: streamPlan.MediaPartId.toString(),
      })
    }

    // Server duration
    if (this.playbackState.serverDuration) {
      const durationSeconds = Math.floor(
        this.playbackState.serverDuration / 1000,
      )
      const hours = Math.floor(durationSeconds / 3600)
      const minutes = Math.floor((durationSeconds % 3600) / 60)
      const seconds = durationSeconds % 60

      let durationStr = ''
      if (hours > 0) {
        durationStr = `${String(hours)}h ${String(minutes)}m ${String(seconds)}s`
      } else if (minutes > 0) {
        durationStr = `${String(minutes)}m ${String(seconds)}s`
      } else {
        durationStr = `${String(seconds)}s`
      }

      stats.stats.push({
        label: 'Duration',
        value: durationStr,
      })
    }

    return stats.stats.length > 0 ? stats : null
  }

  private getNetworkStatsCategory(
    shakaStats: null | shaka.extern.Stats,
  ): null | PlayerStatsCategory {
    if (!shakaStats) return null

    const stats: PlayerStatsCategory = {
      name: 'Network',
      stats: [],
    }

    // Buffer health
    if (shakaStats.bufferingTime > 0) {
      stats.stats.push({
        label: 'Time Spent Buffering',
        value: `${shakaStats.bufferingTime.toFixed(1)}s`,
      })
    }

    // Stream bandwidth
    if (shakaStats.streamBandwidth) {
      stats.stats.push({
        label: 'Current Bandwidth',
        value: formatBitrate(shakaStats.streamBandwidth),
      })
    }

    // Latency (if available)
    if (shakaStats.manifestTimeSeconds > 0) {
      stats.stats.push({
        label: 'Manifest Time',
        value: `${shakaStats.manifestTimeSeconds.toFixed(2)}s`,
      })
    }

    return stats.stats.length > 0 ? stats : null
  }

  private async getPlaybackInfoCategory(
    player: shaka.Player,
    video: HTMLVideoElement,
    streamPlan: null | StreamPlanInfo,
    shakaStats: null | shaka.extern.Stats,
  ): Promise<PlayerStatsCategory> {
    const stats: PlayerStatsCategory = {
      name: 'Playback Info',
      stats: [],
    }

    // Player name
    stats.stats.push({
      label: 'Player',
      value: 'Shaka Player',
    })

    // Play method
    const playMethod = formatPlayMethod(streamPlan?.Mode)
    stats.stats.push({
      label: 'Play Method',
      value: playMethod,
    })

    // Server hardware transcoding (only show when transcoding)
    if (streamPlan?.Mode === 2 || streamPlan?.Mode === 'Transcode') {
      stats.stats.push({
        label: 'HW Transcoding',
        value: streamPlan.UseHardwareAcceleration ? 'Yes' : 'No',
      })

      // Show transcode reasons if available
      if (streamPlan.TranscodeReasons && streamPlan.TranscodeReasons > 0) {
        const reasons = formatTranscodeReasons(streamPlan.TranscodeReasons)
        if (reasons) {
          stats.stats.push({
            label: 'Transcode Reason',
            value: reasons,
          })
        }
      }
    }

    // Client hardware decoding detection
    const hwAccelInfo = await this.getHardwareAccelerationInfo(player, video)
    if (hwAccelInfo.hardwareDecoding !== 'Unknown') {
      stats.stats.push({
        label: 'HW Decoding',
        value: hwAccelInfo.hardwareDecoding,
      })
    }
    if (hwAccelInfo.powerEfficient !== undefined) {
      stats.stats.push({
        label: 'Power Efficient',
        value: hwAccelInfo.powerEfficient ? 'Yes' : 'No',
      })
    }

    // Playback session ID
    if (this.playbackState.playbackSessionId) {
      stats.stats.push({
        label: 'Session ID',
        value: this.playbackState.playbackSessionId.slice(0, 8),
      })
    }

    // Stream variant info (from Shaka)
    if (shakaStats) {
      const variantBandwidth = shakaStats.estimatedBandwidth
      if (variantBandwidth > 0) {
        stats.stats.push({
          label: 'Estimated Bandwidth',
          value: formatBitrate(variantBandwidth),
        })
      }

      // Add dropped frames info
      if (shakaStats.droppedFrames > 0) {
        stats.stats.push({
          label: 'Dropped Frames',
          value: shakaStats.droppedFrames.toString(),
        })
      }
    }

    return stats
  }

  private getShakaStats(player: shaka.Player): null | shaka.extern.Stats {
    try {
      return player.getStats()
    } catch {
      return null
    }
  }

  private getStreamInfoCategory(
    player: shaka.Player,
    shakaStats: null | shaka.extern.Stats,
  ): null | PlayerStatsCategory {
    if (!shakaStats) return null

    const stats: PlayerStatsCategory = {
      name: 'Stream Info',
      stats: [],
      subText: 'Current Quality',
    }

    // Get active variant track
    const tracks = player.getVariantTracks()
    const activeTrack = tracks.find((track) => track.active)

    if (activeTrack) {
      // Video codec and resolution
      if (activeTrack.videoCodec) {
        const codecParts = [activeTrack.videoCodec.toUpperCase()]
        if (activeTrack.width && activeTrack.height) {
          stats.stats.push({
            label: 'Resolution',
            value: formatResolution(activeTrack.width, activeTrack.height),
          })
        }
        stats.stats.push({
          label: 'Video Codec',
          value: codecParts.join(' '),
        })
      }

      // Video bitrate
      if (activeTrack.videoBandwidth) {
        stats.stats.push({
          label: 'Video Bitrate',
          value: formatBitrate(activeTrack.videoBandwidth),
        })
      }

      // Framerate
      if (activeTrack.frameRate) {
        stats.stats.push({
          label: 'Framerate',
          value: formatFramerate(activeTrack.frameRate),
        })
      }

      // Audio codec
      if (activeTrack.audioCodec) {
        stats.stats.push({
          label: 'Audio Codec',
          value: activeTrack.audioCodec.toUpperCase(),
        })
      }

      // Audio bitrate
      if (activeTrack.audioBandwidth) {
        stats.stats.push({
          label: 'Audio Bitrate',
          value: formatBitrate(activeTrack.audioBandwidth),
        })
      }

      // Audio channels
      if (activeTrack.channelsCount) {
        stats.stats.push({
          label: 'Audio Channels',
          value: activeTrack.channelsCount.toString(),
        })
      }

      // Total bitrate
      const totalBitrate =
        (activeTrack.videoBandwidth ?? 0) + (activeTrack.audioBandwidth ?? 0)
      if (totalBitrate > 0) {
        stats.stats.push({
          label: 'Total Bitrate',
          value: formatBitrate(totalBitrate),
        })
      }
    }

    return stats.stats.length > 0 ? stats : null
  }

  private parseStreamPlan(): null | StreamPlanInfo {
    if (!this.playbackState.streamPlanJson) return null

    try {
      return JSON.parse(this.playbackState.streamPlanJson) as StreamPlanInfo
    } catch {
      return null
    }
  }
}
