using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using InfinityCode.OnlineMapsExamples;
using CurvedUI;
using TMPro;
using UnityEngine.UI;
using ZXing;
using ZXing.QrCode;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Net.Http;
using System.Net;
using stellar_dotnet_sdk;
using stellar_dotnet_sdk.responses;
using stellar_dotnet_sdk.requests;

#if !UNITY_EDITOR && UNITY_WEBGL
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("stellar-dotnet-sdk")]
#endif

public class GetBlockchainMarkers : MonoBehaviour
{
    [Header("Map References")]
    [SerializeField] private OnlineMaps map; 
    [SerializeField] private OnlineMapsControlBase control;
    [SerializeField] private OnlineMapsMarkerManager markerMGR;
    public GameObject markerPrefab;
    public Texture2D markerTexture;
    public bool use3DMarkers = true;
    private string apiUrl = "http://localhost:3001/api/base_markers";
    [SerializeField] private ApiConfig apiConfig;
    public GameObject stellarMarkerPrefab;
    public GameObject usdcMarkerPrefab;
    [SerializeField] private Material laserMaterial;
    public Color laserColor = new Color(1, 0, 0, 1);
    public float laserWidth = 2f;
    private static Material sharedLaserMaterial;
    private List<LineRenderer> activeLasers = new List<LineRenderer>();
    private const int MAX_LASERS = 120;
    private GameObject laserContainer;

    [Header("UI References")]
    [SerializeField] private GameObject markerKeyPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private CurvedUISettings curvedUISettings;

    [Header("Stellar Settings")]
    [SerializeField] private StellarConfig stellarConfig;

    public struct MarkerData
    {
        public string publicKey;
        public string blockchain;
        public string label;
    }

