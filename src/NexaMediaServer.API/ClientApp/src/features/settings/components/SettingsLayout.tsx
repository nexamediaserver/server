import { Outlet } from '@tanstack/react-router'

import { SettingsSidebar } from './SettingsSidebar'

export function SettingsLayout() {
  return (
    <>
      <SettingsSidebar />
      <div className="p-6">
        <Outlet />
      </div>
    </>
  )
}
