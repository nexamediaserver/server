import { useMutation } from '@apollo/client/react'
import { MoreVertical, RefreshCw, Trash2 } from 'lucide-react'
import {
  cloneElement,
  type MouseEvent,
  type ReactElement,
  useState,
} from 'react'

import { removeLibrarySectionDocument } from '@/app/graphql/library-section'
import { refreshLibraryMetadataDocument } from '@/app/graphql/metadata-refresh'
import {
  type RefreshLibraryMetadataMutation,
  type RefreshLibraryMetadataMutationVariables,
  type RemoveLibrarySectionMutation,
  type RemoveLibrarySectionMutationVariables,
} from '@/shared/api/graphql/graphql'
import { ConfirmDialog } from '@/shared/components/ConfirmDialog'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'

export type LibraryActionsMenuProps = Readonly<{
  librarySectionId: string
  onDeleted?: () => void
  trigger?: TriggerElement
}>

type TriggerElement = ReactElement<{
  onClick?: (event: MouseEvent<HTMLElement>) => void
}>

export function LibraryActionsMenu({
  librarySectionId,
  onDeleted,
  trigger,
}: LibraryActionsMenuProps) {
  const [refreshLibraryMetadata] = useMutation<
    RefreshLibraryMetadataMutation,
    RefreshLibraryMetadataMutationVariables
  >(refreshLibraryMetadataDocument)
  const [removeLibrarySection] = useMutation<
    RemoveLibrarySectionMutation,
    RemoveLibrarySectionMutationVariables
  >(removeLibrarySectionDocument)
  const [open, setOpen] = useState(false)
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const triggerRefresh = () => {
    void refreshLibraryMetadata({
      variables: { librarySectionId },
    })
  }

  const triggerDelete = async () => {
    const result = await removeLibrarySection({
      variables: { librarySectionId },
    })

    if (result.data?.removeLibrarySection.success) {
      onDeleted?.()
    }
  }

  const preventEvent = (event: MouseEvent<HTMLElement>) => {
    event.preventDefault()
    event.stopPropagation()
  }

  const defaultTrigger: TriggerElement = (
    <Button
      aria-label="Open library actions"
      className="h-8 w-8 shrink-0"
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
    <DropdownMenu onOpenChange={setOpen} open={open}>
      <DropdownMenuTrigger asChild>{resolvedTrigger}</DropdownMenuTrigger>
      <DropdownMenuContent align="end" side="right">
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
          Refresh All Metadata
        </DropdownMenuItem>
        <DropdownMenuSeparator />
        <DropdownMenuItem
          onClick={(event) => {
            preventEvent(event)
            setConfirmDeleteOpen(true)
            setOpen(false)
          }}
          onSelect={(event) => {
            event.preventDefault()
            event.stopPropagation()
            setConfirmDeleteOpen(true)
            setOpen(false)
          }}
        >
          <Trash2 className="size-4" />
          Delete Library
        </DropdownMenuItem>
      </DropdownMenuContent>
      <ConfirmDialog
        confirmText="Delete"
        description="This will permanently delete this library section and all associated metadata. This action cannot be undone."
        onConfirm={triggerDelete}
        onOpenChange={setConfirmDeleteOpen}
        open={confirmDeleteOpen}
        title="Delete Library Section?"
        tone="destructive"
      />
    </DropdownMenu>
  )
}
