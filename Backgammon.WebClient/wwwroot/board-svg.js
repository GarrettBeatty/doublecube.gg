// board-svg.js - SVG Board Rendering Module for Backgammon

const BoardSVG = (function() {
    // SVG Namespace
    const SVG_NS = 'http://www.w3.org/2000/svg';

    // Configuration constants
    const CONFIG = {
        viewBox: { width: 1000, height: 500 },
        margin: 30,
        barWidth: 70,
        pointWidth: 72,
        pointHeight: 200,
        padding: 20,
        checkerRadius: 20,
        checkerSpacing: 38,
        bearoffWidth: 50
    };

    // Calculate bar X position (after 6 points + margin)
    CONFIG.barX = CONFIG.margin + (6 * CONFIG.pointWidth);

    // Color palette - Flat Modern Design
    const COLORS = {
        boardBackground: '#4A3728',
        boardBorder: '#2D1F15',
        pointLight: '#D4C4B0',
        pointDark: '#8B7355',
        bar: '#3D2E22',
        bearoff: '#3D2E22',

        checkerWhite: '#F5F5F5',
        checkerWhiteStroke: '#BDBDBD',
        checkerRed: '#D32F2F',
        checkerRedStroke: '#B71C1C',

        highlightSource: 'rgba(255, 213, 79, 0.6)',
        highlightSelected: 'rgba(76, 175, 80, 0.7)',
        highlightDest: 'rgba(33, 150, 243, 0.6)',
        highlightCapture: 'rgba(244, 67, 54, 0.6)',

        textLight: 'rgba(255, 255, 255, 0.5)',
        textDark: 'rgba(0, 0, 0, 0.7)'
    };

    // Pre-calculate point coordinates
    // Layout:
    //   Top: 13,14,15,16,17,18 | BAR | 19,20,21,22,23,24
    //   Bot: 12,11,10,9,8,7    | BAR | 6,5,4,3,2,1
    const POINT_COORDS = {};

    function calculatePointCoords() {
        const rightSideStart = CONFIG.barX + CONFIG.barWidth;

        // Top row - triangles point DOWN (direction = 1)
        // Points 13-18 (left of bar)
        for (let i = 0; i < 6; i++) {
            const pointNum = 13 + i;
            POINT_COORDS[pointNum] = {
                x: CONFIG.margin + (i * CONFIG.pointWidth),
                y: CONFIG.padding,
                direction: 1
            };
        }

        // Points 19-24 (right of bar)
        for (let i = 0; i < 6; i++) {
            const pointNum = 19 + i;
            POINT_COORDS[pointNum] = {
                x: rightSideStart + (i * CONFIG.pointWidth),
                y: CONFIG.padding,
                direction: 1
            };
        }

        // Bottom row - triangles point UP (direction = -1)
        // Points 12-7 (left of bar, right to left visually but numbered 12,11,10...)
        for (let i = 0; i < 6; i++) {
            const pointNum = 12 - i;
            POINT_COORDS[pointNum] = {
                x: CONFIG.margin + (i * CONFIG.pointWidth),
                y: CONFIG.viewBox.height - CONFIG.padding,
                direction: -1
            };
        }

        // Points 6-1 (right of bar)
        for (let i = 0; i < 6; i++) {
            const pointNum = 6 - i;
            POINT_COORDS[pointNum] = {
                x: rightSideStart + (i * CONFIG.pointWidth),
                y: CONFIG.viewBox.height - CONFIG.padding,
                direction: -1
            };
        }
    }

    // Initialize coordinates
    calculatePointCoords();

    // Module state
    let svgElement = null;
    let pointsGroup = null;
    let checkersGroup = null;
    let barGroup = null;
    let bearoffGroup = null;
    let diceGroup = null;
    let initialized = false;
    let clickHandler = null;

    // Create SVG element helper
    function createSVGElement(tag, attributes = {}) {
        const el = document.createElementNS(SVG_NS, tag);
        for (const [key, value] of Object.entries(attributes)) {
            el.setAttribute(key, value);
        }
        return el;
    }

    // Create point triangle polygon
    function createPointTriangle(pointNum) {
        const coords = POINT_COORDS[pointNum];
        const { x, y, direction } = coords;
        const w = CONFIG.pointWidth;
        const h = CONFIG.pointHeight * direction;

        // Triangle points
        let points;
        if (direction > 0) {
            // Top row - pointing down
            points = `${x},${y} ${x + w},${y} ${x + w/2},${y + h}`;
        } else {
            // Bottom row - pointing up
            points = `${x},${y} ${x + w},${y} ${x + w/2},${y + h}`;
        }

        const colorClass = pointNum % 2 === 0 ? 'point-dark' : 'point-light';

        const group = createSVGElement('g', {
            'id': `point-${pointNum}`,
            'class': 'point',
            'data-point': pointNum
        });

        // Triangle
        const triangle = createSVGElement('polygon', {
            'class': `point-triangle ${colorClass}`,
            'points': points
        });

        // Highlight overlay (initially hidden)
        const highlight = createSVGElement('polygon', {
            'class': 'point-highlight',
            'points': points
        });

        group.appendChild(triangle);
        group.appendChild(highlight);

        return group;
    }

    // Get checker position within a point
    function getCheckerPosition(pointNum, stackIndex) {
        // Special case: bar
        if (pointNum === 0 || pointNum === 'bar-white' || pointNum === 'bar-red') {
            const isWhite = pointNum === 'bar-white' || pointNum === 0;
            const barCenterX = CONFIG.barX + CONFIG.barWidth / 2;
            const baseY = isWhite
                ? CONFIG.viewBox.height / 2 + 20
                : CONFIG.viewBox.height / 2 - 20;
            const yOffset = isWhite ? stackIndex * 45 : -stackIndex * 45;
            return { x: barCenterX, y: baseY + yOffset };
        }

        // Special case: bear-off
        if (pointNum === 25 || pointNum === 'bearoff-white' || pointNum === 'bearoff-red') {
            const isWhite = pointNum === 'bearoff-white';
            const bearoffX = CONFIG.viewBox.width - CONFIG.bearoffWidth / 2 - 10;
            const baseY = isWhite
                ? CONFIG.viewBox.height - 40 - (stackIndex * 20)
                : 40 + (stackIndex * 20);
            return { x: bearoffX, y: baseY };
        }

        const coords = POINT_COORDS[pointNum];
        if (!coords) return { x: 0, y: 0 };

        const x = coords.x + CONFIG.pointWidth / 2;
        const r = CONFIG.checkerRadius;
        const spacing = CONFIG.checkerSpacing;

        let baseY;
        if (coords.direction > 0) {
            // Top row - stack downward
            baseY = coords.y + r + 5;
            return { x, y: baseY + (stackIndex * spacing) };
        } else {
            // Bottom row - stack upward
            baseY = coords.y - r - 5;
            return { x, y: baseY - (stackIndex * spacing) };
        }
    }

    // Create checker circle
    function createChecker(pointNum, stackIndex, color, isSelected = false) {
        const pos = getCheckerPosition(pointNum, stackIndex);
        const colorClass = color === 0 ? 'checker-white' : 'checker-red';
        const selectedClass = isSelected ? 'selected' : '';

        const circle = createSVGElement('circle', {
            'class': `checker ${colorClass} ${selectedClass}`.trim(),
            'cx': pos.x,
            'cy': pos.y,
            'r': CONFIG.checkerRadius,
            'data-point': pointNum,
            'data-index': stackIndex,
            'data-color': color
        });

        return circle;
    }

    // Create checker count text
    function createCheckerCount(pointNum, stackIndex, count, color) {
        const pos = getCheckerPosition(pointNum, stackIndex);
        const textClass = color === 0 ? 'count-dark' : 'count-light';

        const text = createSVGElement('text', {
            'class': `checker-count ${textClass}`,
            'x': pos.x,
            'y': pos.y,
            'text-anchor': 'middle',
            'dominant-baseline': 'central'
        });
        text.textContent = count;

        return text;
    }

    // Initialize SVG structure
    function init(containerId) {
        svgElement = document.getElementById(containerId);
        if (!svgElement) {
            console.error('SVG container not found:', containerId);
            return false;
        }

        // Clear existing content
        svgElement.innerHTML = '';

        // Set viewBox
        svgElement.setAttribute('viewBox', `0 0 ${CONFIG.viewBox.width} ${CONFIG.viewBox.height}`);
        svgElement.setAttribute('preserveAspectRatio', 'xMidYMid meet');

        // Background
        const background = createSVGElement('rect', {
            'class': 'board-background',
            'x': 0,
            'y': 0,
            'width': CONFIG.viewBox.width,
            'height': CONFIG.viewBox.height,
            'rx': 8,
            'ry': 8
        });
        svgElement.appendChild(background);

        // Border
        const border = createSVGElement('rect', {
            'class': 'board-border',
            'x': 2,
            'y': 2,
            'width': CONFIG.viewBox.width - 4,
            'height': CONFIG.viewBox.height - 4,
            'rx': 6,
            'ry': 6,
            'fill': 'none',
            'stroke-width': 3
        });
        svgElement.appendChild(border);

        // Bar
        barGroup = createSVGElement('g', { 'id': 'bar' });
        const barRect = createSVGElement('rect', {
            'class': 'bar-background',
            'x': CONFIG.barX,
            'y': 0,
            'width': CONFIG.barWidth,
            'height': CONFIG.viewBox.height
        });
        barGroup.appendChild(barRect);
        svgElement.appendChild(barGroup);

        // Bear-off area (right side)
        bearoffGroup = createSVGElement('g', { 'id': 'bearoff' });
        const bearoffRect = createSVGElement('rect', {
            'class': 'bearoff-background',
            'x': CONFIG.viewBox.width - CONFIG.bearoffWidth - 5,
            'y': 5,
            'width': CONFIG.bearoffWidth,
            'height': CONFIG.viewBox.height - 10,
            'rx': 4
        });
        bearoffGroup.appendChild(bearoffRect);

        // Bear-off divider
        const bearoffDivider = createSVGElement('line', {
            'class': 'bearoff-divider',
            'x1': CONFIG.viewBox.width - CONFIG.bearoffWidth - 5,
            'y1': CONFIG.viewBox.height / 2,
            'x2': CONFIG.viewBox.width - 5,
            'y2': CONFIG.viewBox.height / 2
        });
        bearoffGroup.appendChild(bearoffDivider);
        svgElement.appendChild(bearoffGroup);

        // Points group
        pointsGroup = createSVGElement('g', { 'id': 'points' });
        for (let i = 1; i <= 24; i++) {
            pointsGroup.appendChild(createPointTriangle(i));
        }
        svgElement.appendChild(pointsGroup);

        // Dice group (in center bar)
        diceGroup = createSVGElement('g', { 'id': 'dice' });
        svgElement.appendChild(diceGroup);

        // Checkers group (rendered on top)
        checkersGroup = createSVGElement('g', { 'id': 'checkers' });
        svgElement.appendChild(checkersGroup);

        initialized = true;
        return true;
    }

    // Render checkers based on game state
    function renderCheckers(gameState, selectedChecker) {
        if (!checkersGroup) return;

        // Clear existing checkers
        checkersGroup.innerHTML = '';

        if (!gameState || !gameState.board) return;

        // Render checkers on points 1-24
        gameState.board.forEach((point, index) => {
            const pointNum = index + 1; // board array is 0-indexed, points are 1-indexed
            if (point.count > 0) {
                const maxVisible = Math.min(point.count, 5);
                for (let i = 0; i < maxVisible; i++) {
                    const isSelected = selectedChecker &&
                        selectedChecker.point === pointNum &&
                        i === maxVisible - 1;
                    const checker = createChecker(pointNum, i, point.color, isSelected);
                    checkersGroup.appendChild(checker);
                }

                // Show count if more than 5
                if (point.count > 5) {
                    const countText = createCheckerCount(pointNum, 4, point.count, point.color);
                    checkersGroup.appendChild(countText);
                }
            }
        });

        // Render bar checkers
        if (gameState.whiteCheckersOnBar > 0) {
            const maxVisible = Math.min(gameState.whiteCheckersOnBar, 3);
            for (let i = 0; i < maxVisible; i++) {
                const isSelected = selectedChecker && selectedChecker.point === 0;
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 + 40 + (i * 50)
                };
                const checker = createSVGElement('circle', {
                    'class': `checker checker-white ${isSelected ? 'selected' : ''}`,
                    'cx': pos.x,
                    'cy': pos.y,
                    'r': CONFIG.checkerRadius,
                    'data-point': 0,
                    'data-color': 0
                });
                checkersGroup.appendChild(checker);
            }
            if (gameState.whiteCheckersOnBar > 3) {
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 + 40 + (2 * 50)
                };
                const text = createSVGElement('text', {
                    'class': 'checker-count count-dark',
                    'x': pos.x,
                    'y': pos.y,
                    'text-anchor': 'middle',
                    'dominant-baseline': 'central'
                });
                text.textContent = gameState.whiteCheckersOnBar;
                checkersGroup.appendChild(text);
            }
        }

        if (gameState.redCheckersOnBar > 0) {
            const maxVisible = Math.min(gameState.redCheckersOnBar, 3);
            for (let i = 0; i < maxVisible; i++) {
                const isSelected = selectedChecker && selectedChecker.point === 0;
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 - 40 - (i * 50)
                };
                const checker = createSVGElement('circle', {
                    'class': `checker checker-red ${isSelected ? 'selected' : ''}`,
                    'cx': pos.x,
                    'cy': pos.y,
                    'r': CONFIG.checkerRadius,
                    'data-point': 0,
                    'data-color': 1
                });
                checkersGroup.appendChild(checker);
            }
            if (gameState.redCheckersOnBar > 3) {
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 - 40 - (2 * 50)
                };
                const text = createSVGElement('text', {
                    'class': 'checker-count count-light',
                    'x': pos.x,
                    'y': pos.y,
                    'text-anchor': 'middle',
                    'dominant-baseline': 'central'
                });
                text.textContent = gameState.redCheckersOnBar;
                checkersGroup.appendChild(text);
            }
        }

        // Render bear-off counts
        const bearoffX = CONFIG.viewBox.width - CONFIG.bearoffWidth / 2 - 5;

        if (gameState.whiteBornOff > 0) {
            const text = createSVGElement('text', {
                'class': 'bearoff-count',
                'x': bearoffX,
                'y': CONFIG.viewBox.height - 60,
                'text-anchor': 'middle',
                'dominant-baseline': 'central'
            });
            text.textContent = gameState.whiteBornOff;
            checkersGroup.appendChild(text);

            // Small checker icon
            const icon = createSVGElement('circle', {
                'class': 'checker checker-white',
                'cx': bearoffX,
                'cy': CONFIG.viewBox.height - 100,
                'r': 18
            });
            checkersGroup.appendChild(icon);
        }

        if (gameState.redBornOff > 0) {
            const text = createSVGElement('text', {
                'class': 'bearoff-count',
                'x': bearoffX,
                'y': 60,
                'text-anchor': 'middle',
                'dominant-baseline': 'central'
            });
            text.textContent = gameState.redBornOff;
            checkersGroup.appendChild(text);

            // Small checker icon
            const icon = createSVGElement('circle', {
                'class': 'checker checker-red',
                'cx': bearoffX,
                'cy': 100,
                'r': 18
            });
            checkersGroup.appendChild(icon);
        }
    }

    // Determine which dice have been used
    function getDiceUsedState(originalDice, remainingMoves) {
        if (!originalDice || originalDice.length === 0) return [];
        if (!remainingMoves) remainingMoves = [];

        // Create a copy of remaining moves to track
        const remaining = [...remainingMoves];

        return originalDice.map(value => {
            const idx = remaining.indexOf(value);
            if (idx !== -1) {
                remaining.splice(idx, 1); // Remove from tracking
                return { value, used: false };
            }
            return { value, used: true };
        });
    }

    // Render dice in the center bar
    function renderDice(diceState) {
        if (!diceGroup) return;

        // Clear existing dice
        diceGroup.innerHTML = '';

        if (!diceState || !diceState.dice || diceState.dice.length === 0) return;

        // Check if dice have been rolled (not all zeros)
        const hasRolled = diceState.dice.some(d => d > 0);
        if (!hasRolled) return;

        const diceWithState = getDiceUsedState(diceState.dice, diceState.remainingMoves);
        const numDice = diceWithState.length;

        // Dice dimensions
        const dieSize = 44;
        const dieRadius = 6;
        const barCenterX = CONFIG.barX + CONFIG.barWidth / 2;
        const boardCenterY = CONFIG.viewBox.height / 2;

        // Calculate positions based on number of dice
        let positions = [];
        if (numDice === 2) {
            // Stack vertically in center
            positions = [
                { x: barCenterX - dieSize / 2, y: boardCenterY - dieSize - 8 },
                { x: barCenterX - dieSize / 2, y: boardCenterY + 8 }
            ];
        } else if (numDice === 4) {
            // 2x2 grid for doubles
            const offsetX = 24;
            const offsetY = 26;
            positions = [
                { x: barCenterX - offsetX - dieSize / 2, y: boardCenterY - offsetY - dieSize / 2 },
                { x: barCenterX + offsetX - dieSize / 2, y: boardCenterY - offsetY - dieSize / 2 },
                { x: barCenterX - offsetX - dieSize / 2, y: boardCenterY + offsetY - dieSize / 2 },
                { x: barCenterX + offsetX - dieSize / 2, y: boardCenterY + offsetY - dieSize / 2 }
            ];
        }

        // Render each die
        diceWithState.forEach((die, index) => {
            if (index >= positions.length) return;

            const pos = positions[index];
            const usedClass = die.used ? 'used' : '';

            const group = createSVGElement('g', {
                'class': `board-die ${usedClass}`.trim(),
                'transform': `translate(${pos.x}, ${pos.y})`
            });

            // Die face (rounded rectangle)
            const face = createSVGElement('rect', {
                'class': 'die-face',
                'width': dieSize,
                'height': dieSize,
                'rx': dieRadius,
                'ry': dieRadius
            });
            group.appendChild(face);

            // Die value (text)
            const text = createSVGElement('text', {
                'class': 'die-value',
                'x': dieSize / 2,
                'y': dieSize / 2,
                'text-anchor': 'middle',
                'dominant-baseline': 'central'
            });
            text.textContent = die.value;
            group.appendChild(text);

            diceGroup.appendChild(group);
        });
    }

    // Update highlights based on selection state
    function updateHighlights(validSources, selectedPoint, validDestinations) {
        if (!pointsGroup) return;

        // Reset all point highlights
        pointsGroup.querySelectorAll('.point').forEach(point => {
            point.classList.remove('valid-source', 'selected', 'valid-destination', 'capture');
        });

        // Highlight valid source points
        if (validSources && validSources.length > 0) {
            validSources.forEach(pointNum => {
                const point = pointsGroup.querySelector(`#point-${pointNum}`);
                if (point) {
                    point.classList.add('valid-source');
                }
            });

            // Also highlight bar if it's a valid source (point 0)
            if (validSources.includes(0)) {
                barGroup?.classList.add('valid-source');
            } else {
                barGroup?.classList.remove('valid-source');
            }
        }

        // Highlight selected point
        if (selectedPoint !== null && selectedPoint !== undefined) {
            if (selectedPoint === 0) {
                barGroup?.classList.add('selected');
            } else {
                const point = pointsGroup.querySelector(`#point-${selectedPoint}`);
                if (point) {
                    point.classList.add('selected');
                }
            }
        }

        // Highlight valid destinations
        if (validDestinations && validDestinations.length > 0) {
            validDestinations.forEach(dest => {
                const pointNum = dest.to || dest.point || dest;
                const isCapture = dest.isHit || false;

                if (pointNum === 25) {
                    // Bear-off highlight
                    bearoffGroup?.classList.add('valid-destination');
                } else {
                    const point = pointsGroup.querySelector(`#point-${pointNum}`);
                    if (point) {
                        point.classList.add('valid-destination');
                        if (isCapture) {
                            point.classList.add('capture');
                        }
                    }
                }
            });
        } else {
            bearoffGroup?.classList.remove('valid-destination');
        }
    }

    // Main render function
    function render(gameState, selectedChecker, validDestinations, validSources, diceState) {
        if (!initialized) {
            console.warn('BoardSVG not initialized');
            return;
        }

        renderCheckers(gameState, selectedChecker);
        renderDice(diceState);
        updateHighlights(
            validSources,
            selectedChecker?.point,
            validDestinations
        );
    }

    // Get point number from click coordinates
    function getPointAtPosition(clientX, clientY) {
        if (!svgElement) return null;

        const rect = svgElement.getBoundingClientRect();
        const scaleX = CONFIG.viewBox.width / rect.width;
        const scaleY = CONFIG.viewBox.height / rect.height;

        const x = (clientX - rect.left) * scaleX;
        const y = (clientY - rect.top) * scaleY;

        // Check bar
        if (x >= CONFIG.barX && x <= CONFIG.barX + CONFIG.barWidth) {
            return 0; // Bar
        }

        // Check bear-off
        if (x >= CONFIG.viewBox.width - CONFIG.bearoffWidth - 10) {
            return 25; // Bear-off
        }

        // Check points
        const isTop = y < CONFIG.viewBox.height / 2;
        const isLeftSide = x < CONFIG.barX;

        let pointIndex;
        if (isLeftSide) {
            pointIndex = Math.floor((x - CONFIG.margin) / CONFIG.pointWidth);
            if (pointIndex < 0 || pointIndex > 5) return null;
        } else {
            const rightStart = CONFIG.barX + CONFIG.barWidth;
            pointIndex = Math.floor((x - rightStart) / CONFIG.pointWidth);
            if (pointIndex < 0 || pointIndex > 5) return null;
            pointIndex += 6;
        }

        // Map to point number
        if (isTop) {
            // Top row: 13-18 (left), 19-24 (right)
            if (pointIndex < 6) {
                return 13 + pointIndex;
            } else {
                return 13 + pointIndex; // 19-24
            }
        } else {
            // Bottom row: 12-7 (left), 6-1 (right)
            if (pointIndex < 6) {
                return 12 - pointIndex;
            } else {
                return 12 - pointIndex; // 6-1
            }
        }
    }

    // Animate checker movement
    function animateMove(fromPoint, toPoint, color, callback) {
        const startPos = getCheckerPosition(fromPoint, 0);
        const endPos = getCheckerPosition(toPoint, 0);

        const colorClass = color === 0 ? 'checker-white' : 'checker-red';

        const checker = createSVGElement('circle', {
            'class': `checker ${colorClass} animating`,
            'cx': startPos.x,
            'cy': startPos.y,
            'r': CONFIG.checkerRadius
        });

        checkersGroup.appendChild(checker);

        // Use Web Animations API
        const animation = checker.animate([
            { transform: 'translate(0, 0)' },
            { transform: `translate(${endPos.x - startPos.x}px, ${endPos.y - startPos.y}px)` }
        ], {
            duration: 250,
            easing: 'cubic-bezier(0.4, 0, 0.2, 1)',
            fill: 'forwards'
        });

        animation.onfinish = () => {
            checker.remove();
            if (callback) callback();
        };
    }

    // Public API
    return {
        init,
        render,
        getPointAtPosition,
        animateMove,
        isInitialized: () => initialized,
        getConfig: () => ({ ...CONFIG }),
        getColors: () => ({ ...COLORS })
    };
})();
