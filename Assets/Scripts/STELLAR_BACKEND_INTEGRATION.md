# Stellar Backend Integration

This document explains how to integrate your Unity WebGL project with the Stellar Node.js backend server.

## 🚀 Quick Start

### 1. Start Your Backend Server
Make sure your Node.js backend is running on `http://localhost:3000` with the API key `stellar-api-key-654321`.

### 2. Add Setup Component
Add the `StellarBackendSetup` component to any GameObject in your scene. This will automatically:
- Create the required Stellar components
- Set up a test UI for testing the backend
- Initialize the API client and wallet manager

### 3. Test the Integration
The setup will create a test UI with buttons to:
- Create a new Stellar account
- Get account balance
- Transfer assets
- Issue new assets

## 📁 Files Created

### Core Components
- **`StellarApiClient.cs`** - Handles all API communication with your backend
- **`StellarWalletManager.cs`** - Manages wallet data and operations
- **`StellarBackendTester.cs`** - Test UI for manual testing
- **`StellarBackendSetup.cs`** - Automatic setup component

### Data Classes
- **`StellarAccountResponse`** - Response from account creation
- **`StellarBalanceResponse`** - Response from balance queries
- **`StellarTransactionResponse`** - Response from transactions
- **`StellarWalletData`** - Local wallet data storage

## 🔧 Configuration

### API Configuration
The API client is configured with:
- **Base URL**: `http://localhost:3000`
- **API Key**: `stellar-api-key-654321`

### Backend Endpoints Used
- `POST /create-account` - Creates new Stellar accounts
- `POST /show-balance` - Gets account balances
- `POST /transfer-asset` - Transfers assets
- `POST /issue-asset` - Issues new assets
- `POST /create-trustline` - Creates trustlines
- `POST /call-contract-method` - Calls Soroban contracts

## 🎯 Usage Examples

### Creating a Wallet
```csharp
var walletManager = StellarWalletManager.Instance;
var wallet = await walletManager.CreateWallet();
Debug.Log($"Wallet created: {wallet.publicKey}");
```

### Getting Balance
```csharp
var balance = await walletManager.GetBalance();
foreach (var asset in balance.balances)
{
    Debug.Log($"{asset.asset_code}: {asset.balance}");
}
```

### Transferring Assets
```csharp
var transaction = await walletManager.TransferAsset(
    recipientPublicKey,
    assetCode,
    issuerPublicKey,
    amount
);
Debug.Log($"Transfer hash: {transaction.hash}");
```

### Issuing Assets
```csharp
var transaction = await walletManager.IssueAsset("MYTOKEN");
Debug.Log($"Asset issued: {transaction.hash}");
```

## 🔄 Integration with Passkey Authentication

The integration is designed to work with your existing passkey authentication:

1. **User authenticates** with passkey
2. **Stellar wallet is created** via backend API
3. **Wallet data is stored** locally
4. **Balance is fetched** and displayed
5. **User can perform transactions**

## 🛠️ Manual Setup (Alternative)

If you prefer manual setup:

### 1. Create API Client
```csharp
GameObject apiClientObj = new GameObject("StellarApiClient");
apiClientObj.AddComponent<StellarApiClient>();
```

### 2. Create Wallet Manager
```csharp
GameObject walletManagerObj = new GameObject("StellarWalletManager");
walletManagerObj.AddComponent<StellarWalletManager>();
```

### 3. Test the Connection
```csharp
var apiClient = StellarApiClient.Instance;
var account = await apiClient.CreateAccount();
```

## 🔍 Debugging

### Check Console Logs
All operations log detailed information to the Unity console:
- `[StellarApiClient]` - API communication logs
- `[StellarWalletManager]` - Wallet operation logs
- `[StellarBackendTester]` - Test operation logs

### Common Issues

1. **Backend not running**
   - Ensure your Node.js server is running on port 3000
   - Check the API key matches

2. **CORS issues**
   - Your backend should handle CORS for WebGL builds
   - Add appropriate headers to your backend

3. **Network errors**
   - Check firewall settings
   - Verify localhost is accessible

## 🔐 Security Considerations

### API Key Storage
- The API key is currently hardcoded in `StellarApiClient.cs`
- For production, consider using environment variables or secure storage

### Secret Key Storage
- Wallet secret keys are stored in PlayerPrefs (not secure for production)
- Consider using Unity's secure storage or keeping secrets on the backend only

### Network Security
- All communication is over HTTP (not HTTPS)
- For production, use HTTPS and proper SSL certificates

## 🚀 Next Steps

### Phase 1: Basic Integration ✅
- [x] API client created
- [x] Wallet manager created
- [x] Test UI implemented
- [x] Basic operations working

### Phase 2: UI Integration
- [ ] Integrate with existing passkey UI
- [ ] Add transaction forms
- [ ] Create balance display
- [ ] Add asset management UI

### Phase 3: Advanced Features
- [ ] Real-time transaction updates
- [ ] Soroban contract integration
- [ ] Multi-wallet support
- [ ] Transaction history

## 📞 Support

If you encounter issues:

1. Check the Unity console for error messages
2. Verify your backend server is running
3. Test the API endpoints directly (using Postman or curl)
4. Check the network tab in browser dev tools for WebGL builds

## 🔗 Backend API Reference

Your backend provides these endpoints:

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/create-account` | POST | Creates new Stellar account |
| `/show-balance` | POST | Gets account balance |
| `/transfer-asset` | POST | Transfers assets |
| `/issue-asset` | POST | Issues new assets |
| `/create-trustline` | POST | Creates trustlines |
| `/call-contract-method` | POST | Calls Soroban contracts |

All endpoints require the `Authorization: Bearer stellar-api-key-654321` header. 