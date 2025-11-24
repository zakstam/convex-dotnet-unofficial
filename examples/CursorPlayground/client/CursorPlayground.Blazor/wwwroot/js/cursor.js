// Cursor Playground JavaScript Interop

window.CursorInterop = {
    dotNetRef: null,
    lastX: 0,
    lastY: 0,
    lastMoveTime: 0,

    // Remote cursors state
    remoteCursors: new Map(),
    localCursor: null,
    animationFrame: null,

    // Visual effects state
    particles: [],
    ripples: [],
    reactions: [],
    trailCanvas: null,
    trailCtx: null,

    // Settings
    settings: {
        showTrails: true,
        showParticles: true,
        showRipples: true,
        showReactions: true,
        showConstellations: true
    },

    // User info
    currentUserColor: '#64C8FF',

    initialize: function (dotNetReference, userColor, userName, userEmoji) {
        this.dotNetRef = dotNetReference;
        this.currentUserColor = userColor || '#64C8FF';

        // Create local cursor for the current user
        this.createLocalCursor(userName, userEmoji, userColor);

        // Create trail canvas for cursor trails
        this.createTrailCanvas();

        // Track mouse movement
        document.addEventListener('mousemove', this.handleMouseMove.bind(this));

        // Track clicks for ripple effects
        document.addEventListener('click', this.handleClick.bind(this));

        // Track double-clicks for reactions
        document.addEventListener('dblclick', this.handleDoubleClick.bind(this));

        // Track right-click for reaction picker
        document.addEventListener('contextmenu', this.handleContextMenu.bind(this));

        // Track keyboard shortcuts
        document.addEventListener('keydown', this.handleKeyDown.bind(this));

        // Start animation loop for smooth cursor interpolation and effects
        this.startAnimationLoop();

        console.log('CursorInterop initialized with color:', this.currentUserColor);
    },

    // Create local cursor element for current user
    createLocalCursor: function (userName, emoji, color) {
        this.localCursor = this.createCursorElement(userName || 'You', emoji || '👤', color);
        this.localCursor.style.opacity = '1';
        this.localCursor.style.transform = 'translate(0px, 0px)';
        document.body.appendChild(this.localCursor);
    },

    // Set current user color
    setUserColor: function (color) {
        this.currentUserColor = color;
    },

    // Toggle setting
    toggleSetting: function (settingName) {
        if (this.settings.hasOwnProperty(settingName)) {
            this.settings[settingName] = !this.settings[settingName];
            return this.settings[settingName];
        }
        return false;
    },

    // Create canvas for cursor trails
    createTrailCanvas: function () {
        this.trailCanvas = document.createElement('canvas');
        this.trailCanvas.id = 'trail-canvas';
        this.trailCanvas.width = window.innerWidth;
        this.trailCanvas.height = window.innerHeight;
        this.trailCanvas.style.position = 'fixed';
        this.trailCanvas.style.top = '0';
        this.trailCanvas.style.left = '0';
        this.trailCanvas.style.pointerEvents = 'none';
        this.trailCanvas.style.zIndex = '999';
        document.body.appendChild(this.trailCanvas);
        this.trailCtx = this.trailCanvas.getContext('2d');

        // Resize canvas on window resize
        window.addEventListener('resize', () => {
            this.trailCanvas.width = window.innerWidth;
            this.trailCanvas.height = window.innerHeight;
        });
    },

    handleMouseMove: function (e) {
        if (!this.dotNetRef) return;

        const now = performance.now();

        // Calculate velocity for trail effects
        const timeDelta = now - this.lastMoveTime;
        if (timeDelta > 0) {
            const dx = e.clientX - this.lastX;
            const dy = e.clientY - this.lastY;
            const distance = Math.sqrt(dx * dx + dy * dy);
            const velocity = distance / timeDelta; // pixels per millisecond

            // Send to C# (batching will handle the frequency)
            this.dotNetRef.invokeMethodAsync('OnCursorMove', e.clientX, e.clientY, velocity);
        }

        this.lastX = e.clientX;
        this.lastY = e.clientY;
        this.lastMoveTime = now;

        // Update local cursor position
        if (this.localCursor) {
            this.localCursor.style.transform = `translate(${e.clientX}px, ${e.clientY}px)`;
        }
    },

    handleClick: function (e) {
        if (!this.dotNetRef) return;
        this.dotNetRef.invokeMethodAsync('OnClick', e.clientX, e.clientY);

        // Create particle burst effect with user color
        if (this.settings.showParticles) {
            this.createParticleBurst(e.clientX, e.clientY, this.currentUserColor);
        }
    },

    handleDoubleClick: function (e) {
        if (!this.dotNetRef) return;
        this.dotNetRef.invokeMethodAsync('OnDoubleClick', e.clientX, e.clientY);

        // Create ripple effect with user color
        if (this.settings.showRipples) {
            this.createRipple(e.clientX, e.clientY, this.currentUserColor);
        }
    },

    handleContextMenu: function (e) {
        e.preventDefault();
        if (!this.dotNetRef) return;

        // Show reaction picker at cursor position
        this.showReactionPicker(e.clientX, e.clientY);
    },

    handleKeyDown: function (e) {
        // Press 'R' to show reaction picker at current mouse position
        if (e.key === 'r' || e.key === 'R') {
            e.preventDefault();
            this.showReactionPicker(this.lastX, this.lastY);
        }

        // Press 'T' to toggle trails
        if (e.key === 't' || e.key === 'T') {
            this.toggleSetting('showTrails');
        }

        // Press 'C' to toggle constellations
        if (e.key === 'c' || e.key === 'C') {
            this.toggleSetting('showConstellations');
        }
    },

    // Create particle burst effect on click
    createParticleBurst: function (x, y, color) {
        const particleCount = 12;
        for (let i = 0; i < particleCount; i++) {
            const angle = (Math.PI * 2 * i) / particleCount;
            const velocity = 2 + Math.random() * 2;
            this.particles.push({
                x: x,
                y: y,
                vx: Math.cos(angle) * velocity,
                vy: Math.sin(angle) * velocity,
                life: 1.0,
                color: color,
                size: 3 + Math.random() * 3
            });
        }
    },

    // Create ripple effect on double-click
    createRipple: function (x, y, color) {
        this.ripples.push({
            x: x,
            y: y,
            radius: 0,
            maxRadius: 150,
            life: 1.0,
            color: color
        });
    },

    // Show reaction picker
    showReactionPicker: function (x, y) {
        // Remove existing picker if any
        const existingPicker = document.getElementById('reaction-picker');
        if (existingPicker) {
            existingPicker.remove();
        }

        // Create picker element
        const picker = document.createElement('div');
        picker.id = 'reaction-picker';
        picker.className = 'reaction-picker';
        picker.style.left = `${x}px`;
        picker.style.top = `${y}px`;

        // Reaction emojis
        const reactions = ['❤️', '👍', '😂', '😮', '😢', '🎉', '🔥', '✨'];

        reactions.forEach(emoji => {
            const button = document.createElement('button');
            button.className = 'reaction-button';
            button.textContent = emoji;
            button.onclick = () => {
                this.sendReaction(emoji, x, y);
                picker.remove();
            };
            picker.appendChild(button);
        });

        document.body.appendChild(picker);

        // Auto-remove after 3 seconds
        setTimeout(() => {
            if (picker.parentNode) {
                picker.remove();
            }
        }, 3000);

        // Remove on click outside
        const removeOnClickOutside = (e) => {
            if (!picker.contains(e.target)) {
                picker.remove();
                document.removeEventListener('click', removeOnClickOutside);
            }
        };
        setTimeout(() => {
            document.addEventListener('click', removeOnClickOutside);
        }, 100);
    },

    // Send reaction to C#
    sendReaction: function (emoji, x, y) {
        if (!this.dotNetRef) return;
        this.dotNetRef.invokeMethodAsync('OnReaction', emoji, x, y);

        // Create local reaction animation
        this.createReactionAnimation(emoji, x, y);
    },

    // Create reaction animation
    createReactionAnimation: function (emoji, x, y) {
        if (!this.settings.showReactions) return;

        this.reactions.push({
            emoji: emoji,
            x: x,
            y: y,
            startY: y,
            life: 1.0,
            scale: 0
        });
    },

    // Render reaction from remote user
    renderRemoteReaction: function (emoji, x, y) {
        this.createReactionAnimation(emoji, x, y);
    },

    // Render click effect (particle burst) from remote user
    renderRemoteClickEffect: function (x, y, color) {
        this.createParticleBurst(x, y, color);
    },

    // Update remote cursor position (called from C#)
    updateRemoteCursor: function (userId, x, y, userName, emoji, color) {
        let cursor = this.remoteCursors.get(userId);

        if (!cursor) {
            // Create new cursor element
            cursor = {
                userId: userId,
                x: x,
                y: y,
                targetX: x,
                targetY: y,
                userName: userName,
                emoji: emoji,
                color: color,
                trail: [], // Store recent positions for trail effect
                positionQueue: [], // Queue of positions for smooth interpolation
                element: this.createCursorElement(userName, emoji, color)
            };
            this.remoteCursors.set(userId, cursor);
            document.body.appendChild(cursor.element);

            // Add entry animation
            cursor.element.style.opacity = '0';
            cursor.element.style.transform = `translate(${x}px, ${y}px) scale(0)`;
            setTimeout(() => {
                cursor.element.style.transition = 'opacity 0.3s, transform 0.3s';
                cursor.element.style.opacity = '1';
                cursor.element.style.transform = `translate(${x}px, ${y}px) scale(1)`;
            }, 10);
        } else {
            // Update target position for smooth interpolation
            cursor.targetX = x;
            cursor.targetY = y;
        }
    },

    // Update remote cursor with batch of positions for smooth interpolation
    updateRemoteCursorBatch: function (userId, positions, userName, emoji, color) {
        let cursor = this.remoteCursors.get(userId);

        if (!cursor) {
            // Create new cursor element with position queue
            const firstPos = positions[0] || { x: 0, y: 0 };
            cursor = {
                userId: userId,
                x: firstPos.x,
                y: firstPos.y,
                targetX: firstPos.x,
                targetY: firstPos.y,
                userName: userName,
                emoji: emoji,
                color: color,
                trail: [],
                positionQueue: [...positions.slice(1)], // Queue remaining positions
                element: this.createCursorElement(userName, emoji, color)
            };
            this.remoteCursors.set(userId, cursor);
            document.body.appendChild(cursor.element);

            // Add entry animation
            cursor.element.style.opacity = '0';
            cursor.element.style.transform = `translate(${firstPos.x}px, ${firstPos.y}px) scale(0)`;
            setTimeout(() => {
                cursor.element.style.transition = 'opacity 0.3s, transform 0.3s';
                cursor.element.style.opacity = '1';
                cursor.element.style.transform = `translate(${firstPos.x}px, ${firstPos.y}px) scale(1)`;
            }, 10);
        } else {
            // Add all new positions to the queue
            cursor.positionQueue = cursor.positionQueue || [];
            cursor.positionQueue.push(...positions);
        }
    },

    // Remove remote cursor (user left)
    removeRemoteCursor: function (userId) {
        const cursor = this.remoteCursors.get(userId);
        if (cursor) {
            // Add exit animation
            cursor.element.style.transition = 'opacity 0.3s, transform 0.3s';
            cursor.element.style.opacity = '0';
            cursor.element.style.transform = `translate(${cursor.x}px, ${cursor.y}px) scale(0)`;

            // Remove after animation
            setTimeout(() => {
                cursor.element.remove();
                this.remoteCursors.delete(userId);
            }, 300);
        }
    },

    // Create cursor DOM element
    createCursorElement: function (userName, emoji, color) {
        const div = document.createElement('div');
        div.className = 'remote-cursor';
        div.innerHTML = `
            <div class="cursor-pointer" style="background-color: ${color}"></div>
            <div class="cursor-label">
                <span class="cursor-emoji">${emoji}</span>
                <span class="cursor-name">${userName}</span>
            </div>
        `;
        return div;
    },

    // Smooth interpolation animation loop with visual effects
    startAnimationLoop: function () {
        const smoothness = 0.15; // Lower = smoother but more lag
        const maxTrailLength = 15;

        const animate = () => {
            // Clear trail canvas with fade effect (balanced for trails and particles)
            if (this.trailCtx) {
                this.trailCtx.fillStyle = 'rgba(0, 0, 0, 0.12)'; // Balanced fade - fast enough for particles, smooth for trails
                this.trailCtx.fillRect(0, 0, this.trailCanvas.width, this.trailCanvas.height);
            }

            // Interpolate all remote cursors and update trails
            this.remoteCursors.forEach(cursor => {
                // Check if we have queued positions to play back for smooth movement
                if (cursor.positionQueue && cursor.positionQueue.length > 0) {
                    // Consume next position from queue
                    const nextPos = cursor.positionQueue.shift();
                    cursor.x = nextPos.x;
                    cursor.y = nextPos.y;
                    // Update target for any remaining lerp
                    cursor.targetX = nextPos.x;
                    cursor.targetY = nextPos.y;
                } else {
                    // Fallback to lerp if no queue (for backwards compatibility)
                    cursor.x += (cursor.targetX - cursor.x) * smoothness;
                    cursor.y += (cursor.targetY - cursor.y) * smoothness;
                }

                // Add to trail
                cursor.trail.push({ x: cursor.x, y: cursor.y });
                if (cursor.trail.length > maxTrailLength) {
                    cursor.trail.shift();
                }

                // Draw trail
                this.drawTrail(cursor);

                // Update DOM position (remove scale from here as it's only for entry/exit)
                const currentTransform = cursor.element.style.transform;
                if (!currentTransform.includes('scale(0)')) {
                    cursor.element.style.transform = `translate(${cursor.x}px, ${cursor.y}px)`;
                }
            });

            // Update and draw particles
            if (this.settings.showParticles) {
                this.updateParticles();
            }

            // Update and draw ripples
            if (this.settings.showRipples) {
                this.updateRipples();
            }

            // Update and draw reactions
            if (this.settings.showReactions) {
                this.updateReactions();
            }

            this.animationFrame = requestAnimationFrame(animate);
        };

        animate();
    },

    // Draw cursor trail
    drawTrail: function (cursor) {
        if (!this.settings.showTrails || !this.trailCtx || cursor.trail.length < 2) return;

        const ctx = this.trailCtx;
        ctx.lineCap = 'round';
        ctx.lineJoin = 'round';

        for (let i = 1; i < cursor.trail.length; i++) {
            const point = cursor.trail[i];
            const prevPoint = cursor.trail[i - 1];
            const alpha = i / cursor.trail.length;
            const width = alpha * 4;

            ctx.beginPath();
            ctx.moveTo(prevPoint.x, prevPoint.y);
            ctx.lineTo(point.x, point.y);
            ctx.strokeStyle = `${cursor.color}${Math.floor(alpha * 255).toString(16).padStart(2, '0')}`;
            ctx.lineWidth = width;
            ctx.stroke();
        }
    },

    // Update particles
    updateParticles: function () {
        if (!this.trailCtx) return;

        this.particles = this.particles.filter(particle => {
            // Update position
            particle.x += particle.vx;
            particle.y += particle.vy;
            particle.vy += 0.1; // Gravity
            particle.life -= 0.02;

            // Draw particle
            if (particle.life > 0) {
                this.trailCtx.beginPath();
                this.trailCtx.arc(particle.x, particle.y, particle.size * particle.life, 0, Math.PI * 2);
                this.trailCtx.fillStyle = `${particle.color}${Math.floor(particle.life * 255).toString(16).padStart(2, '0')}`;
                this.trailCtx.fill();
                return true;
            }
            return false;
        });
    },

    // Update ripples
    updateRipples: function () {
        if (!this.trailCtx) return;

        this.ripples = this.ripples.filter(ripple => {
            // Update ripple
            ripple.radius += 3;
            ripple.life -= 0.015;

            // Draw ripple
            if (ripple.life > 0 && ripple.radius < ripple.maxRadius) {
                this.trailCtx.beginPath();
                this.trailCtx.arc(ripple.x, ripple.y, ripple.radius, 0, Math.PI * 2);
                this.trailCtx.strokeStyle = `${ripple.color}${Math.floor(ripple.life * 255).toString(16).padStart(2, '0')}`;
                this.trailCtx.lineWidth = 3;
                this.trailCtx.stroke();
                return true;
            }
            return false;
        });
    },

    // Update reactions
    updateReactions: function () {
        if (!this.trailCtx) return;

        this.reactions = this.reactions.filter(reaction => {
            // Update reaction
            reaction.y -= 1.5; // Float upward
            reaction.life -= 0.015;
            reaction.scale = Math.min(reaction.scale + 0.1, 1.2);

            // Draw reaction
            if (reaction.life > 0) {
                const ctx = this.trailCtx;
                ctx.save();
                ctx.translate(reaction.x, reaction.y);
                ctx.scale(reaction.scale, reaction.scale);
                ctx.globalAlpha = reaction.life;
                ctx.font = '32px Arial';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(reaction.emoji, 0, 0);
                ctx.restore();
                return true;
            }
            return false;
        });
    },

    // Draw constellation lines between nearby cursors
    drawConstellations: function (canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        const maxDistance = 200;

        // Clear canvas
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        // Get all cursor positions
        const positions = Array.from(this.remoteCursors.values()).map(c => ({
            x: c.x,
            y: c.y
        }));

        // Draw lines between nearby cursors
        for (let i = 0; i < positions.length; i++) {
            for (let j = i + 1; j < positions.length; j++) {
                const dx = positions[j].x - positions[i].x;
                const dy = positions[j].y - positions[i].y;
                const distance = Math.sqrt(dx * dx + dy * dy);

                if (distance < maxDistance) {
                    const opacity = (1 - distance / maxDistance) * 0.3;

                    ctx.beginPath();
                    ctx.moveTo(positions[i].x, positions[i].y);
                    ctx.lineTo(positions[j].x, positions[j].y);

                    // Gradient line
                    const gradient = ctx.createLinearGradient(
                        positions[i].x, positions[i].y,
                        positions[j].x, positions[j].y
                    );
                    gradient.addColorStop(0, `rgba(100, 200, 255, ${opacity})`);
                    gradient.addColorStop(1, `rgba(255, 100, 200, ${opacity})`);

                    ctx.strokeStyle = gradient;
                    ctx.lineWidth = 2;
                    ctx.stroke();
                }
            }
        }

        // Continue drawing
        requestAnimationFrame(() => this.drawConstellations(canvasId));
    },

    // Calculate distance between two points
    calculateDistance: function (x1, y1, x2, y2) {
        const dx = x2 - x1;
        const dy = y2 - y1;
        return Math.sqrt(dx * dx + dy * dy);
    },

    dispose: function () {
        if (this.animationFrame) {
            cancelAnimationFrame(this.animationFrame);
        }

        document.removeEventListener('mousemove', this.handleMouseMove);
        document.removeEventListener('click', this.handleClick);
        document.removeEventListener('dblclick', this.handleDoubleClick);
        document.removeEventListener('contextmenu', this.handleContextMenu);
        document.removeEventListener('keydown', this.handleKeyDown);

        // Remove reaction picker if exists
        const picker = document.getElementById('reaction-picker');
        if (picker) {
            picker.remove();
        }

        // Remove trail canvas
        if (this.trailCanvas) {
            this.trailCanvas.remove();
            this.trailCanvas = null;
            this.trailCtx = null;
        }

        // Clear visual effects
        this.particles = [];
        this.ripples = [];
        this.reactions = [];

        // Remove all remote cursors
        this.remoteCursors.forEach(cursor => cursor.element.remove());
        this.remoteCursors.clear();

        this.dotNetRef = null;
    }
};
