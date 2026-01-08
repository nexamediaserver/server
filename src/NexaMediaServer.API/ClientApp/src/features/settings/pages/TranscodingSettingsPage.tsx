import { useMutation, useQuery } from '@apollo/client/react'
import { useForm } from '@tanstack/react-form'
import { useCallback, useEffect, useState } from 'react'
import { toast } from 'sonner'

import {
  serverSettingsDocument,
  updateServerSettingsDocument,
} from '@/app/graphql/server-settings'
import { HardwareAccelerationKind } from '@/shared/api/graphql/graphql'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { Label } from '@/shared/components/ui/label'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/shared/components/ui/select'
import { Skeleton } from '@/shared/components/ui/skeleton'
import { Slider } from '@/shared/components/ui/slider'
import { Switch } from '@/shared/components/ui/switch'

import { SettingsPageContainer, SettingsPageHeader } from '../components'

// Convert bits per second to Mbps
const bitsToMbps = (bits: number) => Math.round(bits / 1_000_000)
// Convert Mbps to bits per second
const mbpsToBits = (mbps: number) => mbps * 1_000_000

// Slider range: 1-150 Mbps
const MIN_MBPS = 1
const MAX_MBPS = 150

export function TranscodingSettingsPage() {
  const { data, loading } = useQuery(serverSettingsDocument)
  const [showAdvancedBitrate, setShowAdvancedBitrate] = useState(false)

  const [updateSettings, { loading: updating }] = useMutation(
    updateServerSettingsDocument,
    {
      onCompleted: () => {
        toast.success('Settings saved successfully')
      },
      onError: (error) => {
        toast.error(`Failed to save settings: ${error.message}`)
      },
      refetchQueries: [serverSettingsDocument],
    },
  )

  const form = useForm({
    defaultValues: {
      allowHEVCEncoding: true,
      allowRemuxing: true,
      dashAudioCodec: 'aac',
      dashSegmentDurationSeconds: 6,
      dashVideoCodec: 'libx264',
      enableToneMapping: true,
      maxStreamingBitrateMbps: 60,
      preferH265: false,
      userPreferredAcceleration: null as HardwareAccelerationKind | null,
    },
    onSubmit: async ({ value }) => {
      const input: Record<string, any> = {
        allowHEVCEncoding: value.allowHEVCEncoding,
        allowRemuxing: value.allowRemuxing,
        dashAudioCodec: value.dashAudioCodec,
        dashSegmentDurationSeconds: value.dashSegmentDurationSeconds,
        dashVideoCodec: value.dashVideoCodec,
        enableToneMapping: value.enableToneMapping,
        maxStreamingBitrate: mbpsToBits(value.maxStreamingBitrateMbps),
        preferH265: value.preferH265,
      }

      // Only include userPreferredAcceleration if it has a value
      if (value.userPreferredAcceleration) {
        input.userPreferredAcceleration = value.userPreferredAcceleration
      }

      await updateSettings({
        variables: {
          input,
        },
      })
    },
  })

  // Update form when data loads
  useEffect(() => {
    if (data?.serverSettings) {
      const settings = data.serverSettings
      form.setFieldValue(
        'maxStreamingBitrateMbps',
        bitsToMbps(settings.maxStreamingBitrate),
      )
      form.setFieldValue('preferH265', settings.preferH265)
      form.setFieldValue('allowRemuxing', settings.allowRemuxing)
      form.setFieldValue('allowHEVCEncoding', settings.allowHEVCEncoding)
      form.setFieldValue('dashVideoCodec', settings.dashVideoCodec ?? 'libx264')
      form.setFieldValue('dashAudioCodec', settings.dashAudioCodec ?? 'aac')
      form.setFieldValue(
        'dashSegmentDurationSeconds',
        settings.dashSegmentDurationSeconds ?? 6,
      )
      form.setFieldValue(
        'enableToneMapping',
        settings.enableToneMapping ?? true,
      )
      form.setFieldValue(
        'userPreferredAcceleration',
        settings.userPreferredAcceleration,
      )
    }
  }, [data, form])

  const handleSliderChange = useCallback(
    (value: number[]) => {
      form.setFieldValue('maxStreamingBitrateMbps', value[0])
    },
    [form],
  )

  if (loading) {
    return (
      <SettingsPageContainer maxWidth="sm">
        <SettingsPageHeader
          description="Configure video transcoding and streaming options"
          title="Transcoding Settings"
        />
        <div className="space-y-4">
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
        </div>
      </SettingsPageContainer>
    )
  }

  return (
    <SettingsPageContainer maxWidth="sm">
      <SettingsPageHeader
        description="Configure video transcoding and streaming options"
        title="Transcoding Settings"
      />

      <form
        className="space-y-8"
        onSubmit={(e) => {
          e.preventDefault()
          e.stopPropagation()
          void form.handleSubmit()
        }}
      >
        {/* Max Streaming Bitrate */}
        <form.Field name="maxStreamingBitrateMbps">
          {(field) => (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Label htmlFor={field.name}>Maximum Streaming Bitrate</Label>
                <span className="text-sm font-medium">
                  {field.state.value} Mbps
                </span>
              </div>
              <Slider
                max={MAX_MBPS}
                min={MIN_MBPS}
                onValueChange={handleSliderChange}
                step={1}
                value={[field.state.value]}
              />
              <div
                className={`
                  flex items-center justify-between text-xs
                  text-muted-foreground
                `}
              >
                <span>{MIN_MBPS} Mbps</span>
                <span>{MAX_MBPS} Mbps</span>
              </div>
              <p className="text-sm text-muted-foreground">
                The maximum bitrate for streaming video. Higher values provide
                better quality but require more bandwidth.
              </p>

              {/* Advanced input toggle */}
              <button
                className={`
                  text-sm text-primary
                  hover:underline
                `}
                onClick={() => {
                  setShowAdvancedBitrate(!showAdvancedBitrate)
                }}
                type="button"
              >
                {showAdvancedBitrate ? 'Hide' : 'Show'} advanced input
              </button>

              {showAdvancedBitrate && (
                <div className="flex items-center gap-2">
                  <Input
                    className="w-32"
                    min={1}
                    onBlur={field.handleBlur}
                    onChange={(e) => {
                      const value = parseInt(e.target.value, 10)
                      if (!isNaN(value) && value >= 1) {
                        field.handleChange(value)
                      }
                    }}
                    type="number"
                    value={field.state.value}
                  />
                  <span className="text-sm text-muted-foreground">Mbps</span>
                </div>
              )}
            </div>
          )}
        </form.Field>

        {/* Prefer H.265 */}
        <form.Field name="preferH265">
          {(field) => (
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-1">
                <Label htmlFor={field.name}>Prefer H.265 (HEVC)</Label>
                <p className="text-sm text-muted-foreground">
                  Prefer H.265 codec when transcoding video. H.265 provides
                  better compression but may not be supported by all devices.
                </p>
              </div>
              <Switch
                checked={field.state.value}
                id={field.name}
                onCheckedChange={field.handleChange}
              />
            </div>
          )}
        </form.Field>

        {/* Allow Remuxing */}
        <form.Field name="allowRemuxing">
          {(field) => (
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-1">
                <Label htmlFor={field.name}>Allow Remuxing</Label>
                <p className="text-sm text-muted-foreground">
                  Allow changing the container format without re-encoding the
                  video. This is faster and preserves quality when possible.
                </p>
              </div>
              <Switch
                checked={field.state.value}
                id={field.name}
                onCheckedChange={field.handleChange}
              />
            </div>
          )}
        </form.Field>

        {/* Allow HEVC Encoding */}
        <form.Field name="allowHEVCEncoding">
          {(field) => (
            <div className="flex items-start justify-between gap-4">
              <div className="space-y-1">
                <Label htmlFor={field.name}>Allow HEVC Encoding</Label>
                <p className="text-sm text-muted-foreground">
                  Allow encoding video to HEVC (H.265) when transcoding. Disable
                  if client devices don&apos;t support HEVC playback.
                </p>
              </div>
              <Switch
                checked={field.state.value}
                id={field.name}
                onCheckedChange={field.handleChange}
              />
            </div>
          )}
        </form.Field>

        {/* DASH Settings Divider */}
        <div className="border-t pt-6">
          <h3 className="mb-1 text-lg font-medium">DASH Streaming Settings</h3>
          <p className="mb-6 text-sm text-muted-foreground">
            Configure MPEG-DASH adaptive streaming parameters
          </p>

          {/* DASH Video Codec */}
          <form.Field name="dashVideoCodec">
            {(field) => (
              <div className="mb-6 space-y-2">
                <Label htmlFor={field.name}>Video Codec</Label>
                <Input
                  id={field.name}
                  onBlur={field.handleBlur}
                  onChange={(e) => {
                    field.handleChange(e.target.value)
                  }}
                  placeholder="libx264"
                  value={field.state.value}
                />
                <p className="text-sm text-muted-foreground">
                  FFmpeg encoder for DASH video (e.g., libx264, libx265,
                  h264_videotoolbox).
                </p>
              </div>
            )}
          </form.Field>

          {/* DASH Audio Codec */}
          <form.Field name="dashAudioCodec">
            {(field) => (
              <div className="mb-6 space-y-2">
                <Label htmlFor={field.name}>Audio Codec</Label>
                <Input
                  id={field.name}
                  onBlur={field.handleBlur}
                  onChange={(e) => {
                    field.handleChange(e.target.value)
                  }}
                  placeholder="aac"
                  value={field.state.value}
                />
                <p className="text-sm text-muted-foreground">
                  FFmpeg encoder for DASH audio (e.g., aac, libopus,
                  libfdk_aac).
                </p>
              </div>
            )}
          </form.Field>

          {/* DASH Segment Duration */}
          <form.Field name="dashSegmentDurationSeconds">
            {(field) => (
              <div className="mb-6 space-y-2">
                <Label htmlFor={field.name}>
                  Segment Duration: {field.state.value} seconds
                </Label>
                <Slider
                  id={field.name}
                  max={30}
                  min={1}
                  onValueChange={([value]) => {
                    field.handleChange(value)
                  }}
                  step={1}
                  value={[field.state.value]}
                />
                <p className="text-sm text-muted-foreground">
                  Duration of each DASH segment in seconds (1-30). Shorter
                  segments enable faster seeking but create more files.
                </p>
              </div>
            )}
          </form.Field>

          {/* Enable Tone Mapping */}
          <form.Field name="enableToneMapping">
            {(field) => (
              <div className="mb-6 flex items-start justify-between gap-4">
                <div className="space-y-1">
                  <Label htmlFor={field.name}>Enable Tone Mapping</Label>
                  <p className="text-sm text-muted-foreground">
                    Apply tone mapping when converting HDR content to SDR for
                    better color reproduction.
                  </p>
                </div>
                <Switch
                  checked={field.state.value}
                  id={field.name}
                  onCheckedChange={field.handleChange}
                />
              </div>
            )}
          </form.Field>

          {/* Hardware Acceleration */}
          <form.Field name="userPreferredAcceleration">
            {(field) => (
              <div className="space-y-2">
                <Label htmlFor={field.name}>Hardware Acceleration</Label>
                <Select
                  onValueChange={(value) => {
                    field.handleChange(
                      value === 'NONE'
                        ? null
                        : (value as HardwareAccelerationKind),
                    )
                  }}
                  value={field.state.value ?? HardwareAccelerationKind.None}
                >
                  <SelectTrigger id={field.name}>
                    <SelectValue placeholder="Select acceleration method" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value={HardwareAccelerationKind.None}>
                      None (Software)
                    </SelectItem>
                    <SelectItem value={HardwareAccelerationKind.Qsv}>
                      Intel Quick Sync (QSV)
                    </SelectItem>
                    <SelectItem value={HardwareAccelerationKind.Nvenc}>
                      NVIDIA NVENC
                    </SelectItem>
                    <SelectItem value={HardwareAccelerationKind.Amf}>
                      AMD AMF
                    </SelectItem>
                    <SelectItem value={HardwareAccelerationKind.Vaapi}>
                      VAAPI (Linux)
                    </SelectItem>
                    <SelectItem value={HardwareAccelerationKind.VideoToolbox}>
                      VideoToolbox (macOS)
                    </SelectItem>
                  </SelectContent>
                </Select>
                <p className="text-sm text-muted-foreground">
                  Hardware acceleration method for video encoding. Availability
                  depends on your hardware.
                </p>
              </div>
            )}
          </form.Field>
        </div>

        <Button disabled={updating} type="submit">
          {updating ? 'Saving...' : 'Save Changes'}
        </Button>
      </form>
    </SettingsPageContainer>
  )
}
