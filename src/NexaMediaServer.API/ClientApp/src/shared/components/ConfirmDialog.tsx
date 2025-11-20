import { type ReactNode, useState } from 'react'

import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'

export function ConfirmDialog({
  cancelText = 'Cancel',
  confirmText = 'Confirm',
  description,
  footer,
  onConfirm,
  onOpenChange,
  open,
  title,
  tone = 'default',
}: {
  cancelText?: string
  confirmText?: string
  description?: ReactNode
  footer?: ReactNode
  onConfirm: () => Promise<void> | void
  onOpenChange: (open: boolean) => void
  open: boolean
  title: ReactNode
  tone?: 'default' | 'destructive'
}) {
  const [submitting, setSubmitting] = useState(false)

  async function handleConfirm() {
    try {
      setSubmitting(true)
      await onConfirm()
      onOpenChange(false)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Dialog onOpenChange={onOpenChange} open={open}>
      <DialogContent aria-describedby="confirm-description">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          {description ? (
            <DialogDescription id="confirm-description">
              {description}
            </DialogDescription>
          ) : null}
        </DialogHeader>
        <DialogFooter>
          {footer}
          <div className="flex w-full justify-end gap-2">
            <Button
              disabled={submitting}
              onClick={() => {
                onOpenChange(false)
              }}
              variant="outline"
            >
              {cancelText}
            </Button>
            <Button
              aria-busy={submitting}
              onClick={() => {
                void handleConfirm()
              }}
              variant={tone === 'destructive' ? 'destructive' : 'default'}
            >
              {confirmText}
            </Button>
          </div>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
