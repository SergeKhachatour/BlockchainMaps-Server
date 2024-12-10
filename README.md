# BlockchainMaps-Server

A Unity-based visualization tool that combines blockchain data with interactive 3D mapping and curved UI interfaces.

## Features

- **Interactive 3D Map**
  - Displays blockchain nodes as 3D markers on a world map
  - Different marker styles for different blockchain types (Stellar, USDC/Circle)
  - Visual laser effects connecting markers to the ground
  - Dynamic marker placement based on geographical coordinates

- **Curved UI Interface**
  - Scrollable list of all blockchain markers
  - Real-time updates when new markers are loaded
  - Displays public keys and blockchain types
  - Utilizes TextMeshPro for high-quality text rendering

- **API Integration**
  - Fetches marker data from a REST API endpoint
  - Supports bearer token authentication
  - Real-time data updates

## Technical Details

### Dependencies
- Unity (with TextMeshPro)
- CurvedUI 3.4
- Online Maps
- Newtonsoft.Json

### Data Structure
Each marker contains:
- Public Key (blockchain address)
- Blockchain Type (Stellar/USDC)
- Label
- Geographical coordinates (latitude/longitude)

### API Endpoint
- Base URL: `http://localhost:3001/api/base_markers`
- Authentication: Bearer token

## Setup

1. Clone the repository
2. Open in Unity
3. Configure the API endpoint and bearer token in GetBlockchainMarkers component
4. Set up the UI hierarchy as specified in documentation
5. Assign required prefabs and references in the Inspector

## Usage

The application automatically:
1. Fetches marker data from the API
2. Places 3D markers on the map
3. Updates the curved UI with marker information
4. Maintains visual connections between markers and the ground

## Contributing

Feel free to submit issues and enhancement requests.