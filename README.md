# 🗺️ Blockchain Maps - Unity Integration

A comprehensive Unity WebGL project that combines interactive 3D mapping with Stellar blockchain functionality, featuring QR code payments, curved UI interfaces, and real-time blockchain data visualization.

## 🚀 Quick Start

### 1. Install Dependencies
```bash
npm install
```

### 2. Start Backend Servers
```bash
# Windows (PowerShell)
.\start-servers.ps1

# Windows (Batch)
start-servers.bat

# Manual (any OS)
npm start
```

### 3. Open Unity Project
1. Open Unity Hub
2. Open `BlockchainMaps Server` project
3. Open `BlockchainMapServer` scene
4. Press Play

## 📁 Project Structure

```
BlockchainMaps Server/
├── Assets/
│   ├── Scripts/
│   │   ├── GetBlockchainMarkers.cs      # Main integration script
│   │   ├── AutoStellarSetup.cs          # Automatic component setup
│   │   ├── StellarApiClient.cs          # Stellar API communication
│   │   ├── StellarWalletManager.cs      # Wallet management
│   │   ├── QRPaymentProcessor.cs        # QR code payments
│   │   └── StellarQRManager.cs          # QR code generation
│   ├── Scenes/
│   │   └── BlockchainMapServer.unity    # Main scene
│   └── Resources/                       # Assets and prefabs
├── Backend Servers/
│   ├── stellar-backend-server.js        # Stellar operations (Port 3000)
│   ├── marker-backend-server.js         # Marker data (Port 3001)
│   └── package.json                     # Dependencies
├── WebGL_Build/                         # Built WebGL application
└── Documentation/
    ├── SETUP_GUIDE.md                   # Complete setup guide
    └── STELLAR_BACKEND_INTEGRATION.md   # Integration documentation
```

## 🔧 Features

### 🗺️ Interactive 3D Mapping
- **Online Maps Integration**: Real-time map data
- **3D Markers**: Interactive blockchain markers
- **Curved UI**: Immersive curved interface
- **Laser Pointers**: Visual interaction system

### ⭐ Stellar Blockchain Integration
- **Account Creation**: Generate new Stellar accounts
- **Balance Queries**: Real-time balance checking
- **Asset Transfers**: Send XLM and custom assets
- **Asset Issuance**: Create new tokens
- **Trustlines**: Manage asset trustlines
- **Soroban Contracts**: Smart contract integration

### 📱 QR Code System
- **QR Generation**: Create payment QR codes
- **QR Scanning**: Scan and process payments
- **Stellar URIs**: Standard payment URI support
- **Payment Processing**: Automated transaction handling

### 🔐 Authentication
- **Passkey Integration**: Modern authentication
- **Wallet Management**: Secure wallet storage
- **API Security**: Key-based authentication

## 🛠️ Backend Architecture

### Stellar Backend (Port 3000)
Handles all Stellar blockchain operations:
- Account creation and funding
- Balance queries
- Asset transfers
- Asset issuance
- Trustline management
- Soroban contract calls

### Marker Backend (Port 3001)
Manages map marker data:
- CRUD operations for markers
- Location-based searches
- Blockchain filtering
- Real-time updates

## 🎮 Unity Components

### Core Scripts
- **`GetBlockchainMarkers`**: Main integration controller
- **`AutoStellarSetup`**: Automatic component initialization
- **`StellarApiClient`**: API communication layer
- **`StellarWalletManager`**: Wallet operations
- **`QRPaymentProcessor`**: Payment processing
- **`StellarQRManager`**: QR code management

### UI Components
- **Curved UI**: Immersive interface
- **Map Controls**: Interactive map navigation
- **Marker System**: 3D blockchain markers
- **Payment UI**: Transaction interfaces

## 🔌 API Endpoints

### Stellar Operations
```
POST /create-account          # Create new account
POST /show-balance           # Get account balance
POST /transfer-asset         # Transfer assets
POST /issue-asset           # Issue new assets
POST /create-trustline      # Create trustlines
POST /call-contract-method  # Soroban contracts
GET  /health               # Health check
```

