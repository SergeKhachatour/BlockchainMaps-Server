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
        private static bool isInitialized = false;
        public bool isScanning = false;
        public float scanDistance = 3000f;
        public Camera scanningCamera;
        private Dictionary<string, bool> scannedQRCodesByPublicKey = new Dictionary<string, bool>();
        public LineRenderer currentLaser;
        private GameObject focusedQRCode;
        private bool isHovering = false;
        private Texture2D qrCodeTexture;
        private Material qrCodeMaterial;

        public delegate void QRScannedHandler(string qrData);
        public event QRScannedHandler OnQRScanned;

        public bool IsQRCodeScanned(string publicKey)
        {
            return scannedQRCodesByPublicKey.ContainsKey(publicKey) && scannedQRCodesByPublicKey[publicKey];
        }

        public void AttachQRCodeToMarker(GameObject marker, string publicKey)
        {
            try
            {
                Debug.Log($"Attaching QR code to marker for public key: {publicKey}");

                // Remove " - Stellar" suffix if present
                publicKey = publicKey.Replace(" - Stellar", "");

                // Generate QR code texture
                Texture2D qrTexture = GenerateQRCode(publicKey);
                if (qrTexture == null)
                {
                    Debug.LogError("Failed to generate QR code texture");
                    return;
                }

                // Find or create QR code plane
                Transform qrPlane = marker.transform.Find("QRCodePlane");
                if (qrPlane == null)
                {
                    GameObject qrPlaneObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    qrPlaneObject.name = "QRCodePlane";
                    qrPlaneObject.transform.SetParent(marker.transform);
                    qrPlane = qrPlaneObject.transform;
                }

                // Set up QR code plane
                qrPlane.localPosition = new Vector3(0, 0.5f, 0);
                qrPlane.localRotation = Quaternion.Euler(90, 0, 0);
                qrPlane.localScale = new Vector3(1, 1, 1);

                // Apply QR code texture
                MeshRenderer meshRenderer = qrPlane.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    Material material = new Material(Shader.Find("Unlit/Texture"));
                    material.mainTexture = qrTexture;
                    meshRenderer.material = material;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error attaching QR code to marker: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        private Texture2D GenerateQRCode(string publicKey)
        {
            try
            {
                Debug.Log($"Generating QR code for public key: {publicKey}");
                return QRCodeWrapper.GenerateQRCode(publicKey);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating QR code: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return null;
            }
        }

        public Texture2D GenerateQRCode(string publicKey, string uriData)
        {
            try
            {
                Debug.Log($"Generating QR code for PublicKey: {publicKey}");
                string combinedData = $"{publicKey}|{uriData}";
                Debug.Log($"Combined QR data: {combinedData}");
                return QRCodeWrapper.GenerateQRCode(combinedData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating QR code: {e.Message}\nStack trace: {e.StackTrace}");
                return null;
            }
        }

        public void ScanQRCode(Camera camera, Vector3 markerPosition)
        {
            if (!isScanning || !isHovering) return;

            GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
            if (qrCodes.Length == 0) return;

            GameObject nearestQR = null;
            float nearestDistance = float.MaxValue;
            foreach (GameObject qr in qrCodes)
            {
                string qrId = qr.GetInstanceID().ToString();
                if (scannedQRCodesByPublicKey.ContainsKey(qrId) && scannedQRCodesByPublicKey[qrId]) continue;
                
                float dist = Vector3.Distance(camera.transform.position, qr.transform.position);
                if (dist < nearestDistance && dist <= scanDistance)
                {
                    nearestDistance = dist;
                    nearestQR = qr;
                }
            }

            if (nearestQR == null || nearestQR != focusedQRCode) return;

            try
            {
                Vector3 direction = (nearestQR.transform.position - camera.transform.position).normalized;
                int layerMask = 1 << 8;
                RaycastHit hit;
                
                if (Physics.Raycast(camera.transform.position, direction, out hit, scanDistance, layerMask))
                {
                    if (hit.collider.gameObject == nearestQR && hit.collider.gameObject.CompareTag("QRCode"))
                    {
                        string qrId = hit.collider.gameObject.GetInstanceID().ToString();
                        if (!scannedQRCodesByPublicKey.ContainsKey(qrId) || !scannedQRCodesByPublicKey[qrId])
                        {
                            Transform markerTransform = hit.collider.gameObject.transform.parent;
                            string markerName = markerTransform != null ? markerTransform.name : "Unknown Marker";
                            
                            Debug.Log($"Scanning QR code for marker: {markerName}");

                            Vector3 qrCodeCenter = hit.collider.gameObject.transform.position;
                            CreateLaserEffect(camera.transform.position, qrCodeCenter, hit.collider.gameObject);

                            string simulatedData = $"{markerName}|web+stellar:pay?destination={markerName}&amount=100";
                            Debug.Log($"Simulated QR scan: {simulatedData}");
                            scannedQRCodesByPublicKey[qrId] = true;
                            OnQRScanned?.Invoke(simulatedData);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scanning QR code: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            // Clean up all materials and textures
            if (qrCodeTexture != null) Destroy(qrCodeTexture);
            if (qrCodeMaterial != null) Destroy(qrCodeMaterial);
        }

        private void HandleQRScanned(string qrData)
        {
            if (OnQRScanned != null)
            {
                OnQRScanned.Invoke(qrData);
            }
        }

        private void CreateLaserEffect(Vector3 start, Vector3 end, GameObject qrCodeObject)
        {
            if (currentLaser != null)
            {
                Destroy(currentLaser.gameObject);
            }

            GameObject laserLine = new GameObject("ScannerLaser");
            currentLaser = laserLine.AddComponent<LineRenderer>();
            
            Material laserMat = new Material(Shader.Find("Particles/Standard Unlit"));
            laserMat.color = new Color(1, 0, 0, 0.8f);
            currentLaser.material = laserMat;
            
            currentLaser.startWidth = 2.0f;
            currentLaser.endWidth = 0.5f;
            currentLaser.positionCount = 2;
            
            currentLaser.useWorldSpace = true;
            currentLaser.SetPosition(0, start);
            currentLaser.SetPosition(1, end);

            currentLaser.sortingOrder = 100;
            currentLaser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            currentLaser.receiveShadows = false;
            currentLaser.material.renderQueue = 4000;
        }
    }
} 