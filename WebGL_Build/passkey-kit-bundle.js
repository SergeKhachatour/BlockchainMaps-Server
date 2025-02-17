(function(global) {
    'use strict';

    class PasskeyKit {
        constructor(config) {
            this.config = config;
            this.initialized = false;
        }

        async initialize() {
            if (this.initialized) return;
            
            // Initialize with provided config
            this.rpcUrl = this.config.rpcUrl || "https://soroban-testnet.stellar.org";
            this.networkPassphrase = this.config.networkPassphrase || "Test SDF Network ; September 2015";
            this.factoryContractId = this.config.factoryContractId;
            
            this.initialized = true;
            return true;
        }

        async authenticate(username) {
            if (!this.initialized) await this.initialize();
            
            // Simulate authentication for now
            console.log('Authenticating user:', username);
            return true;
        }

        async signTransaction(txData) {
            if (!this.initialized) await this.initialize();
            
            // Simulate transaction signing for now
            console.log('Signing transaction:', txData);
            return {
                signature: 'simulated_signature',
                publicKey: 'simulated_public_key'
            };
        }
    }

    // Export to global scope
    global.PasskeyKit = PasskeyKit;
})(typeof window !== 'undefined' ? window : this); 
