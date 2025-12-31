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
        container.innerHTML = '<p class="text-base-content/60 text-center py-4 italic">No friends yet. Search for users to add!</p>';
        return;
    }

    container.innerHTML = '<div class="space-y-2">' + friendsList.map(friend => `
        <div class="flex items-center justify-between p-3 bg-base-200 rounded-lg ${friend.isOnline ? 'border-l-4 border-success' : ''}">
            <div class="flex items-center gap-3">
                <div class="avatar placeholder ${friend.isOnline ? 'online' : 'offline'}">
                    <div class="bg-neutral text-neutral-content rounded-full w-8">
                        <span class="text-xs">${escapeHtml(friend.displayName.charAt(0).toUpperCase())}</span>
                    </div>
                </div>
                <div>
                    <p class="font-semibold text-sm" style="cursor: pointer;" onmouseover="this.style.textDecoration='underline'" onmouseout="this.style.textDecoration='none'" onclick="navigateToProfile('${escapeHtml(friend.username)}')">${escapeHtml(friend.displayName)}</p>
                    <p class="text-xs text-base-content/60" style="cursor: pointer;" onmouseover="this.style.textDecoration='underline'" onmouseout="this.style.textDecoration='none'" onclick="navigateToProfile('${escapeHtml(friend.username)}')">@${escapeHtml(friend.username)}</p>
                </div>
            </div>
            <div class="flex gap-2">
                ${friend.isOnline && currentGameId ? `
                    <button onclick="inviteFriendToGame('${friend.userId}')" class="btn btn-primary btn-xs" title="Invite to game">
                        Invite
                    </button>
                ` : ''}
                <button onclick="removeFriend('${friend.userId}')" class="btn btn-ghost btn-xs text-error" title="Remove friend">
                    ✕
                </button>
            </div>
        </div>
    `).join('') + '</div>';
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
        <div class="mb-4">
            <h4 class="font-semibold text-sm mb-2 flex items-center gap-2">
                Friend Requests
                <span class="badge badge-primary badge-sm">${pendingRequests.length}</span>
            </h4>
            <div class="space-y-2">
                ${pendingRequests.map(req => `
                    <div class="flex items-center justify-between p-3 bg-primary/10 rounded-lg border-l-4 border-primary">
                        <span class="font-semibold" style="cursor: pointer;" onmouseover="this.style.textDecoration='underline'" onmouseout="this.style.textDecoration='none'" onclick="navigateToProfile('${escapeHtml(req.username)}')">${escapeHtml(req.displayName)}</span>
                        <div class="flex gap-2">
                            <button onclick="acceptFriendRequest('${req.userId}')" class="btn btn-success btn-xs">Accept</button>
                            <button onclick="declineFriendRequest('${req.userId}')" class="btn btn-ghost btn-xs">Decline</button>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
    `;
}

/**
 * Render user search results
 */
function renderSearchResults() {
    const container = document.getElementById('userSearchResults');
    if (!container) return;

    if (searchResults.length === 0) {
        container.innerHTML = '<p class="text-base-content/60 text-center py-4 italic">No users found</p>';
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
        container.innerHTML = '<p class="text-base-content/60 text-center py-4 italic">No new users found</p>';
        return;
    }

    container.innerHTML = '<div class="space-y-2 mb-4">' + filteredResults.map(user => `
        <div class="flex items-center justify-between p-3 bg-base-200 rounded-lg">
            <div>
                <p class="font-semibold text-sm" style="cursor: pointer;" onmouseover="this.style.textDecoration='underline'" onmouseout="this.style.textDecoration='none'" onclick="navigateToProfile('${escapeHtml(user.username)}')">${escapeHtml(user.displayName)}</p>
                <p class="text-xs text-base-content/60" style="cursor: pointer;" onmouseover="this.style.textDecoration='underline'" onmouseout="this.style.textDecoration='none'" onclick="navigateToProfile('${escapeHtml(user.username)}')">@${escapeHtml(user.username)}</p>
            </div>
            <button onclick="sendFriendRequest('${user.userId}')" class="btn btn-success btn-xs">
                Add Friend
            </button>
        </div>
    `).join('') + '</div>';
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
