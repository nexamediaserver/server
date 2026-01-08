import type { ReactNode } from 'react'

interface SettingsPageHeaderProps {
  /**
   * Optional actions to display on the right side of the header (e.g., buttons, selects)
   */
  actions?: ReactNode
  /**
   * Optional description text below the title
   */
  description?: string
  /**
   * The page title
   */
  title: string
}

/**
 * Unified header component for settings pages.
 * Provides consistent typography and layout across all settings pages.
 */
export function SettingsPageHeader({
  actions,
  description,
  title,
}: SettingsPageHeaderProps) {
  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-semibold tracking-tight">{title}</h1>
      </div>
      {description && (
        <p className="text-sm text-muted-foreground">{description}</p>
      )}
      {actions && (
        <div className="mt-2 flex items-center justify-end gap-2">
          {actions}
        </div>
      )}
    </div>
  )
}
