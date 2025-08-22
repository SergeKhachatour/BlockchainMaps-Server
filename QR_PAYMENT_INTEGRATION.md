# QR Payment Integration Guide

## Overview

This system allows you to scan QR codes in the game and send XLM payments to the scanned addresses using the backend API. The system integrates with your existing Stellar backend server running on `localhost:3000`.

## Features

- **QR Code Scanning**: Scan QR codes attached to map markers
- **Payment UI**: Clean payment interface with amount input
- **Backend Integration**: Uses your Node.js Stellar backend API
- **Wallet Management**: Integrates with the existing wallet system
- **Real-time Feedback**: Status updates during payment processing

## Setup Instructions

### 1. Backend Setup

Ensure your Node.js backend is running with the provided credentials:
```
PROTOCOL=http
HOST=localhost
PORT=3000
API_KEY=stellar-api-key-654321
```

### 2. Unity Setup

1. **Add StellarBackendSetup Component**:
   - Add the `StellarBackendSetup` component to any GameObject in your scene
   - This will automatically create the required backend components

2. **Add QRPaymentSetup Component**:
   - Add the `QRPaymentSetup` component to any GameObject in your scene
   - This will automatically create the QR payment processor

3. **Verify Components**:
   - Check that `StellarApiClient` and `StellarWalletManager` are created
   - Check that `QRPaymentProcessor` is created

### 3. Authentication

1. **Authenticate with Passkey**: Use the existing passkey authentication system
2. **Create Wallet**: The system will automatically create a Stellar wallet when you authenticate
3. **Verify Wallet**: Check that you have a wallet with XLM balance

## How to Use

### Scanning QR Codes

1. **Enable Scanning**: Press `E` to toggle QR scanning mode
2. **Approach Markers**: Move close to map markers with QR codes
3. **Scan QR Code**: Press `F` when near a QR code to scan it
4. **Payment UI**: A payment interface will appear with the recipient address

### Making Payments

1. **Enter Amount**: Type the amount of XLM you want to send
2. **Confirm Payment**: Click "Confirm Payment" to send the transaction
3. **Wait for Confirmation**: The system will show the transaction status
4. **Success**: You'll see the transaction hash when successful

### Payment UI Features

- **Recipient Display**: Shows the destination address (truncated for privacy)
- **Amount Input**: Enter the XLM amount to send
- **Status Updates**: Real-time feedback during payment processing
- **Error Handling**: Clear error messages if something goes wrong

## Technical Details

### QR Code Format

The system supports multiple QR code formats:

1. **Stellar URI Format**: `web+stellar:pay?destination=ADDRESS&amount=100`
2. **Custom Format**: `PUBLICKEY|web+stellar:pay?destination=ADDRESS&amount=100`
3. **Simple Public Key**: Just the Stellar public key

### Backend API Integration

The system uses these backend endpoints:

- **Account Creation**: `/create-account` (for new wallets)
- **Balance Check**: `/show-balance` (to verify funds)
- **Asset Transfer**: `/transfer-asset` (to send XLM)

### Error Handling

The system handles various error scenarios:

- **No Wallet**: Prompts to authenticate first
- **Insufficient Balance**: Shows available balance
- **Network Errors**: Displays backend connection issues
- **Invalid QR Codes**: Handles malformed QR data

## Troubleshooting

### Common Issues

1. **"No wallet available"**
   - Solution: Authenticate with passkey first
   - Check that `StellarWalletManager` is created

2. **"StellarApiClient not found"**
   - Solution: Run `StellarBackendSetup` component
   - Check that backend components are created

3. **"Payment failed"**
   - Check backend server is running on `localhost:3000`
   - Verify API key is correct
   - Check wallet has sufficient XLM balance

4. **QR codes not scanning**
   - Press `E` to enable scanning mode
   - Move closer to QR codes
   - Press `F` when near a QR code

### Debug Information

The system provides detailed debug logs:

- QR scanning events
- Payment processing steps
- Backend API calls
- Error details

Check the Unity Console for detailed information.

## Configuration

### Default Settings

- **Default Amount**: 10 XLM
- **Default Memo**: "Payment via QR code scan"
- **Backend URL**: `http://localhost:3000`
- **API Key**: `stellar-api-key-654321`

### Customization

You can modify these settings in the component inspectors:

- `QRPaymentProcessor`: Payment UI settings
- `StellarBackendSetup`: Backend configuration
- `StellarApiClient`: API endpoint settings

## Security Considerations

- **Private Keys**: Never expose secret keys in logs
- **API Keys**: Keep backend API keys secure
- **Network**: Use HTTPS in production
- **Validation**: All inputs are validated before processing

## Next Steps

1. **Test the System**: Try scanning QR codes and making payments
2. **Customize UI**: Modify the payment interface as needed
3. **Add Features**: Extend with additional payment options
4. **Production**: Deploy with proper security measures

## Support

If you encounter issues:

1. Check the Unity Console for error messages
2. Verify backend server is running
3. Ensure all components are properly created
4. Check wallet authentication status

The system is designed to be robust and provide clear feedback for any issues that arise. 