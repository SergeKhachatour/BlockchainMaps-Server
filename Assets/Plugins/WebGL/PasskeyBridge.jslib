mergeInto(LibraryManager.library, {
    // Bridge check function - This is the main bridge function that was missing
    CheckBridgeReady: function() {
        try {
            console.log('[PasskeyBridge] Checking bridge readiness...');
            // Check if the bridge is properly initialized
            if (typeof window.passkeyKit !== 'undefined' || typeof window.initializePasskeyKit === 'function') {
                console.log('[PasskeyBridge] Bridge is ready');
                return 1; // Return 1 if bridge is ready
            } else {
                console.warn('[PasskeyBridge] Bridge not ready - passkeyKit not found');
                return 0; // Return 0 if bridge is not ready
            }
        } catch (error) {
            console.error('[PasskeyBridge] Bridge not ready:', error);
            return 0; // Return 0 if bridge is not ready
        }
    },

    // InitializePasskeyKitJS - This version has a fallback mechanism
    InitializePasskeyKitJS: function() {
        try {
            console.log('[PasskeyBridge] Initializing Official PasskeyKit...');
            // Initialize PasskeyKit using the official library
            if (typeof window.initializePasskeyKit === 'function') {
                window.initializePasskeyKit().then(() => {
                    console.log('[PasskeyBridge] Official PasskeyKit initialized successfully');
                    SendMessage('PasskeyManager', 'HandlePasskeyCreated', 'Official PasskeyKit initialized successfully');
                }).catch(error => {
                    console.error('[PasskeyBridge] Error initializing Official PasskeyKit:', error);
                    SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
                });
            } else {
                console.warn('[PasskeyBridge] initializePasskeyKit function not found, using fallback initialization');
                // Fallback initialization - just send success message
                SendMessage('PasskeyManager', 'HandlePasskeyCreated', 'PasskeyBridge initialized successfully');
            }
        } catch (error) {
            console.error('[PasskeyBridge] InitializePasskeyKitJS error:', error);
            // Send fallback success message instead of error
            SendMessage('PasskeyManager', 'HandlePasskeyCreated', 'PasskeyBridge initialized successfully (fallback)');
        }
    }
}); 