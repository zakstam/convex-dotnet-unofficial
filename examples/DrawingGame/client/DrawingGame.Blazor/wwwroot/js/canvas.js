// Canvas Drawing JavaScript Interop

window.CanvasInterop = {
    canvas: null,
    ctx: null,
    dotNetRef: null,
    isDrawing: false,
    isDrawer: false,
    currentTool: 'pencil',
    currentColor: '#000000',
    currentThickness: 5,
    lastX: 0,
    lastY: 0,
    
    // Track rendered events per batch (batchId -> eventCount)
    renderedBatchEventCounts: new Map(),

    initialize: function (canvasElement, dotNetReference, isDrawer) {
        this.canvas = canvasElement;
        this.ctx = this.canvas.getContext('2d');
        this.dotNetRef = dotNetReference;
        this.isDrawer = isDrawer;

        // Set canvas styling
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';
        this.ctx.lineWidth = this.currentThickness;
        this.ctx.strokeStyle = this.currentColor;

        // Reset tracking
        this.renderedBatchEventCounts.clear();

        // Clear canvas
        this.clearCanvas();

        if (isDrawer) {
            // Add drawing event listeners
            this.canvas.addEventListener('mousedown', this.startDrawing.bind(this));
            this.canvas.addEventListener('mousemove', this.draw.bind(this));
            this.canvas.addEventListener('mouseup', this.stopDrawing.bind(this));
            this.canvas.addEventListener('mouseout', this.stopDrawing.bind(this));

            // Touch events for mobile
            this.canvas.addEventListener('touchstart', this.handleTouchStart.bind(this));
            this.canvas.addEventListener('touchmove', this.handleTouchMove.bind(this));
            this.canvas.addEventListener('touchend', this.stopDrawing.bind(this));
        }
    },

    // Get accurate canvas coordinates accounting for scaling and device pixel ratio
    getCanvasCoordinates: function (clientX, clientY) {
        const rect = this.canvas.getBoundingClientRect();
        const scaleX = this.canvas.width / rect.width;
        const scaleY = this.canvas.height / rect.height;
        
        const x = (clientX - rect.left) * scaleX;
        const y = (clientY - rect.top) * scaleY;
        
        return { x: x, y: y };
    },

    startDrawing: function (e) {
        if (!this.isDrawer) return;

        this.isDrawing = true;
        const coords = this.getCanvasCoordinates(e.clientX, e.clientY);
        this.lastX = coords.x;
        this.lastY = coords.y;
    },

    draw: function (e) {
        if (!this.isDrawing || !this.isDrawer) return;

        const coords = this.getCanvasCoordinates(e.clientX, e.clientY);
        const x = coords.x;
        const y = coords.y;

        this.drawLine(this.lastX, this.lastY, x, y);

        // Send point to C#
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnDrawPoint', x, y);
        }

        this.lastX = x;
        this.lastY = y;
    },

    stopDrawing: function () {
        this.isDrawing = false;
    },

    handleTouchStart: function (e) {
        e.preventDefault();
        const touch = e.touches[0];
        const coords = this.getCanvasCoordinates(touch.clientX, touch.clientY);
        const mouseEvent = new MouseEvent('mousedown', {
            clientX: touch.clientX,
            clientY: touch.clientY,
            bubbles: true,
            cancelable: true
        });
        this.canvas.dispatchEvent(mouseEvent);
    },

    handleTouchMove: function (e) {
        e.preventDefault();
        const touch = e.touches[0];
        const coords = this.getCanvasCoordinates(touch.clientX, touch.clientY);
        const mouseEvent = new MouseEvent('mousemove', {
            clientX: touch.clientX,
            clientY: touch.clientY,
            bubbles: true,
            cancelable: true
        });
        this.canvas.dispatchEvent(mouseEvent);
    },

    drawLine: function (x1, y1, x2, y2) {
        this.ctx.beginPath();
        this.ctx.moveTo(x1, y1);
        this.ctx.lineTo(x2, y2);
        this.ctx.stroke();
    },

    setTool: function (tool) {
        this.currentTool = tool;
        if (tool === 'eraser') {
            this.ctx.globalCompositeOperation = 'destination-out';
            this.ctx.lineWidth = this.currentThickness * 2;
        } else {
            this.ctx.globalCompositeOperation = 'source-over';
            this.ctx.lineWidth = this.currentThickness;
        }
    },

    setColor: function (color) {
        this.currentColor = color;
        this.ctx.strokeStyle = color;
    },

    setThickness: function (thickness) {
        this.currentThickness = thickness;
        if (this.currentTool === 'eraser') {
            this.ctx.lineWidth = thickness * 2;
        } else {
            this.ctx.lineWidth = thickness;
        }
    },

    clearCanvas: function () {
        this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);
        // Fill with white background
        this.ctx.fillStyle = '#FFFFFF';
        this.ctx.fillRect(0, 0, this.canvas.width, this.canvas.height);
        // Reset tracking when canvas is cleared
        this.renderedBatchEventCounts.clear();
    },

    // Render multiple batches - processes them in order to avoid race conditions
    renderBatches: function (batches) {
        console.log('renderBatches called with', batches?.length || 0, 'batches');
        
        if (!batches || batches.length === 0) {
            console.warn('renderBatches: no batches to render');
            return;
        }
        
        // Sort batches by batchStartTime to ensure correct rendering order
        const sortedBatches = [...batches].sort((a, b) => {
            const timeA = a.batchStartTime || 0;
            const timeB = b.batchStartTime || 0;
            return timeA - timeB;
        });
        
        console.log('renderBatches: sorted batches, rendering', sortedBatches.length);
        
        // Process each batch in order
        for (let i = 0; i < sortedBatches.length; i++) {
            try {
                this.renderBatch(sortedBatches[i]);
            } catch (error) {
                console.error('renderBatches: error rendering batch', i, error);
            }
        }
    },

    // Render a batch - handles both new batches and incremental updates
    renderBatch: function (batch) {
        if (!batch || !batch.events || batch.events.length === 0) return;
        
        const batchId = batch._id;
        if (!batchId) {
            console.warn('renderBatch: batch missing _id', batch);
            return;
        }

        // Get how many events we've already rendered for this batch
        const alreadyRenderedCount = this.renderedBatchEventCounts.get(batchId) || 0;
        
        // If we've rendered all events, skip (batch hasn't changed)
        if (alreadyRenderedCount >= batch.events.length) {
            return;
        }

        // Sort events by timeSinceBatchStart to ensure correct order
        const sortedEvents = [...batch.events].sort((a, b) => a.timeSinceBatchStart - b.timeSinceBatchStart);
        
        // Get only the new events to render
        const newEvents = sortedEvents.slice(alreadyRenderedCount);
        if (newEvents.length === 0) {
            // Update count even if no new events (shouldn't happen, but be safe)
            this.renderedBatchEventCounts.set(batchId, batch.events.length);
            return;
        }

        // Debug logging
        console.log(`renderBatch: batchId=${batchId}, alreadyRendered=${alreadyRenderedCount}, totalEvents=${batch.events.length}, newEvents=${newEvents.length}`);

        // Set style for this batch
        this.ctx.save();
        
        if (batch.tool === 'eraser') {
            this.ctx.globalCompositeOperation = 'destination-out';
            this.ctx.lineWidth = batch.thickness * 2;
        } else {
            this.ctx.globalCompositeOperation = 'source-over';
            this.ctx.strokeStyle = batch.color;
            this.ctx.lineWidth = batch.thickness;
        }

        // Set line properties
        this.ctx.lineCap = 'round';
        this.ctx.lineJoin = 'round';

        // Detect stroke breaks based on time gaps (>100ms indicates mouse was lifted)
        const STROKE_BREAK_THRESHOLD_MS = 100;

        // Start drawing path
        this.ctx.beginPath();

        let previousEvent = null;
        if (alreadyRenderedCount > 0) {
            previousEvent = sortedEvents[alreadyRenderedCount - 1];
        }

        // Render new events, detecting stroke breaks
        for (let i = 0; i < newEvents.length; i++) {
            const currentEvent = newEvents[i];
            const point = currentEvent.eventData;

            // Check if we should start a new stroke (time gap from previous event)
            let isStrokeBreak = false;
            if (previousEvent) {
                const timeGap = currentEvent.timeSinceBatchStart - previousEvent.timeSinceBatchStart;
                if (timeGap > STROKE_BREAK_THRESHOLD_MS) {
                    isStrokeBreak = true;
                }
            }

            if (i === 0 && previousEvent && !isStrokeBreak) {
                // First new event, continuing from previous stroke
                this.ctx.moveTo(previousEvent.eventData.x, previousEvent.eventData.y);
                this.ctx.lineTo(point.x, point.y);
            } else if (isStrokeBreak || (i === 0 && !previousEvent)) {
                // Start new stroke (time gap detected or first event in batch)
                this.ctx.moveTo(point.x, point.y);
            } else {
                // Continue current stroke
                this.ctx.lineTo(point.x, point.y);
            }

            previousEvent = currentEvent;
        }

        // Stroke the entire path at once
        this.ctx.stroke();
        this.ctx.restore();

        // Update the rendered count AFTER rendering
        this.renderedBatchEventCounts.set(batchId, batch.events.length);
    },

    dispose: function () {
        if (this.canvas) {
            this.canvas.removeEventListener('mousedown', this.startDrawing);
            this.canvas.removeEventListener('mousemove', this.draw);
            this.canvas.removeEventListener('mouseup', this.stopDrawing);
            this.canvas.removeEventListener('mouseout', this.stopDrawing);
            this.canvas.removeEventListener('touchstart', this.handleTouchStart);
            this.canvas.removeEventListener('touchmove', this.handleTouchMove);
            this.canvas.removeEventListener('touchend', this.stopDrawing);
        }
    }
};
