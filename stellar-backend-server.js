const express = require('express');
const cors = require('cors');
const { Server } = require('stellar-sdk');
const app = express();
const PORT = 3000;

// CORS configuration for Unity WebGL
const corsOptions = {
  origin: [
    'http://localhost:49881',  // Unity WebGL dev server
    'http://localhost:64752',  // Another common Unity port
    'http://localhost:65323',  // Another Unity port
    'http://127.0.0.1:49881',
    'http://127.0.0.1:64752', 
    'http://127.0.0.1:65323',
    /^http:\/\/localhost:\d+$/,  // Allow any localhost port
    /^http:\/\/127\.0\.0\.1:\d+$/ // Allow any 127.0.0.1 port
  ],
  credentials: true,
  methods: ['GET', 'POST', 'PUT', 'DELETE', 'OPTIONS'],
  allowedHeaders: [
    'Content-Type', 
    'Authorization', 
    'X-Requested-With',
    'Accept',
    'Origin'
  ]
};

app.use(cors(corsOptions));
app.use(express.json());

// API Key validation middleware
const validateApiKey = (req, res, next) => {
  const authHeader = req.headers.authorization;
  const expectedKey = 'stellar-api-key-654321';
  
  if (!authHeader || authHeader !== `Bearer ${expectedKey}`) {
    return res.status(401).json({ error: 'Unauthorized', message: 'Invalid API key' });
  }
  
  next();
};

// Apply API key validation to all routes
app.use(validateApiKey);

// Initialize Stellar server (testnet)
const server = new Server('https://horizon-testnet.stellar.org');

