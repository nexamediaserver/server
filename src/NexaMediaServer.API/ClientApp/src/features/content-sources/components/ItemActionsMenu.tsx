import { useMutation } from '@apollo/client/react'
import { MoreVertical } from 'lucide-react'
import {
  cloneElement,
  type MouseEvent,
  type ReactElement,
  useState,
} from 'react'

import { analyzeItemDocument } from '@/app/graphql/analyze'
import { refreshItemMetadataDocument } from '@/app/graphql/metadata-refresh'
import {
  promoteItemDocument,
  unpromoteItemDocument,
} from '@/app/graphql/promotion'
import { useIsAdmin } from '@/features/auth'
import { EditMetadataItemDialog } from '@/features/metadata/components/EditMetadataItemDialog'
import {
  type RefreshItemMetadataMutation,
  type RefreshItemMetadataMutationVariables,
} from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'

export type ItemActionsMenuProps = Readonly<{
  isPromoted: boolean
  itemId: string
  onEdit?: () => void
  onOpenChange?: (open: boolean) => void
  trigger?: TriggerElement
}>

type TriggerElement = ReactElement<{
  onClick?: (event: MouseEvent<HTMLElement>) => void
}>

export function ItemActionsMenu({
  isPromoted,
  itemId,
  onEdit,
  onOpenChange,
  trigger,
}: ItemActionsMenuProps) {
  const isAdmin = useIsAdmin()
  const [editDialogOpen, setEditDialogOpen] = useState(false)
  const [refreshItemMetadata] = useMutation<
    RefreshItemMetadataMutation,
    RefreshItemMetadataMutationVariables
  >(refreshItemMetadataDocument)
  const [analyzeItem] = useMutation(analyzeItemDocument)
  const [promoteItem] = useMutation(promoteItemDocument, {
    refetchQueries: ['LibrarySectionChildren'],
  })
  const [unpromoteItem] = useMutation(unpromoteItemDocument, {
    refetchQueries: ['LibrarySectionChildren'],
  })
  const [open, setOpen] = useState(false)

  const handleOpenChange = (newOpen: boolean) => {
    setOpen(newOpen)
    onOpenChange?.(newOpen)
  }

  const closeMenu = () => {
    setOpen(false)
    onOpenChange?.(false)
  }

  const triggerRefresh = () => {
    void refreshItemMetadata({
      variables: {
        includeChildren: true,
        itemId,
      },
    })
  }

  const triggerAnalyze = () => {
    void analyzeItem({
      variables: {
        itemId,
      },
    })
  }

  const togglePromotion = () => {
    if (isPromoted) {
      void unpromoteItem({
        variables: {
          itemId,
        },
      })
    } else {
      void promoteItem({
        variables: {
          itemId,
        },
      })
    }
  }

  const preventEvent = (event: MouseEvent<HTMLElement>) => {
    event.preventDefault()
    event.stopPropagation()
  }

  const defaultTrigger: TriggerElement = (
    <Button
      aria-label="Open item actions"
      className={`
        h-8 w-8 rounded-full bg-black/60 text-white
        hover:bg-black/70
      `}
      onClick={preventEvent}
      size="icon"
      variant="ghost"
    >
      <MoreVertical className="size-4" />
    </Button>
  )

  const resolvedTrigger: TriggerElement =
    trigger == null
      ? defaultTrigger
      : cloneElement(trigger, {
          onClick: (event: MouseEvent<HTMLElement>) => {
            preventEvent(event)
            trigger.props.onClick?.(event)
          },
        })

  return (
    <DropdownMenu modal={false} onOpenChange={handleOpenChange} open={open}>
      <DropdownMenuTrigger asChild>{resolvedTrigger}</DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        alignOffset={-8}
        collisionPadding={8}
        side="bottom"
        sideOffset={8}
      >
        {isAdmin && (
          <>
            <DropdownMenuItem
              onClick={(event) => {
                preventEvent(event)
                if (onEdit) {
                  onEdit()
                } else {
                  setEditDialogOpen(true)
                }
                closeMenu()
              }}
              onSelect={(event) => {
                event.preventDefault()
                event.stopPropagation()
                if (onEdit) {
                  onEdit()
                } else {
                  setEditDialogOpen(true)
                }
                closeMenu()
              }}
            >
              Edit
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={(event) => {
                preventEvent(event)
                togglePromotion()
                closeMenu()
              }}
              onSelect={(event) => {
                event.preventDefault()
                event.stopPropagation()
                togglePromotion()
                closeMenu()
              }}
            >
              {isPromoted ? 'Unpromote' : 'Promote'}
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={(event) => {
                preventEvent(event)
                triggerRefresh()
                closeMenu()
              }}
              onSelect={(event) => {
                event.preventDefault()
                event.stopPropagation()
                triggerRefresh()
                closeMenu()
              }}
            >
              Refresh Metadata
            </DropdownMenuItem>
            <DropdownMenuItem
              onClick={(event) => {
                preventEvent(event)
                triggerAnalyze()
                closeMenu()
              }}
              onSelect={(event) => {
                event.preventDefault()
                event.stopPropagation()
                triggerAnalyze()
                closeMenu()
              }}
            >
              Analyze
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
      {isAdmin && !onEdit && (
        <EditMetadataItemDialog
          itemId={itemId}
          onClose={() => {
            setEditDialogOpen(false)
          }}
          open={editDialogOpen}
        />
      )}
    </DropdownMenu>
  )
}
