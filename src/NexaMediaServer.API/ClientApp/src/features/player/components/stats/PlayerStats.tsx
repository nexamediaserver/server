import type { ReactNode } from 'react'

import { useCallback, useEffect, useRef, useState } from 'react'
import IconClose from '~icons/material-symbols/close'

import { Button } from '@/shared/components/ui/button'

import type {
  PlayerStat,
  PlayerStatsCategory,
  PlayerStatsProvider,
} from './types'

export interface PlayerStatsProps {
  /** Whether stats are currently enabled/visible */
  enabled: boolean

  /** Callback when stats should be disabled */
  onDisable: () => void

  /** The stats provider for the current player type */
  provider?: PlayerStatsProvider
}

interface StatRowProps {
  stat: PlayerStat
}

interface StatsCategoryProps {
  category: PlayerStatsCategory
}

/**
 * PlayerStats component displays real-time playback diagnostics.
 * Similar to Jellyfin's player stats, showing stream info, codecs, bitrates, etc.
 * Adapts to different player types through the provider pattern.
 */
export function PlayerStats({
  enabled,
  onDisable,
  provider,
}: PlayerStatsProps): ReactNode {
  const [categories, setCategories] = useState<PlayerStatsCategory[]>([])
  const intervalRef = useRef<null | number>(null)

  // Fetch and render stats periodically
  const updateStats = useCallback(async () => {
    if (!provider) {
      setCategories([])
      return
    }

    try {
      const stats = await provider.getStats()
      setCategories(stats)
    } catch (error) {
      console.error('[PlayerStats] Error fetching stats:', error)
      setCategories([])
    }
  }, [provider])

  // Set up periodic stat updates when enabled
  useEffect(() => {
    if (!enabled || !provider) {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
      setCategories([])
      return
    }

    // Initial fetch
    void updateStats()

    // Update every 700ms (similar to Jellyfin)
    intervalRef.current = setInterval(() => {
      void updateStats()
    }, 700)

    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current)
        intervalRef.current = null
      }
    }
  }, [enabled, provider, updateStats])

  // Cleanup provider on unmount
  useEffect(() => {
    return () => {
      provider?.cleanup?.()
    }
  }, [provider])

  if (!enabled) {
    return null
  }

  return (
    <div
      className={`
        pointer-events-auto fixed top-20 left-4 z-50 max-h-[80vh] w-80
        overflow-auto rounded-lg bg-black/90 text-white shadow-xl
        backdrop-blur-sm
      `}
    >
      <div
        className={`
          sticky top-0 z-10 flex items-center justify-between border-b
          border-white/10 bg-black/90 p-3 backdrop-blur-sm
        `}
      >
        <h3 className="text-sm font-semibold">Player Statistics</h3>
        <Button
          className={`
            h-8 w-8 rounded-full
            hover:bg-white/20
          `}
          onClick={onDisable}
          size="icon"
          variant="ghost"
        >
          <IconClose className="h-5 w-5" />
        </Button>
      </div>

      <div className="space-y-4 p-4">
        {categories.length === 0 ? (
          <div className="text-center text-sm text-white/60">
            No stats available
          </div>
        ) : (
          categories.map((category, categoryIndex) => (
            <StatsCategory category={category} key={categoryIndex} />
          ))
        )}
      </div>
    </div>
  )
}

function StatRow({ stat }: StatRowProps): ReactNode {
  return (
    <div className="flex items-start justify-between gap-3 text-xs">
      <div className="text-white/60">{stat.label}</div>
      <div
        className="text-right font-mono text-white"
        // Allow HTML in value for line breaks (like Jellyfin's transcode reasons)
        dangerouslySetInnerHTML={{ __html: stat.value }}
      />
    </div>
  )
}

function StatsCategory({ category }: StatsCategoryProps): ReactNode {
  if (category.stats.length === 0) {
    return null
  }

  return (
    <div className="space-y-2">
      {/* Category header */}
      {category.name && (
        <div
          className={`
            flex items-center justify-between border-b border-white/10 pb-1
          `}
        >
          <div
            className={`
              text-xs font-semibold tracking-wider text-white/80 uppercase
            `}
          >
            {category.name}
          </div>
          {category.subText && (
            <div className="text-xs text-white/60">{category.subText}</div>
          )}
        </div>
      )}

      {/* Stats */}
      <div className="space-y-1.5">
        {category.stats.map((stat, statIndex) => (
          <StatRow key={statIndex} stat={stat} />
        ))}
      </div>
    </div>
  )
}
