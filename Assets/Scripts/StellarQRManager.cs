using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlockchainMaps.Authentication;
using InfinityCode.OnlineMapsExamples;

namespace BlockchainMaps
{
    [DefaultExecutionOrder(-50)]
    public class StellarQRManager : MonoBehaviour
    {
        public Camera scanningCamera;
        public event Action<string> OnQRScanned;
        
        private Texture2D qrCodeTexture;
        private Material qrCodeMaterial;
        public LineRenderer currentLaser;
        private Dictionary<string, bool> scannedQRCodesByPublicKey = new Dictionary<string, bool>();
        private Dictionary<string, float> lastScanTimes = new Dictionary<string, float>();
        private GameObject focusedQRCode;
        private bool _isScanning = false;
        private bool _isHovering = false;
        public float scanDistance = 2000f; // Increased to 2000 units for easier scanning from higher positions

        public bool isScanning
        {
            get { return _isScanning; }
            set
            {
                Debug.Log($"[StellarQRManager] Setting scanning from {_isScanning} to {value}");
                _isScanning = value;
                if (!value && currentLaser != null)
                {
                    Destroy(currentLaser.gameObject);
                    currentLaser = null;
                }
                
                // Clear QR cache when starting to scan
                if (value)
                {
                    ClearQRCodeCache();
                }
            }
        }

        public bool isHovering
        {
            get { return _isHovering; }
            private set { _isHovering = value; }
        }

        void Start()
        {
            Debug.Log("StellarQRManager Start");
            if (scanningCamera == null)
            {
                scanningCamera = Camera.main;
                Debug.Log($"Main camera assigned: {scanningCamera != null}");
            }
            
            // Enable scanning by default
            _isScanning = true;
            Debug.Log($"Scanning enabled: {_isScanning}, Scan distance: {scanDistance}");
        }

        void Update()
        {
            if (!isScanning) 
            {
                if (Time.frameCount % 120 == 0) // Log every 2 seconds
                {
                    Debug.Log($"[StellarQRManager] Scanning disabled: {_isScanning}");
                }
                return;
            }

            // Scan every frame for more responsive scanning
            // if (Time.frameCount % 10 != 0) return;

            // Find all QR codes in scene
            GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
            
            // Only log count occasionally to reduce spam
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[StellarQRManager] Found {qrCodes.Length} QR codes in scene");
                if (qrCodes.Length > 0)
                {
                    Debug.Log($"[StellarQRManager] First QR code position: {qrCodes[0].transform.position}");
                    Debug.Log($"[StellarQRManager] Camera position: {scanningCamera.transform.position}");
                }
            }
            
            float closestDistance = float.MaxValue;
            GameObject closestQRCode = null;
            Vector3 closestPoint = Vector3.zero;

