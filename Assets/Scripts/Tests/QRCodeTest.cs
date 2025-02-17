using UnityEngine;
using UnityEngine.UI;
using System;
using BlockchainMaps;

public class QRCodeTest : MonoBehaviour
{
    public RawImage qrDisplay;
    public Text statusText;
    public Camera mainCamera;
    public Button scanButton;
    private StellarQRManager qrManager;
    private bool isScanning = false;
    private float cameraSpeed = 5f;

    void Start()
    {
        qrManager = FindAnyObjectByType<StellarQRManager>(FindObjectsInactive.Include);
        if (qrManager == null)
        {
            GameObject qrManagerObj = new GameObject("StellarQRManager");
            qrManager = qrManagerObj.AddComponent<StellarQRManager>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (scanButton != null)
        {
            scanButton.onClick.AddListener(ToggleScanning);
        }

        qrManager.OnQRScanned += HandleQRScanned;
        TestQRCodeGeneration();
    }

    void Update()
    {
        // Simple camera controls for testing
        if (Input.GetKey(KeyCode.W)) mainCamera.transform.position += mainCamera.transform.forward * Time.deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.S)) mainCamera.transform.position -= mainCamera.transform.forward * Time.deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.A)) mainCamera.transform.position -= mainCamera.transform.right * Time.deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.D)) mainCamera.transform.position += mainCamera.transform.right * Time.deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.Q)) mainCamera.transform.position += Vector3.up * Time.deltaTime * cameraSpeed;
        if (Input.GetKey(KeyCode.E)) mainCamera.transform.position -= Vector3.up * Time.deltaTime * cameraSpeed;

        // Mouse look
        if (Input.GetMouseButton(1)) // Right mouse button
        {
            float mouseX = Input.GetAxis("Mouse X") * 2f;
            float mouseY = Input.GetAxis("Mouse Y") * 2f;
            mainCamera.transform.Rotate(Vector3.up * mouseX, Space.World);
            mainCamera.transform.Rotate(Vector3.left * mouseY, Space.Self);
        }

        // Scanning update
        if (isScanning && qrManager != null)
        {
            qrManager.ScanQRCode(mainCamera, mainCamera.transform.position);
        }

        // Toggle scanning with Space key
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleScanning();
        }
    }

    void TestQRCodeGeneration()
    {
        try
        {
            string testData = "Test QR Code Data|web+stellar:pay?destination=test&amount=100";
            Debug.Log("Generating test QR code...");
            
            // Test QR code generation
            Texture2D qrTexture = qrManager.GenerateQRCode("TestPublicKey", testData);
            
            if (qrTexture != null)
            {
                Debug.Log("QR code generated successfully!");
                if (qrDisplay != null)
                {
                    qrDisplay.texture = qrTexture;
                    UpdateStatus("QR Code generated successfully! Running in " + 
                        #if UNITY_WEBGL
                        "WebGL (using standard ZXing)"
                        #else
                        "Standalone/Editor (using ZXing.Unity)"
                        #endif
                    );
                }

                // Create test markers with QR codes
                CreateTestMarker(new Vector3(0, 1, 5), "TestPublicKey1");
                CreateTestMarker(new Vector3(5, 1, 5), "TestPublicKey2");
                CreateTestMarker(new Vector3(-5, 1, 5), "TestPublicKey3");
            }
            else
            {
                UpdateStatus("Failed to generate QR code!");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in QR code test: {e.Message}\nStack trace: {e.StackTrace}");
            UpdateStatus($"Error: {e.Message}");
        }
    }

    void CreateTestMarker(Vector3 position, string publicKey)
    {
        GameObject testMarker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        testMarker.name = $"TestMarker_{publicKey}";
        testMarker.transform.position = position;
        qrManager.AttachQRCodeToMarker(testMarker, publicKey);
    }

    void ToggleScanning()
    {
        isScanning = !isScanning;
        qrManager.isScanning = isScanning;
        UpdateStatus(isScanning ? "Scanning enabled - Move close to QR codes" : "Scanning disabled");
    }

    void HandleQRScanned(string qrData)
    {
        UpdateStatus($"QR Code scanned: {qrData}");
    }

    void UpdateStatus(string message)
    {
        Debug.Log(message);
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    void OnDestroy()
    {
        if (qrManager != null)
        {
            qrManager.OnQRScanned -= HandleQRScanned;
        }
    }
} 