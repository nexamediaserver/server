import { useMutation } from '@apollo/client/react'
import { MoreVertical, RefreshCw, Star } from 'lucide-react'
import {
  cloneElement,
  type MouseEvent,
  type ReactElement,
  useState,
} from 'react'

import { refreshItemMetadataDocument } from '@/app/graphql/metadata-refresh'
import {
  promoteItemDocument,
  unpromoteItemDocument,
} from '@/app/graphql/promotion'
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
  onOpenChange?: (open: boolean) => void
  trigger?: TriggerElement
}>

type TriggerElement = ReactElement<{
  onClick?: (event: MouseEvent<HTMLElement>) => void
}>

export function ItemActionsMenu({
  isPromoted,
  itemId,
  onOpenChange,
  trigger,
}: ItemActionsMenuProps) {
  const [refreshItemMetadata] = useMutation<
    RefreshItemMetadataMutation,
    RefreshItemMetadataMutationVariables
  >(refreshItemMetadataDocument)
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

  const triggerRefresh = () => {
    void refreshItemMetadata({
      variables: {
        includeChildren: true,
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
    <DropdownMenu onOpenChange={handleOpenChange} open={open}>
      <DropdownMenuTrigger asChild>{resolvedTrigger}</DropdownMenuTrigger>
      <DropdownMenuContent align="start" side="bottom">
        <DropdownMenuItem
          onClick={(event) => {
            preventEvent(event)
            togglePromotion()
            setOpen(false)
          }}
          onSelect={(event) => {
            event.preventDefault()
            event.stopPropagation()
            togglePromotion()
            setOpen(false)
          }}
        >
          <Star className="size-4" />
          {isPromoted ? 'Unpromote' : 'Promote'}
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={(event) => {
            preventEvent(event)
            triggerRefresh()
            setOpen(false)
          }}
          onSelect={(event) => {
            event.preventDefault()
            event.stopPropagation()
            triggerRefresh()
            setOpen(false)
          }}
        >
          <RefreshCw className="size-4" />
          Refresh Metadata
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}
