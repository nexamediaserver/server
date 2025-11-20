import type {
  DirectPlayProfileInput,
  PlaybackCapabilitiesInput,
  PlaybackCapabilityInput,
  ProfileConditionInput,
} from '@/shared/api/graphql/graphql'

import { buildDeviceMetadata } from './deviceIdentity'

type AudioContextConstructor = typeof AudioContext

interface AudioContextGlobal {
  AudioContext?: AudioContextConstructor
  webkitAudioContext?: AudioContextConstructor
}

let cachedCapabilities: null | PlaybackCapabilitiesInput = null

interface AudioSupport {
  supportsAac: boolean
  supportsFlac: boolean
  supportsHeAac: boolean
  supportsMp3: boolean
  supportsOpus: boolean
  videoAudioCodecs: string[]
  webmAudioCodecs: string[]
}

interface VideoSupport {
  primaryVideoCodec: string
  supportsHls: boolean
  videoCodecs: string[]
  webmVideoCodecs: string[]
}

export function buildPlaybackCapabilityInput(
  version?: null | number,
): PlaybackCapabilityInput {
  const device = buildDeviceMetadata()
  const capabilities = detectCapabilities()

  return {
    capabilities,
    deviceId: device.identifier,
    name: device.name,
    version: version ?? undefined,
  }
}

export function resetCachedCapabilities(): void {
  cachedCapabilities = null
}

function baselineCapabilities(): PlaybackCapabilitiesInput {
  return {
    codecProfiles: [],
    containerProfiles: [],
    directPlayProfiles: [
      {
        audioCodec: 'aac,mp3',
        container: 'mp4,m4v',
        type: 'Video',
        videoCodec: 'h264',
      },
      {
        audioCodec: 'mp3',
        container: 'mp3',
        type: 'Audio',
      },
    ],
    maxStaticBitrate: 100_000_000,
    maxStreamingBitrate: 60_000_000,
    musicStreamingTranscodingBitrate: 384_000,
    responseProfiles: [
      {
        container: 'm4v',
        mimeType: 'video/mp4',
        type: 'Video',
      },
    ],
    subtitleProfiles: [
      {
        format: 'vtt',
        languages: [],
        method: 'External',
      },
    ],
    transcodingProfiles: [
      {
        applyConditions: [],
        audioCodec: 'aac,mp3',
        container: 'mp4',
        context: 'Streaming',
        maxAudioChannels: '2',
        protocol: 'hls',
        type: 'Video',
        videoCodec: 'h264',
      },
      {
        applyConditions: [],
        audioCodec: 'mp3',
        container: 'mp3',
        context: 'Streaming',
        protocol: 'http',
        type: 'Audio',
      },
    ],
  }
}

function buildAudioChannelCondition(
  maxChannels: number,
): ProfileConditionInput {
  return {
    condition: 'LessThanEqual',
    isRequired: false,
    isRequiredForTranscoding: false,
    property: 'AudioChannels',
    value: String(maxChannels),
  }
}

function buildCodecProfiles(
  maxAudioChannels: number,
): PlaybackCapabilitiesInput['codecProfiles'] {
  if (maxAudioChannels <= 0) return []

  const condition = buildAudioChannelCondition(maxAudioChannels)
  return [
    {
      conditions: [condition],
      type: 'VideoAudio',
    },
    {
      conditions: [condition],
      type: 'Audio',
    },
  ]
}

