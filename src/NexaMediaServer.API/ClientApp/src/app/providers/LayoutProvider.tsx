import {
  type PropsWithChildren,
  type ReactNode,
  useCallback,
  useMemo,
  useState,
} from 'react'

import { LayoutContext, type LayoutContextValue } from '@/shared/hooks'

/**
 * Provides a context for managing named layout slots within the application.
 *
 * The `LayoutProvider` component maintains a record of slot names mapped to their corresponding React nodes,
 * allowing child components to register or remove content for specific layout slots dynamically.
 * It exposes the current slots and a `setSlot` function via the `LayoutContext`.
 *
 * @param children - The React children to be rendered within the provider.
 * @returns A context provider that supplies slot management functionality to its descendants.
 */
export function LayoutProvider({ children }: PropsWithChildren) {
  const [slots, setSlots] = useState<Record<string, ReactNode>>({})

  const setSlot = useCallback<LayoutContextValue['setSlot']>((name, node) => {
    setSlots((prev) => {
      if (node == null) {
        if (!(name in prev)) return prev
        // Omit removed slot name from the record by filtering entries
        return Object.fromEntries(
          Object.entries(prev).filter(([k]) => k !== name),
        )
      }
      // Overwrite / register slot - only update if different
      if (prev[name] === node) return prev
      return { ...prev, [name]: node }
    })
  }, [])

  // Memoize context value to prevent unnecessary re-renders
  const contextValue = useMemo<LayoutContextValue>(
    () => ({ setSlot, slots }),
    [setSlot, slots],
  )

  return (
    <LayoutContext.Provider value={contextValue}>
      {children}
    </LayoutContext.Provider>
  )
}
