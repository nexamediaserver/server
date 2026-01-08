import type { ReactNode } from 'react'

import IconEdit from '~icons/material-symbols/edit'
import IconMore from '~icons/material-symbols/more-horiz'
import IconPlay from '~icons/material-symbols/play-arrow'

import { useIsAdmin } from '@/features/auth'
import { ItemActionsMenu } from '@/features/content-sources/components/ItemActionsMenu'
import { Button } from '@/shared/components/ui/button'

interface ActionsFieldProps {
  isPromoted: boolean
  itemId: string
  onEditClick: () => void
  onPlayClick: () => void
  playDisabled?: boolean
}

export function ActionsField({
  isPromoted,
  itemId,
  onEditClick,
  onPlayClick,
  playDisabled,
}: ActionsFieldProps): ReactNode {
  const isAdmin = useIsAdmin()

  return (
    <div
      className={`
        flex w-full flex-col gap-2
        md:w-auto md:flex-row md:gap-4
      `}
    >
      {/* Primary play button - full width on mobile, prominent */}
      <Button
        className={`
          w-full
          md:w-auto
        `}
        disabled={playDisabled}
        onClick={onPlayClick}
        size="lg"
      >
        <IconPlay />
        Play
      </Button>

      {/* Secondary actions - flex row on mobile */}
      <div
        className={`
          flex flex-row gap-2
          md:gap-4
        `}
      >
        {isAdmin && (
          <Button
            className={`
              flex-1
              md:flex-none
            `}
            onClick={onEditClick}
            variant="outline"
          >
            <IconEdit />
          </Button>
        )}
        <ItemActionsMenu
          isPromoted={isPromoted}
          itemId={itemId}
          trigger={
            <Button
              className={`
                flex-1
                md:flex-none
              `}
              variant="outline"
            >
              <IconMore />
            </Button>
          }
        />
      </div>
    </div>
  )
}