function buildDirectPlayProfiles(
  videoSupport: VideoSupport,
  audioSupport: AudioSupport,
): PlaybackCapabilitiesInput['directPlayProfiles'] {
  const videoProfiles: DirectPlayProfileInput[] = []

  if (videoSupport.videoCodecs.length) {
    videoProfiles.push({
      audioCodec: audioSupport.videoAudioCodecs.join(',') || undefined,
      container: 'mp4,m4v',
      type: 'Video',
      videoCodec: videoSupport.videoCodecs.join(','),
    })
  }

  if (videoSupport.webmVideoCodecs.length) {
    videoProfiles.push({
      audioCodec: audioSupport.webmAudioCodecs.join(',') || undefined,
      container: 'webm',
      type: 'Video',
      videoCodec: videoSupport.webmVideoCodecs.join(','),
    })
  }

  if (videoSupport.supportsHls && videoSupport.videoCodecs.length) {
    videoProfiles.push({
      audioCodec: audioSupport.videoAudioCodecs.join(',') || undefined,
      container: 'hls',
      type: 'Video',
      videoCodec: videoSupport.videoCodecs.join(','),
    })
  }

  const audioProfiles: DirectPlayProfileInput[] = []

  if (audioSupport.supportsMp3) {
    audioProfiles.push({
      audioCodec: 'mp3',
      container: 'mp3',
      type: 'Audio',
    })
  }

  if (audioSupport.supportsAac || audioSupport.supportsHeAac) {
    audioProfiles.push({
      audioCodec: 'aac',
      container: 'aac,m4a',
      type: 'Audio',
    })
  }

  if (audioSupport.supportsFlac) {
    audioProfiles.push({
      audioCodec: 'flac',
      container: 'flac',
      type: 'Audio',
    })
  }

  if (audioSupport.supportsOpus) {
    audioProfiles.push({
      audioCodec: 'opus',
      container: 'ogg,webm',
      type: 'Audio',
    })
  }

  return [...videoProfiles, ...audioProfiles]
}

function buildTranscodingProfiles(
  videoSupport: VideoSupport,
  audioSupport: AudioSupport,
  maxAudioChannels: number,
): PlaybackCapabilitiesInput['transcodingProfiles'] {
  const audioCodecList = audioSupport.videoAudioCodecs.length
    ? audioSupport.videoAudioCodecs.join(',')
    : 'aac,mp3'

  const maxAudioChannelsValue =
    maxAudioChannels > 0 ? String(maxAudioChannels) : undefined

  const audioChannelCondition =
    maxAudioChannelsValue === undefined
      ? []
      : [buildAudioChannelCondition(maxAudioChannels)]

  const videoProfile: PlaybackCapabilitiesInput['transcodingProfiles'][number] =
    {
      applyConditions: audioChannelCondition,
      audioCodec: audioCodecList,
      container: 'mp4',
      context: 'Streaming',
      maxAudioChannels: maxAudioChannelsValue,
      protocol: videoSupport.supportsHls ? 'hls' : 'http',
      type: 'Video',
      videoCodec: videoSupport.primaryVideoCodec,
    }

  const audioProfile: PlaybackCapabilitiesInput['transcodingProfiles'][number] =
    {
      applyConditions: [],
      audioCodec: 'mp3',
      container: 'mp3',
      context: 'Streaming',
      protocol: 'http',
      type: 'Audio',
    }

  return [videoProfile, audioProfile]
}

function canPlay(element: HTMLMediaElement | null, mime: string): boolean {
  if (!element || typeof element.canPlayType !== 'function') return false

  const result = element.canPlayType(mime)
  return Boolean(result && result.replace(/no/, ''))
}

function createMediaElement(kind: 'audio' | 'video'): HTMLMediaElement | null {
  if (typeof document === 'undefined') return null

  return document.createElement(kind)
}

