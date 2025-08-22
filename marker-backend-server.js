const express = require('express');
const cors = require('cors');
const app = express();
const PORT = 3001;

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
  const expectedKey = 'unityapp-api-key-654321';
  
  if (!authHeader || authHeader !== `Bearer ${expectedKey}`) {
    return res.status(401).json({ error: 'Unauthorized', message: 'Invalid API key' });
  }
  
  next();
};

// Apply API key validation to all routes
app.use(validateApiKey);

// Sample marker data
const sampleMarkers = [
  {
    id: 1,
    latitude: 40.7128,
    longitude: -74.0060,
    label: "New York City",
    blockchain: "stellar",
    publicKey: "GAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
    assetCode: "XLM",
    amount: "100.0000000"
  },
  {
    id: 2,
    latitude: 34.0522,
    longitude: -118.2437,
    label: "Los Angeles",
    blockchain: "stellar",
    publicKey: "GBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
    assetCode: "USDC",
    amount: "50.0000000"
  },
  {
    id: 3,
    latitude: 41.8781,
    longitude: -87.6298,
    label: "Chicago",
    blockchain: "stellar",
    publicKey: "GCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC",
    assetCode: "XLM",
    amount: "75.0000000"
  },
  {
    id: 4,
    latitude: 29.7604,
    longitude: -95.3698,
    label: "Houston",
    blockchain: "stellar",
    publicKey: "GDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD",
    assetCode: "USDC",
    amount: "25.0000000"
  },
  {
    id: 5,
    latitude: 33.7490,
    longitude: -84.3880,
    label: "Atlanta",
    blockchain: "stellar",
    publicKey: "GEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE",
    assetCode: "XLM",
    amount: "150.0000000"
  },
  {
    id: 6,
    latitude: 39.9526,
    longitude: -75.1652,
    label: "Philadelphia",
    blockchain: "stellar",
    publicKey: "GFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
    assetCode: "USDC",
    amount: "200.0000000"
  },
  {
    id: 7,
    latitude: 32.7767,
    longitude: -96.7970,
    label: "Dallas",
    blockchain: "stellar",
    publicKey: "GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG",
    assetCode: "XLM",
    amount: "125.0000000"
  },
  {
    id: 8,
    latitude: 25.7617,
    longitude: -80.1918,
    label: "Miami",
    blockchain: "stellar",
    publicKey: "GHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHHH",
    assetCode: "USDC",
    amount: "300.0000000"
  },
  {
    id: 9,
    latitude: 47.6062,
    longitude: -122.3321,
    label: "Seattle",
    blockchain: "stellar",
    publicKey: "GIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIIII",
    assetCode: "XLM",
    amount: "80.0000000"
  },
  {
    id: 10,
    latitude: 37.7749,
    longitude: -122.4194,
    label: "San Francisco",
    blockchain: "stellar",
    publicKey: "GJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJJ",
    assetCode: "USDC",
    amount: "500.0000000"
  }
];

// Get all markers
app.get('/api/base_markers', (req, res) => {
  console.log('Fetching all markers...');
  
  const response = {
    success: true,
    data: sampleMarkers,
    count: sampleMarkers.length,
    timestamp: new Date().toISOString()
  };
  
  console.log(`Returning ${sampleMarkers.length} markers`);
  res.json(response);
});

// Get markers by blockchain
app.get('/api/base_markers/:blockchain', (req, res) => {
  const { blockchain } = req.params;
  console.log(`Fetching markers for blockchain: ${blockchain}`);
  
  const filteredMarkers = sampleMarkers.filter(marker => 
    marker.blockchain.toLowerCase() === blockchain.toLowerCase()
  );
  
  const response = {
    success: true,
    data: filteredMarkers,
    count: filteredMarkers.length,
    blockchain: blockchain,
    timestamp: new Date().toISOString()
  };
  
  console.log(`Returning ${filteredMarkers.length} markers for ${blockchain}`);
  res.json(response);
});

// Get marker by ID
app.get('/api/base_markers/id/:id', (req, res) => {
  const { id } = req.params;
  console.log(`Fetching marker with ID: ${id}`);
  
  const marker = sampleMarkers.find(m => m.id == id);
  
  if (!marker) {
    return res.status(404).json({
      success: false,
      error: 'Marker not found',
      message: `No marker found with ID: ${id}`
    });
  }
  
  const response = {
    success: true,
    data: marker,
    timestamp: new Date().toISOString()
  };
  
  console.log(`Returning marker: ${marker.label}`);
  res.json(response);
});

