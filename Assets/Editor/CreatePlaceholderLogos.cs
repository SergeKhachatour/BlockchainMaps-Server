using UnityEngine;
using UnityEditor;

namespace BlockchainMaps.Editor
{
    public class CreatePlaceholderLogos : UnityEditor.Editor
    {
        [MenuItem("Tools/Create Blockchain Logos")]
        public static void CreateLogos()
        {
            CreatePlaceholderLogo("Assets/Resources/Images/stellar-logo.png", new Color(0.2f, 0.4f, 0.8f, 1f));
            CreatePlaceholderLogo("Assets/Resources/Images/circle-logo.png", new Color(0.1f, 0.7f, 0.3f, 1f));
            AssetDatabase.Refresh();
        }

        private static void CreatePlaceholderLogo(string path, Color color)
        {
            // Create a 256x256 texture
            Texture2D tex = new Texture2D(256, 256);
            Color[] colors = new Color[256 * 256];

            // Fill with base color
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            // Add a simple design (circle)
            for (int x = 0; x < 256; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    float distanceFromCenter = Vector2.Distance(new Vector2(x, y), new Vector2(128, 128));
                    if (distanceFromCenter < 100)
                    {
                        colors[y * 256 + x] = Color.white;
                    }
                    else if (distanceFromCenter < 110)
                    {
                        colors[y * 256 + x] = Color.Lerp(Color.white, color, (distanceFromCenter - 100) / 10f);
                    }
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            // Save to file
            byte[] bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }
    }
} 