// ==== FRIENDS STATE ====
let friendsList = [];
let pendingRequests = [];
let searchResults = [];

// ==== FRIENDS API ====

/**
 * Load friends list from server
 */
async function loadFriends() {
    if (!isAuthenticated()) return;

    try {
        const response = await authFetch('/api/friends');
        if (response.ok) {
            friendsList = await response.json();
            renderFriendsList();
        }
    } catch (e) {
        console.error('Failed to load friends:', e);
    }
}

/**
 * Load pending friend requests
 */
async function loadFriendRequests() {
    if (!isAuthenticated()) return;

    try {
        const response = await authFetch('/api/friends/requests');
        if (response.ok) {
            pendingRequests = await response.json();
            renderFriendRequests();
        }
    } catch (e) {
        console.error('Failed to load friend requests:', e);
    }
}

/**
 * Send a friend request to a user
 */
async function sendFriendRequest(userId) {
    try {
        const response = await authFetch(`/api/friends/request/${userId}`, {
            method: 'POST'
        });

        if (response.ok) {
            log('Friend request sent!', 'success');
            clearSearchResults();
        } else {
            const error = await response.json();
            log(error.error || 'Failed to send friend request', 'error');
        }
    } catch (e) {
        console.error('Failed to send friend request:', e);
        log('Failed to send friend request', 'error');
    }
}

/**
 * Accept a friend request
 */
async function acceptFriendRequest(userId) {
    try {
        const response = await authFetch(`/api/friends/accept/${userId}`, {
            method: 'POST'
        });

        if (response.ok) {
            log('Friend request accepted!', 'success');
            loadFriends();
            loadFriendRequests();
        } else {
            const error = await response.json();
            log(error.error || 'Failed to accept request', 'error');
        }
    } catch (e) {
        console.error('Failed to accept friend request:', e);
        log('Failed to accept friend request', 'error');
    }
}

/**
 * Decline a friend request
 */
async function declineFriendRequest(userId) {
    try {
        const response = await authFetch(`/api/friends/decline/${userId}`, {
            method: 'POST'
        });

        if (response.ok) {
            log('Friend request declined', 'info');
            loadFriendRequests();
        } else {
            const error = await response.json();
            log(error.error || 'Failed to decline request', 'error');
        }
    } catch (e) {
        console.error('Failed to decline friend request:', e);
    }
}

/**
 * Remove a friend
 */
