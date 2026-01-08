import { useQuery } from '@apollo/client/react'
import { useEffect, useMemo, useState } from 'react'

import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { cn } from '@/shared/lib/utils'

import {
  browseDirectoryQueryDocument,
  fileSystemRootsQueryDocument,
} from '../graphql/fileSystem'

interface DirectoryBrowserModalProps {
  initialPath?: null | string
  onClose: () => void
  onSelect: (path: string) => void
  open: boolean
}

export function DirectoryBrowserModal({
  initialPath,
  onClose,
  onSelect,
  open,
}: DirectoryBrowserModalProps) {
  const [currentPath, setCurrentPath] = useState<null | string>(null)
  const [pathFieldValue, setPathFieldValue] = useState('')

  const {
    data: rootsData,
    error: rootsError,
    loading: rootsLoading,
    refetch: refetchRoots,
  } = useQuery(fileSystemRootsQueryDocument, {
    fetchPolicy: 'cache-and-network',
    skip: !open,
  })

  const availableRoots = useMemo(
    () => rootsData?.fileSystemRoots ?? [],
    [rootsData],
  )

  const {
    data: directoryData,
    error: directoryError,
    loading: directoryLoading,
    refetch: refetchDirectory,
  } = useQuery(browseDirectoryQueryDocument, {
    fetchPolicy: 'network-only',
    skip: !open || !currentPath,
    variables: { path: currentPath ?? '' },
  })

  const directoryEntries = useMemo(
    () => directoryData?.browseDirectory.entries ?? [],
    [directoryData],
  )
  const parentPath = directoryData?.browseDirectory.parentPath ?? null

  // Derive selected root from current path and available roots
  const selectedRoot = useMemo(() => {
    if (!currentPath || !availableRoots.length) {
      return null
    }
    const match = availableRoots.find((root) =>
      pathBelongsToRoot(currentPath, root.path),
    )
    return match?.path ?? null
  }, [availableRoots, currentPath])

  // Initialize with first root when modal opens and roots become available
  useEffect(() => {
    if (open && !currentPath && availableRoots.length > 0) {
      const pathToUse = initialPath ?? availableRoots[0].path
      // eslint-disable-next-line react-hooks/set-state-in-effect -- Desired behavior
      setCurrentPath(pathToUse)
      setPathFieldValue(pathToUse)
    }
  }, [availableRoots, currentPath, initialPath, open])

  const handleNavigateFromInput = () => {
    const trimmed = pathFieldValue.trim()
    if (trimmed.length === 0) {
      return
    }
    setCurrentPath(trimmed)
  }

  const handleSelectDirectory = () => {
    if (!currentPath || directoryLoading || directoryError) {
      return
    }
    onSelect(currentPath)
  }

  const canConfirmSelection =
    Boolean(currentPath) && !directoryLoading && !directoryError

  const handleRootSelection = (path: string) => {
    setCurrentPath(path)
    setPathFieldValue(path)
  }

  const handleEntryClick = (entryPath: string, selectable: boolean) => {
    if (!selectable) {
      return
    }
    setCurrentPath(entryPath)
    setPathFieldValue(entryPath)
  }

  const handleOpenChange = (nextOpen: boolean) => {
    if (!nextOpen) {
      // Reset when closing
      setCurrentPath(null)
      setPathFieldValue('')
      onClose()
    }
  }

  const rootContent = useMemo(() => {
    if (rootsLoading) {
      return (
        <div className="space-y-2">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-full" />
        </div>
      )
    }

    if (rootsError) {
      return (
        <div
          className={`
            rounded border border-destructive/40 bg-destructive/10 p-3 text-sm
          `}
        >
          <p className="font-semibold">Unable to load server roots.</p>
          <p className="text-xs text-destructive-foreground/80">
            {rootsError.message}
          </p>
          <Button
            className="mt-2"
            onClick={() => void refetchRoots()}
            size="sm"
            variant="outline"
          >
            Retry
          </Button>
        </div>
      )
    }

    if (!availableRoots.length) {
      return (
        <p className="text-sm text-muted-foreground">
          No filesystem roots detected.
        </p>
      )
    }

    return (
      <ul className="max-h-96 space-y-2 overflow-y-auto p-2">
        {availableRoots.map((root) => {
          const isActive = selectedRoot ? root.path === selectedRoot : false
          return (
            <li key={root.id}>
              <button
                className={cn(
                  `
                    w-full rounded border px-3 py-2 text-left text-sm
                    transition-colors
                  `,
                  isActive
                    ? 'border-primary bg-primary/10 text-primary'
                    : `
                      border-border
                      hover:border-primary/60
                    `,
                )}
                onClick={() => {
                  handleRootSelection(root.path)
                }}
                type="button"
              >
                <p className="font-medium">{root.label}</p>
              </button>
            </li>
          )
        })}
      </ul>
    )
  }, [availableRoots, refetchRoots, rootsError, rootsLoading, selectedRoot])

  const directoryContent = useMemo(() => {
    if (!currentPath) {
      return (
        <p className="text-sm text-muted-foreground">
          Select a root to start browsing.
        </p>
      )
    }

    if (directoryLoading) {
      return (
        <div className="space-y-2">
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-full" />
          <Skeleton className="h-8 w-full" />
        </div>
      )
    }

    if (directoryError) {
      return (
        <div
          className={`
            rounded border border-destructive/40 bg-destructive/10 p-3 text-sm
          `}
        >
          <p className="font-semibold">Unable to read {currentPath}</p>
          <p className="text-xs text-destructive-foreground/80">
            {directoryError.message}
          </p>
          <Button
            className="mt-2"
            onClick={() => void refetchDirectory()}
            size="sm"
            variant="outline"
          >
            Retry
          </Button>
        </div>
      )
    }

    if (!directoryEntries.length && !parentPath) {
      return (
        <p className="text-sm text-muted-foreground">
          This directory is empty.
        </p>
      )
    }

    return (
      <ul className="max-h-96 space-y-2 overflow-y-auto p-2">
        {parentPath ? (
          <li>
            <button
              className={`
                flex w-full items-center justify-between rounded border
                border-dashed px-3 py-2 text-left text-sm
              `}
              onClick={() => {
                setCurrentPath(parentPath)
              }}
              type="button"
            >
              <span className="font-semibold">..</span>
              <span className="text-xs text-muted-foreground">
                Up one level
              </span>
            </button>
          </li>
        ) : null}
        {directoryEntries.map((entry) => (
          <li key={entry.path}>
            <button
              className={cn(
                `
                  flex w-full items-center justify-between rounded border px-3
                  py-2 text-left text-sm transition-colors
                `,
                entry.isSelectable
                  ? `
                    border-border
                    hover:border-primary/60
                  `
                  : `
                    cursor-not-allowed border-border/60 bg-muted
                    text-muted-foreground
                  `,
              )}
              disabled={!entry.isSelectable}
              onClick={() => {
                handleEntryClick(entry.path, entry.isSelectable)
              }}
              type="button"
            >
              <span className="font-medium">{entry.name}</span>
            </button>
          </li>
        ))}
      </ul>
    )
  }, [
    currentPath,
    directoryEntries,
    directoryError,
    directoryLoading,
    parentPath,
    refetchDirectory,
  ])

  return (
    <Dialog onOpenChange={handleOpenChange} open={open}>
      <DialogContent
        className={`
          max-w-3xl gap-0
          lg:min-w-3xl
        `}
      >
        <DialogHeader className="border-b bg-stone-700">
          <DialogTitle>Browse for media folder</DialogTitle>
        </DialogHeader>
        <div className={`flex flex-col gap-4 p-4`}>
          <Input
            id="current-path-input"
            onChange={(event) => {
              setPathFieldValue(event.target.value)
            }}
            onKeyDown={(event) => {
              if (event.key === 'Enter') {
                event.preventDefault()
                handleNavigateFromInput()
              }
            }}
            placeholder="/mnt/media"
            value={pathFieldValue}
          />
          <div
            className={`
              grid gap-4
              md:grid-cols-2
            `}
          >
            <div className={`rounded border border-border bg-background/60`}>
              {rootContent}
            </div>
            <div className="rounded border border-border bg-background/60">
              {directoryContent}
            </div>
          </div>
        </div>
        <DialogFooter className="mt-auto flex flex-wrap gap-2 bg-stone-700 pt-4">
          <Button
            onClick={() => {
              handleOpenChange(false)
            }}
            type="button"
            variant="secondary"
          >
            Cancel
          </Button>
          <Button
            disabled={!canConfirmSelection}
            onClick={handleSelectDirectory}
            type="button"
          >
            Add folder
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}

function pathBelongsToRoot(path: string, rootPath: string) {
  if (rootPath === '/') {
    return true
  }

  const rootLooksWindows = rootPath.includes('\\') || rootPath.includes(':')

  if (rootLooksWindows) {
    return path.toLowerCase().startsWith(rootPath.toLowerCase())
  }

  const normalizedRoot = rootPath.endsWith('/') ? rootPath : `${rootPath}/`
  return path === rootPath || path.startsWith(normalizedRoot)
}
