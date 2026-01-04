import React from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { useSignalR } from '@/contexts/SignalRContext'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { ConnectionState } from '@/types/signalr.types'

interface HeaderProps {
  onLoginClick: () => void
  onSignupClick: () => void
  onDebugToggle: () => void
}

export const Header: React.FC<HeaderProps> = ({
  onLoginClick,
  onSignupClick,
  onDebugToggle,
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

  const getConnectionIndicator = () => {
    switch (connectionState) {
      case ConnectionState.Connected:
        return <Badge variant="default" className="bg-green-500">Connected</Badge>
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

  return (
    <header className="bg-card border-b sticky top-0 z-50 shadow-sm">
      <div className="max-w-7xl mx-auto px-6 py-4 flex items-center justify-between">
        {/* Left: Logo */}
        <div
          className="flex items-center gap-4 cursor-pointer"
          onClick={handleLogoClick}
        >
          <h1 className="text-2xl font-bold text-foreground">Backgammon Online</h1>
        </div>

        {/* Right: Connection + Auth */}
        <div className="flex items-center gap-4">
          {/* Debug Toggle Button */}
          <Button
            variant="ghost"
            size="sm"
            onClick={onDebugToggle}
            title="Toggle Debug Panel"
          >
            🐛
          </Button>

          {/* Connection Indicator */}
          {getConnectionIndicator()}

          {/* Auth Section */}
          {!isAuthenticated ? (
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={onLoginClick}
              >
                Login
              </Button>
              <Button
                variant="default"
                size="sm"
                onClick={onSignupClick}
              >
                Sign Up
              </Button>
            </div>
          ) : (
            <div className="flex items-center gap-2">
              <span className="font-semibold text-foreground">{user?.username}</span>
              <Button
                variant="ghost"
                size="sm"
                onClick={handleProfileClick}
              >
                Profile
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={handleLogout}
              >
                Logout
              </Button>
            </div>
          )}
        </div>
      </div>
    </header>
  )
}