async function removeFriend(userId) {
    if (!confirm('Remove this friend?')) return;

    try {
        const response = await authFetch(`/api/friends/${userId}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            log('Friend removed', 'info');
            loadFriends();
        } else {
            const error = await response.json();
            log(error.error || 'Failed to remove friend', 'error');
        }
    } catch (e) {
        console.error('Failed to remove friend:', e);
    }
}

/**
 * Invite a friend to the current game
 */
async function inviteFriendToGame(friendUserId) {
    if (!currentGameId) {
        log('Create or join a game first', 'warning');
        return;
    }

    try {
        const response = await authFetch(`/api/friends/invite/${friendUserId}/game/${currentGameId}`, {
            method: 'POST'
        });

        if (response.ok) {
            log('Game invite sent!', 'success');
        } else {
            const error = await response.json();
            log(error.error || 'Failed to send invite', 'error');
        }
    } catch (e) {
        console.error('Failed to invite friend:', e);
        log('Failed to send game invite', 'error');
    }
}

/**
 * Search for users by username
 */
async function searchUsers(query) {
    if (!query || query.length < 2) {
        clearSearchResults();
        return;
    }

    try {
        const response = await authFetch(`/api/users/search?q=${encodeURIComponent(query)}`);
        if (response.ok) {
            searchResults = await response.json();
            renderSearchResults();
        }
    } catch (e) {
        console.error('Failed to search users:', e);
    }
}

function clearSearchResults() {
    searchResults = [];
    const container = document.getElementById('userSearchResults');
    if (container) {
        container.innerHTML = '';
    }
}

// ==== RENDER FUNCTIONS ====

/**
 * Render the friends list
 */
function renderFriendsList() {
    const container = document.getElementById('friendsList');
    if (!container) return;

    if (friendsList.length === 0) {
        container.innerHTML = '<p class="empty-friends">No friends yet. Search for users to add!</p>';
        return;
    }

    container.innerHTML = friendsList.map(friend => `
        <div class="friend-item ${friend.isOnline ? 'online' : ''}">
            <div class="friend-info">
                <span class="online-indicator ${friend.isOnline ? 'online' : ''}"></span>
                <span class="friend-name">${escapeHtml(friend.displayName)}</span>
                <span class="friend-username">@${escapeHtml(friend.username)}</span>
            </div>
            <div class="friend-actions">
                ${friend.isOnline && currentGameId ? `
                    <button onclick="inviteFriendToGame('${friend.userId}')" class="btn-small btn-invite" title="Invite to game">
                        Invite
                    </button>
                ` : ''}
                <button onclick="removeFriend('${friend.userId}')" class="btn-small btn-remove" title="Remove friend">
                    &times;
                </button>
            </div>
        </div>
    `).join('');
}

/**
 * Render pending friend requests
 */
function renderFriendRequests() {
    const container = document.getElementById('friendRequests');
    if (!container) return;

    if (pendingRequests.length === 0) {
        container.innerHTML = '';
        return;
    }

    container.innerHTML = `
        <h4>Friend Requests (${pendingRequests.length})</h4>
        ${pendingRequests.map(req => `
            <div class="friend-request">
                <span class="request-name">${escapeHtml(req.displayName)}</span>
                <div class="request-actions">
                    <button onclick="acceptFriendRequest('${req.userId}')" class="btn-small btn-accept">Accept</button>
                    <button onclick="declineFriendRequest('${req.userId}')" class="btn-small btn-decline">Decline</button>
                </div>
            </div>
        `).join('')}
    `;
}

/**
 * Render user search results
 */
function renderSearchResults() {
    const container = document.getElementById('userSearchResults');
    if (!container) return;

    if (searchResults.length === 0) {
        container.innerHTML = '<p class="no-results">No users found</p>';
        return;
    }

    // Filter out current user and existing friends
    const filteredResults = searchResults.filter(user => {
        if (currentUser && user.userId === currentUser.userId) return false;
        if (friendsList.some(f => f.userId === user.userId)) return false;
        if (pendingRequests.some(r => r.userId === user.userId)) return false;
        return true;
    });

    if (filteredResults.length === 0) {
        container.innerHTML = '<p class="no-results">No new users found</p>';
        return;
    }

    container.innerHTML = filteredResults.map(user => `
        <div class="search-result">
            <div class="result-info">
                <span class="result-name">${escapeHtml(user.displayName)}</span>
                <span class="result-username">@${escapeHtml(user.username)}</span>
            </div>
            <button onclick="sendFriendRequest('${user.userId}')" class="btn-small btn-add">
                Add Friend
            </button>
        </div>
    `).join('');
}

// ==== HELPER FUNCTIONS ====

/**
 * Escape HTML to prevent XSS
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ==== EVENT HANDLERS ====

/**
 * Handle friend search input
 */
function performUserSearch() {
    const query = document.getElementById('friendSearch').value.trim();
    searchUsers(query);
}

// Set up search input with debounce
let searchTimeout = null;
document.addEventListener('DOMContentLoaded', () => {
    const searchInput = document.getElementById('friendSearch');
    if (searchInput) {
        searchInput.addEventListener('input', () => {
            clearTimeout(searchTimeout);
            searchTimeout = setTimeout(() => {
                performUserSearch();
            }, 300);
        });

        searchInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') {
                clearTimeout(searchTimeout);
                performUserSearch();
            }
        });
    }
});