// Search markers by location (within radius)
app.get('/api/base_markers/search', (req, res) => {
  const { lat, lng, radius = 1000 } = req.query;
  
  if (!lat || !lng) {
    return res.status(400).json({
      success: false,
      error: 'Missing parameters',
      message: 'Latitude and longitude are required'
    });
  }
  
  console.log(`Searching markers near: ${lat}, ${lng} (radius: ${radius}km)`);
  
  // Simple distance calculation (Haversine formula)
  const calculateDistance = (lat1, lon1, lat2, lon2) => {
    const R = 6371; // Earth's radius in km
    const dLat = (lat2 - lat1) * Math.PI / 180;
    const dLon = (lon2 - lon1) * Math.PI / 180;
    const a = Math.sin(dLat/2) * Math.sin(dLat/2) +
              Math.cos(lat1 * Math.PI / 180) * Math.cos(lat2 * Math.PI / 180) *
              Math.sin(dLon/2) * Math.sin(dLon/2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1-a));
    return R * c;
  };
  
  const nearbyMarkers = sampleMarkers.filter(marker => {
    const distance = calculateDistance(
      parseFloat(lat), 
      parseFloat(lng), 
      marker.latitude, 
      marker.longitude
    );
    return distance <= parseFloat(radius);
  });
  
  const response = {
    success: true,
    data: nearbyMarkers,
    count: nearbyMarkers.length,
    searchLocation: { lat: parseFloat(lat), lng: parseFloat(lng) },
    radius: parseFloat(radius),
    timestamp: new Date().toISOString()
  };
  
  console.log(`Found ${nearbyMarkers.length} markers within ${radius}km`);
  res.json(response);
});

// Add new marker
app.post('/api/base_markers', (req, res) => {
  const { latitude, longitude, label, blockchain, publicKey, assetCode, amount } = req.body;
  
  if (!latitude || !longitude || !label) {
    return res.status(400).json({
      success: false,
      error: 'Missing required fields',
      message: 'Latitude, longitude, and label are required'
    });
  }
  
  console.log(`Adding new marker: ${label}`);
  
  const newMarker = {
    id: sampleMarkers.length + 1,
    latitude: parseFloat(latitude),
    longitude: parseFloat(longitude),
    label: label,
    blockchain: blockchain || 'stellar',
    publicKey: publicKey || 'G' + '0'.repeat(55),
    assetCode: assetCode || 'XLM',
    amount: amount || '100.0000000'
  };
  
  sampleMarkers.push(newMarker);
  
  const response = {
    success: true,
    data: newMarker,
    message: 'Marker added successfully',
    timestamp: new Date().toISOString()
  };
  
  console.log(`Marker added: ${newMarker.label}`);
  res.status(201).json(response);
});

// Update marker
app.put('/api/base_markers/:id', (req, res) => {
  const { id } = req.params;
  const updateData = req.body;
  
  console.log(`Updating marker ID: ${id}`);
  
  const markerIndex = sampleMarkers.findIndex(m => m.id == id);
  
  if (markerIndex === -1) {
    return res.status(404).json({
      success: false,
      error: 'Marker not found',
      message: `No marker found with ID: ${id}`
    });
  }
  
  // Update marker with new data
  sampleMarkers[markerIndex] = {
    ...sampleMarkers[markerIndex],
    ...updateData
  };
  
  const response = {
    success: true,
    data: sampleMarkers[markerIndex],
    message: 'Marker updated successfully',
    timestamp: new Date().toISOString()
  };
  
  console.log(`Marker updated: ${sampleMarkers[markerIndex].label}`);
  res.json(response);
});

// Delete marker
app.delete('/api/base_markers/:id', (req, res) => {
  const { id } = req.params;
  
  console.log(`Deleting marker ID: ${id}`);
  
  const markerIndex = sampleMarkers.findIndex(m => m.id == id);
  
  if (markerIndex === -1) {
    return res.status(404).json({
      success: false,
      error: 'Marker not found',
      message: `No marker found with ID: ${id}`
    });
  }
  
  const deletedMarker = sampleMarkers[markerIndex];
  sampleMarkers.splice(markerIndex, 1);
  
  const response = {
    success: true,
    data: deletedMarker,
    message: 'Marker deleted successfully',
    timestamp: new Date().toISOString()
  };
  
  console.log(`Marker deleted: ${deletedMarker.label}`);
  res.json(response);
});

// Health check endpoint
app.get('/health', (req, res) => {
  res.json({ 
    status: 'healthy', 
    service: 'Marker Backend',
    port: PORT,
    markerCount: sampleMarkers.length,
    timestamp: new Date().toISOString()
  });
});

app.listen(PORT, () => {
  console.log(`🗺️  Marker Backend Server running on http://localhost:${PORT}`);
  console.log(`📋 Available endpoints:`);
  console.log(`   GET  /api/base_markers`);
  console.log(`   GET  /api/base_markers/:blockchain`);
  console.log(`   GET  /api/base_markers/id/:id`);
  console.log(`   GET  /api/base_markers/search?lat=&lng=&radius=`);
  console.log(`   POST /api/base_markers`);
  console.log(`   PUT  /api/base_markers/:id`);
  console.log(`   DELETE /api/base_markers/:id`);
  console.log(`   GET  /health`);
  console.log(`🔑 API Key: unityapp-api-key-654321`);
  console.log(`📍 Sample markers loaded: ${sampleMarkers.length}`);
}); 