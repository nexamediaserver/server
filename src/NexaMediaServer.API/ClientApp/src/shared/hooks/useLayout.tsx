import {
  createContext,
  type DependencyList,
  type ReactNode,
  useContext,
  useEffect,
} from 'react'

export interface LayoutContextValue {
  setSlot: (name: string, node: null | ReactNode) => void
  slots: Record<string, ReactNode>
}

// LayoutContext allows child routes to register named slot content (e.g., header, footer, sidebar)
// The layout component renders the collected slots around <Outlet />.
export const LayoutContext = createContext<LayoutContextValue | null>(null)

export function useLayout() {
  const ctx = useContext(LayoutContext)
  if (!ctx) throw new Error('useLayout must be used within a LayoutProvider')
  return ctx
}

export function useLayoutSlot(
  name: string,
  node: ReactNode,
  deps: DependencyList = [],
) {
  const { setSlot } = useLayout()
  useEffect(() => {
    setSlot(name, node)
    return () => {
      setSlot(name, null)
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps -- Intended
  }, [name, node, setSlot, ...deps])
}
