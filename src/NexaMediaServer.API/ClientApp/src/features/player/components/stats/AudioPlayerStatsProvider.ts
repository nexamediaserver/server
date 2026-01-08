import type shaka from 'shaka-player'

import type { PlaybackState } from '../../store'
import type { PlayerStatsCategory, PlayerStatsProvider } from './types'

import { formatBitrate, formatPlayMethod } from './utils'

export interface AudioPlayerStatsProviderOptions {
  getMediaElement: () => HTMLMediaElement | null
  getPlayer: () => null | shaka.Player
  playbackState: PlaybackState
}

/**
 * Stats provider for audio playback using Shaka Player.
 * Displays information relevant to audio streaming:
 * - Playback method and player info
 * - Audio codec and quality
 * - Network statistics
 * - Track metadata
 */
export class AudioPlayerStatsProvider implements PlayerStatsProvider {
  private getMediaElement: () => HTMLMediaElement | null
  private getPlayer: () => null | shaka.Player
  private playbackState: PlaybackState

  constructor(options: AudioPlayerStatsProviderOptions) {
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

    // Playback Info Category
    categories.push(this.getPlaybackInfoCategory(streamPlan, shakaStats))

    // Audio Stream Info Category
    const audioInfo = this.getAudioInfoCategory(player, shakaStats)
    if (audioInfo) {
      categories.push(audioInfo)
    }

    // Track Metadata Category
    const trackInfo = this.getTrackInfoCategory()
    if (trackInfo) {
      categories.push(trackInfo)
    }

    // Network Stats Category
    const networkStats = this.getNetworkStatsCategory(shakaStats)
    if (networkStats) {
      categories.push(networkStats)
    }

    return categories
  }

  private getAudioInfoCategory(
    player: shaka.Player,
    shakaStats: null | shaka.extern.Stats,
  ): null | PlayerStatsCategory {
    if (!shakaStats) return null

    const stats: PlayerStatsCategory = {
      name: 'Audio Stream Info',
      stats: [],
    }

    // Get active variant track
    const tracks = player.getVariantTracks()
    const activeTrack = tracks.find((track) => track.active)

    if (activeTrack) {
      // Audio codec
      if (activeTrack.audioCodec) {
        stats.stats.push({
          label: 'Codec',
          value: activeTrack.audioCodec.toUpperCase(),
        })
      }

      // Sample rate
      if (activeTrack.audioSamplingRate) {
        stats.stats.push({
          label: 'Sample Rate',
          value: `${(activeTrack.audioSamplingRate / 1000).toFixed(1)} kHz`,
        })
      }

      // Audio channels
      if (activeTrack.channelsCount) {
        stats.stats.push({
          label: 'Channels',
          value: activeTrack.channelsCount.toString(),
        })
      }

      // Audio bitrate
      if (activeTrack.audioBandwidth) {
        stats.stats.push({
          label: 'Bitrate',
          value: formatBitrate(activeTrack.audioBandwidth),
        })
      }
    }

    // Container format
    const streamPlan = this.parseStreamPlan()
    if (streamPlan?.Container) {
      stats.stats.push({
        label: 'Container',
        value: streamPlan.Container.toUpperCase(),
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

    return stats.stats.length > 0 ? stats : null
  }

  private getPlaybackInfoCategory(
    streamPlan: null | {
      Container?: string
      MediaPartId?: number
      Mode?: number | string
    },
    shakaStats: null | shaka.extern.Stats,
  ): PlayerStatsCategory {
    const stats: PlayerStatsCategory = {
      name: 'Playback Info',
      stats: [],
    }

    // Player name
    stats.stats.push({
      label: 'Player',
      value: 'Shaka Player (Audio)',
    })

    // Play method
    const playMethod = formatPlayMethod(streamPlan?.Mode)
    stats.stats.push({
      label: 'Play Method',
      value: playMethod,
    })

    // Playback session ID
    if (this.playbackState.playbackSessionId) {
      stats.stats.push({
        label: 'Session ID',
        value: this.playbackState.playbackSessionId.slice(0, 8),
      })
    }

    // Estimated bandwidth
    if (shakaStats?.estimatedBandwidth) {
      stats.stats.push({
        label: 'Estimated Bandwidth',
        value: formatBitrate(shakaStats.estimatedBandwidth),
      })
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

  private getTrackInfoCategory(): null | PlayerStatsCategory {
    const originator = this.playbackState.originator
    if (!originator) return null

    const stats: PlayerStatsCategory = {
      name: 'Track Info',
      stats: [],
    }

    // Track title
    if (originator.title) {
      stats.stats.push({
        label: 'Title',
        value: originator.title,
      })
    }

    // Artist (originalTitle for music tracks)
    if (originator.originalTitle) {
      stats.stats.push({
        label: 'Artist',
        value: originator.originalTitle,
      })
    }

    // Album (parentTitle for music tracks)
    if (originator.parentTitle) {
      stats.stats.push({
        label: 'Album',
        value: originator.parentTitle,
      })
    }

    // Duration
    if (this.playbackState.serverDuration) {
      const durationSeconds = Math.floor(
        this.playbackState.serverDuration / 1000,
      )
      const minutes = Math.floor(durationSeconds / 60)
      const seconds = durationSeconds % 60

      stats.stats.push({
        label: 'Duration',
        value: `${String(minutes)}:${seconds.toString().padStart(2, '0')}`,
      })
    }

    return stats.stats.length > 0 ? stats : null
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
