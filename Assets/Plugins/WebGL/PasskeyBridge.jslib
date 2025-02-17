mergeInto(LibraryManager.library, {
    InitializePasskey: function() {
        try {
            // Check if we're in WebGL context
            if (typeof window === 'undefined') {
                console.error('[PasskeyBridge] Not running in WebGL context');
                return;
            }

            // Create a promise to handle async initialization
            var initPromise = new Promise(function(resolve, reject) {
                var maxAttempts = 10;
                var attempt = 0;
                var checkPasskeyKit = function() {
                    if (typeof PasskeyKit !== 'undefined') {
                        resolve();
                    } else if (attempt >= maxAttempts) {
                        reject(new Error('PasskeyKit failed to load after ' + maxAttempts + ' attempts'));
                    } else {
                        attempt++;
                        setTimeout(checkPasskeyKit, 500);
                    }
                };
                checkPasskeyKit();
            });

            initPromise.then(function() {
                console.log('[PasskeyBridge] PasskeyKit class found, initializing...');
                if (!window.passkeyKit) {
                    window.passkeyKit = new PasskeyKit({
                        rpcUrl: "https://soroban-testnet.stellar.org",
                        networkPassphrase: "Test SDF Network ; September 2015",
                        factoryContractId: "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC"
                    });
                    return window.passkeyKit.initialize();
                }
                return Promise.resolve();
            }).then(function() {
                console.log('[PasskeyBridge] PasskeyKit initialized successfully');
                try {
                    SendMessage('PasskeyManager', 'HandlePasskeyCreated', 'Initialized');
                } catch (error) {
                    console.error('[PasskeyBridge] Error sending success message:', error);
                }
            }).catch(function(error) {
                console.error('[PasskeyBridge] PasskeyKit initialization error:', error);
                try {
                    SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
                } catch (sendError) {
                    console.error('[PasskeyBridge] Error sending error message:', sendError);
                }
            });
        } catch (error) {
            console.error('[PasskeyBridge] PasskeyKit initialization error:', error);
            try {
                SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
            } catch (sendError) {
                console.error('[PasskeyBridge] Error sending error message:', sendError);
            }
        }
    },

    LogOffPasskey: function() {
        try {
            console.log('[PasskeyBridge] Logging off...');
            if (window.passkeyKit) {
                window.passkeyKit = null;
            }
            console.log('[PasskeyBridge] Successfully logged off');
        } catch (error) {
            console.error('[PasskeyBridge] Error during log off:', error);
        }
    },

    AuthenticateUser: function(usernamePtr) {
        try {
            var username = UTF8ToString(usernamePtr);
            
            if (!window.passkeyKit) {
                throw new Error('PasskeyKit not initialized');
            }

            console.log('[PasskeyBridge] Attempting to authenticate user:', username);
            window.passkeyKit.authenticate(username)
                .then(function(result) {
                    console.log('[PasskeyBridge] Authentication successful');
                    SendMessage('PasskeyManager', 'HandleAuthenticationSuccess');
                })
                .catch(function(error) {
                    console.error('[PasskeyBridge] Authentication error:', error);
                    SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
                });
        } catch (error) {
            console.error('[PasskeyBridge] Authentication error:', error);
            SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
        }
    },

    SignStellarTransaction: function(txDataPtr) {
        try {
            var txData = UTF8ToString(txDataPtr);
            
            if (!window.passkeyKit) {
                throw new Error('PasskeyKit not initialized');
            }

            console.log('[PasskeyBridge] Attempting to sign transaction');
            window.passkeyKit.signTransaction(txData)
                .then(function(signedTx) {
                    SendMessage('PasskeyManager', 'HandleTransactionSigned', JSON.stringify(signedTx));
                })
                .catch(function(error) {
                    console.error('[PasskeyBridge] Transaction signing error:', error);
                    SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
                });
        } catch (error) {
            console.error('[PasskeyBridge] Transaction signing error:', error);
            SendMessage('PasskeyManager', 'HandleAuthenticationError', error.toString());
        }
    },

    // Add a health check function
    CheckPasskeyKitStatus: function() {
        try {
            var status = {
                isLoaded: typeof PasskeyKit !== 'undefined',
                isInitialized: window.passkeyKit !== undefined,
                version: window.passkeyKit ? window.passkeyKit.version : 'unknown'
            };
            
            var statusStr = JSON.stringify(status);
            var bufferSize = lengthBytesUTF8(statusStr) + 1;
            var buffer = _malloc(bufferSize);
            stringToUTF8(statusStr, buffer, bufferSize);
            return buffer;
        } catch (error) {
            console.error('Status check error:', error);
            return null;
        }
    }
}); 