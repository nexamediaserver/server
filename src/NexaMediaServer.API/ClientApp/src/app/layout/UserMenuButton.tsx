import { Link } from '@tanstack/react-router'
import IconLogout from '~icons/material-symbols/logout'
import IconPerson from '~icons/material-symbols/person'
import IconSettings from '~icons/material-symbols/settings'

import { useAuth, useIsAdmin } from '@/features/auth'
import { Button } from '@/shared/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/shared/components/ui/dropdown-menu'

export function UserMenuButton() {
  const { logout, user } = useAuth()
  const isAdmin = useIsAdmin()

  const handleLogout = () => {
    void logout()
  }

  const initials = getInitials(user)

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          className={`
            relative size-8 rounded-full bg-purple-600 text-xs font-semibold
            text-white
            hover:bg-purple-700
          `}
          size="icon"
          variant="ghost"
        >
          {initials}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col space-y-1">
            <p className="text-sm leading-none font-medium">
              {user?.username ?? 'User'}
            </p>
            {user?.email && (
              <p className="text-xs leading-none text-muted-foreground">
                {user.email}
              </p>
            )}
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem>
          <IconPerson className="mr-2 size-4" />
          Profile
        </DropdownMenuItem>
        {isAdmin && (
          <DropdownMenuItem asChild>
            <Link to="/settings/general">
              <IconSettings className="mr-2 size-4" />
              Server Settings
            </Link>
          </DropdownMenuItem>
        )}
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={handleLogout}>
          <IconLogout className="mr-2 size-4" />
          Sign out
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

function getInitials(
  user: null | { email?: string; username: string },
): string {
  if (user?.username) {
    return user.username.slice(0, 2).toUpperCase()
  }
  if (user?.email) {
    return user.email.slice(0, 2).toUpperCase()
  }
  return '??'
}
