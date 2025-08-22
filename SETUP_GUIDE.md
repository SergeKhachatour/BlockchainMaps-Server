# 🚀 Unity Blockchain Maps - Complete Setup Guide

This guide will help you set up the complete Unity blockchain integration with Stellar backend servers.

## 📋 Prerequisites

- **Node.js** (version 16 or higher)
- **Unity** (2022.3 LTS or newer)
- **Git** (for version control)

## 🛠️ Backend Setup

### Step 1: Install Dependencies

```bash
# Install all required packages
npm install

# Or install individually:
npm install express cors stellar-sdk concurrently nodemon
```

### Step 2: Start the Backend Servers

You have two options:

#### Option A: Start Both Servers Together
```bash
npm start
```

#### Option B: Start Servers Individually
```bash
# Terminal 1 - Stellar Backend (Port 3000)
npm run start:stellar

# Terminal 2 - Marker Backend (Port 3001)
npm run start:marker
```

#### Option C: Development Mode (with auto-restart)
```bash
npm run dev
```

### Step 3: Verify Backend Servers

Check that both servers are running:

- **Stellar Backend**: http://localhost:3000/health
- **Marker Backend**: http://localhost:3001/health

You should see:
```json
{
  "status": "healthy",
  "service": "Stellar Backend",
  "port": 3000,
  "timestamp": "2024-01-01T00:00:00.000Z"
}
```

## 🎮 Unity Setup

### Step 1: Open Unity Project

1. Open Unity Hub
2. Open the `BlockchainMaps Server` project
3. Wait for Unity to compile all scripts

### Step 2: Verify Scene Setup

1. Open the `BlockchainMapServer` scene
2. Check that the following components are present:
   - `GetBlockchainMarkers` script
   - `AutoStellarSetup` script
   - Map components (OnlineMaps)

### Step 3: Configure API Keys

The project is already configured with the correct API keys:
- **Stellar API Key**: `stellar-api-key-654321`
- **Marker API Key**: `unityapp-api-key-654321`

### Step 4: Test the Integration

1. **Build and Run** the Unity project
2. **Check Console** for initialization messages:
   ```
   === Auto Stellar Setup Starting ===
   Creating StellarApiClient...
   Creating StellarWalletManager...
   Creating QRPaymentProcessor...
   === Auto Stellar Setup Complete ===
   ```

3. **Verify Map Markers** appear on the map
4. **Test QR Code Scanning** by clicking on markers

## 🔧 Configuration Details

### Backend URLs
- **Stellar Operations**: `http://localhost:3000`
- **Marker Data**: `http://localhost:3001`

### API Endpoints

#### Stellar Backend (Port 3000)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/create-account` | POST | Creates new Stellar account |
| `/show-balance` | POST | Gets account balance |
| `/transfer-asset` | POST | Transfers assets |
| `/issue-asset` | POST | Issues new assets |
| `/create-trustline` | POST | Creates trustlines |
| `/call-contract-method` | POST | Calls Soroban contracts |
| `/health` | GET | Health check |

#### Marker Backend (Port 3001)
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/base_markers` | GET | Get all markers |
| `/api/base_markers/:blockchain` | GET | Get markers by blockchain |
| `/api/base_markers/id/:id` | GET | Get marker by ID |
| `/api/base_markers/search` | GET | Search markers by location |
| `/api/base_markers` | POST | Add new marker |
| `/api/base_markers/:id` | PUT | Update marker |
| `/api/base_markers/:id` | DELETE | Delete marker |
| `/health` | GET | Health check |

## 🧪 Testing the Integration

### Test 1: Backend Health
```bash
# Test Stellar backend
curl http://localhost:3000/health

# Test Marker backend
curl http://localhost:3001/health
```

### Test 2: Create Stellar Account
```bash
curl -X POST http://localhost:3000/create-account \
  -H "Authorization: Bearer stellar-api-key-654321" \
  -H "Content-Type: application/json"
```

### Test 3: Get Markers
```bash
curl http://localhost:3001/api/base_markers \
  -H "Authorization: Bearer unityapp-api-key-654321"
```

### Test 4: Unity Integration
1. **Run Unity** in Play mode
2. **Check Console** for successful API calls
3. **Verify Markers** appear on the map
4. **Test QR Codes** by clicking markers

## 🔍 Troubleshooting

### Common Issues

#### 1. Backend Servers Not Starting
```bash
# Check if ports are in use
netstat -an | grep :3000
netstat -an | grep :3001

# Kill processes if needed
taskkill /F /PID <PID_NUMBER>
```

#### 2. CORS Errors in Unity
- Ensure both backend servers are running
- Check that CORS is properly configured
- Verify API keys match

#### 3. Unity Build Issues
- Check that all scripts are compiled
- Verify WebGL build settings
- Ensure all required assets are included

#### 4. Network Connection Issues
```bash
# Test localhost connectivity
curl http://localhost:3000/health
curl http://localhost:3001/health

# Check firewall settings
# Allow Node.js through Windows Firewall
```

### Debug Mode

#### Backend Debug
```bash
# Run with detailed logging
DEBUG=* npm run dev
```

#### Unity Debug
1. Open **Console** window in Unity
2. Look for `[StellarApiClient]`, `[StellarWalletManager]` logs
3. Check for error messages

## 📱 WebGL Build

### Building for WebGL
1. **File** → **Build Settings**
2. **Platform**: WebGL
3. **Player Settings** → **WebGL** → **Publishing Settings**
4. **Compression Format**: Disabled (for development)
5. **Build** the project

### Testing WebGL Build
1. **Start backend servers**
2. **Serve the WebGL build** (using any HTTP server)
3. **Open in browser** and test functionality

## 🔐 Security Notes

### Development Environment
- API keys are hardcoded for development
- Using HTTP (not HTTPS) for local development
- CORS allows all localhost ports

### Production Considerations
- Use environment variables for API keys
- Implement HTTPS
- Restrict CORS to specific domains
- Add rate limiting
- Implement proper authentication

## 📚 Additional Resources

### Documentation
- [Stellar SDK Documentation](https://stellar.github.io/js-stellar-sdk/)
- [Unity WebGL Documentation](https://docs.unity3d.com/Manual/webgl.html)
- [Express.js Documentation](https://expressjs.com/)

### Sample Data
The marker backend includes 10 sample markers across major US cities:
- New York City, Los Angeles, Chicago, Houston, Atlanta
- Philadelphia, Dallas, Miami, Seattle, San Francisco

### API Testing
Use tools like **Postman** or **curl** to test the backend APIs before integrating with Unity.

## 🎯 Next Steps

1. **Customize Markers**: Add your own marker data
2. **Implement Soroban**: Add smart contract functionality
3. **Add Authentication**: Implement user authentication
4. **Deploy to Production**: Set up production servers
5. **Add Real-time Updates**: Implement WebSocket connections

## 📞 Support

If you encounter issues:

1. **Check the logs** in both backend terminals
2. **Verify Unity Console** for error messages
3. **Test API endpoints** directly with curl/Postman
4. **Check network connectivity** between Unity and backend
5. **Review CORS configuration** if seeing cross-origin errors

---

**Happy Coding! 🚀** 