    public delegate void MarkersLoadedHandler(List<MarkerData> markers);
    [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Event will be used by external components")]
    public event MarkersLoadedHandler OnMarkersLoaded;

    private StellarQRManager qrManager;

    void Start()
    {
        Debug.Log("=== Checking Stellar SDK Setup ===");
        
        // Initialize StellarConfig if not set
        if (stellarConfig == null)
        {
            stellarConfig = Resources.Load<StellarConfig>("StellarConfig");
            if (stellarConfig == null)
            {
                Debug.LogError("Failed to load StellarConfig from Resources. Please ensure StellarConfig asset exists in a Resources folder.");
                return;
            }
        }
        
        try
        {
            var dllPath = System.IO.Path.Combine(Application.dataPath, "Plugins", "StellarSDK", "stellar-dotnet-sdk.dll");
            Debug.Log($"Looking for DLL at: {dllPath}");
            
            if (!System.IO.File.Exists(dllPath))
            {
                Debug.LogError("DLL file not found!");
                return;
            }
            
            var assembly = Assembly.LoadFrom("Assets/Plugins/StellarSDK/stellar-dotnet-sdk.dll");
            Debug.Log($"Assembly loaded: {assembly.FullName}");
            
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Name == "KeyPair" || type.Name == "TransactionBuilder" || type.Name.Contains("Operation"))
                {
                    Debug.Log($"Found type: {type.FullName} in namespace {type.Namespace}");
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            Debug.LogError("ReflectionTypeLoadException: " + ex.Message);
            if (ex.LoaderExceptions != null)
            {
                foreach (var loaderException in ex.LoaderExceptions)
                {
                    if (loaderException != null)
                    {
                        Debug.LogError($"Loader Exception: {loaderException.Message}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error loading assembly: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
        }

        if (map == null) map = OnlineMaps.instance;
        if (control == null) control = OnlineMapsControlBase.instance;
        if (markerMGR == null) markerMGR = OnlineMapsMarkerManager.instance;

        if (map == null || control == null || markerMGR == null)
        {
            Debug.LogError("One or more required components are missing. Please check the inspector.");
            return;
        }

        if (use3DMarkers)
        {
            Debug.Log("Loading marker prefabs...");
            
            // Load USDC marker even if default markerPrefab is assigned
            if (usdcMarkerPrefab == null)
            {
                usdcMarkerPrefab = Resources.Load<GameObject>("usdcMarker2");
                if (usdcMarkerPrefab == null)
                {
                    Debug.LogWarning("USDC marker prefab not found - will use default marker as fallback");
                    usdcMarkerPrefab = markerPrefab;
                }
            }
            
            // Use existing markerPrefab as stellarMarkerPrefab if not already set
            if (stellarMarkerPrefab == null)
            {
                stellarMarkerPrefab = markerPrefab;
            }
        }

        // Find references if not assigned
        if (curvedUISettings == null)
        {
            curvedUISettings = FindAnyObjectByType<CurvedUISettings>();
        }
        
        if (contentParent == null)
        {
            // Find the Content object under ScrollView
            var scrollView = GameObject.Find("ScrollView");
            if (scrollView != null)
            {
                contentParent = scrollView.transform.Find("Viewport/Content");
            }
        }

        // Add UI initialization
        InitializeUI();
        
        Debug.Log("GetBlockchainMarkers Start method called.");
        StartCoroutine(FetchMarkers());

        // Create a single container for all lasers
        laserContainer = new GameObject("LaserContainer");
        laserContainer.transform.position = Vector3.zero;
        laserContainer.transform.rotation = Quaternion.identity;
        
        // Create shared material once
        if (sharedLaserMaterial == null && laserMaterial != null)
        {
            sharedLaserMaterial = new Material(laserMaterial);
        }

        qrManager = gameObject.AddComponent<StellarQRManager>();
        qrManager.OnQRScanned += HandleQRScanned;

        try {
            // Get the assembly
            var assembly = Assembly.LoadFrom("Assets/Plugins/StellarSDK/stellar-dotnet-sdk.dll");
            
            // List all types in the assembly
            Debug.Log("Available types in StellarDotnetSdk:");
            foreach (Type type in assembly.GetTypes())
            {
                Debug.Log($"Type: {type.FullName}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error inspecting SDK: {e.Message}");
        }
    }

    private void InitializeUI()
    {
        Debug.Log("=== Initializing UI ===");
        
        if (contentParent == null)
        {
            Debug.LogError("Content parent is null!");
            return;
        }

        // Verify hierarchy
        Debug.Log($"Content parent hierarchy: {GetGameObjectPath(contentParent.gameObject)}");

        // Configure content layout
        VerticalLayoutGroup verticalLayout = contentParent.GetComponent<VerticalLayoutGroup>();
        if (verticalLayout == null)
        {
            verticalLayout = contentParent.gameObject.AddComponent<VerticalLayoutGroup>();
            verticalLayout.spacing = 2;
            verticalLayout.padding = new RectOffset(10, 10, 10, 10);
            Debug.Log("Added VerticalLayoutGroup to content");
        }

        // Configure content size fitter
        ContentSizeFitter sizeFitter = contentParent.GetComponent<ContentSizeFitter>();
        if (sizeFitter == null)
        {
            sizeFitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            Debug.Log("Added ContentSizeFitter to content");
        }

        // Configure scroll view
        ScrollRect scrollRect = contentParent.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            Debug.Log("Configured ScrollRect");
        }
        else
        {
            Debug.LogError("No ScrollRect found in parent hierarchy!");
        }
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }

    private void UpdateUI(List<MarkerData> markers)
    {
        if (contentParent == null || map == null) return;

        // Clear existing UI items
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Get visible bounds of the map
        double tlx, tly, brx, bry;
        map.GetTileCorners(out tlx, out tly, out brx, out bry);
        Debug.Log($"Map visible bounds: TL({tlx}, {tly}) BR({brx}, {bry})");

        // Get all 3D markers
        var visibleMarkers = new List<MarkerData>();
        var marker3DManager = OnlineMapsMarker3DManager.instance;
        
        if (marker3DManager != null)
        {
            foreach (var marker3D in marker3DManager.items)
            {
                if (marker3D != null && marker3D.enabled)
                {
                    // Get marker coordinates
                    Vector2 markerCoords = marker3D.position;
                    
                    // Convert lat/long to tile coordinates
                    double mx, my;
                    OnlineMaps.instance.projection.CoordinatesToTile(markerCoords.x, markerCoords.y, map.zoom, out mx, out my);
                    Debug.Log($"Marker at lat/long ({markerCoords.x}, {markerCoords.y}) -> tile ({mx}, {my})");

                    // Check if marker is visible on map
                    if (mx >= tlx && mx <= brx && my >= tly && my <= bry)
                    {
                        var markerData = markers.FirstOrDefault(m => marker3D.label.Contains(m.publicKey));
                        if (markerData.publicKey != null)
                        {
                            visibleMarkers.Add(markerData);
                            Debug.Log($"Added visible marker: {markerData.publicKey}");
                        }
                    }
                }
            }
        }

        Debug.Log($"Creating UI for {visibleMarkers.Count} visible markers");

        // Create UI items for visible markers
        foreach (var marker in visibleMarkers)
        {
            GameObject keyItem = Instantiate(markerKeyPrefab, contentParent);
            keyItem.name = $"MarkerItem_{marker.blockchain}";

            TextMeshProUGUI tmpText = keyItem.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                string displayText = $"{marker.blockchain} - {marker.publicKey}";
                tmpText.text = displayText;
                tmpText.fontSize = 24;
                tmpText.color = Color.white;
                tmpText.alignment = TextAlignmentOptions.Left;
                Debug.Log($"Created UI item: {displayText}");
            }
        }

        // Force layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    void Update()
    {
        // Update lasers
        foreach (LineRenderer laser in activeLasers)
        {
            if (laser != null && laser.transform.parent != null)
            {
                Transform markerTransform = laser.transform.parent;
                Vector3 markerPos = markerTransform.position;
                Vector3 mapPosition = control.transform.position;
                
                laser.SetPosition(0, markerPos);
                laser.SetPosition(1, new Vector3(markerPos.x, mapPosition.y, markerPos.z));
            }
        }

        // Check for QR code scanning
        if (Input.GetKeyDown(KeyCode.E)) // Press E to start scanning
        {
            Debug.Log("Started QR code scanning (E pressed)");
            qrManager.isScanning = true;
        }

        // If scanning is active, check nearby markers
        if (qrManager.isScanning && Camera.main != null)
        {
            var marker3DManager = OnlineMapsMarker3DManager.instance;
            if (marker3DManager != null && marker3DManager.items != null)
            {
                foreach (var marker3D in marker3DManager.items)
                {
                    if (marker3D != null && marker3D.instance != null)
                    {
                        qrManager.ScanQRCode(Camera.main, marker3D.instance.transform.position);
                    }
                }
            }
        }

        // Check if map has moved or needs update
        if (map != null)
        {
            // Get visible bounds of the map
            double tlx, tly, brx, bry;
            map.GetTileCorners(out tlx, out tly, out brx, out bry);

            var marker3DManager = OnlineMapsMarker3DManager.instance;
            if (marker3DManager != null && marker3DManager.items != null)
            {
                var visibleMarkers = new List<MarkerData>();
                
                foreach (var marker3D in marker3DManager.items)
                {
                    if (marker3D != null && marker3D.enabled && !string.IsNullOrEmpty(marker3D.label))
                    {
                        // Convert marker position to tile coordinates
                        double mx, my;
                        OnlineMaps.instance.projection.CoordinatesToTile(marker3D.position.x, marker3D.position.y, map.zoom, out mx, out my);

                        // Only add if marker is within visible bounds
                        if (mx >= tlx && mx <= brx && my >= tly && my <= bry)
                        {
                            string[] parts = marker3D.label.Split('-');
                            if (parts.Length >= 2)
                            {
                                visibleMarkers.Add(new MarkerData
                                {
                                    publicKey = parts[0].Trim(),
                                    blockchain = parts[1].Trim(),
                                    label = marker3D.label
                                });
                            }
                        }

                        // Check proximity to player and rotate if needed
                        if (marker3D.instance != null)
                        {
                            Camera playerCamera = Camera.main;
                            if (playerCamera != null)
                            {
                                float distance = Vector3.Distance(playerCamera.transform.position, marker3D.instance.transform.position);
                                
                                // If player is within 5 units of the marker
                                if (distance < 5f)
                                {
                                    // Calculate direction to player
                                    Vector3 directionToPlayer = (playerCamera.transform.position - marker3D.instance.transform.position).normalized;
                                    
                                    // Rotate marker to face player
                                    marker3D.instance.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                                }
                                else
                                {
                                    // Reset rotation when player moves away
                                    marker3D.instance.transform.rotation = Quaternion.Euler(-90, 180, 0);
                                }
                            }
                        }
                    }
                }

                // Update UI with only visible markers
                UpdateUI(visibleMarkers);
            }
        }

        // Check if we need to clean up the laser effect
        if (qrManager.currentLaser != null)
        {
            bool shouldDestroyLaser = true;
            if (qrManager.isScanning && Camera.main != null)
            {
                // Find nearest QR code
                GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
                foreach (var qrCode in qrCodes)
                {
                    float distance = Vector3.Distance(Camera.main.transform.position, qrCode.transform.position);
                    if (distance <= qrManager.scanDistance)
                    {
                        shouldDestroyLaser = false;
                        break;
                    }
                }
            }

            if (shouldDestroyLaser)
            {
                Destroy(qrManager.currentLaser.gameObject);
                qrManager.currentLaser = null;
            }
        }
    }

    IEnumerator FetchMarkers()
    {
        Debug.Log($"Fetching markers from: {apiUrl}");
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        request.SetRequestHeader("Authorization", "Bearer " + apiConfig.BearerToken);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"Received JSON: {jsonResponse}");
            
            JArray markers = JArray.Parse(jsonResponse);
            List<MarkerData> markerDataList = new List<MarkerData>();

            foreach (JObject marker in markers)
            {
                // Parse marker data
                string publicKey = marker["publicKey"].ToString();
                string blockchain = marker["blockchain"].ToString();
                double latitude = marker["latitude"].ToObject<double>();
                double longitude = marker["longitude"].ToObject<double>();
                string label = $"{publicKey} - {blockchain}";

                Debug.Log($"Processing marker - Lat: {latitude}, Lon: {longitude}, Label: {label}");

                // Create marker data for UI
                MarkerData markerData = new MarkerData
                {
                    publicKey = publicKey,
                    blockchain = blockchain,
                    label = label
                };
                markerDataList.Add(markerData);

                // Create map marker
                if (use3DMarkers)
                {
                    Add3DMarker(latitude, longitude, label, blockchain);
                }
                else
                {
                    Add2DMarker(latitude, longitude, label);
                }
            }

            // Update UI
            UpdateUI(markerDataList);
            
            // Notify subscribers about loaded markers
            OnMarkersLoaded?.Invoke(markerDataList);
            
            // Redraw map
            if (map != null)
            {
                map.Redraw();
                Debug.Log("Map redrawn with markers");
            }
        }
        else
        {
            Debug.LogError($"Error fetching markers: {request.error}");
        }
    }

    void Add2DMarker(double latitude, double longitude, string label)
    {
        if (markerMGR == null)
        {
            Debug.LogError("MarkerManager is null. Cannot add 2D marker.");
            return;
        }

        try
        {
            OnlineMapsMarker marker = markerMGR.Create(longitude, latitude, markerTexture);
            marker.label = label;
            marker.enabled = true;
            
            // Add click handler to the map
            control.OnMapClick += () =>
            {
                double lng, lat;
                control.GetCoords(out lng, out lat);
                double distance = CalculateDistance(lng, lat, marker.position.x, marker.position.y);
                if (distance < 0.1) // Adjust this value to change click sensitivity (in km)
                {
                    marker.enabled = !marker.enabled;
                }
            };
            marker.scale = 10.0f; // Adjust this value as needed
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error adding 2D marker: {e.Message}");
        }
    }

    void Add3DMarker(double latitude, double longitude, string label, string blockchain)
    {
        Debug.Log($"Adding 3D marker for blockchain: {blockchain}, USDC prefab exists: {usdcMarkerPrefab != null}, Stellar prefab exists: {stellarMarkerPrefab != null}");
        
        GameObject prefabToUse;
        switch (blockchain.ToLower())
        {
            case "stellar":
                prefabToUse = stellarMarkerPrefab;
                break;
            case "circle":
            case "usdc":
                prefabToUse = usdcMarkerPrefab ?? stellarMarkerPrefab;  // Fallback to stellar if null
                break;
            default:
                prefabToUse = stellarMarkerPrefab;
                break;
        }

        if (prefabToUse == null)
        {
            Debug.LogError($"Missing marker prefab for blockchain type: {blockchain}. StellarPrefab: {stellarMarkerPrefab != null}, USDCPrefab: {usdcMarkerPrefab != null}");
            return;
        }

        if (activeLasers.Count >= MAX_LASERS)
        {
            Debug.LogWarning($"Maximum laser limit reached ({MAX_LASERS}). Skipping laser for: {label}");
            return;
        }

        try
        {
            OnlineMapsMarker3D marker3D = OnlineMapsMarker3DManager.CreateItem(longitude, latitude, prefabToUse);
            marker3D.label = label;
            marker3D.enabled = true;
            
            // Adjust settings based on blockchain type
            if (blockchain.ToLower() == "circle" || blockchain.ToLower() == "usdc")
            {
                marker3D.scale = 100f;  // Made much smaller for USDC/Circle markers
                marker3D.rotation = Quaternion.Euler(0, 180, 0);//Quaternion.identity;  // Reset rotation
                marker3D.altitude = 120;  // Slightly higher altitude
                Debug.Log($"USDC Marker created - Position: {marker3D.transform.position}, Scale: {marker3D.scale}, Rotation: {marker3D.rotation}");
            }
            else
            {
                marker3D.scale = 10f;  // Original scale for other markers
                marker3D.rotation = Quaternion.Euler(-90, 180, 0);
                marker3D.altitude = 72;
            }

            // Create laser
            GameObject laserObject = new GameObject($"Laser_{label}");
            laserObject.transform.SetParent(marker3D.transform);
            laserObject.transform.localPosition = Vector3.zero;
            
            LineRenderer laser = laserObject.AddComponent<LineRenderer>();
            laser.material = sharedLaserMaterial != null ? sharedLaserMaterial : laserMaterial;
            laser.startColor = laser.endColor = Color.red;
            laser.startWidth = 5f;
            laser.endWidth = 5f;
            laser.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            laser.receiveShadows = false;
            laser.positionCount = 2;
            laser.useWorldSpace = true;

            laser.SetPosition(0, marker3D.transform.position);
            laser.SetPosition(1, new Vector3(marker3D.transform.position.x, control.transform.position.y, marker3D.transform.position.z));

            Debug.Log($"Laser created at position: {marker3D.transform.position}, Material: {laser.material.name}, Shader: {laser.material.shader.name}");
            
            activeLasers.Add(laser);

            if (blockchain.ToLower() == "stellar")
            {
                // Generate Stellar SEP-0007 URI
                string stellarUri = GenerateStellarUri(label);
                qrManager.AttachQRCodeToMarker(marker3D.instance, label, stellarUri);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error adding 3D marker for {blockchain}: {e.Message}");
        }
    }

    private void ParseJson(string jsonString)
    {
        JArray jsonArray = JArray.Parse(jsonString);
    }

    private double CalculateDistance(double lon1, double lat1, double lon2, double lat2)
    {
        const double R = 6371; // Earth's radius in km
        double dLat = ToRad(lat2 - lat1);
        double dLon = ToRad(lon2 - lon1);
        double a = Math.Sin(dLat/2) * Math.Sin(dLat/2) +
                   Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                   Math.Sin(dLon/2) * Math.Sin(dLon/2);
        return 2 * R * Math.Asin(Math.Sqrt(a));
    }

    private double ToRad(double deg) => deg * Math.PI / 180;

    void OnDestroy()
    {
        if (sharedLaserMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(sharedLaserMaterial);
            else
                DestroyImmediate(sharedLaserMaterial);
        }

        if (laserContainer != null)
        {
            if (Application.isPlaying)
                Destroy(laserContainer);
            else
                DestroyImmediate(laserContainer);
        }
    }

    private void HandleQRScanned(string qrData)
    {
        string[] parts = qrData.Split('|');
        if (parts.Length == 2)
        {
            string publicKey = parts[0];
            string uriData = parts[1];
            
            // Parse the URI parameters
            var queryParams = new Dictionary<string, string>();
            var queryStart = uriData.IndexOf('?');
            
            if (queryStart >= 0 && queryStart < uriData.Length - 1)
            {
                var queryString = uriData.Substring(queryStart + 1);
                foreach (var pair in queryString.Split('&'))
                {
                    var parts2 = pair.Split(new[] { '=' }, 2);
                    if (parts2.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(parts2[0]);
                        var value = Uri.UnescapeDataString(parts2[1]);
                        queryParams[key] = value;
                    }
                }
            }

            // Display payment information
            Debug.Log("=== Stellar Payment Information ===");
            Debug.Log($"Destination: {queryParams.GetValueOrDefault("destination")}");
            Debug.Log($"Amount: {queryParams.GetValueOrDefault("amount")} {queryParams.GetValueOrDefault("asset_code", "XLM")}");
            Debug.Log($"Network: {queryParams.GetValueOrDefault("network", "Test SDF Network")}");
            Debug.Log($"Message: {queryParams.GetValueOrDefault("msg", "Payment via map marker")}");
            Debug.Log("================================");
        }
    }

    private IEnumerator HandleStellarUriCoroutine(string uriData)
    {
        // Parse and display URI data
        var queryStart = uriData.IndexOf('?');
        if (queryStart >= 0)
        {
            var queryString = uriData.Substring(queryStart + 1);
            var parameters = queryString.Split('&')
                .Select(p => p.Split('='))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => Uri.UnescapeDataString(parts[0]),
                    parts => Uri.UnescapeDataString(parts[1])
                );

            Debug.Log("=== Stellar Payment Details ===");
            foreach (var param in parameters)
            {
                Debug.Log($"{param.Key}: {param.Value}");
            }
            Debug.Log("============================");
        }
        yield return null;
    }

    /// <summary>
    /// Result of a Stellar transaction processing operation
    /// </summary>
    public class StellarTransactionResult
    {
        public bool Success { get; set; }
        public string TransactionHash { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> OperationErrors { get; set; }
    }

    private const string STELLAR_URI_PREFIX = "web+stellar:";
    private const string DEFAULT_MEMO = "Payment via map marker";
    private const int DEFAULT_TIMEOUT_MINUTES = 5;
    private const int DEFAULT_BASE_FEE = 100;
    private const int MAX_RETRY_ATTEMPTS = 3;

    private async Task<StellarTransactionResult> ProcessStellarUri(string uri)
    {
        try
        {
            // Input validation
            if (string.IsNullOrEmpty(uri))
            {
                throw new ArgumentException("URI cannot be null or empty");
            }

            // URI format validation
            if (!uri.StartsWith(STELLAR_URI_PREFIX, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"URI must start with {STELLAR_URI_PREFIX}");
            }

            // Extract the payload after the prefix
            var uriData = uri.Substring(STELLAR_URI_PREFIX.Length);
            if (string.IsNullOrEmpty(uriData))
            {
                throw new ArgumentException("URI payload is empty");
            }

            // Parse the URI parameters
            var queryParams = new Dictionary<string, string>();
            var queryStart = uriData.IndexOf('?');
            
            if (queryStart >= 0 && queryStart < uriData.Length - 1)
            {
                var queryString = uriData.Substring(queryStart + 1);
                foreach (var pair in queryString.Split('&'))
                {
                    var parts = pair.Split(new[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        var key = Uri.UnescapeDataString(parts[0]);
                        var value = Uri.UnescapeDataString(parts[1]);
                        queryParams[key] = value;
                    }
                }
            }
            else
            {
                throw new ArgumentException("URI must contain query parameters");
            }

            // Configuration validation
            if (string.IsNullOrEmpty(stellarConfig.SecretKey) || string.IsNullOrEmpty(stellarConfig.HorizonUrl))
            {
                throw new InvalidOperationException("Stellar configuration is incomplete");
            }

            if (!Uri.TryCreate(stellarConfig.HorizonUrl, UriKind.Absolute, out _))
            {
                throw new ArgumentException("Invalid Horizon URL");
            }

            // Parameter validation
            if (string.IsNullOrEmpty(queryParams.GetValueOrDefault("destination")) || string.IsNullOrEmpty(queryParams.GetValueOrDefault("amount")))
            {
                throw new ArgumentException("Missing required parameters in URI");
            }

            // Amount validation
            var amountStr = queryParams.GetValueOrDefault("amount");
            if (!decimal.TryParse(amountStr, out decimal parsedAmount))
            {
                throw new ArgumentException("Invalid amount format");
            }
            if (parsedAmount <= 0)
            {
                throw new ArgumentException("Amount must be greater than 0");
            }
            if (parsedAmount > stellarConfig.MaximumTransactionAmount)
            {
                throw new ArgumentException($"Amount exceeds maximum allowed ({stellarConfig.MaximumTransactionAmount} XLM)");
            }

            // Validate amount precision (7 decimal places for Stellar)
            var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(parsedAmount)[3])[2];
            if (decimalPlaces > 7)
            {
                throw new ArgumentException("Amount cannot have more than 7 decimal places");
            }

            Debug.Log($"Amount validated: {parsedAmount} {queryParams.GetValueOrDefault("asset_code", "XLM")}");

            // Asset handling
            var assetCode = queryParams.GetValueOrDefault("asset_code", "XLM").ToUpperInvariant();
            var issuer = queryParams.GetValueOrDefault("asset_issuer");
            
            Asset asset;
            try
            {
                if (assetCode == "XLM")
                {
                    asset = new AssetTypeNative();
                }
                else if (string.IsNullOrEmpty(issuer))
                {
                    throw new ArgumentException($"Issuer required for asset {assetCode}");
                }
                else if (assetCode.Length <= 4)
                {
                    asset = new AssetTypeCreditAlphaNum4(assetCode, issuer);
                }
                else
                {
                    asset = new AssetTypeCreditAlphaNum12(assetCode, issuer);
                }

                Debug.Log($"Asset configured: {assetCode}" + (assetCode == "XLM" ? "" : $" (Issuer: {issuer})"));
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Invalid asset configuration: {ex.Message}");
            }
            
            Debug.Log($"Processing payment to: {queryParams.GetValueOrDefault("destination")}");
            Debug.Log($"Amount: {parsedAmount} {queryParams.GetValueOrDefault("asset_code", "XLM")}");
            
            // Set up the network based on configuration
            if (stellarConfig.UseTestNetwork)
            {
                Network.UseTestNetwork();
                Debug.Log("Using Stellar Test Network");
            }
            else
            {
                Network.Use(new Network(stellarConfig.NetworkPassphrase));
                Debug.Log("Using Stellar Public Network");
            }

            var server = new Server(stellarConfig.HorizonUrl);
            Debug.Log($"Connected to Horizon at: {stellarConfig.HorizonUrl}");
            
            // Get the source account
            var sourceKeypair = KeyPair.FromSecretSeed(stellarConfig.SecretKey);
            var sourceAccount = await server.Accounts.Account(sourceKeypair.AccountId);
            Debug.Log($"Source account loaded: {sourceKeypair.AccountId}");

            // Validate destination account exists
            try
            {
                await server.Accounts.Account(queryParams.GetValueOrDefault("destination"));
                Debug.Log($"Destination account verified: {queryParams.GetValueOrDefault("destination")}");
            }
            catch (Exception)
            {
                throw new ArgumentException("Destination account does not exist");
            }
            
            // Create the transaction
            var now = DateTimeOffset.UtcNow;
            var transaction = new TransactionBuilder(sourceAccount)
                .AddOperation(
                    new PaymentOperation.Builder(
                        KeyPair.FromAccountId(queryParams.GetValueOrDefault("destination")),
                        asset,
                        parsedAmount.ToString()
                    ).Build()
                )
                .SetFee(DEFAULT_BASE_FEE)
                .AddMemo(Memo.Text(DEFAULT_MEMO))
                .AddTimeBounds(new TimeBounds(
                    now.ToUnixTimeSeconds(),
                    now.AddMinutes(DEFAULT_TIMEOUT_MINUTES).ToUnixTimeSeconds()
                ))
                .Build();

            Debug.Log($"Transaction built with hash: {transaction.Hash().ToString()}");
            
            // Sign and submit with retry logic
            transaction.Sign(sourceKeypair);
            
            SubmitTransactionResponse response = null;
            for (int attempt = 0; attempt < MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                    {
                        response = await server.SubmitTransaction(transaction);
                        break;
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    if (attempt == MAX_RETRY_ATTEMPTS - 1)
                        throw;
                    
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    Debug.LogWarning($"Retry attempt {attempt + 1} after {delay.TotalSeconds}s: {ex.Message}");
                    await Task.Delay(delay);
                }
            }
            
            var result = new StellarTransactionResult();
            
            if (response.IsSuccess())
            {
                result.Success = true;
                result.TransactionHash = response.Hash;
                Debug.Log($"Transaction successful!");
                Debug.Log($"Hash: {result.TransactionHash}");
                Debug.Log($"Ledger: {response.Ledger}");
            }
            else
            {
                result.Success = false;
                result.ErrorMessage = response.ResultXdr;
                Debug.LogError($"Transaction failed: {result.ErrorMessage}");
            }
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            Debug.LogError($"Network error: {ex.Message}");
            return new StellarTransactionResult { Success = false, ErrorMessage = $"Network error: {ex.Message}" };
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"Configuration error: {ex.Message}");
            return new StellarTransactionResult { Success = false, ErrorMessage = $"Configuration error: {ex.Message}" };
        }
        catch (ArgumentException ex)
        {
            Debug.LogError($"Invalid argument: {ex.Message}");
            return new StellarTransactionResult { Success = false, ErrorMessage = $"Invalid argument: {ex.Message}" };
        }
        catch (Exception e)
        {
            Debug.LogError($"Error processing Stellar URI: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return new StellarTransactionResult 
            { 
                Success = false, 
                ErrorMessage = $"Unexpected error: {e.Message}" 
            };
        }
    }

    private string GenerateStellarUri(string publicKey)
    {
        try
        {
            if (stellarConfig == null)
            {
                Debug.LogError("StellarConfig is not initialized!");
                return string.Empty;
            }

            // Build a basic SEP-0007 URI without using KeyPair
            var parameters = new Dictionary<string, string>
            {
                { "destination", publicKey },
                { "amount", "100" },
                { "asset_code", "XLM" },
                { "network", stellarConfig.NetworkPassphrase ?? "Test SDF Network ; September 2015" },
                { "msg", "Payment via map marker" },
                { "origin_domain", "map.example.com" }
            };

            string queryString = string.Join("&", parameters.Select(p => 
                $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value)}"));
                
            string uri = $"web+stellar:pay?{queryString}";
            Debug.Log($"Generated Stellar URI: {uri}");
            return uri;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error generating Stellar URI: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return string.Empty;
        }
    }

}

public class StellarQRManager : MonoBehaviour
{
    private Texture2D qrCodeTexture;
    private Material qrCodeMaterial;
    public bool isScanning = false;
    public float scanDistance = 2f;
    private Material laserMaterial;
    public LineRenderer currentLaser;
    public Camera scanningCamera;
    
    public delegate void QRScannedHandler(string qrData);
    public event QRScannedHandler OnQRScanned;
    
    void Start()
    {
        // Create laser material
        laserMaterial = new Material(Shader.Find("Sprites/Default"));
        laserMaterial.color = Color.red;
    }

    private void CreateLaserEffect(Vector3 start, Vector3 end, GameObject qrCodeObject)
    {
        // Clean up previous laser if it exists
        if (currentLaser != null)
        {
            Destroy(currentLaser.gameObject);
        }

        // Get QR code size
        var qrTransform = qrCodeObject.transform;
        float qrSize = qrTransform.localScale.x; // Since it's a square, we can use x
        float laserSize = qrSize * 1.2f; // Make laser slightly bigger than QR code

        GameObject laserLine = new GameObject("ScannerLaser");
        currentLaser = laserLine.AddComponent<LineRenderer>();
        
        currentLaser.material = laserMaterial;
        currentLaser.startColor = new Color(1, 0, 0, 0.8f);
        currentLaser.endColor = new Color(1, 0, 0, 0.4f);
        currentLaser.startWidth = laserSize;
        currentLaser.endWidth = laserSize;
        currentLaser.SetPosition(0, start);
        currentLaser.SetPosition(1, end);
        currentLaser.useWorldSpace = true;
        
        // Add glow effect
        currentLaser.material.EnableKeyword("_EMISSION");
        currentLaser.material.SetColor("_EmissionColor", new Color(1, 0, 0, 1) * 2f);
    }

    void OnDestroy()
    {
        if (laserMaterial != null)
            Destroy(laserMaterial);
        if (currentLaser != null)
            Destroy(currentLaser.gameObject);
    }

    public Texture2D GenerateQRCode(string publicKey, string uriData)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions
            {
                Height = 256,
                Width = 256,
                Margin = 1
            }
        };

        // Combine the URI data with the public key
        string combinedData = $"{publicKey}|{uriData}";
        
        // Generate QR code
        var color32 = writer.Write(combinedData);
        qrCodeTexture = new Texture2D(256, 256);
        qrCodeTexture.SetPixels32(color32);
        qrCodeTexture.Apply();

        return qrCodeTexture;
    }

    public void AttachQRCodeToMarker(GameObject marker, string publicKey, string uriData)
    {
        // Create QR code plane behind the marker
        GameObject qrPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        qrPlane.transform.SetParent(marker.transform);
        qrPlane.transform.localPosition = new Vector3(0, 0, -0.1f); // Slightly behind marker
        qrPlane.transform.localRotation = Quaternion.Euler(0, 180, 0);
        qrPlane.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        qrPlane.tag = "QRCode";

        // Generate and apply QR code texture
        Texture2D qrTexture = GenerateQRCode(publicKey, uriData);
        Material qrMaterial = new Material(Shader.Find("Standard"));
        qrMaterial.mainTexture = qrTexture;
        qrPlane.GetComponent<MeshRenderer>().material = qrMaterial;
    }

    public void ScanQRCode(Camera camera, Vector3 markerPosition)
    {
        if (!isScanning) return;

        RaycastHit hit;
        Vector3 direction = (markerPosition - camera.transform.position).normalized;
        
        Debug.Log($"Scanning from camera at {camera.transform.position} towards marker at {markerPosition}, distance: {Vector3.Distance(camera.transform.position, markerPosition)}");

        if (Physics.Raycast(camera.transform.position, direction, out hit, scanDistance))
        {
            Debug.Log($"Raycast hit object: {hit.collider.gameObject.name}, Tag: {hit.collider.gameObject.tag}, Distance: {hit.distance}");
            
            if (hit.collider.gameObject.CompareTag("QRCode"))
            {
                Debug.Log("Found QR code object, attempting to scan...");
                // Simulate laser scanning effect with QR code object reference
                CreateLaserEffect(camera.transform.position, hit.point, hit.collider.gameObject);
                
                // Get the QR code texture
                var meshRenderer = hit.collider.GetComponent<MeshRenderer>();
                if (meshRenderer == null || meshRenderer.material == null || meshRenderer.material.mainTexture == null)
                {
                    Debug.LogError("QR code object is missing required components");
                    return;
                }
                
                Texture2D qrTexture = meshRenderer.material.mainTexture as Texture2D;
                Debug.Log($"Got QR texture: {qrTexture != null}, Size: {(qrTexture != null ? $"{qrTexture.width}x{qrTexture.height}" : "null")}");
                
                // Decode QR code
                var reader = new BarcodeReader();
                var result = reader.Decode(qrTexture.GetPixels32(), qrTexture.width, qrTexture.height);
                
                if (result != null)
                {
                    Debug.Log($"Successfully decoded QR code: {result.Text}");
                    string[] parts = result.Text.Split('|');
                    if (parts.Length >= 1)
                    {
                        Debug.Log($"Successfully scanned QR code for PublicKey: {parts[0]}");
                    }
                    OnQRScanned?.Invoke(result.Text);
                    isScanning = false;
                }
                else
                {
                    Debug.LogError("Failed to decode QR code");
                }
            }
        }
        else
        {
            Debug.Log("No object hit by raycast");
        }
    }
}

