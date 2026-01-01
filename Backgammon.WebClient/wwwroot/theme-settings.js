// theme-settings.js - Theme settings UI component

const ThemeSettings = (function() {
    let isOpen = false;
    let activeTab = 'themes';
    
    // Create settings button
    function createSettingsButton() {
        const button = document.createElement('button');
        button.id = 'themeSettingsBtn';
        button.className = 'btn btn-circle btn-primary fixed bottom-4 left-4 z-50';
        button.innerHTML = '🎨';
        button.title = 'Theme Settings';
        button.onclick = toggleSettings;
        document.body.appendChild(button);
    }
    
    // Toggle settings modal
    function toggleSettings() {
        if (isOpen) {
            closeSettings();
        } else {
            openSettings();
        }
    }
    
    // Open settings modal
    function openSettings() {
        isOpen = true;
        const modal = createModal();
        document.body.appendChild(modal);
        
        // Animate in
        requestAnimationFrame(() => {
            modal.classList.add('modal-open');
        });
        
        // Load content for active tab
        loadTabContent(activeTab);
    }
    
    // Close settings modal
    function closeSettings() {
        isOpen = false;
        const modal = document.getElementById('themeSettingsModal');
        if (modal) {
            modal.classList.remove('modal-open');
            setTimeout(() => modal.remove(), 300);
        }
    }
    
    // Create modal structure
    function createModal() {
        const modal = document.createElement('div');
        modal.id = 'themeSettingsModal';
        modal.className = 'modal';
        
        modal.innerHTML = `
            <div class="modal-box max-w-4xl">
                <h3 class="font-bold text-lg mb-4">Theme Settings</h3>
                <button class="btn btn-sm btn-circle btn-ghost absolute right-2 top-2" onclick="ThemeSettings.close()">✕</button>
                
                <div class="tabs tabs-boxed mb-4">
                    <a class="tab ${activeTab === 'themes' ? 'tab-active' : ''}" onclick="ThemeSettings.switchTab('themes')">Themes</a>
                    <a class="tab ${activeTab === 'customize' ? 'tab-active' : ''}" onclick="ThemeSettings.switchTab('customize')">Customize</a>
                    <a class="tab ${activeTab === 'import' ? 'tab-active' : ''}" onclick="ThemeSettings.switchTab('import')">Import/Export</a>
                </div>
                
                <div id="themeTabContent" class="min-h-[400px]">
                    <!-- Content loaded dynamically -->
                </div>
            </div>
        `;
        
        return modal;
    }
    
    // Switch tabs
    function switchTab(tab) {
        activeTab = tab;
        
        // Update tab UI
        document.querySelectorAll('.tabs .tab').forEach(t => {
            t.classList.remove('tab-active');
        });
        event.target.classList.add('tab-active');
        
        // Load new content
        loadTabContent(tab);
    }
    
    // Load tab content
    function loadTabContent(tab) {
        const container = document.getElementById('themeTabContent');
        if (!container) return;
        
        switch (tab) {
            case 'themes':
                loadThemesTab(container);
                break;
            case 'customize':
                loadCustomizeTab(container);
                break;
            case 'import':
                loadImportExportTab(container);
                break;
        }
    }
    
    // Themes tab - pre-built themes
    function loadThemesTab(container) {
        const themes = ThemeManager.getAllThemes();
        const currentTheme = ThemeManager.getCurrentTheme();
        
        let html = '<div class="grid grid-cols-1 md:grid-cols-2 gap-4">';
        
        Object.entries(themes).forEach(([id, theme]) => {
            const isActive = currentTheme && currentTheme.id === id;
            html += `
                <div class="card bg-base-200 shadow-xl ${isActive ? 'ring-2 ring-primary' : ''}">
                    <div class="card-body">
                        <h4 class="card-title">${theme.name}</h4>
                        <p class="text-sm opacity-70">${theme.description}</p>
                        <div class="mt-2">
                            ${createThemePreview(theme)}
                        </div>
                        <div class="card-actions justify-end mt-4">
                            ${id.startsWith('custom_') ? `<button class="btn btn-sm btn-error" onclick="ThemeSettings.deleteTheme('${id}')">Delete</button>` : ''}
                            <button class="btn btn-sm ${isActive ? 'btn-disabled' : 'btn-primary'}" 
                                    onclick="ThemeSettings.applyTheme('${id}')"
                                    ${isActive ? 'disabled' : ''}>
                                ${isActive ? 'Active' : 'Apply'}
                            </button>
                        </div>
                    </div>
                </div>
            `;
        });
        
        html += '</div>';
        container.innerHTML = html;
    }
    
    // Create mini board preview
    function createThemePreview(theme) {
        const scale = 0.15;
        const width = 200;
        const height = 100;
        
        return `
            <svg viewBox="0 0 ${width} ${height}" style="width: 100%; height: 80px; border-radius: 4px; background: ${theme.colors.boardBackground}">
                <!-- Board border -->
                <rect x="2" y="2" width="${width-4}" height="${height-4}" 
                      fill="none" stroke="${theme.colors.boardBorder}" stroke-width="2" rx="4"/>
                
                <!-- Points -->
                ${createPreviewPoints(theme, width, height)}
                
                <!-- Bar -->
                <rect x="${width/2 - 10}" y="0" width="20" height="${height}" 
                      fill="${theme.colors.bar}"/>
                
                <!-- Sample checkers -->
                <circle cx="30" cy="25" r="8" fill="${theme.colors.checkerWhite}" 
                        stroke="${theme.colors.checkerWhiteStroke}" stroke-width="1"/>
                <circle cx="50" cy="25" r="8" fill="${theme.colors.checkerRed}" 
                        stroke="${theme.colors.checkerRedStroke}" stroke-width="1"/>
            </svg>
        `;
    }
    
    // Create preview points
    function createPreviewPoints(theme, width, height) {
        let svg = '';
        const pointWidth = width / 13;
        const pointHeight = height * 0.4;
        
        // Top points
        for (let i = 0; i < 12; i++) {
            if (i === 6) continue; // Skip middle (bar)
            const x = i * pointWidth + (i > 6 ? pointWidth : 0);
            const color = i % 2 === 0 ? theme.colors.pointLight : theme.colors.pointDark;
            svg += `<polygon points="${x},0 ${x + pointWidth/2},${pointHeight} ${x + pointWidth},0" fill="${color}"/>`;
        }
        
        // Bottom points
        for (let i = 0; i < 12; i++) {
            if (i === 6) continue; // Skip middle (bar)
            const x = i * pointWidth + (i > 6 ? pointWidth : 0);
            const color = i % 2 === 0 ? theme.colors.pointDark : theme.colors.pointLight;
            svg += `<polygon points="${x},${height} ${x + pointWidth/2},${height - pointHeight} ${x + pointWidth},${height}" fill="${color}"/>`;
        }
        
        return svg;
    }
    
    // Customize tab - create custom theme
    function loadCustomizeTab(container) {
        const currentTheme = ThemeManager.getCurrentTheme() || ThemeManager.getDefaultThemes().classic;
        
        let html = `
            <div class="form-control">
                <label class="label">
                    <span class="label-text">Theme Name</span>
                </label>
                <input type="text" id="customThemeName" placeholder="My Custom Theme" 
                       class="input input-bordered w-full" value="${currentTheme.name} (Custom)"/>
            </div>
            
            <div class="form-control mt-4">
                <label class="label">
                    <span class="label-text">Description</span>
                </label>
                <input type="text" id="customThemeDesc" placeholder="A unique theme design" 
                       class="input input-bordered w-full" value="${currentTheme.description || ''}"/>
            </div>
            
            <div class="divider">Colors</div>
            
            <div class="grid grid-cols-1 md:grid-cols-2 gap-4 max-h-96 overflow-y-auto">
        `;
        
        // Group colors by category
        const colorGroups = {
            'Board': ['boardBackground', 'boardBorder', 'bar', 'bearoff'],
            'Points': ['pointLight', 'pointDark'],
            'Checkers': ['checkerWhite', 'checkerWhiteStroke', 'checkerRed', 'checkerRedStroke'],
            'Highlights': ['highlightSource', 'highlightSelected', 'highlightDest', 'highlightCapture'],
            'Dice': ['diceBackground', 'diceBorder', 'diceValue'],
            'Cube': ['cubeBackground', 'cubeBorder', 'cubeText']
        };
        
        Object.entries(colorGroups).forEach(([group, colors]) => {
            html += `
                <div class="card bg-base-200 p-4">
                    <h5 class="font-semibold mb-2">${group}</h5>
                    <div class="space-y-2">
            `;
            
            colors.forEach(colorKey => {
                const value = currentTheme.colors[colorKey];
                const label = colorKey.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase());
                html += `
                    <div class="flex items-center gap-2">
                        <input type="color" id="color_${colorKey}" 
                               value="${convertToHex(value)}" 
                               onchange="ThemeSettings.updateCustomColor('${colorKey}')"
                               class="w-12 h-8 rounded cursor-pointer"/>
                        <label for="color_${colorKey}" class="text-sm flex-1">${label}</label>
                    </div>
                `;
            });
            
            html += `
                    </div>
                </div>
            `;
        });
        
        html += `
            </div>
            
            <div class="mt-6 flex justify-end gap-2">
                <button class="btn btn-primary" onclick="ThemeSettings.saveCustomTheme()">
                    Save Theme
                </button>
                <button class="btn btn-secondary" onclick="ThemeSettings.previewCustomTheme()">
                    Preview
                </button>
            </div>
        `;
        
        container.innerHTML = html;
    }
    
    // Import/Export tab
    function loadImportExportTab(container) {
        const currentTheme = ThemeManager.getCurrentTheme();
        
        let html = `
            <div class="space-y-6">
                <div>
                    <h4 class="text-lg font-semibold mb-2">Export Current Theme</h4>
                    <p class="text-sm opacity-70 mb-4">Export your current theme configuration as a JSON file to share with others.</p>
                    <button class="btn btn-primary" onclick="ThemeSettings.exportCurrentTheme()">
                        Export Theme
                    </button>
                </div>
                
                <div class="divider"></div>
                
                <div>
                    <h4 class="text-lg font-semibold mb-2">Import Theme</h4>
                    <p class="text-sm opacity-70 mb-4">Import a theme from a JSON file.</p>
                    <input type="file" id="themeImportFile" accept=".json" class="file-input file-input-bordered w-full max-w-xs"/>
                    <button class="btn btn-primary ml-2" onclick="ThemeSettings.importFromFile()">
                        Import
                    </button>
                </div>
                
                <div class="divider"></div>
                
                <div>
                    <h4 class="text-lg font-semibold mb-2">Import from Text</h4>
                    <p class="text-sm opacity-70 mb-4">Paste theme JSON data directly.</p>
                    <textarea id="themeImportText" class="textarea textarea-bordered w-full h-32" 
                              placeholder="Paste theme JSON here..."></textarea>
                    <button class="btn btn-primary mt-2" onclick="ThemeSettings.importFromText()">
                        Import from Text
                    </button>
                </div>
            </div>
        `;
        
        container.innerHTML = html;
    }
    
    // Helper to convert colors to hex
    function convertToHex(color) {
        // If already hex, return as is
        if (color.startsWith('#')) {
            return color.length === 4 ? 
                '#' + color[1] + color[1] + color[2] + color[2] + color[3] + color[3] : 
                color;
        }
        
        // Handle rgba
        if (color.startsWith('rgba')) {
            const match = color.match(/rgba?\((\d+),\s*(\d+),\s*(\d+)/);
            if (match) {
                const r = parseInt(match[1]).toString(16).padStart(2, '0');
                const g = parseInt(match[2]).toString(16).padStart(2, '0');
                const b = parseInt(match[3]).toString(16).padStart(2, '0');
                return '#' + r + g + b;
            }
        }
        
        return '#000000';
    }
    
    // Update custom color
    function updateCustomColor(colorKey) {
        const input = document.getElementById(`color_${colorKey}`);
        if (!input) return;
        
        // Apply immediately for preview
        const root = document.documentElement;
        const cssVarName = `--theme-${colorKey.replace(/([A-Z])/g, '-$1').toLowerCase()}`;
        root.style.setProperty(cssVarName, input.value);
    }
    
    // Preview custom theme
    function previewCustomTheme() {
        const colors = {};
        
        // Collect all color values
        Object.keys(ThemeManager.getDefaultThemes().classic.colors).forEach(key => {
            const input = document.getElementById(`color_${key}`);
            if (input) {
                colors[key] = input.value;
            }
        });
        
        // Apply preview
        ThemeManager.applyTheme({
            name: 'Preview',
            colors: colors,
            id: 'preview'
        });
    }
    
    // Save custom theme
    function saveCustomTheme() {
        const name = document.getElementById('customThemeName').value || 'Custom Theme';
        const desc = document.getElementById('customThemeDesc').value || 'User created theme';
        
        const colors = {};
        
        // Collect all color values
        Object.keys(ThemeManager.getDefaultThemes().classic.colors).forEach(key => {
            const input = document.getElementById(`color_${key}`);
            if (input) {
                colors[key] = input.value;
            }
        });
        
        const id = 'custom_' + Date.now();
        const theme = {
            name,
            description: desc,
            colors,
            id
        };
        
        ThemeManager.saveCustomTheme(id, theme);
        ThemeManager.applyTheme(theme);
        
        // Switch to themes tab to show new theme
        activeTab = 'themes';
        loadTabContent('themes');
        
        // Show success message
        showToast('Theme saved successfully!');
    }
    
    // Apply theme
    function applyTheme(id) {
        const themes = ThemeManager.getAllThemes();
        const theme = themes[id];
        if (theme) {
            theme.id = id;
            ThemeManager.applyTheme(theme);
            loadTabContent('themes'); // Refresh to update UI
            showToast(`Applied theme: ${theme.name}`);
        }
    }
    
    // Delete custom theme
    function deleteTheme(id) {
        if (confirm('Are you sure you want to delete this theme?')) {
            ThemeManager.deleteCustomTheme(id);
            loadTabContent('themes');
            showToast('Theme deleted');
        }
    }
    
    // Export current theme
    function exportCurrentTheme() {
        const theme = ThemeManager.getCurrentTheme();
        if (!theme) return;
        
        const json = ThemeManager.exportTheme(theme);
        const blob = new Blob([json], { type: 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `backgammon-theme-${theme.name.toLowerCase().replace(/\s+/g, '-')}.json`;
        a.click();
        URL.revokeObjectURL(url);
        
        showToast('Theme exported!');
    }
    
    // Import from file
    function importFromFile() {
        const input = document.getElementById('themeImportFile');
        if (!input.files.length) {
            showToast('Please select a file', 'error');
            return;
        }
        
        const file = input.files[0];
        const reader = new FileReader();
        
        reader.onload = function(e) {
            try {
                const theme = ThemeManager.importTheme(e.target.result);
                ThemeManager.applyTheme(theme);
                loadTabContent('themes');
                showToast('Theme imported successfully!');
            } catch (err) {
                showToast('Invalid theme file', 'error');
            }
        };
        
        reader.readAsText(file);
    }
    
    // Import from text
    function importFromText() {
        const text = document.getElementById('themeImportText').value;
        if (!text.trim()) {
            showToast('Please paste theme JSON', 'error');
            return;
        }
        
        try {
            const theme = ThemeManager.importTheme(text);
            ThemeManager.applyTheme(theme);
            loadTabContent('themes');
            showToast('Theme imported successfully!');
            document.getElementById('themeImportText').value = '';
        } catch (err) {
            showToast('Invalid theme JSON', 'error');
        }
    }
    
    // Show toast notification
    function showToast(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `alert alert-${type} fixed bottom-20 left-4 z-50 max-w-sm`;
        toast.innerHTML = `
            <span>${message}</span>
        `;
        document.body.appendChild(toast);
        
        setTimeout(() => {
            toast.remove();
        }, 3000);
    }
    
    // Initialize
    function init() {
        createSettingsButton();
    }
    
    // Public API
    return {
        init,
        toggle: toggleSettings,
        close: closeSettings,
        switchTab,
        applyTheme,
        deleteTheme,
        updateCustomColor,
        previewCustomTheme,
        saveCustomTheme,
        exportCurrentTheme,
        importFromFile,
        importFromText
    };
})();

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', ThemeSettings.init);
} else {
    ThemeSettings.init();
}