            // Find the single closest QR code
            foreach (var qrCode in qrCodes)
            {
                if (qrCode != null)
                {
                    float distance = Vector3.Distance(scanningCamera.transform.position, qrCode.transform.position);
                    
                    // Debug distance calculation occasionally
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[StellarQRManager] QR code {qrCode.name} at distance: {distance:F1} (max: {scanDistance})");
                    }
                    
                    // Only consider QR codes within scan distance
                    if (distance <= scanDistance)
                    {
                        // Check if we can see the QR code (raycast to it)
                        Vector3 direction = (qrCode.transform.position - scanningCamera.transform.position).normalized;
                        Ray ray = new Ray(scanningCamera.transform.position, direction);
                        RaycastHit hit;

                        if (Physics.Raycast(ray, out hit, scanDistance))
                        {
                            if (hit.collider.gameObject == qrCode)
                            {
                                if (distance < closestDistance)
                                {
                                    closestDistance = distance;
                                    closestQRCode = qrCode;
                                    closestPoint = hit.point;
                                    
                                    if (Time.frameCount % 60 == 0)
                                    {
                                        Debug.Log($"[StellarQRManager] Found visible QR code: {qrCode.name} at distance: {distance:F1}");
                                    }
                                }
                            }
                        }
                        else if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[StellarQRManager] Raycast failed for QR code: {qrCode.name}");
                        }
                    }
                }
            }

            // Update focused QR code - only process the single closest one
            if (closestQRCode != null)
            {
                _isHovering = true;
                focusedQRCode = closestQRCode;
                
                // Show distance info and allow scanning
                Debug.Log($"Closest QR code at distance: {closestDistance:F1} - Press E to scan");
                
                // Manual scan with E key
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log($"Manual scan triggered at distance: {closestDistance:F1}");
                    ScanQRCode(scanningCamera, closestPoint);
                }
            }
            else
            {
                _isHovering = false;
                focusedQRCode = null;
            }

            // Debug key bindings
            if (Input.GetKeyDown(KeyCode.F1))
            {
                DebugScanningState();
            }
            
            if (Input.GetKeyDown(KeyCode.F2))
            {
                EnableScanning();
            }
            
            if (Input.GetKeyDown(KeyCode.F3))
            {
                DisableScanning();
            }
        }

        public void AttachQRCodeToMarker(GameObject marker, string publicKey)
        {
            AttachQRCodeToMarker(marker, publicKey, null);
        }

        public void AttachQRCodeToMarker(GameObject marker, string publicKey, string uriData)
        {
            try
            {
                Debug.Log("=== Starting QR Code Attachment ===");
                Debug.Log($"Marker: {marker?.name}, PublicKey: {publicKey}, HasUriData: {!string.IsNullOrEmpty(uriData)}");

                if (marker == null)
                {
                    Debug.LogError("Cannot attach QR code: marker is null");
                    return;
                }

                // Remove " - Stellar" suffix if present
                publicKey = publicKey.Replace(" - Stellar", "");

                // Generate QR code texture
                Debug.Log("Generating QR code texture...");
                Texture2D qrTexture = string.IsNullOrEmpty(uriData) ? 
                    QRCodeWrapper.GenerateQRCode(publicKey) : 
                    QRCodeWrapper.GenerateQRCode(uriData);

                if (qrTexture == null)
                {
                    Debug.LogError("Failed to generate QR code texture");
                    return;
                }
                Debug.Log("QR code texture generated successfully");

                // Find or create QR code plane
                Transform qrPlane = marker.transform.Find("QRCodePlane");
                if (qrPlane == null)
                {
                    Debug.Log("Creating new QR code plane");
                    GameObject qrPlaneObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    if (qrPlaneObject == null)
                    {
                        Debug.LogError("Failed to create QR code plane");
                        return;
                    }
                    qrPlaneObject.name = "QRCodePlane";
                    qrPlaneObject.transform.SetParent(marker.transform);
                    qrPlane = qrPlaneObject.transform;
                    qrPlaneObject.tag = "QRCode";
                }
                else
                {
                    Debug.Log("Found existing QR code plane");
                }

                // Position and scale the QR code plane - positioned high above marker for visibility
                qrPlane.localPosition = new Vector3(0, 15f, 0); // Much higher position to avoid being hidden under marker
                qrPlane.localRotation = Quaternion.Euler(0, 180, 0); // Face horizontally instead of upward
                qrPlane.localScale = new Vector3(3f, 3f, 3f); // Slightly smaller scale
                Debug.Log($"QR Plane configured - Position: {qrPlane.localPosition}, Rotation: {qrPlane.localRotation.eulerAngles}, Scale: {qrPlane.localScale}");

                // Apply QR code texture with optimized material settings
                MeshRenderer meshRenderer = qrPlane.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Debug.Log("Setting up QR code material");
                    
                    // Try to find appropriate shader
                    Shader shader = Shader.Find("Unlit/Texture");
                    if (shader == null)
                    {
                        shader = Shader.Find("Mobile/Unlit");
                    }
                    if (shader == null)
                    {
                        shader = Shader.Find("Sprites/Default");
                    }
                    
                    if (shader == null)
                    {
                        Debug.LogError("Could not find any suitable shader for QR code material");
                        return;
                    }
                    
                    Debug.Log($"Using shader: {shader.name}");
                    Material material = new Material(shader);
                    material.mainTexture = qrTexture;
                    material.color = Color.white;
                    
                    // Make QR code visible from both sides
                    if (material.HasProperty("_Cull"))
                    {
                        material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
                    }
                    
                    // Ensure QR code is always visible
                    if (material.HasProperty("_ZWrite"))
                    {
                        material.SetFloat("_ZWrite", 0);
                    }
                    if (material.HasProperty("_ZTest"))
                    {
                        material.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
                    }
                    
                    material.renderQueue = 3100;
                    
                    meshRenderer.material = material;
                    meshRenderer.receiveShadows = false;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                    meshRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                    Debug.Log("Material settings applied successfully");

                    MeshCollider meshCollider = qrPlane.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                    {
                        meshCollider = qrPlane.gameObject.AddComponent<MeshCollider>();
                        Debug.Log("Added mesh collider");
                    }
                    meshCollider.convex = true;
                    meshCollider.isTrigger = true;
                }

                // Store the public key in the QR code GameObject name for easy retrieval
                qrPlane.gameObject.name = $"QRCode_{publicKey}";
                
                // Remove billboard behavior - keep QR codes static
                QRCodeBillboard billboard = qrPlane.gameObject.GetComponent<QRCodeBillboard>();
                if (billboard != null)
                {
                    DestroyImmediate(billboard);
                }

                Debug.Log($"QR code setup complete - World Position: {qrPlane.position}, Local Position: {qrPlane.localPosition}");
                Debug.Log("=== QR Code Attachment Complete ===");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error attaching QR code to marker: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        public void ScanQRCode(Camera camera, Vector3 hitPoint)
        {
            if (!isScanning || focusedQRCode == null) 
            {
                Debug.LogWarning($"Cannot scan QR code - Scanning: {isScanning}, FocusedQRCode: {focusedQRCode != null}");
                return;
            }

            try
            {
                // Extract the actual public key from the QR code GameObject name
                string publicKey = ExtractPublicKeyFromMarker(focusedQRCode);
                
                // Allow re-scanning after 1 second for more responsive scanning
                bool canScan = !scannedQRCodesByPublicKey.ContainsKey(publicKey) || 
                              !scannedQRCodesByPublicKey[publicKey] ||
                              (Time.time - GetLastScanTime(publicKey)) > 1f;
                
                if (canScan)
                {
                    Debug.Log($"=== QR CODE SCANNED ===");
                    Debug.Log($"Marker: {publicKey}");
                    Debug.Log($"QR Code Position: {focusedQRCode.transform.position}");
                    Debug.Log($"Camera Position: {camera.transform.position}");
                    Debug.Log($"Distance: {Vector3.Distance(camera.transform.position, focusedQRCode.transform.position):F1}");

                    CreateLaserEffect(camera.transform.position, hitPoint);

                    string simulatedData = $"{publicKey}|web+stellar:pay?destination={publicKey}&amount=100";
                    Debug.Log($"QR Data: {simulatedData}");
                    Debug.Log($"=== SCAN COMPLETE ===");
                    
                    scannedQRCodesByPublicKey[publicKey] = true;
                    SetLastScanTime(publicKey);
                    
                    // Debug the event invocation
                    Debug.Log($"[StellarQRManager] Invoking OnQRScanned event with data: {simulatedData}");
                    Debug.Log($"[StellarQRManager] Event subscribers count: {OnQRScanned?.GetInvocationList()?.Length ?? 0}");
                    
                    OnQRScanned?.Invoke(simulatedData);
                    
                    Debug.Log($"[StellarQRManager] Event invocation completed");
                }
                else
                {
                    Debug.Log($"QR code already scanned for: {publicKey} (wait 5 seconds to re-scan)");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scanning QR code: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        private void CreateLaserEffect(Vector3 start, Vector3 end)
        {
            // Clean up old laser if it exists
            if (currentLaser != null)
            {
                Destroy(currentLaser.gameObject);
                currentLaser = null;
            }

            // Create new laser
            GameObject laserLine = new GameObject("ScannerLaser");
            currentLaser = laserLine.AddComponent<LineRenderer>();
            
            Material laserMat = new Material(Shader.Find("Particles/Standard Unlit"));
            laserMat.color = new Color(1, 0, 0, 0.8f);
            currentLaser.material = laserMat;
            
            currentLaser.startWidth = 0.1f;
            currentLaser.endWidth = 0.01f;
            currentLaser.positionCount = 2;
            currentLaser.useWorldSpace = true;
            currentLaser.SetPosition(0, start);
            currentLaser.SetPosition(1, end);

            // Auto-destroy laser after 2 seconds
            Destroy(laserLine, 2f);
        }

        private void OnDestroy()
        {
            if (qrCodeTexture != null) Destroy(qrCodeTexture);
            if (qrCodeMaterial != null) Destroy(qrCodeMaterial);
            if (currentLaser != null) Destroy(currentLaser.gameObject);
        }

        public bool IsQRCodeScanned(string publicKey)
        {
            return scannedQRCodesByPublicKey.ContainsKey(publicKey) && scannedQRCodesByPublicKey[publicKey];
        }
        
        public void ClearQRCodeCache()
        {
            scannedQRCodesByPublicKey.Clear();
            Debug.Log("QR code cache cleared");
        }
        
        public void ResetQRCodeScan(string publicKey)
        {
            if (scannedQRCodesByPublicKey.ContainsKey(publicKey))
            {
                scannedQRCodesByPublicKey[publicKey] = false;
                lastScanTimes.Remove(publicKey);
                Debug.Log($"QR code scan reset for: {publicKey}");
            }
        }
        
        private float GetLastScanTime(string publicKey)
        {
            return lastScanTimes.ContainsKey(publicKey) ? lastScanTimes[publicKey] : 0f;
        }
        
        private void SetLastScanTime(string publicKey)
        {
            lastScanTimes[publicKey] = Time.time;
        }

        private string ExtractPublicKeyFromMarker(GameObject qrCode)
        {
            try
            {
                // First, try to get the public key from the QR code GameObject name
                if (qrCode.name.StartsWith("QRCode_"))
                {
                    string publicKey = qrCode.name.Substring("QRCode_".Length);
                    Debug.Log($"Found public key in QR code name: {publicKey}");
                    return publicKey;
                }
                
                // Fallback: Try to find the marker component that contains the label
                Transform markerTransform = qrCode.transform.parent;
                if (markerTransform != null)
                {
                    // Look for OnlineMapsMarker3D component
                    var marker3D = markerTransform.GetComponent<OnlineMapsMarker3D>();
                    if (marker3D != null && !string.IsNullOrEmpty(marker3D.label))
                    {
                        Debug.Log($"Found marker label: {marker3D.label}");
                        // Extract public key from label (format: "PUBLICKEY - Blockchain")
                        string[] parts = marker3D.label.Split('-');
                        if (parts.Length >= 2)
                        {
                            string publicKey = parts[0].Trim();
                            Debug.Log($"Extracted public key: {publicKey}");
                            return publicKey;
                        }
                        Debug.Log($"Using full label as public key: {marker3D.label}");
                        return marker3D.label;
                    }
                    
                    // Try to find OnlineMapsMarker component
                    var marker = markerTransform.GetComponent<OnlineMapsMarker>();
                    if (marker != null && !string.IsNullOrEmpty(marker.label))
                    {
                        Debug.Log($"Found 2D marker label: {marker.label}");
                        string[] parts = marker.label.Split('-');
                        if (parts.Length >= 2)
                        {
                            string publicKey = parts[0].Trim();
                            Debug.Log($"Extracted public key from 2D marker: {publicKey}");
                            return publicKey;
                        }
                        return marker.label;
                    }
                    
                    // Fallback to marker name
                    string fallbackName = markerTransform.name.Replace("(Clone)", "").Trim();
                    Debug.Log($"Using fallback name: {fallbackName}");
                    return fallbackName;
                }
                
                Debug.LogWarning("No marker transform found");
                return "Unknown";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error extracting public key: {e.Message}");
                return "Error";
            }
        }

        // Public method to manually enable scanning
        public void EnableScanning()
        {
            Debug.Log("[StellarQRManager] Manually enabling scanning");
            isScanning = true;
        }

        // Public method to manually disable scanning
        public void DisableScanning()
        {
            Debug.Log("[StellarQRManager] Manually disabling scanning");
            isScanning = false;
        }

        // Debug method to show current scanning state
        public void DebugScanningState()
        {
            Debug.Log($"[StellarQRManager] Current scanning state: {_isScanning}");
            Debug.Log($"[StellarQRManager] Scan distance: {scanDistance}");
            Debug.Log($"[StellarQRManager] Camera assigned: {scanningCamera != null}");
            if (scanningCamera != null)
            {
                Debug.Log($"[StellarQRManager] Camera position: {scanningCamera.transform.position}");
            }
            
            GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
            Debug.Log($"[StellarQRManager] Found {qrCodes.Length} QR codes with tag 'QRCode'");
            for (int i = 0; i < Math.Min(qrCodes.Length, 5); i++) // Show first 5
            {
                if (qrCodes[i] != null)
                {
                    Debug.Log($"[StellarQRManager] QR Code {i}: {qrCodes[i].name} at {qrCodes[i].transform.position}");
                }
            }
            
            Debug.Log($"[StellarQRManager] OnQRScanned event subscribers: {OnQRScanned?.GetInvocationList()?.Length ?? 0}");
        }
    }
} 