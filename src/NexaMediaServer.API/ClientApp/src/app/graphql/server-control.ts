import { graphql } from '@/shared/api/graphql'

export const restartServerDocument = graphql(`
  mutation RestartServer {
    restartServer
  }
`)
