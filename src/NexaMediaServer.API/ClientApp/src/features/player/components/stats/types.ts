/**
 * Represents a single stat entry with a label and value
 */
export interface PlayerStat {
  label: string
  value: string
}

/**
 * A category of stats with an optional header
 */
export interface PlayerStatsCategory {
  /** Optional name for the category header */
  name?: string
  /** Array of stats in this category */
  stats: PlayerStat[]
  /** Optional subtext shown next to the category name */
  subText?: string
}

/**
 * Interface that player-specific stat providers must implement.
 * Allows for extensible stat collection across different player types.
 */
export interface PlayerStatsProvider {
  /**
   * Optional cleanup function called when stats provider is unmounted
   */
  cleanup?: () => void

  /**
   * Collect current stats for display.
   * Should return a promise that resolves to an array of stat categories.
   */
  getStats: () => Promise<PlayerStatsCategory[]>
}
