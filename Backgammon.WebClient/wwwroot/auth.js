// ==== AUTH STATE ====
let authToken = null;
let currentUser = null;

// ==== TOKEN MANAGEMENT ====

/**
 * Get stored JWT token from localStorage
 */
function getStoredToken() {
    return localStorage.getItem('backgammon_auth_token');
}

/**
 * Store JWT token in localStorage
 */
function storeToken(token) {
    localStorage.setItem('backgammon_auth_token', token);
    authToken = token;
}

/**
 * Clear stored token and user state
 */
function clearToken() {
    localStorage.removeItem('backgammon_auth_token');
    authToken = null;
    currentUser = null;
}

// ==== API HELPERS ====

/**
 * Get the base API URL
 */
function getApiBaseUrl() {
    // Use the same server as SignalR
    const serverUrlInput = document.getElementById('serverUrl');
    if (serverUrlInput && serverUrlInput.value) {
        // Extract base URL from SignalR URL (remove /gamehub)
        return serverUrlInput.value.replace('/gamehub', '');
    }
    return 'http://localhost:5000';
}

/**
 * Fetch wrapper that adds Authorization header
 */
async function authFetch(url, options = {}) {
    const headers = options.headers || {};
    if (authToken) {
        headers['Authorization'] = `Bearer ${authToken}`;
    }
    headers['Content-Type'] = 'application/json';

    const fullUrl = url.startsWith('http') ? url : `${getApiBaseUrl()}${url}`;

    return fetch(fullUrl, { ...options, headers });
}

// ==== AUTH ACTIONS ====

/**
 * Register a new user
 */
async function register(username, password, displayName, email) {
    const anonymousId = localStorage.getItem('backgammon_player_id');

    try {
        const response = await fetch(`${getApiBaseUrl()}/api/auth/register`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                username,
                password,
                displayName: displayName || username,
                email: email || null,
                anonymousPlayerId: anonymousId
            })
        });

        const result = await response.json();
        if (result.success) {
            storeToken(result.token);
            currentUser = result.user;
            updateAuthUI();
            log('Account created successfully!', 'success');
        }
        return result;
    } catch (error) {
        console.error('Registration error:', error);
        return { success: false, error: 'Registration failed. Please try again.' };
    }
}

/**
 * Login with username and password
 */
async function login(username, password) {
    try {
        const response = await fetch(`${getApiBaseUrl()}/api/auth/login`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ username, password })
        });

        if (!response.ok) {
            return { success: false, error: 'Invalid username or password' };
        }

        const result = await response.json();
        if (result.success) {
            storeToken(result.token);
            currentUser = result.user;
            updateAuthUI();
            log('Logged in successfully!', 'success');
        }
        return result;
    } catch (error) {
        console.error('Login error:', error);
        return { success: false, error: 'Login failed. Please try again.' };
    }
}

/**
 * Logout - clear local state
 */
function logout() {
    clearToken();
    currentUser = null;
    updateAuthUI();
    log('Logged out', 'info');

    // Hide friends section
    const friendsSection = document.getElementById('friendsSection');
    if (friendsSection) {
        friendsSection.style.display = 'none';
    }
}

/**
 * Check if user is still authenticated (validate token)
 */
async function checkAuth() {
    const token = getStoredToken();
    if (!token) return false;

    authToken = token;
    try {
        const response = await authFetch('/api/auth/me');
        if (response.ok) {
            currentUser = await response.json();
            return true;
        }
    } catch (e) {
        console.error('Auth check failed:', e);
    }

    clearToken();
    return false;
}

/**
 * Check if user is currently authenticated
 */
function isAuthenticated() {
    return currentUser !== null;
}

/**
 * Get effective player ID (authenticated user ID or anonymous ID)
 */
function getEffectivePlayerId() {
    if (currentUser) {
        return currentUser.userId;
    }
    return getOrCreatePlayerId();
}

// ==== UI UPDATES ====

/**
 * Update the authentication UI based on current state
 */
function updateAuthUI() {
    const authSection = document.getElementById('authSection');
    const userSection = document.getElementById('userSection');
    const usernameDisplay = document.getElementById('usernameDisplay');
    const friendsSection = document.getElementById('friendsSection');

    if (currentUser) {
        if (authSection) authSection.style.display = 'none';
        if (userSection) userSection.style.display = 'flex';
        if (usernameDisplay) usernameDisplay.textContent = currentUser.displayName;
        if (friendsSection) friendsSection.style.display = 'block';
    } else {
        if (authSection) authSection.style.display = 'flex';
        if (userSection) userSection.style.display = 'none';
        if (friendsSection) friendsSection.style.display = 'none';
    }
}

// ==== MODAL FUNCTIONS ====

function showLoginModal() {
    const modal = document.getElementById('loginModal');
    modal.showModal();
    document.getElementById('loginUsername').focus();
    document.getElementById('loginError').textContent = '';
}

