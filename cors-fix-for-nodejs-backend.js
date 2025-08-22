// CORS Fix for your Node.js Backend
// Add this to your app.js file right after your imports and before your routes

import cors from 'cors';

// Configure CORS to allow Unity WebGL requests
const corsOptions = {
  origin: [
    'http://localhost:49881',  // Unity WebGL dev server
    'http://localhost:64752',  // Another common Unity port
    'http://localhost:65323',  // Another Unity port from your logs
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

// Apply CORS middleware
app.use(cors(corsOptions));

// Handle preflight requests
app.options('*', cors(corsOptions));

// Alternative manual CORS setup (if you don't want to install cors package):
/*
app.use((req, res, next) => {
  const origin = req.headers.origin;
  const allowedOrigins = [
    'http://localhost:49881',
    'http://localhost:64752',
    'http://localhost:65323',
    'http://127.0.0.1:49881',
    'http://127.0.0.1:64752',
    'http://127.0.0.1:65323'
  ];
  
  if (allowedOrigins.includes(origin) || /^http:\/\/(localhost|127\.0\.0\.1):\d+$/.test(origin)) {
    res.setHeader('Access-Control-Allow-Origin', origin);
  }
  
  res.setHeader('Access-Control-Allow-Credentials', 'true');
  res.setHeader('Access-Control-Allow-Methods', 'GET, POST, PUT, DELETE, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type, Authorization, X-Requested-With, Accept, Origin');
  
  if (req.method === 'OPTIONS') {
    res.sendStatus(200);
    return;
  }
  
  next();
});
*/

console.log('CORS configured for Unity WebGL requests'); 