/* global goHome, toggleDebugPanel, showLoginModal, showRegisterModal, showProfileModal, logout, createGame, createAiGame, showCreateMatchDialog, createAnalysisGame, performUserSearch, toggleBoardFlip, showAbandonConfirm, exportPosition, showImportModal, showProfileTab, saveProfileSettings, performLogin, hideLoginModal, performRegister, hideRegisterModal, updateProfile, hideProfileModal, confirmAbandon, cancelAbandon, acceptDouble, declineDouble, confirmOfferDouble, cancelOfferDouble, createMatchFromDialog, hideCreateMatchDialog, copyPosition, applyPosition, closePositionModal, clearDebugLog */

/**
 * Event Handlers Setup
 * Attaches all event listeners programmatically instead of using inline onclick attributes
 */

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', () => {
    setupNavigationHandlers();
    setupAuthHandlers();
    setupGameCreationHandlers();
    setupGameControlHandlers();
    setupProfileHandlers();
    setupModalHandlers();
    setupDebugHandlers();
});

// Navigation Handlers
function setupNavigationHandlers() {
    const logoArea = document.querySelector('.flex.items-center.gap-4.cursor-pointer');
    if (logoArea) {
        logoArea.addEventListener('click', goHome);
    }
}

// Auth Handlers
function setupAuthHandlers() {
    // Login buttons
    const loginBtn = document.getElementById('loginBtn');
    if (loginBtn) loginBtn.addEventListener('click', showLoginModal);

    const signupBtn = document.getElementById('signupBtn');
    if (signupBtn) signupBtn.addEventListener('click', showRegisterModal);

    const profileBtn = document.getElementById('profileBtn');
    if (profileBtn) profileBtn.addEventListener('click', showProfileModal);

    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) logoutBtn.addEventListener('click', logout);

    // Modal actions
    const performLoginBtn = document.getElementById('performLoginBtn');
    if (performLoginBtn) performLoginBtn.addEventListener('click', performLogin);

    const cancelLoginBtn = document.getElementById('cancelLoginBtn');
    if (cancelLoginBtn) cancelLoginBtn.addEventListener('click', hideLoginModal);

    const switchToRegisterLink = document.getElementById('switchToRegisterLink');
    if (switchToRegisterLink) {
        switchToRegisterLink.addEventListener('click', (e) => {
            e.preventDefault();
            hideLoginModal();
            showRegisterModal();
        });
    }

    const performRegisterBtn = document.getElementById('performRegisterBtn');
    if (performRegisterBtn) performRegisterBtn.addEventListener('click', performRegister);

    const cancelRegisterBtn = document.getElementById('cancelRegisterBtn');
    if (cancelRegisterBtn) cancelRegisterBtn.addEventListener('click', hideRegisterModal);

    const switchToLoginLink = document.getElementById('switchToLoginLink');
    if (switchToLoginLink) {
        switchToLoginLink.addEventListener('click', (e) => {
            e.preventDefault();
            hideRegisterModal();
            showLoginModal();
        });
    }

    const updateProfileBtn = document.getElementById('updateProfileBtn');
    if (updateProfileBtn) updateProfileBtn.addEventListener('click', updateProfile);

    const closeProfileModalBtn = document.getElementById('closeProfileModalBtn');
    if (closeProfileModalBtn) closeProfileModalBtn.addEventListener('click', hideProfileModal);
}

// Game Creation Handlers
function setupGameCreationHandlers() {
    const createGameBtn = document.getElementById('createGameBtn');
    if (createGameBtn) createGameBtn.addEventListener('click', createGame);

    const createAiGameBtn = document.getElementById('createAiGameBtn');
    if (createAiGameBtn) createAiGameBtn.addEventListener('click', createAiGame);

    const createMatchBtn = document.getElementById('createMatchBtn');
    if (createMatchBtn) createMatchBtn.addEventListener('click', showCreateMatchDialog);

    const createAnalysisBtn = document.getElementById('createAnalysisBtn');
    if (createAnalysisBtn) createAnalysisBtn.addEventListener('click', createAnalysisGame);

    const userSearchBtn = document.getElementById('userSearchBtn');
    if (userSearchBtn) userSearchBtn.addEventListener('click', performUserSearch);
}