### Marker Operations
```
GET  /api/base_markers     # Get all markers
GET  /api/base_markers/:blockchain  # Filter by blockchain
GET  /api/base_markers/search       # Location search
POST /api/base_markers     # Add new marker
PUT  /api/base_markers/:id # Update marker
DELETE /api/base_markers/:id # Delete marker
GET  /health              # Health check
```

## 🧪 Testing

### Backend Testing
```bash
# Test health endpoints
curl http://localhost:3000/health
curl http://localhost:3001/health

# Test Stellar account creation
curl -X POST http://localhost:3000/create-account \
  -H "Authorization: Bearer stellar-api-key-654321"

# Test marker retrieval
curl http://localhost:3001/api/base_markers \
  -H "Authorization: Bearer unityapp-api-key-654321"
```

### Unity Testing
1. **Run Unity** in Play mode
2. **Check Console** for initialization messages
3. **Verify Markers** appear on map
4. **Test QR Codes** by clicking markers
5. **Test Payments** through QR scanning

## 🔍 Troubleshooting

### Common Issues

#### Backend Servers Not Starting
```bash
# Check if ports are in use
netstat -an | findstr :3000
netstat -an | findstr :3001

# Kill processes if needed
taskkill /F /PID <PID_NUMBER>
```

#### CORS Errors
- Ensure both servers are running
- Check CORS configuration
- Verify API keys match

#### Unity Build Issues
- Check script compilation
- Verify WebGL settings
- Ensure all assets included

### Debug Mode
```bash
# Backend with detailed logging
DEBUG=* npm run dev

# Unity Console
# Look for [StellarApiClient], [StellarWalletManager] logs
```

## 📱 WebGL Deployment

### Building for WebGL
1. **File** → **Build Settings**
2. **Platform**: WebGL
3. **Player Settings** → **WebGL** → **Publishing Settings**
4. **Compression Format**: Disabled (development)
5. **Build** the project

### Production Deployment
1. **Start backend servers** on production server
2. **Serve WebGL build** with HTTP server
3. **Configure CORS** for production domain
4. **Set up HTTPS** for security

## 🔐 Security

### Development
- API keys hardcoded for development
- HTTP for local development
- CORS allows all localhost ports

### Production
- Use environment variables for API keys
- Implement HTTPS
- Restrict CORS to specific domains
- Add rate limiting
- Implement proper authentication

## 📚 Documentation

- **[SETUP_GUIDE.md](SETUP_GUIDE.md)**: Complete setup instructions
- **[STELLAR_BACKEND_INTEGRATION.md](Assets/Scripts/STELLAR_BACKEND_INTEGRATION.md)**: Integration details
- **[Stellar SDK Documentation](https://stellar.github.io/js-stellar-sdk/)**: Official Stellar docs
- **[Unity WebGL Documentation](https://docs.unity3d.com/Manual/webgl.html)**: Unity WebGL guide

## 🎯 Roadmap

### Phase 1: Basic Integration ✅
- [x] Stellar API client
- [x] Wallet management
- [x] QR code system
- [x] Map integration
- [x] Basic UI

### Phase 2: Enhanced Features
- [ ] Real-time transaction updates
- [ ] Advanced Soroban integration
- [ ] Multi-wallet support
- [ ] Transaction history
- [ ] Advanced UI components

### Phase 3: Production Features
- [ ] User authentication
- [ ] Database integration
- [ ] Real-time WebSocket updates
- [ ] Advanced security
- [ ] Mobile optimization

## 🤝 Contributing

1. **Fork** the repository
2. **Create** a feature branch
3. **Make** your changes
4. **Test** thoroughly
5. **Submit** a pull request

## 📞 Support

For issues and questions:
1. Check the **[SETUP_GUIDE.md](SETUP_GUIDE.md)**
2. Review the troubleshooting section
3. Check Unity Console for errors
4. Test API endpoints directly
5. Verify network connectivity

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

**Built with ❤️ using Unity, Stellar, and Node.js**