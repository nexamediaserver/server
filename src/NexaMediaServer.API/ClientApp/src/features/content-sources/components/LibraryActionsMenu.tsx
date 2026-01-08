import { useMutation } from '@apollo/client/react'
import { MoreVertical } from 'lucide-react'
import {
  cloneElement,
  type MouseEvent,
  type ReactElement,
  useState,
} from 'react'
import { toast } from 'sonner'

import { librarySectionsQueryDocument } from '@/app/graphql/content-source'
import { startLibraryScanDocument } from '@/app/graphql/library-scan'
import { removeLibrarySectionDocument } from '@/app/graphql/library-section'
import { refreshLibraryMetadataDocument } from '@/app/graphql/metadata-refresh'
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
  const [refreshLibraryMetadata] = useMutation(refreshLibraryMetadataDocument)
  const [startLibraryScan] = useMutation(startLibraryScanDocument)
  const [removeLibrarySection] = useMutation(removeLibrarySectionDocument, {
    refetchQueries: [librarySectionsQueryDocument],
  })
  const [open, setOpen] = useState(false)
  const [confirmDeleteOpen, setConfirmDeleteOpen] = useState(false)

  const triggerRefresh = () => {
    void refreshLibraryMetadata({
      variables: { librarySectionId },
    })
  }

  const triggerScan = () => {
    void startLibraryScan({
      onCompleted: () => {
        toast.success('Library scan started')
      },
      onError: (error) => {
        toast.error(`Failed to start scan: ${error.message}`)
      },
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
          Refresh All Metadata
        </DropdownMenuItem>
        <DropdownMenuItem
          onClick={(event) => {
            preventEvent(event)
            triggerScan()
            setOpen(false)
          }}
          onSelect={(event) => {
            event.preventDefault()
            event.stopPropagation()
            triggerScan()
            setOpen(false)
          }}
        >
          Scan Library
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
