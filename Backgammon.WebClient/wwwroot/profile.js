// Profile page functionality
let currentProfileUsername = null;
let currentProfileData = null;

// Navigation functions
function showProfilePage() {
    document.getElementById('landingPage').style.display = 'none';
    document.getElementById('gamePage').style.display = 'none';
    document.getElementById('profilePage').style.display = 'block';
}

function setProfileUrl(username) {
    const newUrl = `/profile/${username}`;
    if (window.location.pathname !== newUrl) {
        window.history.pushState({ username }, '', newUrl);
    }
}

function getProfileUsernameFromUrl() {
    const pathParts = window.location.pathname.split('/');
    if (pathParts.length >= 3 && pathParts[1] === 'profile') {
        return decodeURIComponent(pathParts[2]);
    }
    return null;
}

// Navigate to a profile
async function navigateToProfile(username) {
    setProfileUrl(username);
    showProfilePage();
    await loadProfile(username);
}

// Load profile data
async function loadProfile(username) {
    if (!username) return;
    
    currentProfileUsername = username;
    
    // Reset UI
    document.getElementById('profileDisplayName').textContent = 'Loading...';
    document.getElementById('profileUsername').textContent = '...';
    document.getElementById('profileJoinDate').textContent = '...';
    document.getElementById('profilePrivacyNotice').style.display = 'none';
    document.getElementById('profileActions').innerHTML = '';
    
    // Show loading state for all tabs
    resetProfileTabs();
    
    try {
        // Get profile data via SignalR
        currentProfileData = await connection.invoke('GetPlayerProfile', username);
        
        if (!currentProfileData) {
            showError('Profile not found');
            return;
        }
        
        // Update profile header
        updateProfileHeader(currentProfileData);
        
        // Update stats
        updateProfileStats(currentProfileData);
        
        // Load initial tab content
        await showProfileTab('games');
        
    } catch (error) {
        console.error('Error loading profile:', error);
        showError('Failed to load profile');
    }
}

function updateProfileHeader(profile) {
    // Update basic info
    document.getElementById('profileDisplayName').textContent = profile.displayName;
    document.getElementById('profileUsername').textContent = profile.username;
    
    // Format join date
    const joinDate = new Date(profile.createdAt);
    document.getElementById('profileJoinDate').textContent = joinDate.toLocaleDateString();
    
    // Set avatar initial
    const initial = profile.displayName.charAt(0).toUpperCase();
    document.getElementById('profileAvatar').textContent = initial;
    
    // Show privacy notice if profile is private
    if (profile.isPrivate) {
        document.getElementById('profilePrivacyNotice').style.display = 'block';
    }
    
    // Update actions based on whether it's own profile
    const actionsDiv = document.getElementById('profileActions');
    const currentUser = getCurrentUsername();
    
    if (currentUser === profile.username) {
        // Own profile - show settings
        actionsDiv.innerHTML = `
            <button class="btn btn-primary btn-sm" onclick="editProfile()">
                Edit Profile
            </button>
        `;
        // Show settings tab
        document.getElementById('tabSettings').style.display = 'block';
        // Load privacy settings
        loadPrivacySettings(profile);
    } else if (currentUser) {
        // Other user's profile - show friend actions
        if (profile.isFriend) {
            actionsDiv.innerHTML = `
                <button class="btn btn-secondary btn-sm" disabled>
                    ✓ Friends
                </button>
            `;
        } else {
            actionsDiv.innerHTML = `
                <button class="btn btn-primary btn-sm" onclick="sendFriendRequest('${profile.username}')">
                    Add Friend
                </button>
            `;
        }
    }
}

function updateProfileStats(profile) {
    if (profile.stats) {
        document.getElementById('statTotalGames').textContent = profile.stats.totalGames || 0;
        document.getElementById('statWins').textContent = profile.stats.wins || 0;
        document.getElementById('statLosses').textContent = profile.stats.losses || 0;
        document.getElementById('statBestStreak').textContent = profile.stats.bestWinStreak || 0;
        
        // Calculate win rate
        const totalGames = profile.stats.totalGames || 0;
        const wins = profile.stats.wins || 0;
        const winRate = totalGames > 0 ? Math.round((wins / totalGames) * 100) : 0;
        document.getElementById('statWinRate').textContent = winRate;
        
        // Detailed stats
        document.getElementById('statNormalWins').textContent = profile.stats.normalWins || 0;
        document.getElementById('statGammonWins').textContent = profile.stats.gammonWins || 0;
        document.getElementById('statBackgammonWins').textContent = profile.stats.backgammonWins || 0;
        document.getElementById('statTotalStakes').textContent = profile.stats.totalStakes || 0;
    } else {
        // No stats available (private profile)
        document.querySelectorAll('[id^="stat"]').forEach(el => {
            if (el.id !== 'statWinRate') {
                el.textContent = '-';
            }
        });
        document.getElementById('statWinRate').textContent = '-';
    }
}

function resetProfileTabs() {
    document.getElementById('tabContentGames').innerHTML = '<div class="text-center py-8"><span class="loading loading-spinner loading-lg"></span></div>';
    document.getElementById('tabContentFriends').innerHTML = '<div class="text-center py-8"><span class="loading loading-spinner loading-lg"></span></div>';
}

