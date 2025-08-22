using UnityEngine;
using System;

namespace BlockchainMaps
{
    public static class QRCodeWrapper
    {
        private const string STELLAR_URI_PREFIX = "web+stellar:";

        public static Texture2D GenerateQRCode(string data, int width = 256, int height = 256)
        {
            if (string.IsNullOrEmpty(data))
            {
                Debug.LogError("Cannot generate QR code: data is null or empty");
                return CreateErrorTexture(width, height);
            }

            // Ensure data has Stellar URI prefix if not present
            if (!data.StartsWith(STELLAR_URI_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                data = STELLAR_URI_PREFIX + data;
            }

            // Create a simple QR-like pattern for testing
            Debug.Log($"Generating QR code for: {data}");
            return CreateQRPatternTexture(width, height, data);
        }

        public static string DecodeQRCode(Texture2D qrTexture)
        {
            if (qrTexture == null) return null;

            Debug.LogWarning("QR code decoding not implemented");
            return null;
        }

        private static Texture2D CreateErrorTexture(int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color32[width * height];
            
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = new Color32(255, 200, 200, 255);
            }
            
            texture.SetPixels32(colors);
            texture.Apply(false);
            return texture;
        }

        private static Texture2D CreateQRPatternTexture(int width, int height, string data)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color32[width * height];
            
            // Create a simple QR-like pattern based on the data hash
            int hash = data.GetHashCode();
            System.Random random = new System.Random(hash);
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Create a pattern that looks like a QR code
                    bool isBlack = false;
                    
                    // Border pattern
                    if (x < 10 || x > width - 10 || y < 10 || y > height - 10)
                    {
                        isBlack = (x + y) % 2 == 0;
                    }
                    // Corner squares (QR code positioning patterns)
                    else if ((x < 50 && y < 50) || (x > width - 50 && y < 50) || (x < 50 && y > height - 50))
                    {
                        isBlack = (x % 8 < 6 && y % 8 < 6) || (x % 8 >= 6 && y % 8 >= 6);
                    }
                    // Data pattern
                    else
                    {
                        isBlack = random.Next(100) < 50;
                    }
                    
                    colors[y * width + x] = isBlack ? 
                        new Color32(0, 0, 0, 255) : // Black
                        new Color32(255, 255, 255, 255); // White
                }
            }
            
            texture.SetPixels32(colors);
            texture.Apply(false);
            return texture;
        }
    }
} 