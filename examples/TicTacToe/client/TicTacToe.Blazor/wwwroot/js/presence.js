// Browser Lifecycle Management for Presence and Connection Loss Detection

window.presenceManager = {
    dotnetHelper: null,
    visibilityTimer: null,
    isHidden: false,

    /**
     * Initialize lifecycle event handlers
     * @param {any} dotnetHelper - Reference to .NET component for callbacks
     */
    initialize: function(dotnetHelper) {
        this.dotnetHelper = dotnetHelper;

        // Handle page visibility changes (tab switch, minimize)
        document.addEventListener('visibilitychange', () => {
            this.handleVisibilityChange();
        });

        // Handle page unload (tab close, navigation away)
        window.addEventListener('beforeunload', (e) => {
            this.handleBeforeUnload(e);
        });

        console.log('Presence manager initialized');
    },

    /**
     * Handle visibility changes - detect when user switches away from tab
     */
    handleVisibilityChange: function() {
        if (document.hidden) {
            // Tab became hidden - start timer to forfeit if hidden too long
            this.isHidden = true;
            console.log('Tab hidden - starting disconnect timer');

            // Wait 10 seconds before considering it a disconnection
            // This prevents forfeiting when user just quickly switches tabs
            this.visibilityTimer = setTimeout(() => {
                if (this.isHidden && this.dotnetHelper) {
                    console.log('Tab hidden for 10s - triggering disconnect handler');
                    this.dotnetHelper.invokeMethodAsync('OnConnectionLost')
                        .catch(err => console.error('Failed to call OnConnectionLost:', err));
                }
            }, 10000);
        } else {
            // Tab became visible again - cancel disconnect timer
            this.isHidden = false;
            if (this.visibilityTimer) {
                console.log('Tab visible again - canceling disconnect timer');
                clearTimeout(this.visibilityTimer);
                this.visibilityTimer = null;
            }
        }
    },

    /**
     * Handle beforeunload - page is closing or navigating away
     */
    handleBeforeUnload: function(e) {
        console.log('Page unloading - attempting cleanup');

        // Call synchronous cleanup
        // Note: async operations are unreliable in beforeunload
        // The WebSocket connection will be dropped anyway, triggering server-side cleanup
        if (this.dotnetHelper) {
            try {
                // Best effort - this may not complete before page closes
                this.dotnetHelper.invokeMethodAsync('OnConnectionLost');
            } catch (err) {
                console.error('Failed to call cleanup on unload:', err);
            }
        }

        // Don't show confirmation dialog
        // If we wanted to warn user, we could do:
        // e.preventDefault();
        // e.returnValue = '';
    },

    /**
     * Cleanup - remove event listeners
     */
    dispose: function() {
        if (this.visibilityTimer) {
            clearTimeout(this.visibilityTimer);
        }
        document.removeEventListener('visibilitychange', this.handleVisibilityChange);
        window.removeEventListener('beforeunload', this.handleBeforeUnload);
        console.log('Presence manager disposed');
    }
};
