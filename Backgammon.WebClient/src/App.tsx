import { useState, useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { SignalRProvider } from './contexts/SignalRContext'
import { MatchProvider } from './contexts/MatchContext'
import { useSignalREvents } from './hooks/useSignalREvents'
import { Layout } from './components/layout/Layout'
import { LoginModal } from './components/modals/LoginModal'
import { RegisterModal } from './components/modals/RegisterModal'
import { CreateMatchModal } from './components/modals/CreateMatchModal'
import { HomePage } from './pages/HomePage'
import { GamePage } from './pages/GamePage'
import { MatchResultsPage } from './pages/MatchResultsPage'
import { ProfilePage } from './pages/ProfilePage'
import { PlayersPage } from './pages/PlayersPage'
import { FriendsPage } from './pages/FriendsPage'
import { AnalysisPage } from './pages/AnalysisPage'
import { DailyPuzzlePage } from './pages/DailyPuzzlePage'
import { NotFoundPage } from './pages/NotFoundPage'
import { AboutPage } from './pages/AboutPage'
import { FAQPage } from './pages/FAQPage'
import { ContactPage } from './pages/ContactPage'
import { MobileAppPage } from './pages/MobileAppPage'
import { TermsOfServicePage } from './pages/TermsOfServicePage'
import { PrivacyPage } from './pages/PrivacyPage'
import { SettingsPage } from './pages/SettingsPage'
import { Toaster } from './components/ui/toaster'
import { FriendsWidget } from './components/FriendsWidget'
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
  const [showCreateMatchModal, setShowCreateMatchModal] = useState(false)

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

  const handleCreateLobbyClick = () => {
    setShowCreateMatchModal(true)
  }

  return (
    <>
      <SignalREventHandler />
      <Layout onLoginClick={handleLoginClick} onSignupClick={handleSignupClick} onCreateLobbyClick={handleCreateLobbyClick}>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/match/:matchId/game/:gameId" element={<GamePage />} />
          <Route path="/match/:matchId/results" element={<MatchResultsPage />} />
          <Route path="/profile/:username" element={<ProfilePage />} />
          <Route path="/players" element={<PlayersPage />} />
          <Route path="/friends" element={<FriendsPage />} />
          <Route path="/analysis/game/:gameId" element={<AnalysisPage />} />
          <Route path="/analysis/:sgf?" element={<AnalysisPage />} />
          <Route path="/puzzle" element={<DailyPuzzlePage />} />
          <Route path="/about" element={<AboutPage />} />
          <Route path="/faq" element={<FAQPage />} />
          <Route path="/contact" element={<ContactPage />} />
          <Route path="/mobile-app" element={<MobileAppPage />} />
          <Route path="/terms-of-service" element={<TermsOfServicePage />} />
          <Route path="/privacy" element={<PrivacyPage />} />
          <Route path="/settings" element={<SettingsPage />} />
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

      <CreateMatchModal
        isOpen={showCreateMatchModal}
        onClose={() => setShowCreateMatchModal(false)}
        defaultOpponentType="OpenLobby"
      />

      <Toaster />
      <FriendsWidget />
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
