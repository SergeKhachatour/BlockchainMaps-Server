using UnityEngine;
using System;

namespace BlockchainMaps
{
    public static class QRCodeWrapper
    {
        public static Texture2D GenerateQRCode(string data, int width = 256, int height = 256)
        {
            try
            {
                var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
                texture.filterMode = FilterMode.Point;
                
                Color32[] colors = new Color32[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        bool isQRPixel = false;
                        
                        // Create a simple visual pattern that looks like a QR code
                        if (x < width/10 || x > width*9/10 || y < height/10 || y > height*9/10)
                        {
                            isQRPixel = false;
                        }
                        else if ((x < width/4 && y < height/4) || 
                                (x > width*3/4 && y < height/4) || 
                                (x < width/4 && y > height*3/4))   
                        {
                            isQRPixel = true;
                        }
                        else
                        {
                            // Create a pattern based on the input data
                            int dataIndex = ((x + y) * data.Length) % data.Length;
                            isQRPixel = ((x + y + data[dataIndex]) % 32 < 16);
                        }
                        
                        colors[y * width + x] = isQRPixel ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                    }
                }
                
                texture.SetPixels32(colors);
                texture.Apply(false);
                return texture;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating QR code: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return null;
            }
        }

        public static string DecodeQRCode(Texture2D qrTexture)
        {
            Debug.LogWarning("QR code decoding is not implemented without the ZXing library");
            return null;
        }
    }
} 