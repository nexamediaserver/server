import type { ReactNode } from 'react'

import IconVolumeDown from '~icons/material-symbols/volume-down'
import IconVolumeMute from '~icons/material-symbols/volume-mute'
import IconVolumeOff from '~icons/material-symbols/volume-off'
import IconVolumeUp from '~icons/material-symbols/volume-up'

import { Button } from '@/shared/components/ui/button'
import { Slider } from '@/shared/components/ui/slider'

interface VolumeControlsProps {
  /** Whether audio is muted */
  isMuted: boolean
  /** Handler for mute toggle */
  onToggleMute: () => void
  /** Handler for volume change */
  onVolumeChange: (values: number[]) => void
  /** Current volume (0-1) */
  volume: number
}

/**
 * Volume control with mute button and vertical slider.
 */
export function VolumeControls({
  isMuted,
  onToggleMute,
  onVolumeChange,
  volume,
}: VolumeControlsProps): ReactNode {
  // Convert linear slider value to exponential volume (x³ curve)
  const linearToVolume = (linear: number): number => {
    return Math.pow(linear, 3)
  }

  // Convert exponential volume to linear slider value
  const volumeToLinear = (vol: number): number => {
    return Math.pow(vol, 1 / 3)
  }

  const handleVolumeChange = (values: number[]) => {
    const linear = values[0] ?? 0
    const newVolume = linearToVolume(linear)
    onVolumeChange([newVolume])
  }

  // Get appropriate volume icon based on volume level and mute state
  const getVolumeIcon = () => {
    if (isMuted || volume === 0) {
      return <IconVolumeOff className="h-5 w-5" />
    } else if (volume < 0.3) {
      return <IconVolumeMute className="h-5 w-5" />
    } else if (volume < 0.7) {
      return <IconVolumeDown className="h-5 w-5" />
    } else {
      return <IconVolumeUp className="h-5 w-5" />
    }
  }

  const currentLinearVolume = volumeToLinear(volume)

  return (
    <>
      <Button
        aria-label="Mute/Unmute"
        onClick={onToggleMute}
        size="icon"
        variant="ghost"
      >
        {getVolumeIcon()}
      </Button>
      <Slider
        aria-label="Volume"
        className="data-[orientation=vertical]:min-h-20"
        max={1}
        min={0}
        onValueChange={handleVolumeChange}
        orientation="vertical"
        step={0.01}
        value={[currentLinearVolume]}
      />
    </>
  )
}
