// theme-settings.js - Theme Settings UI Component

const ThemeSettings = (function() {
    let isOpen = false;
    let modal = null;

    // Initialize the theme settings component
    function init() {
        // Create settings button
        createSettingsButton();

        // Create settings modal
        createSettingsModal();

        // Initialize theme system
        ThemeManager.init();

        // Listen for theme changes to re-render board
        window.addEventListener('themeChanged', (event) => {
            // Force board re-render if game is active
            if (typeof window.renderBoard === 'function') {
                window.renderBoard();
            }
        });
    }

    // Create floating settings button
    function createSettingsButton() {
        const button = document.createElement('button');
        button.id = 'themeSettingsBtn';
        button.className = 'btn btn-circle btn-primary fixed bottom-4 left-4 shadow-lg z-50';
        button.innerHTML = `
            <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6V4m0 2a2 2 0 100 4m0-4a2 2 0 110 4m-6 8a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4m6 6v10m6-2a2 2 0 100-4m0 4a2 2 0 110-4m0 4v2m0-6V4"></path>
            </svg>
        `;
        button.title = 'Theme Settings';
        button.addEventListener('click', toggle);
        
        document.body.appendChild(button);
    }

    // Create settings modal
    function createSettingsModal() {
        modal = document.createElement('div');
        modal.id = 'themeSettingsModal';
        modal.className = 'modal';
        modal.innerHTML = `
            <div class="modal-box max-w-2xl">
                <h3 class="font-bold text-lg mb-4">Board Theme Settings</h3>
                
                <div class="tabs tabs-boxed mb-4">
                    <a class="tab tab-active" data-tab="themes">Themes</a>
                    <a class="tab" data-tab="customize">Customize</a>
                    <a class="tab" data-tab="import-export">Import/Export</a>
                </div>
                
                <!-- Themes Tab -->
                <div id="themesTab" class="tab-content">
                    <p class="text-sm opacity-70 mb-4">Choose a pre-defined theme for your board</p>
                    <div id="themeGrid" class="grid grid-cols-2 gap-4">
                        <!-- Theme previews will be inserted here -->
                    </div>
                </div>
                
                <!-- Customize Tab -->
                <div id="customizeTab" class="tab-content hidden">
                    <p class="text-sm opacity-70 mb-4">Create your own custom theme</p>
                    <div class="space-y-4" id="colorPickers">
                        <!-- Color pickers will be inserted here -->
                    </div>
                    <div class="mt-6 flex gap-2">
                        <button class="btn btn-primary" onclick="ThemeSettings.saveCustomTheme()">Save Theme</button>
                        <button class="btn btn-ghost" onclick="ThemeSettings.resetCustomColors()">Reset</button>
                    </div>
                </div>
                
                <!-- Import/Export Tab -->
                <div id="importExportTab" class="tab-content hidden">
                    <p class="text-sm opacity-70 mb-4">Share themes with others</p>
                    <div class="form-control mb-4">
                        <label class="label">
                            <span class="label-text">Export Current Theme</span>
                        </label>
                        <div class="flex gap-2">
                            <select id="exportThemeSelect" class="select select-bordered flex-1">
                                <!-- Theme options will be inserted here -->
                            </select>
                            <button class="btn btn-primary" onclick="ThemeSettings.exportTheme()">Export</button>
                        </div>
                    </div>
                    <div class="form-control">
                        <label class="label">
                            <span class="label-text">Import Theme</span>
                        </label>
                        <textarea id="importThemeText" class="textarea textarea-bordered h-32" placeholder="Paste theme JSON here..."></textarea>
                        <button class="btn btn-primary mt-2" onclick="ThemeSettings.importTheme()">Import</button>
                    </div>
                </div>
                
                <div class="modal-action">
                    <button class="btn" onclick="ThemeSettings.close()">Close</button>
                </div>
            </div>
            <div class="modal-backdrop" onclick="ThemeSettings.close()"></div>
        `;
        
        document.body.appendChild(modal);

        // Set up tab switching
        modal.querySelectorAll('.tab').forEach(tab => {
            tab.addEventListener('click', () => switchTab(tab.dataset.tab));
        });

        // Load themes
        loadThemes();
        loadColorPickers();
        loadExportOptions();
    }

    // Load theme previews
    function loadThemes() {
        const themes = ThemeManager.getAllThemes();
        const currentTheme = ThemeManager.getCurrentTheme();
        const grid = document.getElementById('themeGrid');
        
        grid.innerHTML = '';
        
        Object.values(themes).forEach(theme => {
            const isActive = currentTheme && currentTheme.id === theme.id;
            const preview = createThemePreview(theme, isActive);
            grid.appendChild(preview);
        });
    }

    // Create theme preview card
    function createThemePreview(theme, isActive) {
        const card = document.createElement('div');
        card.className = `card bg-base-200 shadow-xl cursor-pointer transition-all ${isActive ? 'ring-2 ring-primary' : ''}`;
        card.onclick = () => applyTheme(theme.id);
        
        // Create mini board preview
        const preview = createMiniBoardPreview(theme);
        
        card.innerHTML = `
            <div class="card-body p-4">
                ${preview}
                <h4 class="font-semibold mt-2">${theme.name}</h4>
                <p class="text-sm opacity-70">${theme.description}</p>
                ${isActive ? '<div class="badge badge-primary mt-2">Active</div>' : ''}
            </div>
        `;
        
        return card;
    }

    // Create mini board preview SVG
    function createMiniBoardPreview(theme) {
        return `
            <svg viewBox="0 0 200 100" class="w-full h-24 rounded">
                <!-- Board background -->
                <rect x="0" y="0" width="200" height="100" fill="${theme.colors.boardBackground}" />
                
                <!-- Points -->
                <polygon points="10,0 30,0 20,40" fill="${theme.colors.pointLight}" />
                <polygon points="30,0 50,0 40,40" fill="${theme.colors.pointDark}" />
                <polygon points="50,0 70,0 60,40" fill="${theme.colors.pointLight}" />
                <polygon points="70,0 90,0 80,40" fill="${theme.colors.pointDark}" />
                
                <polygon points="110,0 130,0 120,40" fill="${theme.colors.pointLight}" />
                <polygon points="130,0 150,0 140,40" fill="${theme.colors.pointDark}" />
                <polygon points="150,0 170,0 160,40" fill="${theme.colors.pointLight}" />
                <polygon points="170,0 190,0 180,40" fill="${theme.colors.pointDark}" />
                
                <polygon points="10,100 30,100 20,60" fill="${theme.colors.pointDark}" />
                <polygon points="30,100 50,100 40,60" fill="${theme.colors.pointLight}" />
                <polygon points="50,100 70,100 60,60" fill="${theme.colors.pointDark}" />
                <polygon points="70,100 90,100 80,60" fill="${theme.colors.pointLight}" />
                
                <polygon points="110,100 130,100 120,60" fill="${theme.colors.pointDark}" />
                <polygon points="130,100 150,100 140,60" fill="${theme.colors.pointLight}" />
                <polygon points="150,100 170,100 160,60" fill="${theme.colors.pointDark}" />
                <polygon points="170,100 190,100 180,60" fill="${theme.colors.pointLight}" />
                
                <!-- Bar -->
                <rect x="95" y="0" width="10" height="100" fill="${theme.colors.bar}" />
                
                <!-- Sample checkers -->
                <circle cx="20" cy="15" r="8" fill="${theme.colors.checkerWhite}" stroke="${theme.colors.checkerWhiteStroke}" />
                <circle cx="20" cy="30" r="8" fill="${theme.colors.checkerWhite}" stroke="${theme.colors.checkerWhiteStroke}" />
                <circle cx="180" cy="15" r="8" fill="${theme.colors.checkerRed}" stroke="${theme.colors.checkerRedStroke}" />
                <circle cx="180" cy="30" r="8" fill="${theme.colors.checkerRed}" stroke="${theme.colors.checkerRedStroke}" />
            </svg>
        `;
    }

    // Load color pickers for customization
    function loadColorPickers() {
        const container = document.getElementById('colorPickers');
        const currentTheme = ThemeManager.getCurrentTheme();
        if (!currentTheme) return;

        const colorGroups = {
            'Board Colors': ['boardBackground', 'boardBorder', 'bar', 'bearoff'],
            'Point Colors': ['pointLight', 'pointDark'],
            'Checker Colors': ['checkerWhite', 'checkerWhiteStroke', 'checkerRed', 'checkerRedStroke'],
            'Highlight Colors': ['highlightSource', 'highlightSelected', 'highlightDest', 'highlightCapture'],
            'Dice Colors': ['diceBackground', 'diceBorder', 'diceText'],
            'Button Colors': ['buttonRollBg', 'buttonConfirmBg', 'buttonUndoBg', 'buttonDoubleBg']
        };

        container.innerHTML = '';

        Object.entries(colorGroups).forEach(([groupName, colors]) => {
            const group = document.createElement('div');
            group.className = 'border rounded-lg p-4';
            group.innerHTML = `<h4 class="font-semibold mb-2">${groupName}</h4>`;
            
            const grid = document.createElement('div');
            grid.className = 'grid grid-cols-2 gap-2';
            
            colors.forEach(colorKey => {
                const colorValue = currentTheme.colors[colorKey] || '#000000';
                const label = colorKey.replace(/([A-Z])/g, ' $1').trim();
                
                const inputWrapper = document.createElement('div');
                inputWrapper.className = 'flex items-center gap-2';
                inputWrapper.innerHTML = `
                    <input type="color" id="color-${colorKey}" value="${colorValue}" class="w-12 h-8" 
                           onchange="ThemeSettings.updateCustomColor('${colorKey}', this.value)">
                    <label for="color-${colorKey}" class="text-sm">${label}</label>
                `;
                
                grid.appendChild(inputWrapper);
            });
            
            group.appendChild(grid);
            container.appendChild(group);
        });
    }

    // Load export options
    function loadExportOptions() {
        const select = document.getElementById('exportThemeSelect');
        const themes = ThemeManager.getAllThemes();
        
        select.innerHTML = '';
        Object.values(themes).forEach(theme => {
            const option = document.createElement('option');
            option.value = theme.id;
            option.textContent = theme.name;
            select.appendChild(option);
        });
    }

    // Switch tabs
    function switchTab(tabName) {
        modal.querySelectorAll('.tab').forEach(tab => {
            tab.classList.toggle('tab-active', tab.dataset.tab === tabName);
        });
        
        modal.querySelectorAll('.tab-content').forEach(content => {
            content.classList.add('hidden');
        });
        
        const activeContent = document.getElementById(`${tabName}Tab`);
        if (activeContent) {
            activeContent.classList.remove('hidden');
        }
    }

    // Apply theme
    function applyTheme(themeId) {
        ThemeManager.applyTheme(themeId);
        loadThemes(); // Refresh preview
        loadColorPickers(); // Refresh color pickers
    }

    // Update custom color
    function updateCustomColor(colorKey, value) {
        // This updates the preview in real-time
        document.documentElement.style.setProperty(`--theme-${colorKey.replace(/([A-Z])/g, '-$1').toLowerCase()}`, value);
    }

    // Save custom theme
    function saveCustomTheme() {
        const name = prompt('Enter a name for your custom theme:');
        if (!name) return;

        const colors = {};
        document.querySelectorAll('#colorPickers input[type="color"]').forEach(input => {
            const key = input.id.replace('color-', '');
            colors[key] = input.value;
        });

        const theme = ThemeManager.createThemeFromColors(name, colors);
        if (ThemeManager.saveCustomTheme(theme)) {
            alert('Theme saved successfully!');
            loadThemes();
            loadExportOptions();
            applyTheme(theme.id);
        } else {
            alert('Failed to save theme');
        }
    }

    // Reset custom colors
    function resetCustomColors() {
        const currentTheme = ThemeManager.getCurrentTheme();
        if (currentTheme) {
            loadColorPickers();
            applyTheme(currentTheme.id);
        }
    }

    // Export theme
    function exportTheme() {
        const select = document.getElementById('exportThemeSelect');
        const themeId = select.value;
        const json = ThemeManager.exportTheme(themeId);
        
        if (json) {
            // Copy to clipboard
            navigator.clipboard.writeText(json).then(() => {
                alert('Theme copied to clipboard!');
            }).catch(() => {
                // Fallback: show in textarea
                const textarea = document.createElement('textarea');
                textarea.value = json;
                textarea.className = 'textarea textarea-bordered w-full h-48 mt-4';
                textarea.readOnly = true;
                select.parentElement.appendChild(textarea);
                textarea.select();
            });
        }
    }

    // Import theme
    function importTheme() {
        const textarea = document.getElementById('importThemeText');
        const json = textarea.value.trim();
        
        if (!json) {
            alert('Please paste theme JSON');
            return;
        }

        const theme = ThemeManager.importTheme(json);
        if (theme) {
            alert('Theme imported successfully!');
            textarea.value = '';
            loadThemes();
            loadExportOptions();
            applyTheme(theme.id);
        } else {
            alert('Invalid theme format');
        }
    }

    // Toggle modal
    function toggle() {
        isOpen = !isOpen;
        if (isOpen) {
            modal.classList.add('modal-open');
        } else {
            modal.classList.remove('modal-open');
        }
    }

    // Open modal
    function open() {
        isOpen = true;
        modal.classList.add('modal-open');
    }

    // Close modal
    function close() {
        isOpen = false;
        modal.classList.remove('modal-open');
    }

    // Public API
    return {
        init,
        open,
        close,
        toggle,
        saveCustomTheme,
        resetCustomColors,
        exportTheme,
        importTheme,
        updateCustomColor
    };
})();