function detectAudioSupport(audio: HTMLMediaElement): AudioSupport {
  const supportsMp3 = canPlay(audio, 'audio/mpeg')
  const supportsAac = canPlay(audio, 'audio/mp4; codecs="mp4a.40.2"')
  const supportsHeAac = canPlay(audio, 'audio/mp4; codecs="mp4a.40.5"')
  const supportsOpus =
    canPlay(audio, 'audio/ogg; codecs="opus"') ||
    canPlay(audio, 'audio/webm; codecs="opus"')
  const supportsFlac =
    canPlay(audio, 'audio/flac') ||
    canPlay(audio, 'audio/x-flac') ||
    canPlay(audio, 'audio/mp4; codecs="flac"')

  const videoAudioCodecs = [
    ...(supportsAac ? ['aac'] : []),
    ...(supportsHeAac ? ['aac'] : []),
    ...(supportsMp3 ? ['mp3'] : []),
    ...(supportsOpus ? ['opus'] : []),
    ...(supportsFlac ? ['flac'] : []),
  ]

  const webmAudioCodecs = supportsOpus ? ['opus'] : []

  return {
    supportsAac,
    supportsFlac,
    supportsHeAac,
    supportsMp3,
    supportsOpus,
    videoAudioCodecs,
    webmAudioCodecs,
  }
}

function detectCapabilities(): PlaybackCapabilitiesInput {
  if (cachedCapabilities) return cachedCapabilities

  const video = createMediaElement('video')
  const audio = createMediaElement('audio')

  if (!video || !audio) {
    cachedCapabilities = baselineCapabilities()
    return cachedCapabilities
  }

  const videoSupport = detectVideoSupport(video)
  const audioSupport = detectAudioSupport(audio)
  const maxAudioChannels = detectMaxAudioChannels()

  cachedCapabilities = {
    codecProfiles: buildCodecProfiles(maxAudioChannels),
    containerProfiles: [],
    directPlayProfiles: buildDirectPlayProfiles(videoSupport, audioSupport),
    maxStaticBitrate: 100_000_000,
    maxStreamingBitrate: 60_000_000,
    musicStreamingTranscodingBitrate: 384_000,
    responseProfiles: [
      {
        container: 'm4v',
        mimeType: 'video/mp4',
        type: 'Video',
      },
    ],
    subtitleProfiles: [
      {
        format: 'vtt',
        languages: [],
        method: 'External',
      },
    ],
    transcodingProfiles: buildTranscodingProfiles(
      videoSupport,
      audioSupport,
      maxAudioChannels,
    ),
  }

  return cachedCapabilities
}

function detectMaxAudioChannels(): number {
  const maybeGlobal = globalThis as AudioContextGlobal
  const audioContextCtor =
    maybeGlobal.AudioContext ?? maybeGlobal.webkitAudioContext

  if (!audioContextCtor) return 2

  try {
    const ctx = new audioContextCtor()
    const channels = ctx.destination.maxChannelCount
    void ctx.close()
    return channels > 0 ? channels : 2
  } catch {
    return 2
  }
}

function detectVideoSupport(video: HTMLMediaElement): VideoSupport {
  const supportsH264 = canPlay(
    video,
    'video/mp4; codecs="avc1.42E01E, mp4a.40.2"',
  )
  const supportsHevc = canPlay(video, 'video/mp4; codecs="hvc1.1.6.L93.B0"')
  const supportsAv1 = canPlay(video, 'video/mp4; codecs="av01.0.05M.08"')
  const supportsVp9 = canPlay(video, 'video/webm; codecs="vp9"')
  const supportsVp8 = canPlay(video, 'video/webm; codecs="vp8"')
  const supportsHls =
    canPlay(video, 'application/vnd.apple.mpegURL') ||
    canPlay(video, 'application/x-mpegURL')

  const videoCodecs = [
    ...(supportsH264 ? ['h264'] : []),
    ...(supportsHevc ? ['hevc'] : []),
    ...(supportsAv1 ? ['av1'] : []),
    ...(supportsVp9 ? ['vp9'] : []),
  ]

  const webmVideoCodecs = [
    ...(supportsAv1 ? ['av1'] : []),
    ...(supportsVp9 ? ['vp9'] : []),
    ...(supportsVp8 ? ['vp8'] : []),
  ]

  const primaryVideoCodec = supportsH264 ? 'h264' : (videoCodecs[0] ?? 'h264')

  return { primaryVideoCodec, supportsHls, videoCodecs, webmVideoCodecs }
}