async function showProfileTab(tab) {
    // Update tab active state
    document.querySelectorAll('.tabs .tab').forEach(t => t.classList.remove('tab-active'));
    document.getElementById('tab' + tab.charAt(0).toUpperCase() + tab.slice(1)).classList.add('tab-active');
    
    // Hide all tab content
    document.querySelectorAll('[id^="tabContent"]').forEach(content => content.style.display = 'none');
    
    // Show selected tab content
    const tabContentId = 'tabContent' + tab.charAt(0).toUpperCase() + tab.slice(1);
    document.getElementById(tabContentId).style.display = 'block';
    
    // Load content for the tab
    switch(tab) {
        case 'games':
            await loadRecentGames();
            break;
        case 'friends':
            await loadFriendsList();
            break;
        case 'stats':
            // Stats are already loaded
            break;
        case 'settings':
            // Settings are already loaded
            break;
    }
}

async function loadRecentGames() {
    const container = document.getElementById('tabContentGames');
    
    if (!currentProfileData || !currentProfileData.recentGames || currentProfileData.recentGames.length === 0) {
        container.innerHTML = '<p class="text-center text-base-content/60 py-8">No recent games</p>';
        return;
    }
    
    let html = '';
    for (const game of currentProfileData.recentGames) {
        const date = new Date(game.completedAt).toLocaleDateString();
        const winClass = game.won ? 'badge-success' : 'badge-error';
        const winText = game.won ? 'Won' : 'Lost';
        const winTypeText = game.winType ? ` (${game.winType})` : '';
        
        html += `
            <div class="card bg-base-200 p-4">
                <div class="flex items-center justify-between">
                    <div>
                        <p class="font-semibold">vs ${game.opponentUsername}</p>
                        <p class="text-sm text-base-content/60">${date}</p>
                    </div>
                    <div class="text-right">
                        <span class="badge ${winClass}">${winText}${winTypeText}</span>
                        <p class="text-sm text-base-content/60">Stakes: ${game.stakes}</p>
                    </div>
                </div>
            </div>
        `;
    }
    
    container.innerHTML = html;
}

async function loadFriendsList() {
    const container = document.getElementById('tabContentFriends');
    
    if (!currentProfileData || !currentProfileData.friends || currentProfileData.friends.length === 0) {
        container.innerHTML = '<p class="text-center text-base-content/60 py-8">No friends to display</p>';
        return;
    }
    
    let html = '';
    for (const friend of currentProfileData.friends) {
        const onlineClass = friend.isOnline ? 'badge-success' : 'badge-ghost';
        const onlineText = friend.isOnline ? 'Online' : 'Offline';
        
        html += `
            <div class="card bg-base-200 p-4">
                <div class="flex items-center justify-between">
                    <div class="flex items-center gap-3">
                        <div class="avatar placeholder">
                            <div class="bg-neutral text-neutral-content rounded-full w-12">
                                <span>${friend.displayName.charAt(0).toUpperCase()}</span>
                            </div>
                        </div>
                        <div>
                            <p class="font-semibold cursor-pointer hover:underline" onclick="navigateToProfile('${friend.username}')">
                                ${friend.displayName}
                            </p>
                            <p class="text-sm text-base-content/60">@${friend.username}</p>
                        </div>
                    </div>
                    <span class="badge ${onlineClass}">${onlineText}</span>
                </div>
            </div>
        `;
    }
    
    container.innerHTML = html;
}

function loadPrivacySettings(profile) {
    document.getElementById('settingProfilePrivacy').value = profile.profilePrivacy;
    document.getElementById('settingGameHistoryPrivacy').value = profile.gameHistoryPrivacy;
    document.getElementById('settingFriendsListPrivacy').value = profile.friendsListPrivacy;
}

async function saveProfileSettings() {
    const profilePrivacy = parseInt(document.getElementById('settingProfilePrivacy').value);
    const gameHistoryPrivacy = parseInt(document.getElementById('settingGameHistoryPrivacy').value);
    const friendsListPrivacy = parseInt(document.getElementById('settingFriendsListPrivacy').value);
    
    try {
        const response = await fetch('/api/users/profile', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${getAuthToken()}`
            },
            body: JSON.stringify({
                profilePrivacy,
                gameHistoryPrivacy,
                friendsListPrivacy
            })
        });
        
        if (!response.ok) {
            throw new Error('Failed to update privacy settings');
        }
        
        showSuccess('Privacy settings updated');
        
        // Reload profile to reflect changes
        await loadProfile(currentProfileUsername);
        
    } catch (error) {
        console.error('Error saving privacy settings:', error);
        showError('Failed to save privacy settings');
    }
}

function editProfile() {
    // This would open a modal or navigate to an edit page
    // For now, just switch to settings tab
    showProfileTab('settings');
}

function getCurrentUsername() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return user.username || null;
}

function getAuthToken() {
    const user = JSON.parse(localStorage.getItem('user') || '{}');
    return user.token || null;
}

function showError(message) {
    // You could implement a toast notification system
    console.error(message);
    alert(message);
}

function showSuccess(message) {
    // You could implement a toast notification system
    console.log(message);
    alert(message);
}