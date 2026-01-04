import {
  ApiResponse,
  ServerConfig,
  Profile,
  Friend,
  Match,
} from '@/types/game.types'

class ApiService {
  private baseUrl: string = ''
  private authToken: string | null = null

  async initialize(): Promise<void> {
    try {
      const response = await fetch('/api/config')
      const config: ServerConfig = await response.json()

      // Extract base URL from SignalR URL
      const signalrUrl = config.signalrUrl
      this.baseUrl = signalrUrl.replace('/gamehub', '')

      console.log('[ApiService] Initialized with base URL:', this.baseUrl)
    } catch (error) {
      console.error('[ApiService] Failed to fetch config, using default:', error)
      this.baseUrl = 'http://localhost:5000'
    }
  }

  setAuthToken(token: string | null): void {
    this.authToken = token
  }

  getAuthToken(): string | null {
    return this.authToken
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<ApiResponse<T>> {
    const url = `${this.baseUrl}${endpoint}`

    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    }

    if (this.authToken) {
      headers['Authorization'] = `Bearer ${this.authToken}`
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      })

      if (!response.ok) {
        const errorText = await response.text()
        return {
          success: false,
          error: errorText || `HTTP ${response.status}: ${response.statusText}`,
        }
      }

      const data = await response.json()
      return {
        success: true,
        data,
      }
    } catch (error) {
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Unknown error',
      }
    }
  }

  // Profile endpoints
  async getProfile(username: string): Promise<ApiResponse<Profile>> {
    return this.request<Profile>(`/api/users/${username}/profile`)
  }

  async updateProfile(data: { email?: string }): Promise<ApiResponse<void>> {
    return this.request<void>('/api/users/me/profile', {
      method: 'PUT',
      body: JSON.stringify(data),
    })
  }

  // Friend endpoints
  async getFriends(): Promise<ApiResponse<Friend[]>> {
    return this.request<Friend[]>('/api/friends')
  }

  async getFriendRequests(): Promise<ApiResponse<Friend[]>> {
    return this.request<Friend[]>('/api/friends/requests')
  }

  async sendFriendRequest(username: string): Promise<ApiResponse<void>> {
    return this.request<void>('/api/friends/request', {
      method: 'POST',
      body: JSON.stringify({ username }),
    })
  }

  async acceptFriendRequest(userId: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/api/friends/${userId}/accept`, {
      method: 'POST',
    })
  }

  async declineFriendRequest(userId: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/api/friends/${userId}/decline`, {
      method: 'POST',
    })
  }

  async removeFriend(userId: string): Promise<ApiResponse<void>> {
    return this.request<void>(`/api/friends/${userId}`, {
      method: 'DELETE',
    })
  }

  // Match endpoints
  async getMatch(matchId: string): Promise<ApiResponse<Match>> {
    return this.request<Match>(`/api/matches/${matchId}`)
  }

  async getActiveMatches(): Promise<ApiResponse<Match[]>> {
    return this.request<Match[]>('/api/matches/active')
  }
}

export const apiService = new ApiService()
