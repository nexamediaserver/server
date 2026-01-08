import { useEffect, useRef, useState } from 'react'

export interface UseConfigurationDefaultsOptions<TType> {
  /**
   * Available options/items that can be enabled or disabled
   */
  availableOptions: TType[]

  /**
   * Configuration data from the backend
   * - enabled: items that are explicitly enabled
   * - disabled: items that are explicitly disabled
   * - undefined: still loading
   * - null: no configuration exists (use defaults)
   */
  configData:
    | null
    | undefined
    | {
        disabled: TType[]
        enabled: TType[]
      }

  /**
   * Optional: Custom comparator function to check if two items are equal
   * Default: (a, b) => a === b (reference equality)
   */
  isEqual?: (a: TType, b: TType) => boolean

  /**
   * Dependencies that should trigger a reset of enabled/disabled state
   * e.g., [context, metadataType, libraryId]
   */
  resetDependencies: React.DependencyList

  /**
   * Optional: Whether to track hidden items (items in config but not in availableOptions)
   * Default: false
   */
  trackHidden?: boolean
}

export interface UseConfigurationDefaultsResult<TType> {
  /** Items that are currently disabled - null during loading */
  disabled: null | TType[]

  /** Items that are currently enabled (visible + new items by default) - null during loading */
  enabled: null | TType[]

  /** Items in config.disabled but not in availableOptions (only if trackHidden=true) */
  hiddenDisabled: TType[]

  /** Items in config.enabled but not in availableOptions (only if trackHidden=true) */
  hiddenEnabled: TType[]

  /** Setters for state */
  setDisabled: React.Dispatch<React.SetStateAction<null | TType[]>>
  setEnabled: React.Dispatch<React.SetStateAction<null | TType[]>>
  setHiddenDisabled: React.Dispatch<React.SetStateAction<TType[]>>
  setHiddenEnabled: React.Dispatch<React.SetStateAction<TType[]>>
}

/**
 * Hook for managing configuration defaults with enabled/disabled state.
 * Syncs backend config with available options and handles missing items.
 *
 * When configData is:
 * - undefined: Returns null arrays (loading state)
 * - null: All availableOptions are enabled by default (no config exists)
 * - object: Syncs with backend config and merges new options as enabled
 *
 * @template TType - The type of items being configured (HubType, FieldDescriptor, etc.)
 */
export function useConfigurationDefaults<TType>(
  options: UseConfigurationDefaultsOptions<TType>,
): UseConfigurationDefaultsResult<TType> {
  const {
    availableOptions,
    configData,
    isEqual = (a, b) => a === b,
    resetDependencies,
    trackHidden = false,
  } = options

  // Store isEqual in a ref to prevent it from triggering effect re-runs
  const isEqualRef = useRef(isEqual)
  useEffect(() => {
    isEqualRef.current = isEqual
  }, [isEqual])

  const [enabled, setEnabled] = useState<null | TType[]>(null)
  const [disabled, setDisabled] = useState<null | TType[]>(null)
  const [hiddenEnabled, setHiddenEnabled] = useState<TType[]>([])
  const [hiddenDisabled, setHiddenDisabled] = useState<TType[]>([])

  // Reset state when dependencies change
  useEffect(() => {
    setEnabled(null)
    setDisabled(null)
    setHiddenEnabled([])
    setHiddenDisabled([])
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, resetDependencies)

  // Sync state when config data or available options change
  useEffect(() => {
    // Still loading
    if (configData === undefined) {
      return
    }

    // No configuration exists - use all available options as enabled
    if (configData === null) {
      setEnabled([...availableOptions])
      setDisabled([])
      setHiddenEnabled([])
      setHiddenDisabled([])
      return
    }

    // Configuration exists - sync with backend
    const { disabled: configDisabled, enabled: configEnabled } = configData

    // Filter items that are known (exist in availableOptions)
    const knownEnabled = configEnabled.filter((configItem) =>
      availableOptions.some((availItem) =>
        isEqualRef.current(availItem, configItem),
      ),
    )

    const knownDisabled = configDisabled.filter((configItem) =>
      availableOptions.some((availItem) =>
        isEqualRef.current(availItem, configItem),
      ),
    )

    // Track hidden items if requested
    if (trackHidden) {
      const unseenEnabled = configEnabled.filter(
        (configItem) =>
          !availableOptions.some((availItem) =>
            isEqualRef.current(availItem, configItem),
          ),
      )

      const unseenDisabled = configDisabled.filter(
        (configItem) =>
          !availableOptions.some((availItem) =>
            isEqualRef.current(availItem, configItem),
          ),
      )

      setHiddenEnabled(unseenEnabled)
      setHiddenDisabled(unseenDisabled)
    }

    // Find items that are new (not in config yet)
    const missing = availableOptions.filter(
      (availItem) =>
        !knownEnabled.some((configItem) =>
          isEqualRef.current(availItem, configItem),
        ) &&
        !knownDisabled.some((configItem) =>
          isEqualRef.current(availItem, configItem),
        ),
    )

    // Add missing items to enabled by default
    setEnabled([...knownEnabled, ...missing])
    setDisabled(knownDisabled)
  }, [configData, availableOptions, trackHidden])

  return {
    disabled,
    enabled,
    hiddenDisabled,
    hiddenEnabled,
    setDisabled,
    setEnabled,
    setHiddenDisabled,
    setHiddenEnabled,
  }
}
