import React, { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '@/contexts/AuthContext'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Switch } from '@/components/ui/switch'
import { Slider } from '@/components/ui/slider'
import { Separator } from '@/components/ui/separator'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { Settings, Volume2, VolumeX, Bell, Moon, Sun, Shield, User } from 'lucide-react'
import { audioService } from '@/services/audio.service'

interface UserSettings {
  soundEnabled: boolean
  soundVolume: number
  theme: 'dark' | 'light' | 'system'
  notificationsEnabled: boolean
  showOnlineStatus: boolean
  allowFriendRequests: boolean
}

const STORAGE_KEY = 'backgammon_user_settings'

const loadSettings = (): UserSettings => {
  try {
    const stored = localStorage.getItem(STORAGE_KEY)
    if (stored) {
      return JSON.parse(stored)
    }
  } catch (err) {
    console.error('Failed to load settings:', err)
  }

  // Default settings
  return {
    soundEnabled: audioService.isEnabled(),
    soundVolume: audioService.getVolume(),
    theme: 'dark',
    notificationsEnabled: true,
    showOnlineStatus: true,
    allowFriendRequests: true,
  }
}

const saveSettings = (settings: UserSettings): void => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings))
  } catch (err) {
    console.error('Failed to save settings:', err)
  }
}

