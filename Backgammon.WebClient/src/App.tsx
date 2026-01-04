import { useState, useEffect } from 'react'
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom'
import { AuthProvider } from './contexts/AuthContext'
import { SignalRProvider, useSignalR } from './contexts/SignalRContext'
import { MatchProvider } from './contexts/MatchContext'
import { useSignalREvents } from './hooks/useSignalREvents'
import { HubMethods } from './types/signalr.types'
import { Layout } from './components/layout/Layout'
import { LoginModal } from './components/modals/LoginModal'
import { RegisterModal } from './components/modals/RegisterModal'
import { CreateMatchModal } from './components/modals/CreateMatchModal'
import { GamePage } from './pages/GamePage'
import { ProfilePage } from './pages/ProfilePage'
import { MatchLobbyPage } from './pages/MatchLobbyPage'
import { MatchResultsPage } from './pages/MatchResultsPage'
import { Toaster } from './components/ui/toaster'
import { audioService } from './services/audio.service'
import { useToast } from './hooks/use-toast'
import { Plus, Bot, BarChart3 } from 'lucide-react'

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
  const { invoke, isConnected } = useSignalR()
  const { toast } = useToast()

  // Initialize audio service
  useEffect(() => {
    audioService.init()
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

  // Game creation handlers
  const [matchModalOpponentType, setMatchModalOpponentType] = useState<'AI' | 'OpenLobby'>('AI')

  const handleCreateGame = () => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return
    }
    setMatchModalOpponentType('OpenLobby')
    setShowCreateMatchModal(true)
  }

  const handlePlayVsComputer = () => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return
    }
    setMatchModalOpponentType('AI')
    setShowCreateMatchModal(true)
  }

  const handleCreateAnalysis = async () => {
    if (!isConnected) {
      toast({
        title: 'Not connected',
        description: 'Please wait for connection to server...',
        variant: 'destructive',
      })
      return
    }

    try {
      await invoke(HubMethods.CreateAnalysisGame)
      // Navigation will happen automatically when GameStart event is received
    } catch (error) {
      console.error('Failed to create analysis game:', error)
      toast({
        title: 'Error',
        description: 'Failed to create analysis game. Please try again.',
        variant: 'destructive',
      })
    }
  }

  return (
    <>
      <SignalREventHandler />
      <Layout onLoginClick={handleLoginClick} onSignupClick={handleSignupClick}>
        <Routes>
          <Route
            path="/"
            element={
              <div className="min-h-screen bg-background">
                <div className="max-w-5xl mx-auto px-4 py-8">
                  <div className="bg-card border rounded-lg shadow-lg p-8">
                    <h1 className="text-4xl font-bold mb-4">Backgammon Online</h1>
                    <p className="text-muted-foreground mb-8">
                      React + TypeScript + Vite + shadcn/ui migration in progress...
                    </p>

                    <div className="space-y-2 mb-8">
                      <p className="text-sm">✅ Phase 1 Complete: Foundation</p>
                      <p className="text-sm">✅ Phase 2 Complete: State Management</p>
                      <p className="text-sm">✅ Phase 3 Complete: UI Components</p>
                      <p className="text-sm">✅ Phase 4 Complete: Game Components</p>
                      <p className="text-sm">⏳ Phase 5: Pages & Integration (Next)</p>
                    </div>

                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                      <button
                        onClick={handlePlayVsComputer}
                        className="bg-primary hover:bg-primary/90 text-primary-foreground font-semibold py-6 px-4 rounded-lg transition-colors disabled:opacity-50"
                        disabled={!isConnected}
                      >
                        <div className="mb-2">
                          <Bot className="h-8 w-8 mx-auto" />
                        </div>
                        <div className="text-lg">Play vs Computer</div>
                        <div className="text-sm opacity-70">Single player</div>
                      </button>
                      <button
                        onClick={handleCreateGame}
                        className="bg-primary hover:bg-primary/90 text-primary-foreground font-semibold py-6 px-4 rounded-lg transition-colors disabled:opacity-50"
                        disabled={!isConnected}
                      >
                        <div className="mb-2">
                          <Plus className="h-8 w-8 mx-auto" />
                        </div>
                        <div className="text-lg">Play Online</div>
                        <div className="text-sm opacity-70">Multiplayer</div>
                      </button>
                      <button
                        onClick={handleCreateAnalysis}
                        className="bg-secondary hover:bg-secondary/90 text-secondary-foreground font-semibold py-6 px-4 rounded-lg transition-colors disabled:opacity-50"
                        disabled={!isConnected}
                      >
                        <div className="mb-2">
                          <BarChart3 className="h-8 w-8 mx-auto" />
                        </div>
                        <div className="text-lg">Analysis Mode</div>
                        <div className="text-sm opacity-70">Practice & test positions</div>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            }
          />
          <Route path="/game/:gameId" element={<GamePage />} />
          <Route path="/profile/:username" element={<ProfilePage />} />
          <Route path="/match-lobby/:matchId" element={<MatchLobbyPage />} />
          <Route path="/match-results/:matchId" element={<MatchResultsPage />} />
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
        defaultOpponentType={matchModalOpponentType}
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
