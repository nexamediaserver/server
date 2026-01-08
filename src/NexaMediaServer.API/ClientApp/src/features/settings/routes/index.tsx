import { type AnyRoute, createRoute, redirect } from '@tanstack/react-router'

import { SettingsLayout } from '../components/SettingsLayout'
import { DetailFieldLayoutPage } from '../pages/DetailFieldLayoutPage'
import { FieldConfigurationPage } from '../pages/FieldConfigurationPage'
import { GeneralSettingsPage } from '../pages/GeneralSettingsPage'
import { GenreMappingsPage } from '../pages/GenreMappingsPage'
import { HubConfigurationPage } from '../pages/HubConfigurationPage'
import { TagModerationPage } from '../pages/TagModerationPage'
import { TranscodingSettingsPage } from '../pages/TranscodingSettingsPage'

let adminRouteRef: AnyRoute | null = null

// Settings layout route
export const settingsLayoutRoute = createRoute({
  component: SettingsLayout,
  getParentRoute: () => {
    if (!adminRouteRef) {
      throw new Error('settingsLayoutRoute parent route not registered yet')
    }
    return adminRouteRef
  },
  path: 'settings',
})

// Redirect /settings to /settings/general
export const settingsIndexRoute = createRoute({
  beforeLoad: () => {
    throw redirect({ to: '/settings/general' })
  },
  getParentRoute: () => settingsLayoutRoute,
  path: '/',
})

// General settings page
export const generalSettingsRoute = createRoute({
  component: GeneralSettingsPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'general',
})

// Transcoding settings page
export const transcodingSettingsRoute = createRoute({
  component: TranscodingSettingsPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'transcoding',
})

// Field configuration settings page
export const fieldConfigurationRoute = createRoute({
  component: FieldConfigurationPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'fields',
})

export const hubConfigurationRoute = createRoute({
  component: HubConfigurationPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'hubs',
})

export const detailFieldLayoutRoute = createRoute({
  component: DetailFieldLayoutPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'field-layouts',
})

export const tagModerationRoute = createRoute({
  component: TagModerationPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'tags',
})

export const genreMappingsRoute = createRoute({
  component: GenreMappingsPage,
  getParentRoute: () => settingsLayoutRoute,
  path: 'genres',
})

export function registerSettingsRoutes({
  adminRoute,
}: {
  adminRoute: AnyRoute
}) {
  adminRouteRef = adminRoute
  return [
    settingsLayoutRoute.addChildren([
      settingsIndexRoute,
      generalSettingsRoute,
      transcodingSettingsRoute,
      fieldConfigurationRoute,
      hubConfigurationRoute,
      detailFieldLayoutRoute,
      tagModerationRoute,
      genreMappingsRoute,
    ]),
  ]
}