// Game Control Handlers
function setupGameControlHandlers() {
    const flipBoardBtn = document.getElementById('flipBoardBtn');
    if (flipBoardBtn) flipBoardBtn.addEventListener('click', toggleBoardFlip);

    const abandonBtn = document.getElementById('abandonBtn');
    if (abandonBtn) abandonBtn.addEventListener('click', showAbandonConfirm);

    const exportBtn = document.getElementById('exportBtn');
    if (exportBtn) exportBtn.addEventListener('click', exportPosition);

    const importBtn = document.getElementById('importBtn');
    if (importBtn) importBtn.addEventListener('click', showImportModal);

    // Abandon confirmation modal
    const confirmAbandonBtn = document.getElementById('confirmAbandonBtn');
    if (confirmAbandonBtn) confirmAbandonBtn.addEventListener('click', confirmAbandon);

    const cancelAbandonBtn = document.getElementById('cancelAbandonBtn');
    if (cancelAbandonBtn) cancelAbandonBtn.addEventListener('click', cancelAbandon);

    // Doubling cube modals
    const acceptDoubleBtn = document.getElementById('acceptDoubleBtn');
    if (acceptDoubleBtn) acceptDoubleBtn.addEventListener('click', acceptDouble);

    const declineDoubleBtn = document.getElementById('declineDoubleBtn');
    if (declineDoubleBtn) declineDoubleBtn.addEventListener('click', declineDouble);

    const confirmOfferDoubleBtn = document.getElementById('confirmOfferDoubleBtn');
    if (confirmOfferDoubleBtn) confirmOfferDoubleBtn.addEventListener('click', confirmOfferDouble);

    const cancelOfferDoubleBtn = document.getElementById('cancelOfferDoubleBtn');
    if (cancelOfferDoubleBtn) cancelOfferDoubleBtn.addEventListener('click', cancelOfferDouble);

    // Position modal
    const copyPositionBtn = document.getElementById('copyPositionBtn');
    if (copyPositionBtn) copyPositionBtn.addEventListener('click', copyPosition);

    const importPositionBtn = document.getElementById('importPositionBtn');
    if (importPositionBtn) importPositionBtn.addEventListener('click', applyPosition);

    const closePositionModalBtn = document.getElementById('closePositionModalBtn');
    if (closePositionModalBtn) closePositionModalBtn.addEventListener('click', closePositionModal);
}

// Profile Tab Handlers
function setupProfileHandlers() {
    const tabGames = document.getElementById('tabGames');
    if (tabGames) tabGames.addEventListener('click', () => showProfileTab('games'));

    const tabFriends = document.getElementById('tabFriends');
    if (tabFriends) tabFriends.addEventListener('click', () => showProfileTab('friends'));

    const tabStats = document.getElementById('tabStats');
    if (tabStats) tabStats.addEventListener('click', () => showProfileTab('stats'));

    const tabSettings = document.getElementById('tabSettings');
    if (tabSettings) tabSettings.addEventListener('click', () => showProfileTab('settings'));

    const saveProfileSettingsBtn = document.getElementById('saveProfileSettingsBtn');
    if (saveProfileSettingsBtn) saveProfileSettingsBtn.addEventListener('click', saveProfileSettings);
}

// Modal Handlers
function setupModalHandlers() {
    const createMatchConfirmBtn = document.getElementById('createMatchConfirmBtn');
    if (createMatchConfirmBtn) createMatchConfirmBtn.addEventListener('click', createMatchFromDialog);

    const cancelMatchBtn = document.getElementById('cancelMatchBtn');
    if (cancelMatchBtn) cancelMatchBtn.addEventListener('click', hideCreateMatchDialog);
}

// Debug Handlers
function setupDebugHandlers() {
    const debugToggleBtn = document.getElementById('debugToggleBtn');
    if (debugToggleBtn) debugToggleBtn.addEventListener('click', toggleDebugPanel);

    const clearDebugBtn = document.getElementById('clearDebugBtn');
    if (clearDebugBtn) clearDebugBtn.addEventListener('click', clearDebugLog);

    const closeDebugBtn = document.getElementById('closeDebugBtn');
    if (closeDebugBtn) closeDebugBtn.addEventListener('click', toggleDebugPanel);
}
