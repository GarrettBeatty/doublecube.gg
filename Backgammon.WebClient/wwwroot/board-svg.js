// board-svg.js - SVG Board Rendering Module for Backgammon

const BoardSVG = (function() {
    // SVG Namespace
    const SVG_NS = 'http://www.w3.org/2000/svg';

    // Configuration constants
    const CONFIG = {
        viewBox: { width: 1020, height: 500 }, // Reduced width (removed sidebar)
        sidebarWidth: 0, // No sidebar (cube moved to center)
        margin: 30,
        barWidth: 70,
        pointWidth: 72,
        pointHeight: 200,
        padding: 20,
        checkerRadius: 20,
        checkerSpacing: 38,
        bearoffWidth: 50
    };

    // Calculate board start X (after sidebar)
    CONFIG.boardStartX = CONFIG.sidebarWidth + CONFIG.margin;

    // Calculate bar X position (after sidebar + 6 points + margin)
    CONFIG.barX = CONFIG.boardStartX + (6 * CONFIG.pointWidth);

    // Color palette - Match Backgammon Galaxy style
    const COLORS = {
        boardBackground: '#5d4e37',
        boardBorder: '#4a7c4e',
        pointLight: '#d4b896',
        pointDark: '#6b5a47',
        bar: '#3d3024',
        bearoff: '#3d3024',

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
                x: CONFIG.boardStartX + (i * CONFIG.pointWidth),
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
                x: CONFIG.boardStartX + (i * CONFIG.pointWidth),
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
    let cubeGroup = null;
    let initialized = false;
    let clickHandler = null;

    // Drag and drop state
    let dragState = {
        isDragging: false,
        draggedChecker: null,
        draggedCheckerOriginalPos: null,
        sourcePoint: null,
        ghostChecker: null,
        offset: { x: 0, y: 0 }
    };

    // Callback handlers for drag and drop
    let moveHandlers = {
        onSelectChecker: null,
        onExecuteMove: null,
        getRenderCallback: null,
        getValidDestinations: null
    };

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
    function createChecker(pointNum, stackIndex, color, isSelected = false, isDraggable = false) {
        const pos = getCheckerPosition(pointNum, stackIndex);
        const colorClass = color === 0 ? 'checker-white' : 'checker-red';
        const selectedClass = isSelected ? 'selected' : '';
        const draggableClass = isDraggable ? 'draggable' : '';

        console.log(`[BoardSVG] Creating checker at point ${pointNum}, index ${stackIndex}, isDraggable: ${isDraggable}`);

        const circle = createSVGElement('circle', {
            'class': `checker ${colorClass} ${selectedClass} ${draggableClass}`.trim(),
            'cx': pos.x,
            'cy': pos.y,
            'r': CONFIG.checkerRadius,
            'data-point': pointNum,
            'data-index': stackIndex,
            'data-color': color
        });

        // Make draggable checkers interactive
        if (isDraggable) {
            console.log(`[BoardSVG] Making checker at point ${pointNum} draggable, adding listeners to:`, circle);
            circle.style.cursor = 'grab';

            // Add a simple click test
            circle.addEventListener('click', (e) => {
                console.log(`[BoardSVG] CLICK EVENT on checker at point ${pointNum}!`);
            });

            circle.addEventListener('mousedown', handleCheckerMouseDown, true); // Use capture phase
            circle.addEventListener('touchstart', handleCheckerTouchStart, { passive: false, capture: true });

            // Verify listener was added
            console.log(`[BoardSVG] Event listeners added to checker at point ${pointNum}`);
        }

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

        // Doubling cube group (in center bar)
        cubeGroup = createSVGElement('g', { 'id': 'cube' });
        svgElement.appendChild(cubeGroup);

        // Checkers group (rendered on top)
        checkersGroup = createSVGElement('g', { 'id': 'checkers' });
        svgElement.appendChild(checkersGroup);

        initialized = true;
        return true;
    }

    // ===== DRAG AND DROP HANDLERS =====

    function handleCheckerMouseDown(event) {
        console.log('[BoardSVG] Mouse down on checker', event.currentTarget);
        event.preventDefault();
        event.stopPropagation();
        startDrag(event, event.currentTarget, event.clientX, event.clientY);
    }

    function handleCheckerTouchStart(event) {
        console.log('[BoardSVG] Touch start on checker', event.currentTarget);
        event.preventDefault();
        event.stopPropagation();
        const touch = event.touches[0];
        startDrag(event, event.currentTarget, touch.clientX, touch.clientY);
    }

    function startDrag(event, checkerElement, clientX, clientY) {
        const pointNum = parseInt(checkerElement.getAttribute('data-point'));
        const color = parseInt(checkerElement.getAttribute('data-color'));

        console.log(`[BoardSVG] Starting drag from point ${pointNum}, color ${color}`);
        console.log('[BoardSVG] Move handlers:', moveHandlers);

        // Get SVG coordinates
        const svgPoint = svgElement.createSVGPoint();
        svgPoint.x = clientX;
        svgPoint.y = clientY;
        const ctm = svgElement.getScreenCTM();
        const svgCoords = svgPoint.matrixTransform(ctm.inverse());

        console.log(`[BoardSVG] Client coords: (${clientX}, ${clientY}), SVG coords: (${svgCoords.x}, ${svgCoords.y})`);

        // Store drag state
        dragState.isDragging = true;
        dragState.sourcePoint = pointNum;
        dragState.draggedChecker = checkerElement;
        dragState.draggedCheckerOriginalPos = {
            cx: parseFloat(checkerElement.getAttribute('cx')),
            cy: parseFloat(checkerElement.getAttribute('cy'))
        };
        dragState.offset = {
            x: svgCoords.x - dragState.draggedCheckerOriginalPos.cx,
            y: svgCoords.y - dragState.draggedCheckerOriginalPos.cy
        };

        console.log('[BoardSVG] Drag state set:', dragState);

        // Create ghost checker for drag visual
        dragState.ghostChecker = createSVGElement('circle', {
            'class': `checker ${color === 0 ? 'checker-white' : 'checker-red'} dragging`,
            'cx': svgCoords.x - dragState.offset.x,
            'cy': svgCoords.y - dragState.offset.y,
            'r': CONFIG.checkerRadius,
            'opacity': '0.7',
            'pointer-events': 'none'
        });
        checkersGroup.appendChild(dragState.ghostChecker);
        console.log('[BoardSVG] Ghost checker created and appended');

        // Hide original checker
        checkerElement.style.opacity = '0.3';
        checkerElement.style.cursor = 'grabbing';

        // Add global mouse/touch move and up listeners
        document.addEventListener('mousemove', handleDragMove);
        document.addEventListener('mouseup', handleDragEnd);
        document.addEventListener('touchmove', handleDragMove, { passive: false });
        document.addEventListener('touchend', handleDragEnd);
        console.log('[BoardSVG] Global drag listeners added');

        // Get valid destinations for this point and show highlights without re-rendering
        console.log(`[BoardSVG] Drag started, manually highlighting valid destinations`);

        // Get valid destinations from the callback
        if (moveHandlers.getValidDestinations) {
            const validDests = moveHandlers.getValidDestinations(pointNum);
            console.log(`[BoardSVG] Valid destinations for point ${pointNum}:`, validDests);

            // Highlight valid destinations without re-rendering
            updateHighlights([pointNum], pointNum, validDests);
        } else {
            console.warn('[BoardSVG] No getValidDestinations handler registered!');
        }
    }

    function handleDragMove(event) {
        if (!dragState.isDragging || !dragState.ghostChecker) return;

        event.preventDefault();

        let clientX, clientY;
        if (event.type === 'touchmove') {
            const touch = event.touches[0];
            clientX = touch.clientX;
            clientY = touch.clientY;
        } else {
            clientX = event.clientX;
            clientY = event.clientY;
        }

        // Convert to SVG coordinates
        const svgPoint = svgElement.createSVGPoint();
        svgPoint.x = clientX;
        svgPoint.y = clientY;
        const ctm = svgElement.getScreenCTM();
        const svgCoords = svgPoint.matrixTransform(ctm.inverse());

        // Update ghost checker position
        dragState.ghostChecker.setAttribute('cx', svgCoords.x - dragState.offset.x);
        dragState.ghostChecker.setAttribute('cy', svgCoords.y - dragState.offset.y);
    }

    function handleDragEnd(event) {
        if (!dragState.isDragging) {
            console.log('[BoardSVG] Drag end called but not dragging');
            return;
        }

        console.log('[BoardSVG] Drag end');
        event.preventDefault();

        let clientX, clientY;
        if (event.type === 'touchend') {
            const touch = event.changedTouches[0];
            clientX = touch.clientX;
            clientY = touch.clientY;
        } else {
            clientX = event.clientX;
            clientY = event.clientY;
        }

        console.log(`[BoardSVG] Drop at client coords: (${clientX}, ${clientY})`);

        // Determine drop target
        const targetPoint = getPointAtPosition(clientX, clientY);
        console.log(`[BoardSVG] Drop target point: ${targetPoint}, source point: ${dragState.sourcePoint}`);

        // Clean up drag state
        if (dragState.ghostChecker) {
            dragState.ghostChecker.remove();
        }
        if (dragState.draggedChecker) {
            dragState.draggedChecker.style.opacity = '1';
            dragState.draggedChecker.style.cursor = 'grab';
        }

        // Remove global listeners
        document.removeEventListener('mousemove', handleDragMove);
        document.removeEventListener('mouseup', handleDragEnd);
        document.removeEventListener('touchmove', handleDragMove);
        document.removeEventListener('touchend', handleDragEnd);

        const sourcePoint = dragState.sourcePoint;

        // Reset drag state
        dragState.isDragging = false;
        dragState.draggedChecker = null;
        dragState.ghostChecker = null;
        dragState.sourcePoint = null;

        // Clear highlights
        updateHighlights([], null, []);

        // Execute move if valid target
        if (targetPoint !== null && targetPoint !== sourcePoint) {
            console.log(`[BoardSVG] Executing move from ${sourcePoint} to ${targetPoint}`);
            if (moveHandlers.onExecuteMove) {
                moveHandlers.onExecuteMove(sourcePoint, targetPoint);
            } else {
                console.warn('[BoardSVG] No onExecuteMove handler registered!');
            }
        } else {
            console.log('[BoardSVG] Invalid drop, cancelling');
            // Invalid drop - just deselect and re-render
            if (moveHandlers.getRenderCallback) {
                const renderFn = moveHandlers.getRenderCallback();
                if (renderFn) renderFn();
            }
        }
    }

    // Render checkers based on game state
    function renderCheckers(gameState, selectedChecker, validSources = []) {
        console.log(`[BoardSVG] renderCheckers called, validSources:`, validSources);
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
                    // Top checker (last in stack) should be draggable if it's a valid source
                    const isTopChecker = i === maxVisible - 1;
                    const isDraggable = isTopChecker && validSources.includes(pointNum);
                    console.log(`[BoardSVG] Point ${pointNum}, index ${i}, isTopChecker: ${isTopChecker}, validSources includes ${pointNum}: ${validSources.includes(pointNum)}, isDraggable: ${isDraggable}`);
                    const checker = createChecker(pointNum, i, point.color, isSelected, isDraggable);
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
            const maxVisible = Math.min(gameState.whiteCheckersOnBar, 2);
            const isBarDraggable = validSources.includes(0);
            console.log(`[BoardSVG] Rendering ${gameState.whiteCheckersOnBar} white bar checkers, isBarDraggable: ${isBarDraggable}`);
            for (let i = 0; i < maxVisible; i++) {
                const isSelected = selectedChecker && selectedChecker.point === 0;
                const isTopBarChecker = i === maxVisible - 1;
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 + 100 + (i * 50) // Moved further down to avoid dice
                };
                const checker = createSVGElement('circle', {
                    'class': `checker checker-white ${isSelected ? 'selected' : ''} ${isTopBarChecker && isBarDraggable ? 'draggable' : ''}`.trim(),
                    'cx': pos.x,
                    'cy': pos.y,
                    'r': CONFIG.checkerRadius,
                    'data-point': 0,
                    'data-color': 0
                });

                // Make draggable if it's the top bar checker and bar is valid source
                if (isTopBarChecker && isBarDraggable) {
                    console.log(`[BoardSVG] Making white bar checker draggable (index ${i})`);
                    checker.style.cursor = 'grab';

                    // Add a simple click test
                    checker.addEventListener('click', (e) => {
                        console.log(`[BoardSVG] CLICK EVENT on white bar checker!`);
                    });

                    checker.addEventListener('mousedown', handleCheckerMouseDown, true); // Use capture phase
                    checker.addEventListener('touchstart', handleCheckerTouchStart, { passive: false, capture: true });
                    console.log(`[BoardSVG] Event listeners added to white bar checker`);
                }

                checkersGroup.appendChild(checker);
            }
            if (gameState.whiteCheckersOnBar > 2) {
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 + 100
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
            const maxVisible = Math.min(gameState.redCheckersOnBar, 2);
            const isBarDraggable = validSources.includes(0);
            console.log(`[BoardSVG] Rendering ${gameState.redCheckersOnBar} red bar checkers, isBarDraggable: ${isBarDraggable}`);
            for (let i = 0; i < maxVisible; i++) {
                const isSelected = selectedChecker && selectedChecker.point === 0;
                const isTopBarChecker = i === maxVisible - 1;
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 - 200 - (i * 50) // Moved further up to avoid doubling cube
                };
                const checker = createSVGElement('circle', {
                    'class': `checker checker-red ${isSelected ? 'selected' : ''} ${isTopBarChecker && isBarDraggable ? 'draggable' : ''}`.trim(),
                    'cx': pos.x,
                    'cy': pos.y,
                    'r': CONFIG.checkerRadius,
                    'data-point': 0,
                    'data-color': 1
                });

                // Make draggable if it's the top bar checker and bar is valid source
                if (isTopBarChecker && isBarDraggable) {
                    console.log(`[BoardSVG] Making red bar checker draggable (index ${i})`);
                    checker.style.cursor = 'grab';

                    // Add a simple click test
                    checker.addEventListener('click', (e) => {
                        console.log(`[BoardSVG] CLICK EVENT on red bar checker!`);
                    });

                    checker.addEventListener('mousedown', handleCheckerMouseDown, true); // Use capture phase
                    checker.addEventListener('touchstart', handleCheckerTouchStart, { passive: false, capture: true });
                    console.log(`[BoardSVG] Event listeners added to red bar checker`);
                }

                checkersGroup.appendChild(checker);
            }
            if (gameState.redCheckersOnBar > 2) {
                const pos = {
                    x: CONFIG.barX + CONFIG.barWidth / 2,
                    y: CONFIG.viewBox.height / 2 - 200
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

        // Detect doubles: originalDice is [5,5] but for doubles we need to treat it as [5,5,5,5]
        const isDoubles = originalDice.length === 2 && originalDice[0] === originalDice[1];

        // For doubles, expand [5,5] to [5,5,5,5] for rendering 4 dice
        const diceToRender = isDoubles
            ? [originalDice[0], originalDice[0], originalDice[0], originalDice[0]]
            : originalDice;

        // Simple binary logic: sequentially mark dice as used based on remaining moves
        const remaining = [...remainingMoves];
        return diceToRender.map(value => {
            const idx = remaining.indexOf(value);
            if (idx !== -1) {
                remaining.splice(idx, 1);
                return { value, used: false };
            }
            return { value, used: true };
        });
    }

    // Render Roll button on right side of board
    function renderRollButton(gameState) {
        console.log('[BoardSVG] renderRollButton called', { gameState, hasDiceGroup: !!diceGroup });

        if (!diceGroup) {
            console.warn('[BoardSVG] No diceGroup - cannot render Roll button');
            return;
        }

        // Clear existing content
        diceGroup.innerHTML = '';

        if (!gameState) {
            console.warn('[BoardSVG] No gameState - cannot render Roll button');
            return;
        }

        console.log('[BoardSVG] Rendering Roll button centered in right half of board');

        // Calculate center of right half of board (between bar and bear-off area)
        const rightHalfStart = CONFIG.barX + CONFIG.barWidth;
        const rightHalfEnd = CONFIG.viewBox.width - CONFIG.margin - CONFIG.bearoffWidth;
        const rightHalfCenterX = (rightHalfStart + rightHalfEnd) / 2;
        const centerY = CONFIG.viewBox.height / 2;

        // Create button group
        const buttonGroup = createSVGElement('g', {
            'class': 'roll-button',
            'style': 'cursor: pointer;'
        });

        const buttonWidth = 100;
        const buttonHeight = 60;

        // Button background - centered at rightHalfCenterX
        const buttonBg = createSVGElement('rect', {
            'x': rightHalfCenterX - buttonWidth / 2,
            'y': centerY - buttonHeight / 2,
            'width': buttonWidth,
            'height': buttonHeight,
            'rx': 8,
            'ry': 8,
            'fill': 'rgba(76, 175, 80, 0.9)',
            'stroke': 'rgba(255, 255, 255, 0.3)',
            'stroke-width': 2
        });
        buttonGroup.appendChild(buttonBg);

        // Button text - centered
        const buttonText = createSVGElement('text', {
            'x': rightHalfCenterX,
            'y': centerY,
            'text-anchor': 'middle',
            'dominant-baseline': 'central',
            'fill': '#ffffff',
            'font-size': '18',
            'font-weight': 'bold',
            'pointer-events': 'none'
        });
        buttonText.textContent = '🎲 Roll';
        buttonGroup.appendChild(buttonText);

        // Add click handler
        buttonGroup.addEventListener('click', () => {
            // Trigger the roll button click in the game
            const rollBtn = document.getElementById('rollBtn');
            if (rollBtn && !rollBtn.disabled) {
                rollBtn.click();
            }
        });

        // Add hover effect
        buttonGroup.addEventListener('mouseenter', () => {
            buttonBg.setAttribute('fill', 'rgba(76, 175, 80, 1)');
        });
        buttonGroup.addEventListener('mouseleave', () => {
            buttonBg.setAttribute('fill', 'rgba(76, 175, 80, 0.9)');
        });

        diceGroup.appendChild(buttonGroup);
    }

    // Helper function to create End Turn button group (without clearing diceGroup)
    function createEndTurnButtonGroup(yOffset = 0) {
        // Calculate center of right half of board (between bar and bear-off area)
        const rightHalfStart = CONFIG.barX + CONFIG.barWidth;
        const rightHalfEnd = CONFIG.viewBox.width - CONFIG.margin - CONFIG.bearoffWidth;
        const rightHalfCenterX = (rightHalfStart + rightHalfEnd) / 2;
        const centerY = CONFIG.viewBox.height / 2 + yOffset;

        // Create button group
        const buttonGroup = createSVGElement('g', {
            'class': 'end-turn-button',
            'style': 'cursor: pointer;'
        });

        const buttonWidth = 100;
        const buttonHeight = 60;

        // Button background (blue color) - centered at rightHalfCenterX
        const buttonBg = createSVGElement('rect', {
            'x': rightHalfCenterX - buttonWidth / 2,
            'y': centerY - buttonHeight / 2,
            'width': buttonWidth,
            'height': buttonHeight,
            'rx': 8,
            'ry': 8,
            'fill': 'rgba(33, 150, 243, 0.9)',
            'stroke': 'rgba(255, 255, 255, 0.3)',
            'stroke-width': 2
        });
        buttonGroup.appendChild(buttonBg);

        // Button text - centered
        const buttonText = createSVGElement('text', {
            'x': rightHalfCenterX,
            'y': centerY,
            'text-anchor': 'middle',
            'dominant-baseline': 'central',
            'fill': '#ffffff',
            'font-size': '16',
            'font-weight': 'bold',
            'pointer-events': 'none'
        });
        buttonText.textContent = '✓ Confirm';
        buttonGroup.appendChild(buttonText);

        // Add click handler
        buttonGroup.addEventListener('click', () => {
            // Trigger the end turn button click in the game
            const endTurnBtn = document.getElementById('endTurnBtn');
            if (endTurnBtn && !endTurnBtn.disabled) {
                endTurnBtn.click();
            }
        });

        // Add hover effect
        buttonGroup.addEventListener('mouseenter', () => {
            buttonBg.setAttribute('fill', 'rgba(33, 150, 243, 1)');
        });
        buttonGroup.addEventListener('mouseleave', () => {
            buttonBg.setAttribute('fill', 'rgba(33, 150, 243, 0.9)');
        });

        return buttonGroup;
    }

    // Render End Turn (Confirm) button on right side of board
    function renderEndTurnButton(gameState) {
        if (!diceGroup) return;

        // Clear existing content
        diceGroup.innerHTML = '';

        if (!gameState) return;

        const buttonGroup = createEndTurnButtonGroup();
        diceGroup.appendChild(buttonGroup);
    }

    // Render Undo button on left side of board in the center
    function renderUndoButton(gameState) {
        if (!diceGroup) return;

        // Only show if moves have been made this turn
        if (!gameState || gameState.movesMadeThisTurn === 0) return;

        // Calculate center of left half of board (between bear-off area and bar)
        const leftHalfStart = CONFIG.margin + CONFIG.bearoffWidth;
        const leftHalfEnd = CONFIG.barX;
        const leftHalfCenterX = (leftHalfStart + leftHalfEnd) / 2;
        const centerY = CONFIG.viewBox.height / 2;

        const buttonGroup = createSVGElement('g', {
            'class': 'undo-button',
            'style': 'cursor: pointer;'
        });

        const buttonWidth = 100;
        const buttonHeight = 50;

        // Button background (yellow/warning color)
        const buttonBg = createSVGElement('rect', {
            'x': leftHalfCenterX - buttonWidth / 2,
            'y': centerY - buttonHeight / 2,
            'width': buttonWidth,
            'height': buttonHeight,
            'rx': 8,
            'ry': 8,
            'fill': 'rgba(245, 158, 11, 0.9)', // Yellow
            'stroke': 'rgba(255, 255, 255, 0.3)',
            'stroke-width': 2
        });
        buttonGroup.appendChild(buttonBg);

        // Button text
        const buttonText = createSVGElement('text', {
            'x': leftHalfCenterX,
            'y': centerY,
            'text-anchor': 'middle',
            'dominant-baseline': 'central',
            'fill': '#ffffff',
            'font-size': '16',
            'font-weight': 'bold',
            'pointer-events': 'none'
        });
        buttonText.textContent = '↶ Undo';
        buttonGroup.appendChild(buttonText);

        // Click handler
        buttonGroup.addEventListener('click', () => {
            const undoBtn = document.getElementById('undoBtn');
            if (undoBtn && !undoBtn.disabled) {
                undoBtn.click();
            }
        });

        // Hover effect
        buttonGroup.addEventListener('mouseenter', () => {
            buttonBg.setAttribute('fill', 'rgba(245, 158, 11, 1)');
        });
        buttonGroup.addEventListener('mouseleave', () => {
            buttonBg.setAttribute('fill', 'rgba(245, 158, 11, 0.9)');
        });

        diceGroup.appendChild(buttonGroup);
    }

    // Render dice on right side of board
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

        // Calculate center of right half of board (between bar and bear-off area)
        const rightHalfStart = CONFIG.barX + CONFIG.barWidth;
        const rightHalfEnd = CONFIG.viewBox.width - CONFIG.margin - CONFIG.bearoffWidth;
        const rightHalfCenterX = (rightHalfStart + rightHalfEnd) / 2;
        const boardCenterY = CONFIG.viewBox.height / 2;

        // Calculate positions based on number of dice - centered at rightHalfCenterX
        let positions = [];
        if (numDice === 2) {
            // Stack vertically, centered horizontally
            positions = [
                { x: rightHalfCenterX - dieSize / 2, y: boardCenterY - dieSize - 8 },
                { x: rightHalfCenterX - dieSize / 2, y: boardCenterY + 8 }
            ];
        } else if (numDice === 4) {
            // 2x2 grid for doubles, centered at rightHalfCenterX
            const offsetX = 24;
            const offsetY = 26;
            positions = [
                { x: rightHalfCenterX - offsetX - dieSize / 2, y: boardCenterY - offsetY - dieSize / 2 },
                { x: rightHalfCenterX + offsetX - dieSize / 2, y: boardCenterY - offsetY - dieSize / 2 },
                { x: rightHalfCenterX - offsetX - dieSize / 2, y: boardCenterY + offsetY - dieSize / 2 },
                { x: rightHalfCenterX + offsetX - dieSize / 2, y: boardCenterY + offsetY - dieSize / 2 }
            ];
        }

        // Render each die
        diceWithState.forEach((die, index) => {
            if (index >= positions.length) return;

            const pos = positions[index];

            // Determine CSS class based on usage state
            const usageClass = die.used ? 'used' : '';

            const group = createSVGElement('g', {
                'class': `board-die ${usageClass}`.trim(),
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

    // Render doubling cube
    function renderCube(gameState) {
        if (!cubeGroup) return;

        // Clear existing cube
        cubeGroup.innerHTML = '';

        if (!gameState) return;

        const cubeValue = gameState.doublingCubeValue || 1;
        const cubeOwner = gameState.doublingCubeOwner; // null, "White", or "Red"

        // Position cube in bar center based on owner
        let cubeX, cubeY;
        const barCenterX = CONFIG.barX + CONFIG.barWidth / 2; // Center of bar
        const boardCenterY = CONFIG.viewBox.height / 2;

        cubeX = barCenterX; // Always centered horizontally in bar

        if (cubeOwner === null) {
            // Above center when neutral
            cubeY = boardCenterY - 100;
        } else if (cubeOwner === "White") {
            // Lower position (White's side)
            cubeY = boardCenterY + 120;
        } else if (cubeOwner === "Red") {
            // Upper position (Red's side)
            cubeY = boardCenterY - 140;
        }

        const cubeSize = 60;
        const cubeRadius = 8;

        // Create cube (rounded square)
        const cubeRect = createSVGElement('rect', {
            'class': 'doubling-cube',
            'x': cubeX - cubeSize / 2,
            'y': cubeY - cubeSize / 2,
            'width': cubeSize,
            'height': cubeSize,
            'rx': cubeRadius,
            'ry': cubeRadius
        });
        cubeGroup.appendChild(cubeRect);

        // Cube value text
        const valueText = createSVGElement('text', {
            'class': 'cube-value',
            'x': cubeX,
            'y': cubeY,
            'text-anchor': 'middle',
            'dominant-baseline': 'central'
        });
        valueText.textContent = cubeValue;
        cubeGroup.appendChild(valueText);

        // Owner indicator text (small label)
        if (cubeOwner !== null) {
            const ownerText = createSVGElement('text', {
                'class': 'cube-owner',
                'x': cubeX,
                'y': cubeY + cubeSize / 2 + 15,
                'text-anchor': 'middle',
                'dominant-baseline': 'central'
            });
            ownerText.textContent = `${cubeOwner}`;
            cubeGroup.appendChild(ownerText);
        }
    }

    // Update highlights based on selection state
    function updateHighlights(validSources, selectedPoint, validDestinations) {
        if (!pointsGroup) return;

        // Reset all point highlights
        pointsGroup.querySelectorAll('.point').forEach(point => {
            point.classList.remove('valid-source', 'selected', 'valid-destination', 'capture');
        });

        // Reset bar and bear-off highlights
        barGroup?.classList.remove('valid-source', 'selected', 'valid-destination', 'capture');
        bearoffGroup?.classList.remove('valid-destination');

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
        }
    }

    // Render point numbers at top and bottom of board
    function renderPointNumbers() {
        if (!svgElement) return;

        // Remove existing point numbers if any
        const existing = svgElement.querySelectorAll('.point-number');
        existing.forEach(el => el.remove());

        const fontSize = 12;
        const fillColor = 'rgba(255, 255, 255, 0.4)';

        // Bottom numbers: 12, 11, 10, 9, 8, 7 | BAR | 6, 5, 4, 3, 2, 1
        // Left side (12-7)
        for (let i = 0; i < 6; i++) {
            const pointNum = 12 - i;
            const x = CONFIG.boardStartX + (i * CONFIG.pointWidth) + CONFIG.pointWidth / 2;
            const y = CONFIG.viewBox.height - 8;

            const text = createSVGElement('text', {
                'class': 'point-number',
                'x': x,
                'y': y,
                'text-anchor': 'middle',
                'font-size': fontSize,
                'fill': fillColor
            });
            text.textContent = pointNum;
            svgElement.appendChild(text);
        }

        // Right side (6-1)
        const rightStart = CONFIG.barX + CONFIG.barWidth;
        for (let i = 0; i < 6; i++) {
            const pointNum = 6 - i;
            const x = rightStart + (i * CONFIG.pointWidth) + CONFIG.pointWidth / 2;
            const y = CONFIG.viewBox.height - 8;

            const text = createSVGElement('text', {
                'class': 'point-number',
                'x': x,
                'y': y,
                'text-anchor': 'middle',
                'font-size': fontSize,
                'fill': fillColor
            });
            text.textContent = pointNum;
            svgElement.appendChild(text);
        }

        // Top numbers: 13, 14, 15, 16, 17, 18 | BAR | 19, 20, 21, 22, 23, 24
        // Left side (13-18)
        for (let i = 0; i < 6; i++) {
            const pointNum = 13 + i;
            const x = CONFIG.boardStartX + (i * CONFIG.pointWidth) + CONFIG.pointWidth / 2;
            const y = 18;

            const text = createSVGElement('text', {
                'class': 'point-number',
                'x': x,
                'y': y,
                'text-anchor': 'middle',
                'font-size': fontSize,
                'fill': fillColor
            });
            text.textContent = pointNum;
            svgElement.appendChild(text);
        }

        // Right side (19-24)
        for (let i = 0; i < 6; i++) {
            const pointNum = 19 + i;
            const x = rightStart + (i * CONFIG.pointWidth) + CONFIG.pointWidth / 2;
            const y = 18;

            const text = createSVGElement('text', {
                'class': 'point-number',
                'x': x,
                'y': y,
                'text-anchor': 'middle',
                'font-size': fontSize,
                'fill': fillColor
            });
            text.textContent = pointNum;
            svgElement.appendChild(text);
        }
    }

    // Main render function
    function render(gameState, selectedChecker, validDestinations, validSources, diceState) {
        if (!initialized) {
            console.warn('BoardSVG not initialized');
            return;
        }

        renderCheckers(gameState, selectedChecker, validSources); // Pass validSources!
        renderCube(gameState);

        // Determine what to show on right side: Roll button, Dice, or End Turn button
        const hasRolledDice = diceState && diceState.dice && diceState.dice.some(d => d > 0);
        const hasRemainingMoves = diceState && diceState.remainingMoves && diceState.remainingMoves.length > 0;

        // Check if it's the player's turn (not opponent's turn)
        const isPlayerTurn = gameState && gameState.yourColor !== undefined &&
                             gameState.currentPlayer === gameState.yourColor;

        // Check if game is waiting for player (Status: 0 = WaitingForPlayer)
        const isWaitingForPlayer = gameState && gameState.status === 0;

        console.log('[BoardSVG] Button decision logic:', {
            gameState: gameState ? {
                yourColor: gameState.yourColor,
                currentPlayer: gameState.currentPlayer,
                status: gameState.status
            } : null,
            diceState: diceState ? {
                dice: diceState.dice,
                remainingMoves: diceState.remainingMoves
            } : null,
            hasRolledDice,
            hasRemainingMoves,
            isPlayerTurn,
            isWaitingForPlayer
        });

        // Only show controls on player's turn
        if (isPlayerTurn && !isWaitingForPlayer) {
            console.log('[BoardSVG] Player turn and game started');

            // Check if there are valid moves available
            const validMoves = gameState.validMoves || [];
            const noValidMoves = validMoves.length === 0;

            if (!hasRolledDice) {
                // Show Roll button when dice haven't been rolled yet
                console.log('[BoardSVG] Should show Roll button');
                renderRollButton(gameState);
            } else if (hasRolledDice && hasRemainingMoves && noValidMoves) {
                // Show greyed-out dice and End Turn button when no valid moves exist
                console.log('[BoardSVG] Should show greyed Dice and End Turn button (no valid moves)');
                console.log('[BoardSVG] diceState:', diceState);
                console.log('[BoardSVG] validMoves:', validMoves);
                // Show dice greyed out by passing empty remaining moves
                const greyedDiceState = {
                    dice: diceState.dice,
                    remainingMoves: [] // Empty to grey out all dice
                };
                renderDice(greyedDiceState);
                // Add End Turn button below the dice
                const buttonGroup = createEndTurnButtonGroup(80); // Offset down by 80px
                diceGroup.appendChild(buttonGroup);
                // Add undo button if moves have been made
                renderUndoButton(gameState);
            } else if (hasRolledDice && hasRemainingMoves) {
                // Show Dice when rolled and still have moves to make
                console.log('[BoardSVG] Should show Dice (with remaining moves)');
                renderDice(diceState);
                // Add undo button if moves have been made
                renderUndoButton(gameState);
            } else if (hasRolledDice && !hasRemainingMoves) {
                // Show End Turn button when all moves are used
                console.log('[BoardSVG] Should show End Turn button');
                renderEndTurnButton(gameState);
                // Add undo button if moves have been made
                renderUndoButton(gameState);
            }
        } else if (hasRolledDice) {
            // Show opponent's dice (but no button controls)
            console.log('[BoardSVG] Opponent turn - showing dice only');
            renderDice(diceState);
        } else {
            console.log('[BoardSVG] No controls shown - waiting or not player turn');
            // Clear any lingering buttons (e.g., confirm button from previous turn)
            if (diceGroup) {
                diceGroup.innerHTML = '';
            }
        }

        renderPointNumbers(); // Add point numbers
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

        // Calculate the actual aspect ratio of the rendered SVG
        const renderedAspect = rect.width / rect.height;
        const viewBoxAspect = CONFIG.viewBox.width / CONFIG.viewBox.height;

        // Adjust for non-uniform scaling due to CSS constraints
        let effectiveWidth = rect.width;
        let effectiveHeight = rect.height;
        let offsetX = 0;
        let offsetY = 0;

        if (renderedAspect > viewBoxAspect) {
            // SVG is letterboxed (black bars on sides)
            effectiveWidth = rect.height * viewBoxAspect;
            offsetX = (rect.width - effectiveWidth) / 2;
        } else if (renderedAspect < viewBoxAspect) {
            // SVG is pillarboxed (black bars on top/bottom)
            effectiveHeight = rect.width / viewBoxAspect;
            offsetY = (rect.height - effectiveHeight) / 2;
        }

        const scaleX = CONFIG.viewBox.width / effectiveWidth;
        const scaleY = CONFIG.viewBox.height / effectiveHeight;

        let x = (clientX - rect.left - offsetX) * scaleX;
        let y = (clientY - rect.top - offsetY) * scaleY;

        // Account for board flip - invert coordinates if flipped
        if (svgElement.classList.contains('flipped')) {
            x = CONFIG.viewBox.width - x;
            y = CONFIG.viewBox.height - y;
        }

        console.log(`Click at client (${clientX}, ${clientY}), SVG coords (${x.toFixed(1)}, ${y.toFixed(1)})`);
        console.log(`  rect: left=${rect.left.toFixed(1)}, top=${rect.top.toFixed(1)}, width=${rect.width.toFixed(1)}, height=${rect.height.toFixed(1)}`);
        console.log(`  effective: width=${effectiveWidth.toFixed(1)}, height=${effectiveHeight.toFixed(1)}, offset=(${offsetX.toFixed(1)}, ${offsetY.toFixed(1)})`);
        console.log(`  scale: X=${scaleX.toFixed(3)}, Y=${scaleY.toFixed(3)}`);

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
            pointIndex = Math.floor((x - CONFIG.boardStartX) / CONFIG.pointWidth);
            if (pointIndex < 0 || pointIndex > 5) return null;
        } else {
            const rightStart = CONFIG.barX + CONFIG.barWidth;
            pointIndex = Math.floor((x - rightStart) / CONFIG.pointWidth);
            if (pointIndex < 0 || pointIndex > 5) return null;
        }

        // Map to point number
        let result;
        if (isTop) {
            // Top row: 13-18 (left), 19-24 (right)
            if (isLeftSide) {
                result = 13 + pointIndex;  // Points 13-18
            } else {
                result = 19 + pointIndex;  // Points 19-24
            }
        } else {
            // Bottom row: 12-7 (left), 6-1 (right)
            if (isLeftSide) {
                result = 12 - pointIndex;  // Points 12-7
            } else {
                result = 6 - pointIndex;   // Points 6-1
            }
        }
        console.log(`  isTop=${isTop}, isLeftSide=${isLeftSide}, pointIndex=${pointIndex} → point ${result}`);
        return result;
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

    // Set handlers for drag and drop callbacks
    function setMoveHandlers(handlers) {
        console.log('[BoardSVG] setMoveHandlers called with:', handlers);
        if (handlers.onSelectChecker) moveHandlers.onSelectChecker = handlers.onSelectChecker;
        if (handlers.onExecuteMove) moveHandlers.onExecuteMove = handlers.onExecuteMove;
        if (handlers.getRenderCallback) moveHandlers.getRenderCallback = handlers.getRenderCallback;
        if (handlers.getValidDestinations) moveHandlers.getValidDestinations = handlers.getValidDestinations;
        console.log('[BoardSVG] Move handlers now:', moveHandlers);
    }

    // Public API
    return {
        init,
        render,
        getPointAtPosition,
        animateMove,
        setMoveHandlers,
        isInitialized: () => initialized,
        getConfig: () => ({ ...CONFIG }),
        getColors: () => ({ ...COLORS })
    };
})();