export const SettingsPage: React.FC = () => {
  const navigate = useNavigate()
  const { user, isAuthenticated } = useAuth()
  const [settings, setSettings] = useState<UserSettings>(loadSettings)
  const [hasChanges, setHasChanges] = useState(false)

  // Sync audio service with settings on mount
  useEffect(() => {
    const currentAudioSettings = audioService.getSettings()
    setSettings((prev) => ({
      ...prev,
      soundEnabled: currentAudioSettings.enabled,
      soundVolume: currentAudioSettings.volume,
    }))
  }, [])

  const updateSetting = <K extends keyof UserSettings>(key: K, value: UserSettings[K]) => {
    setSettings((prev) => {
      const updated = { ...prev, [key]: value }
      saveSettings(updated)
      setHasChanges(true)
      return updated
    })

    // Apply settings immediately
    if (key === 'soundEnabled') {
      audioService.setEnabled(value as boolean)
    } else if (key === 'soundVolume') {
      audioService.setVolume(value as number)
    } else if (key === 'theme') {
      applyTheme(value as 'dark' | 'light' | 'system')
    }
  }

  const applyTheme = (theme: 'dark' | 'light' | 'system') => {
    const root = document.documentElement
    if (theme === 'dark') {
      root.classList.add('dark')
    } else if (theme === 'light') {
      root.classList.remove('dark')
    } else {
      // System preference
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
      if (prefersDark) {
        root.classList.add('dark')
      } else {
        root.classList.remove('dark')
      }
    }
  }

  const handleTestSound = () => {
    audioService.playSound('checker-move')
  }

  if (!isAuthenticated) {
    return (
      <div className="min-h-screen bg-background">
        <div className="max-w-2xl mx-auto px-4 py-8">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Settings className="h-5 w-5" />
                Settings
              </CardTitle>
              <CardDescription>Please sign in to access your settings</CardDescription>
            </CardHeader>
            <CardContent>
              <Button onClick={() => navigate('/')}>Go to Home</Button>
            </CardContent>
          </Card>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen bg-background">
      <div className="max-w-2xl mx-auto px-4 py-8">
        <Button variant="outline" onClick={() => navigate(-1)} className="mb-6">
          &larr; Back
        </Button>

        <div className="space-y-6">
          {/* Header */}
          <div>
            <h1 className="text-3xl font-bold flex items-center gap-2">
              <Settings className="h-8 w-8" />
              Settings
            </h1>
            <p className="text-muted-foreground mt-1">
              Manage your account preferences and application settings
            </p>
          </div>

          {/* Account Section */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-5 w-5" />
                Account
              </CardTitle>
              <CardDescription>Your account information</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex justify-between items-center">
                <div>
                  <Label>Username</Label>
                  <p className="text-sm text-muted-foreground">@{user?.username}</p>
                </div>
              </div>
              <div className="flex justify-between items-center">
                <div>
                  <Label>Email</Label>
                  <p className="text-sm text-muted-foreground">
                    {user?.email || 'Not set'}
                  </p>
                </div>
              </div>
              <Separator />
              <div className="flex justify-between items-center">
                <div>
                  <Label>View Profile</Label>
                  <p className="text-sm text-muted-foreground">
                    See your public profile
                  </p>
                </div>
                <Button
                  variant="outline"
                  onClick={() => navigate(`/profile/${user?.username}`)}
                >
                  View Profile
                </Button>
              </div>
            </CardContent>
          </Card>

          {/* Sound Settings */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {settings.soundEnabled ? (
                  <Volume2 className="h-5 w-5" />
                ) : (
                  <VolumeX className="h-5 w-5" />
                )}
                Sound
              </CardTitle>
              <CardDescription>Configure audio and sound effects</CardDescription>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="sound-enabled">Sound Effects</Label>
                  <p className="text-sm text-muted-foreground">
                    Play sounds for moves, dice rolls, and notifications
                  </p>
                </div>
                <Switch
                  id="sound-enabled"
                  checked={settings.soundEnabled}
                  onCheckedChange={(checked) => updateSetting('soundEnabled', checked)}
                />
              </div>

              <div className="space-y-3">
                <div className="flex items-center justify-between">
                  <Label>Volume</Label>
                  <span className="text-sm text-muted-foreground">
                    {Math.round(settings.soundVolume * 100)}%
                  </span>
                </div>
                <div className="flex items-center gap-4">
                  <Slider
                    value={[settings.soundVolume]}
                    onValueChange={(value) => updateSetting('soundVolume', value[0])}
                    max={1}
                    step={0.05}
                    disabled={!settings.soundEnabled}
                    className="flex-1"
                  />
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={handleTestSound}
                    disabled={!settings.soundEnabled}
                  >
                    Test
                  </Button>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Appearance Settings */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                {settings.theme === 'dark' ? (
                  <Moon className="h-5 w-5" />
                ) : (
                  <Sun className="h-5 w-5" />
                )}
                Appearance
              </CardTitle>
              <CardDescription>Customize how the app looks</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label>Theme</Label>
                  <p className="text-sm text-muted-foreground">
                    Choose your preferred color scheme
                  </p>
                </div>
                <Select
                  value={settings.theme}
                  onValueChange={(value: 'dark' | 'light' | 'system') =>
                    updateSetting('theme', value)
                  }
                >
                  <SelectTrigger className="w-[140px]">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="dark">
                      <span className="flex items-center gap-2">
                        <Moon className="h-4 w-4" />
                        Dark
                      </span>
                    </SelectItem>
                    <SelectItem value="light">
                      <span className="flex items-center gap-2">
                        <Sun className="h-4 w-4" />
                        Light
                      </span>
                    </SelectItem>
                    <SelectItem value="system">
                      <span className="flex items-center gap-2">System</span>
                    </SelectItem>
                  </SelectContent>
                </Select>
              </div>
            </CardContent>
          </Card>

          {/* Notification Settings */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Bell className="h-5 w-5" />
                Notifications
              </CardTitle>
              <CardDescription>Manage notification preferences</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="notifications-enabled">Game Notifications</Label>
                  <p className="text-sm text-muted-foreground">
                    Get notified when it's your turn or someone challenges you
                  </p>
                </div>
                <Switch
                  id="notifications-enabled"
                  checked={settings.notificationsEnabled}
                  onCheckedChange={(checked) =>
                    updateSetting('notificationsEnabled', checked)
                  }
                />
              </div>
            </CardContent>
          </Card>

          {/* Privacy Settings */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <Shield className="h-5 w-5" />
                Privacy
              </CardTitle>
              <CardDescription>Control your privacy settings</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="show-online">Show Online Status</Label>
                  <p className="text-sm text-muted-foreground">
                    Let others see when you're online
                  </p>
                </div>
                <Switch
                  id="show-online"
                  checked={settings.showOnlineStatus}
                  onCheckedChange={(checked) =>
                    updateSetting('showOnlineStatus', checked)
                  }
                />
              </div>

              <Separator />

              <div className="flex items-center justify-between">
                <div className="space-y-0.5">
                  <Label htmlFor="allow-friend-requests">Allow Friend Requests</Label>
                  <p className="text-sm text-muted-foreground">
                    Allow other players to send you friend requests
                  </p>
                </div>
                <Switch
                  id="allow-friend-requests"
                  checked={settings.allowFriendRequests}
                  onCheckedChange={(checked) =>
                    updateSetting('allowFriendRequests', checked)
                  }
                />
              </div>
            </CardContent>
          </Card>

          {/* Save indicator */}
          {hasChanges && (
            <p className="text-sm text-muted-foreground text-center">
              Settings are saved automatically
            </p>
          )}
        </div>
      </div>
    </div>
  )
}
