mergeInto(LibraryManager.library, {
    InitializeWebGL: function() {
        // Basic WebGL initialization
        console.log("Initializing WebGL plugin...");
        
        // Initialize Soroban SDK
        try {
            if (typeof SorobanKit === 'undefined') {
                console.warn('SorobanKit not found, loading from CDN...');
                var script = document.createElement('script');
                script.src = 'https://cdn.jsdelivr.net/npm/@stellar/soroban-sdk/dist/soroban-sdk.min.js';
                script.onload = function() {
                    console.log('SorobanKit loaded successfully');
                };
                document.head.appendChild(script);
            }
        } catch (error) {
            console.error('Error initializing Soroban SDK:', error);
        }
        
        return 1;
    },

    // Initialize Soroban SDK
    InitializeSoroban: function() {
        try {
            if (typeof window.sorobanKit !== 'undefined') {
                console.log('SorobanKit already initialized');
                return;
            }

            if (typeof SorobanKit === 'undefined') {
                throw new Error('SorobanKit not loaded');
            }

            window.sorobanKit = new SorobanKit({
                server: 'https://soroban-testnet.stellar.org',
                networkPassphrase: 'Test SDF Network ; September 2015',
                timeout: 30000
            });

            console.log('SorobanKit initialized successfully');
            SendMessage('SorobanManager', 'HandleSorobanInitialized', 'true');
        } catch (error) {
            console.error('Error initializing SorobanKit:', error);
            SendMessage('SorobanManager', 'HandleSorobanError', error.toString());
        }
    },

    // Execute Smart Contract Method
    ExecuteContractMethod: function(paramsPtr) {
        try {
            const params = JSON.parse(UTF8ToString(paramsPtr));
            const { contractId, method, args } = params;

            if (!window.sorobanKit) {
                throw new Error('SorobanKit not initialized');
            }

            console.log(`Executing contract ${contractId}.${method} with args:`, args);
            
            window.sorobanKit.contract(contractId)
                .call(method, args)
                .then(result => {
                    SendMessage('SorobanManager', 'HandleContractResponse', JSON.stringify(result));
                })
                .catch(error => {
                    SendMessage('SorobanManager', 'HandleSorobanError', error.toString());
                });
        } catch (error) {
            console.error('Contract execution error:', error);
            SendMessage('SorobanManager', 'HandleSorobanError', error.toString());
        }
    },

    // Get Contract State
    GetContractState: function(contractIdPtr) {
        try {
            const contractId = UTF8ToString(contractIdPtr);

            if (!window.sorobanKit) {
                throw new Error('SorobanKit not initialized');
            }

            window.sorobanKit.getContractState(contractId)
                .then(state => {
                    SendMessage('SorobanManager', 'HandleContractState', JSON.stringify(state));
                })
                .catch(error => {
                    SendMessage('SorobanManager', 'HandleSorobanError', error.toString());
                });
        } catch (error) {
            console.error('Contract state query error:', error);
            SendMessage('SorobanManager', 'HandleSorobanError', error.toString());
        }
    }
}); 