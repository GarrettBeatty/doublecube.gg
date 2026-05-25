import React, { useState, ReactNode } from 'react'
import { Header } from './Header'
import { DebugPanel } from './DebugPanel'

interface LayoutProps {
  children: ReactNode
  onLoginClick: () => void
  onSignupClick: () => void
}

export const Layout: React.FC<LayoutProps> = ({
  children,
  onLoginClick,
  onSignupClick,
}) => {
  const [showDebugPanel, setShowDebugPanel] = useState(false)

  const handleDebugToggle = () => {
    setShowDebugPanel((prev) => !prev)
  }

  return (
    <div className="min-h-screen bg-background">
      <Header
        onLoginClick={onLoginClick}
        onSignupClick={onSignupClick}
        onDebugToggle={handleDebugToggle}
      />
      <main>{children}</main>
      <DebugPanel isVisible={showDebugPanel} />
    </div>
  )
}
