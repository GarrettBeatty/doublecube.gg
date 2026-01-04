import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useSignalR } from '@/contexts/SignalRContext'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import { ConnectionState } from '@/types/signalr.types'
import { Gamepad2, Trophy, Users, Settings } from 'lucide-react'

interface HeaderProps {
  onLoginClick: () => void
  onSignupClick: () => void
  onDebugToggle?: () => void
}

export const Header: React.FC<HeaderProps> = ({
  onLoginClick,
  onSignupClick,
}) => {
  const navigate = useNavigate()
  const { user, isAuthenticated, logout } = useAuth()
  const { connectionState } = useSignalR()

  const handleLogoClick = () => {
    navigate('/')
  }

  const handleProfileClick = () => {
    if (user?.username) {
      navigate(`/profile/${user.username}`)
    }
  }

  const handleLogout = () => {
    logout()
    navigate('/')
  }

  const getConnectionBadge = () => {
    switch (connectionState) {
      case ConnectionState.Connected:
        return (
          <Badge variant="default" className="bg-green-500 hover:bg-green-600">
            Connected
          </Badge>
        )
      case ConnectionState.Connecting:
        return <Badge variant="secondary">Connecting...</Badge>
      case ConnectionState.Reconnecting:
        return <Badge variant="secondary">Reconnecting...</Badge>
      case ConnectionState.Failed:
        return <Badge variant="destructive">Disconnected</Badge>
      default:
        return <Badge variant="secondary">Disconnected</Badge>
    }
  }

  const getUserInitials = () => {
    if (!user?.username) return '??'
    return user.username.slice(0, 2).toUpperCase()
  }

  return (
    <header className="border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 sticky top-0 z-50">
      <div className="container mx-auto px-4 py-4">
        <div className="flex items-center justify-between">
          {/* Left: Logo + Branding */}
          <div
            className="flex items-center gap-2 cursor-pointer"
            onClick={handleLogoClick}
          >
            <Gamepad2 className="h-8 w-8 text-primary" />
            <h1 className="text-2xl font-bold">doublecube.gg</h1>
          </div>

          {/* Center: Navigation Menu (placeholders) */}
          <nav className="hidden md:flex items-center gap-6">
            <Button variant="ghost" className="flex items-center gap-2">
              <Gamepad2 className="h-4 w-4" />
              Play
            </Button>
            <Button variant="ghost" className="flex items-center gap-2">
              <Trophy className="h-4 w-4" />
              Tournaments
            </Button>
            <Button variant="ghost" className="flex items-center gap-2">
              <Users className="h-4 w-4" />
              Community
            </Button>
          </nav>

          {/* Right: Connection + Settings + Auth */}
          <div className="flex items-center gap-2">
            {/* Connection Indicator */}
            {getConnectionBadge()}

            {/* Settings Icon */}
            <Button variant="ghost" size="icon" title="Settings (coming soon)">
              <Settings className="h-5 w-5" />
            </Button>

            {/* Auth Section */}
            {!isAuthenticated ? (
              <>
                <Button variant="outline" onClick={onLoginClick}>
                  Sign In
                </Button>
                <Button onClick={onSignupClick}>Sign Up</Button>
              </>
            ) : (
              <DropdownMenu>
                <DropdownMenuTrigger asChild>
                  <Button variant="ghost" className="flex items-center gap-2">
                    <Avatar className="h-8 w-8">
                      <AvatarFallback>{getUserInitials()}</AvatarFallback>
                    </Avatar>
                    <span className="hidden sm:inline">{user?.username}</span>
                  </Button>
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={handleProfileClick}>
                    Profile
                  </DropdownMenuItem>
                  <DropdownMenuItem onClick={handleLogout}>
                    Logout
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            )}
          </div>
        </div>
      </div>
    </header>
  )
}