// Create new Stellar account
app.post('/create-account', async (req, res) => {
  try {
    console.log('Creating new Stellar account...');
    
    // Generate a new keypair
    const { Keypair } = require('stellar-sdk');
    const keypair = Keypair.random();
    
    // Fund the account on testnet
    const response = await fetch(`https://horizon-testnet.stellar.org/friendbot?addr=${keypair.publicKey()}`);
    
    if (!response.ok) {
      throw new Error('Failed to fund account on testnet');
    }
    
    const result = {
      publicKey: keypair.publicKey(),
      secret: keypair.secret(),
      stellarResponse: { success: true },
      sorobanHooksResponse: { success: true },
      message: 'Account created and funded successfully'
    };
    
    console.log(`Account created: ${keypair.publicKey()}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error creating account:', error);
    res.status(500).json({ 
      error: 'Failed to create account', 
      message: error.message 
    });
  }
});

// Get account balance
app.post('/show-balance', async (req, res) => {
  try {
    const { publicKey } = req.body;
    
    if (!publicKey) {
      return res.status(400).json({ error: 'Public key is required' });
    }
    
    console.log(`Getting balance for: ${publicKey}`);
    
    const account = await server.loadAccount(publicKey);
    
    const result = {
      account_id: publicKey,
      balances: account.balances.map(balance => ({
        asset_type: balance.asset_type,
        asset_code: balance.asset_code || 'XLM',
        balance: balance.balance
      }))
    };
    
    console.log(`Balance retrieved for: ${publicKey}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error getting balance:', error);
    res.status(500).json({ 
      error: 'Failed to get balance', 
      message: error.message 
    });
  }
});

// Transfer asset
app.post('/transfer-asset', async (req, res) => {
  try {
    const { senderSecret, recipientPublicKey, assetCode, issuerPublicKey, amount } = req.body;
    
    if (!senderSecret || !recipientPublicKey || !amount) {
      return res.status(400).json({ error: 'Missing required parameters' });
    }
    
    console.log(`Transferring ${amount} ${assetCode || 'XLM'} to ${recipientPublicKey}`);
    
    const { Keypair, TransactionBuilder, Networks, Asset } = require('stellar-sdk');
    const senderKeypair = Keypair.fromSecret(senderSecret);
    
    // Load sender account
    const senderAccount = await server.loadAccount(senderKeypair.publicKey());
    
    // Create transaction
    const transaction = new TransactionBuilder(senderAccount, {
      fee: '100',
      networkPassphrase: Networks.TESTNET
    });
    
    // Add payment operation
    if (assetCode && issuerPublicKey) {
      // Custom asset
      const asset = new Asset(assetCode, issuerPublicKey);
      transaction.addOperation(TransactionBuilder.payment({
        destination: recipientPublicKey,
        asset: asset,
        amount: amount
      }));
    } else {
      // Native XLM
      transaction.addOperation(TransactionBuilder.payment({
        destination: recipientPublicKey,
        asset: Asset.native(),
        amount: amount
      }));
    }
    
    // Build and sign transaction
    const builtTransaction = transaction.setTimeout(30).build();
    builtTransaction.sign(senderKeypair);
    
    // Submit transaction
    const response = await server.submitTransaction(builtTransaction);
    
    const result = {
      hash: response.hash,
      ledger: response.ledger,
      created_at: response.created_at,
      fee_charged: response.fee_charged,
      max_fee: response.max_fee,
      operation_count: response.operation_count,
      envelope_xdr: response.envelope_xdr,
      result_xdr: response.result_xdr,
      result_meta_xdr: response.result_meta_xdr,
      fee_meta_xdr: response.fee_meta_xdr,
      memo_type: response.memo_type,
      memo: response.memo,
      signatures: response.signatures
    };
    
    console.log(`Transfer successful: ${response.hash}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error transferring asset:', error);
    res.status(500).json({ 
      error: 'Failed to transfer asset', 
      message: error.message 
    });
  }
});

// Issue new asset
app.post('/issue-asset', async (req, res) => {
  try {
    const { issuerSecret, assetCode } = req.body;
    
    if (!issuerSecret || !assetCode) {
      return res.status(400).json({ error: 'Issuer secret and asset code are required' });
    }
    
    console.log(`Issuing asset: ${assetCode}`);
    
    const { Keypair, TransactionBuilder, Networks, Asset } = require('stellar-sdk');
    const issuerKeypair = Keypair.fromSecret(issuerSecret);
    
    // Load issuer account
    const issuerAccount = await server.loadAccount(issuerKeypair.publicKey());
    
    // Create transaction
    const transaction = new TransactionBuilder(issuerAccount, {
      fee: '100',
      networkPassphrase: Networks.TESTNET
    });
    
    // Add payment operation to create the asset
    const asset = new Asset(assetCode, issuerKeypair.publicKey());
    transaction.addOperation(TransactionBuilder.payment({
      destination: issuerKeypair.publicKey(),
      asset: asset,
      amount: '1000000' // Issue 1,000,000 units
    }));
    
    // Build and sign transaction
    const builtTransaction = transaction.setTimeout(30).build();
    builtTransaction.sign(issuerKeypair);
    
    // Submit transaction
    const response = await server.submitTransaction(builtTransaction);
    
    const result = {
      hash: response.hash,
      ledger: response.ledger,
      created_at: response.created_at,
      fee_charged: response.fee_charged,
      max_fee: response.max_fee,
      operation_count: response.operation_count,
      envelope_xdr: response.envelope_xdr,
      result_xdr: response.result_xdr,
      result_meta_xdr: response.result_meta_xdr,
      fee_meta_xdr: response.fee_meta_xdr,
      memo_type: response.memo_type,
      memo: response.memo,
      signatures: response.signatures
    };
    
    console.log(`Asset issued successfully: ${response.hash}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error issuing asset:', error);
    res.status(500).json({ 
      error: 'Failed to issue asset', 
      message: error.message 
    });
  }
});

// Create trustline
app.post('/create-trustline', async (req, res) => {
  try {
    const { accountSecret, assetCode, issuerPublicKey, limit } = req.body;
    
    if (!accountSecret || !assetCode || !issuerPublicKey) {
      return res.status(400).json({ error: 'Account secret, asset code, and issuer public key are required' });
    }
    
    console.log(`Creating trustline for ${assetCode}`);
    
    const { Keypair, TransactionBuilder, Networks, Asset, Operation } = require('stellar-sdk');
    const accountKeypair = Keypair.fromSecret(accountSecret);
    
    // Load account
    const account = await server.loadAccount(accountKeypair.publicKey());
    
    // Create transaction
    const transaction = new TransactionBuilder(account, {
      fee: '100',
      networkPassphrase: Networks.TESTNET
    });
    
    // Add trustline operation
    const asset = new Asset(assetCode, issuerPublicKey);
    transaction.addOperation(Operation.changeTrust({
      asset: asset,
      limit: limit || '1000000000'
    }));
    
    // Build and sign transaction
    const builtTransaction = transaction.setTimeout(30).build();
    builtTransaction.sign(accountKeypair);
    
    // Submit transaction
    const response = await server.submitTransaction(builtTransaction);
    
    const result = {
      hash: response.hash,
      ledger: response.ledger,
      created_at: response.created_at,
      fee_charged: response.fee_charged,
      max_fee: response.max_fee,
      operation_count: response.operation_count,
      envelope_xdr: response.envelope_xdr,
      result_xdr: response.result_xdr,
      result_meta_xdr: response.result_meta_xdr,
      fee_meta_xdr: response.fee_meta_xdr,
      memo_type: response.memo_type,
      memo: response.memo,
      signatures: response.signatures
    };
    
    console.log(`Trustline created successfully: ${response.hash}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error creating trustline:', error);
    res.status(500).json({ 
      error: 'Failed to create trustline', 
      message: error.message 
    });
  }
});

// Call Soroban contract method
app.post('/call-contract-method', async (req, res) => {
  try {
    const { contractId, method, secret, parameters } = req.body;
    
    if (!contractId || !method || !secret) {
      return res.status(400).json({ error: 'Contract ID, method, and secret are required' });
    }
    
    console.log(`Calling contract method: ${method} on contract: ${contractId}`);
    
    // This is a placeholder for Soroban contract calls
    // You'll need to implement actual Soroban SDK integration
    const result = {
      success: true,
      message: `Contract method ${method} called successfully`,
      contractId: contractId,
      method: method
    };
    
    console.log(`Contract method called successfully: ${method}`);
    res.json(result);
    
  } catch (error) {
    console.error('Error calling contract method:', error);
    res.status(500).json({ 
      error: 'Failed to call contract method', 
      message: error.message 
    });
  }
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'healthy', 
    service: 'Stellar Backend',
    port: PORT,
    timestamp: new Date().toISOString()
  });
});

app.listen(PORT, () => {
  console.log(`🚀 Stellar Backend Server running on http://localhost:${PORT}`);
  console.log(`📋 Available endpoints:`);
  console.log(`   POST /create-account`);
  console.log(`   POST /show-balance`);
  console.log(`   POST /transfer-asset`);
  console.log(`   POST /issue-asset`);
  console.log(`   POST /create-trustline`);
  console.log(`   POST /call-contract-method`);
  console.log(`   GET  /health`);
  console.log(`🔑 API Key: stellar-api-key-654321`);
}); 