import type { ReactNode } from 'react'

import IconMoreVert from '~icons/material-symbols/more-vert'
import IconQueryStats from '~icons/material-symbols/query-stats'

import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'

export interface PlayerMenuProps {
  /** Callback when stats toggle is clicked */
  onToggleStats: () => void

  /** Whether player stats are currently enabled */
  statsEnabled: boolean
}

/**
 * PlayerMenu component provides a dropdown menu for player options.
 * Currently includes a toggle for player statistics.
 */
export function PlayerMenu({
  onToggleStats,
  statsEnabled,
}: PlayerMenuProps): ReactNode {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          aria-label="Player menu"
          className="h-8 w-8"
          size="icon"
          variant="ghost"
        >
          <IconMoreVert className="h-5 w-5" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" side="top">
        <DropdownMenuItem onClick={onToggleStats}>
          <IconQueryStats className="mr-2 h-4 w-4" />
          {statsEnabled ? 'Hide' : 'Show'} Player Stats
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
