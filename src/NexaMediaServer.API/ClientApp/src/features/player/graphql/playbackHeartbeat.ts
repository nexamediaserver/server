import { graphql } from '@/shared/api/graphql'

export const PlaybackHeartbeatDocument = graphql(`
  mutation PlaybackHeartbeat($input: PlaybackHeartbeatInput!) {
    playbackHeartbeat(input: $input) {
      playbackSessionId
      capabilityProfileVersion
      capabilityVersionMismatch
    }
  }
`)
