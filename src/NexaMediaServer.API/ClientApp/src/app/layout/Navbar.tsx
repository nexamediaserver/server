import { Link } from '@tanstack/react-router'

import { useAuth } from '@/features/auth'
import { Button } from '@/shared/components/ui/button'
import { SidebarTrigger } from '@/shared/components/ui/sidebar'
import { useSidebar } from '@/shared/hooks'

export function Navbar() {
  const { logout } = useAuth()
  const { toggleSidebar } = useSidebar()

  return (
    <header
      className={`
        sticky top-0 z-30 flex h-14 items-center justify-between gap-3 border-b
        border-border px-3
        md:px-4
        dark:bg-stone-800
      `}
    >
      <div className="flex items-center gap-2">
        {/* Mobile sidebar trigger */}
        <div className="md:hidden">
          <SidebarTrigger
            onClick={() => {
              toggleSidebar()
            }}
          />
        </div>
        <Link className="px-1 pb-1 text-3xl font-bold" to="/">
          ne<span className="font-black text-purple-500">x</span>a
        </Link>
      </div>
      <div className="flex items-center gap-3">
        <Button
          onClick={() => {
            void logout()
          }}
          size="sm"
          variant="outline"
        >
          Sign out
        </Button>
      </div>
    </header>
  )
}
