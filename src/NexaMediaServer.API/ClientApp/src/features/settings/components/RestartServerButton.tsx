import { useMutation } from '@apollo/client/react'
import { AlertTriangle } from 'lucide-react'
import { useState } from 'react'
import { toast } from 'sonner'

import { restartServerDocument } from '@/app/graphql/server-control'
import { Button } from '@/shared/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/shared/components/ui/dialog'

export function RestartServerButton() {
  const [showConfirm, setShowConfirm] = useState(false)
  const [restartServer, { loading }] = useMutation(restartServerDocument, {
    onCompleted: () => {
      toast.success('Server is restarting...', {
        description:
          'The server will be back online shortly. You may need to refresh the page.',
      })
      setShowConfirm(false)
    },
    onError: (error) => {
      toast.error(`Failed to restart server: ${error.message}`)
    },
  })

  const handleRestart = async () => {
    await restartServer()
  }

  return (
    <>
      <Button
        onClick={() => {
          setShowConfirm(true)
        }}
        variant="destructive"
      >
        Restart Server
      </Button>

      <Dialog onOpenChange={setShowConfirm} open={showConfirm}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertTriangle className="h-5 w-5 text-destructive" />
              Restart Server
            </DialogTitle>
            <DialogDescription>
              Are you sure you want to restart the server? This will temporarily
              interrupt all active connections and playback sessions.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              onClick={() => {
                setShowConfirm(false)
              }}
              variant="outline"
            >
              Cancel
            </Button>
            <Button
              disabled={loading}
              onClick={() => {
                void handleRestart()
              }}
              variant="destructive"
            >
              {loading ? 'Restarting...' : 'Restart Server'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  )
}
