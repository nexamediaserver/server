import { graphql } from '@/shared/api/graphql'

export const serverSettingsDocument = graphql(`
  query ServerSettings {
    serverSettings {
      serverName
      maxStreamingBitrate
      preferH265
      allowRemuxing
      allowHEVCEncoding
      dashVideoCodec
      dashAudioCodec
      dashSegmentDurationSeconds
      enableToneMapping
      userPreferredAcceleration
      allowedTags
      blockedTags
      genreMappings {
        key
        value
      }
      logLevel
    }
  }
`)

export const updateServerSettingsDocument = graphql(`
  mutation UpdateServerSettings($input: UpdateServerSettingsInput!) {
    updateServerSettings(input: $input) {
      serverName
      maxStreamingBitrate
      preferH265
      allowRemuxing
      allowHEVCEncoding
      dashVideoCodec
      dashAudioCodec
      dashSegmentDurationSeconds
      enableToneMapping
      userPreferredAcceleration
      allowedTags
      blockedTags
      genreMappings {
        key
        value
      }
      logLevel
    }
  }
`)