function hideLoginModal() {
    const modal = document.getElementById('loginModal');
    modal.close();
    document.getElementById('loginUsername').value = '';
    document.getElementById('loginPassword').value = '';
    document.getElementById('loginError').textContent = '';
}

function showRegisterModal() {
    const modal = document.getElementById('registerModal');
    modal.showModal();
    document.getElementById('regUsername').focus();
    document.getElementById('registerError').textContent = '';
}

function hideRegisterModal() {
    const modal = document.getElementById('registerModal');
    modal.close();
    document.getElementById('regUsername').value = '';
    document.getElementById('regDisplayName').value = '';
    document.getElementById('regPassword').value = '';
    document.getElementById('regPasswordConfirm').value = '';
    document.getElementById('regEmail').value = '';
    document.getElementById('registerError').textContent = '';
}

function showProfileModal() {
    if (!currentUser) return;

    const modal = document.getElementById('profileModal');
    modal.showModal();
    document.getElementById('profileUsername').textContent = currentUser.username;
    document.getElementById('profileDisplayName').value = currentUser.displayName;

    // Display stats
    const stats = currentUser.stats || {};
    document.getElementById('statTotalGames').textContent = stats.totalGames || 0;
    document.getElementById('statWins').textContent = stats.wins || 0;

    const winRate = stats.totalGames > 0
        ? Math.round((stats.wins / stats.totalGames) * 100)
        : 0;
    document.getElementById('statWinRate').textContent = `${winRate}%`;
    document.getElementById('statBestStreak').textContent = stats.bestWinStreak || 0;
}

function hideProfileModal() {
    const modal = document.getElementById('profileModal');
    modal.close();
}

// ==== FORM HANDLERS ====

async function performLogin() {
    const username = document.getElementById('loginUsername').value.trim();
    const password = document.getElementById('loginPassword').value;
    const errorEl = document.getElementById('loginError');

    if (!username || !password) {
        errorEl.textContent = 'Please enter username and password';
        return;
    }

    errorEl.textContent = 'Logging in...';

    const result = await login(username, password);

    if (result.success) {
        hideLoginModal();
        // Load friends if authenticated
        if (typeof loadFriends === 'function') {
            loadFriends();
            loadFriendRequests();
        }
    } else {
        errorEl.textContent = result.error || 'Login failed';
    }
}

async function performRegister() {
    const username = document.getElementById('regUsername').value.trim();
    const displayName = document.getElementById('regDisplayName').value.trim();
    const password = document.getElementById('regPassword').value;
    const passwordConfirm = document.getElementById('regPasswordConfirm').value;
    const email = document.getElementById('regEmail').value.trim();
    const errorEl = document.getElementById('registerError');

    // Validation
    if (!username) {
        errorEl.textContent = 'Username is required';
        return;
    }

    if (username.length < 3 || username.length > 20) {
        errorEl.textContent = 'Username must be 3-20 characters';
        return;
    }

    if (!/^[a-zA-Z0-9_]+$/.test(username)) {
        errorEl.textContent = 'Username can only contain letters, numbers, and underscores';
        return;
    }

    if (!password || password.length < 8) {
        errorEl.textContent = 'Password must be at least 8 characters';
        return;
    }

    if (password !== passwordConfirm) {
        errorEl.textContent = 'Passwords do not match';
        return;
    }

    errorEl.textContent = 'Creating account...';

    const result = await register(username, password, displayName, email);

    if (result.success) {
        hideRegisterModal();
        // Load friends if authenticated
        if (typeof loadFriends === 'function') {
            loadFriends();
            loadFriendRequests();
        }
    } else {
        errorEl.textContent = result.error || 'Registration failed';
    }
}

async function updateProfile() {
    const displayName = document.getElementById('profileDisplayName').value.trim();

    if (!displayName) {
        alert('Display name cannot be empty');
        return;
    }

    try {
        const response = await authFetch('/api/users/profile', {
            method: 'PUT',
            body: JSON.stringify({ displayName })
        });

        if (response.ok) {
            const updatedUser = await response.json();
            currentUser = updatedUser;
            updateAuthUI();
            log('Profile updated!', 'success');
        } else {
            const error = await response.json();
            alert(error.error || 'Failed to update profile');
        }
    } catch (error) {
        console.error('Profile update error:', error);
        alert('Failed to update profile');
    }
}

// ==== KEYBOARD HANDLERS ====

// Add Enter key support for login/register forms
document.addEventListener('DOMContentLoaded', () => {
    const loginPassword = document.getElementById('loginPassword');
    if (loginPassword) {
        loginPassword.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') performLogin();
        });
    }

    const regPasswordConfirm = document.getElementById('regPasswordConfirm');
    if (regPasswordConfirm) {
        regPasswordConfirm.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') performRegister();
        });
    }

    // DaisyUI dialog handles Escape and backdrop clicks natively
});
