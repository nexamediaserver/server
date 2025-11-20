import { createContext, useContext } from 'react'

export interface SidebarContextProps {
  isMobile: boolean
  open: boolean
  openMobile: boolean
  setOpen: (open: boolean) => void
  setOpenMobile: (open: boolean) => void
  state: 'collapsed' | 'expanded'
  toggleSidebar: () => void
}

export const SidebarContext = createContext<null | SidebarContextProps>(null)

export function useSidebar() {
  const ctx = useContext(SidebarContext)
  if (!ctx) throw new Error('useSidebar must be used within a SidebarProvider')
  return ctx
}
