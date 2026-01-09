import { useState, useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { SignalRProvider } from './contexts/SignalRContext'
import { MatchProvider } from './contexts/MatchContext'
import { useSignalREvents } from './hooks/useSignalREvents'
import { Layout } from './components/layout/Layout'
import { LoginModal } from './components/modals/LoginModal'
import { RegisterModal } from './components/modals/RegisterModal'
import { HomePage } from './pages/HomePage'
import { GamePage } from './pages/GamePage'
import { ProfilePage } from './pages/ProfilePage'
import { MatchResultsPage } from './pages/MatchResultsPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { NotFoundPage } from './pages/NotFoundPage'
import { Toaster } from './components/ui/toaster'
import { audioService } from './services/audio.service'

// Component that registers SignalR events
function SignalREventHandler() {
  useSignalREvents()
  return null
}

// Component that handles modals
function AppContent() {
  const [showLoginModal, setShowLoginModal] = useState(false)
  const [showRegisterModal, setShowRegisterModal] = useState(false)

  // Initialize audio service and dark mode
  useEffect(() => {
    audioService.init()
    document.documentElement.classList.add('dark')
  }, [])

  const handleLoginClick = () => {
    setShowLoginModal(true)
  }

  const handleSignupClick = () => {
    setShowRegisterModal(true)
  }

  const handleSwitchToSignup = () => {
    setShowLoginModal(false)
    setShowRegisterModal(true)
  }

  const handleSwitchToLogin = () => {
    setShowRegisterModal(false)
    setShowLoginModal(true)
  }

  // Game creation handlers - will be added to HomePage in Phase 3

  return (
    <>
      <SignalREventHandler />
      <Layout onLoginClick={handleLoginClick} onSignupClick={handleSignupClick}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/game/:gameId" element={<GamePage />} />
          <Route path="/profile/:username" element={<ProfilePage />} />
          <Route path="/match-results/:matchId" element={<MatchResultsPage />} />
          <Route path="/analysis/:sgf?" element={<AnalysisPage />} />
          <Route path="*" element={<NotFoundPage />} />
        </Routes>
      </Layout>

      <LoginModal
        isOpen={showLoginModal}
        onClose={() => setShowLoginModal(false)}
        onSwitchToSignup={handleSwitchToSignup}
      />

      <RegisterModal
        isOpen={showRegisterModal}
        onClose={() => setShowRegisterModal(false)}
        onSwitchToLogin={handleSwitchToLogin}
      />

      <Toaster />
    </>
  )
}

function App() {
  return (
    <AuthProvider>
      <SignalRProvider>
        <MatchProvider>
          <Router>
            <AppContent />
          </Router>
        </MatchProvider>
      </SignalRProvider>
    </AuthProvider>
  )
}

export default App
