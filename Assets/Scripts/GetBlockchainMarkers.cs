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
using StellarDotnetSdk;
using StellarDotnetSdk.Responses;
using StellarDotnetSdk.Requests;
using BlockchainMaps.Authentication;

#if !UNITY_EDITOR && UNITY_WEBGL
using System.Runtime.InteropServices;
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("stellar-dotnet-sdk")]
#endif

namespace BlockchainMaps.Authentication
{
    [DefaultExecutionOrder(-100)] // Ensure this script initializes early
    public class GetBlockchainMarkers : MonoBehaviour
    {
        private static bool isInitialized = false;
        private PasskeyUIManager passkeyUIManager;
        
        void Awake()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                Debug.Log("Initializing GetBlockchainMarkers...");
                
                #if !UNITY_EDITOR && UNITY_WEBGL
                // Ensure all required components are available before proceeding
                StartCoroutine(WaitForComponents());
                #else
                InitializeComponents();
                #endif
            }
        }

        private IEnumerator WaitForComponents()
        {
            Debug.Log("Waiting for components to initialize...");
            
            // Wait for a frame to ensure all components are properly initialized
            yield return null;
            
            // Wait until required components are available
            while (map == null || control == null || markerMGR == null)
            {
                if (map == null) map = FindAnyObjectByType<OnlineMaps>(FindObjectsInactive.Include);
                if (control == null) control = FindAnyObjectByType<OnlineMapsControlBase>(FindObjectsInactive.Include);
                if (markerMGR == null) markerMGR = FindAnyObjectByType<OnlineMapsMarkerManager>(FindObjectsInactive.Include);
                
                yield return new WaitForSeconds(0.1f);
            }
            
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            try
            {
                Debug.Log("=== Starting GetBlockchainMarkers Initialization ===");
                
                // Find PasskeyUIManager
                passkeyUIManager = FindAnyObjectByType<PasskeyUIManager>(FindObjectsInactive.Include);
                if (passkeyUIManager == null)
                {
                    Debug.LogWarning("PasskeyUIManager not found in scene. Authentication UI will not be available.");
                }
                
                // Initialize map components
                InitializeMapComponents();

                // Initialize UI components
                InitializeUI();
                
                // Initialize laser components
                InitializeLaserComponents();

                // Initialize QR manager
                InitializeQRManager();

                // Load Stellar SDK
                LoadStellarSDK();
                
                // Start fetching markers
                StartCoroutine(FetchMarkers());
                
                Debug.Log("GetBlockchainMarkers initialization completed successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during initialization: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        void Start()
        {
            mainCamera = Camera.main;
            // Start is now just a backup in case Awake didn't run
            if (!isInitialized)
            {
                Debug.LogWarning("Start called before initialization completed. Running initialization...");
                #if !UNITY_EDITOR && UNITY_WEBGL
                StartCoroutine(WaitForComponents());
                #else
                InitializeComponents();
                #endif
            }
        }

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
        private List<MarkerData> visibleMarkers = new List<MarkerData>(); // Track visible markers

        [Header("UI References")]
        [SerializeField] private GameObject markerKeyPrefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private CurvedUISettings curvedUISettings;

        [Header("Stellar Settings")]
        [SerializeField] private StellarConfig stellarConfig;

        #if !UNITY_EDITOR && UNITY_WEBGL
        [DllImport("__Internal")]
        private static extern void InitializeWebGL();
        #endif

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

        private float lastScanToggleTime = 0f;
        private const float SCAN_TOGGLE_COOLDOWN = 0.5f; // Half second cooldown

        private List<MarkerData> currentVisibleMarkers = new List<MarkerData>();
        private List<MarkerData> tempMarkerList = new List<MarkerData>();
        private Camera mainCamera;
        private readonly Vector3 defaultRotation = new Vector3(-90, 180, 0);

        private void EnsureRequiredComponents()
        {
            // Add OnlineMaps if missing
            if (map == null)
            {
                map = GetComponent<OnlineMaps>();
                if (map == null)
                {
                    Debug.Log("Adding OnlineMaps component");
                    map = gameObject.AddComponent<OnlineMaps>();
                }
            }

            // Add OnlineMapsControlBase if missing
            if (control == null)
            {
                control = GetComponent<OnlineMapsControlBase>();
                if (control == null)
                {
                    Debug.Log("Adding OnlineMapsControlBase component");
                    control = gameObject.AddComponent<OnlineMapsControlBase>();
                }
            }

            // Add OnlineMapsMarkerManager if missing
            if (markerMGR == null)
            {
                markerMGR = GetComponent<OnlineMapsMarkerManager>();
                if (markerMGR == null)
                {
                    Debug.Log("Adding OnlineMapsMarkerManager component");
                    markerMGR = gameObject.AddComponent<OnlineMapsMarkerManager>();
                }
            }

            // Add OnlineMapsMarker3DManager if using 3D markers
            if (use3DMarkers && GetComponent<OnlineMapsMarker3DManager>() == null)
            {
                Debug.Log("Adding OnlineMapsMarker3DManager component");
                gameObject.AddComponent<OnlineMapsMarker3DManager>();
            }

            // Verify all components are present
            if (map == null || control == null || markerMGR == null)
            {
                Debug.LogError("Failed to initialize required components. Please check the inspector.");
                enabled = false;
                return;
            }
        }

        private void InitializeMapComponents()
        {
            // Set up map zoom settings
            map.zoomRange = new OnlineMapsRange(2, 20);
            map.zoom = 15;
            map.notInteractUnderGUI = true;
            map.width = 24576;
            map.height = 24576;
            map.redrawOnPlay = true;

            // Add zoom change listener
            map.OnChangeZoom += OnMapZoomChanged;

            if (use3DMarkers)
            {
                Debug.Log("Loading marker prefabs...");
                
                // Load USDC marker
                if (usdcMarkerPrefab == null)
                {
                    usdcMarkerPrefab = Resources.Load<GameObject>("usdcMarker2");
                    if (usdcMarkerPrefab == null)
                    {
                        Debug.LogWarning("USDC marker prefab not found - will use default marker as fallback");
                        usdcMarkerPrefab = markerPrefab;
                    }
                }
                
                // Set stellar marker
                if (stellarMarkerPrefab == null)
                {
                    stellarMarkerPrefab = markerPrefab;
                }
            }
        }

        private void InitializeLaserComponents()
        {
            // Create laser container
            if (laserContainer == null)
            {
                laserContainer = new GameObject("LaserContainer");
                laserContainer.transform.position = Vector3.zero;
                laserContainer.transform.rotation = Quaternion.identity;
            }
            
            // Create shared material
            if (sharedLaserMaterial == null && laserMaterial != null)
            {
                sharedLaserMaterial = new Material(laserMaterial);
            }
        }

        private void InitializeQRManager()
        {
            if (qrManager == null)
            {
                qrManager = gameObject.AddComponent<StellarQRManager>();
                if (qrManager != null)
                {
                    qrManager.OnQRScanned += HandleQRScanned;
                }
                else
                {
                    Debug.LogError("Failed to add StellarQRManager component");
                }
            }
        }

        private void LoadStellarSDK()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            // Skip SDK loading in WebGL builds
            Debug.Log("Skipping Stellar SDK load in WebGL build");
            return;
            #else
            try
            {
                var dllPath = System.IO.Path.Combine(Application.dataPath, "Plugins", "StellarSDK", "stellar-dotnet-sdk.dll");
                Debug.Log($"Looking for Stellar SDK DLL at: {dllPath}");
                
                if (!System.IO.File.Exists(dllPath))
                {
                    Debug.LogError("Stellar SDK DLL file not found!");
                    return;
                }
                
                var assembly = Assembly.LoadFrom(dllPath);
                Debug.Log($"Stellar SDK Assembly loaded: {assembly.FullName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading Stellar SDK: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
            #endif
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
                verticalLayout.childAlignment = TextAnchor.UpperLeft;
                verticalLayout.childControlHeight = true;
                verticalLayout.childControlWidth = true;
                verticalLayout.childForceExpandHeight = false;
                verticalLayout.childForceExpandWidth = true;
                Debug.Log("Added VerticalLayoutGroup to content");
            }

            // Configure content size fitter
            ContentSizeFitter sizeFitter = contentParent.GetComponent<ContentSizeFitter>();
            if (sizeFitter == null)
            {
                sizeFitter = contentParent.gameObject.AddComponent<ContentSizeFitter>();
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                sizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                Debug.Log("Added ContentSizeFitter to content");
            }

            // Configure scroll view
            ScrollRect scrollRect = contentParent.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.verticalScrollbarSpacing = 5f;
                scrollRect.movementType = ScrollRect.MovementType.Elastic;
                scrollRect.elasticity = 0.1f;
                scrollRect.inertia = true;
                scrollRect.decelerationRate = 0.135f;
                Debug.Log("Configured ScrollRect");

                // Ensure the scroll view has a mask
                if (scrollRect.GetComponent<Mask>() == null)
                {
                    var mask = scrollRect.gameObject.AddComponent<Mask>();
                    mask.showMaskGraphic = false;
                }

                // Ensure the scroll view has a curved canvas group
                if (curvedUISettings == null)
                {
                    curvedUISettings = scrollRect.GetComponentInParent<CurvedUISettings>();
                    if (curvedUISettings == null)
                    {
                        Debug.LogWarning("No CurvedUISettings found in parent hierarchy!");
                    }
                }
            }
            else
            {
                Debug.LogError("No ScrollRect found in parent hierarchy!");
            }

            // Force initial layout update
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
            
            // Update CurvedUI if available
            if (curvedUISettings != null)
            {
                curvedUISettings.enabled = false;
                curvedUISettings.enabled = true;
                Debug.Log("Updated CurvedUI settings");
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
            if (contentParent == null || map == null) 
            {
                Debug.LogError("ContentParent or map is null!");
                return;
            }

            Debug.Log($"Updating UI with {markers.Count} markers");

            try
            {
                // Disable CurvedUI temporarily while updating
                if (curvedUISettings != null)
                {
                    curvedUISettings.enabled = false;
                }

                // Clear existing UI items
                foreach (Transform child in contentParent)
                {
                    Destroy(child.gameObject);
                }

                // Create UI items for visible markers
                foreach (var marker in markers)
                {
                    GameObject keyItem = Instantiate(markerKeyPrefab, contentParent);
                    if (keyItem == null)
                    {
                        Debug.LogError("Failed to instantiate marker key prefab!");
                        continue;
                    }

                    keyItem.name = $"MarkerItem_{marker.blockchain}";
                    keyItem.SetActive(true);

                    // Get and configure the TextMeshProUGUI component
                    TextMeshProUGUI tmpText = keyItem.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmpText != null)
                    {
                        string displayText = $"{marker.blockchain} - {marker.publicKey}";
                        tmpText.text = displayText;
                        tmpText.fontSize = 24;
                        tmpText.color = Color.white;
                        tmpText.alignment = TextAlignmentOptions.Left;
                        tmpText.textWrappingMode = TextWrappingModes.Normal;
                        tmpText.overflowMode = TextOverflowModes.Overflow;
                        tmpText.gameObject.SetActive(true);

                        // Ensure RectTransform is properly configured
                        RectTransform rectTransform = tmpText.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            rectTransform.anchorMin = new Vector2(0, 0);
                            rectTransform.anchorMax = new Vector2(1, 1);
                            rectTransform.sizeDelta = Vector2.zero;
                            rectTransform.anchoredPosition = Vector2.zero;
                        }

                        Debug.Log($"Created UI item for marker: {displayText}");
                    }
                    else
                    {
                        Debug.LogError($"TextMeshProUGUI component not found in marker key prefab for {marker.blockchain}");
                    }

                    // Configure the item's RectTransform
                    RectTransform itemRectTransform = keyItem.GetComponent<RectTransform>();
                    if (itemRectTransform != null)
                    {
                        itemRectTransform.anchorMin = new Vector2(0, 1);
                        itemRectTransform.anchorMax = new Vector2(1, 1);
                        itemRectTransform.sizeDelta = new Vector2(0, 30); // Fixed height
                        LayoutRebuilder.ForceRebuildLayoutImmediate(itemRectTransform);
                    }
                }

                // Update the content parent's layout
                RectTransform contentRectTransform = contentParent as RectTransform;
                if (contentRectTransform != null)
                {
                    // Force content size update
                    ContentSizeFitter contentSizeFitter = contentParent.GetComponent<ContentSizeFitter>();
                    if (contentSizeFitter != null)
                    {
                        contentSizeFitter.enabled = false;
                        contentSizeFitter.enabled = true;
                    }

                    // Force vertical layout update
                    VerticalLayoutGroup verticalLayout = contentParent.GetComponent<VerticalLayoutGroup>();
                    if (verticalLayout != null)
                    {
                        verticalLayout.enabled = false;
                        verticalLayout.enabled = true;
                    }

                    // Force layout rebuild
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentRectTransform);
                }

                // Update the scroll view
                ScrollRect scrollRect = contentParent.GetComponentInParent<ScrollRect>();
                if (scrollRect != null)
                {
                    scrollRect.normalizedPosition = Vector2.one; // Reset scroll position to top
                    Canvas.ForceUpdateCanvases();
                }

                // Re-enable and update CurvedUI
                if (curvedUISettings != null)
                {
                    StartCoroutine(UpdateCurvedUIWithDelay());
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating UI: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        private IEnumerator UpdateCurvedUIWithDelay()
        {
            yield return new WaitForEndOfFrame();
            
            if (curvedUISettings != null)
            {
                curvedUISettings.enabled = true;
                yield return null;
                
                // Force another layout update
                Canvas.ForceUpdateCanvases();
                if (contentParent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
                    
                    // Update each child's layout
                    foreach (Transform child in contentParent)
                    {
                        LayoutRebuilder.ForceRebuildLayoutImmediate(child as RectTransform);
                    }
                }
                
                // Ensure the canvas is marked as dirty to force a redraw
                if (contentParent != null)
                {
                    Canvas canvas = contentParent.GetComponentInParent<Canvas>();
                    if (canvas != null)
                    {
                        canvas.enabled = false;
                        canvas.enabled = true;
                    }
                }
            }
        }

        void Update()
        {
            // Update lasers
            foreach (LineRenderer laser in activeLasers.ToArray())
            {
                if (laser != null && laser.transform.parent != null)
                {
                    Transform markerTransform = laser.transform.parent;
                    Vector3 markerPos = markerTransform.position;
                    
                    // Update ground laser positions
                    Vector3 groundPos = new Vector3(markerPos.x, 0, markerPos.z);
                    laser.SetPosition(0, groundPos);
                    laser.SetPosition(1, markerPos);

                    // Update color with pulsing alpha
                    float alpha = 0.4f + Mathf.PingPong(Time.time * 2f, 0.6f);
                    Color color = new Color(laserColor.r, laserColor.g, laserColor.b, alpha);
                    laser.startColor = color;
                    laser.endColor = color;
                }
            }

            // Check if we need to clean up the laser effect
            if (qrManager != null && qrManager.currentLaser != null)
            {
                bool shouldDestroyLaser = true;
                if (qrManager.isScanning && Camera.main != null)
                {
                    // Find nearest QR code
                    GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
                    if (qrCodes.Length > 0)
                    {
                        float nearestDistance = float.MaxValue;
                        foreach (var qrCode in qrCodes)
                        {
                            if (qrCode != null)
                            {
                                float distance = Vector3.Distance(Camera.main.transform.position, qrCode.transform.position);
                                if (distance <= qrManager.scanDistance)
                                {
                                    shouldDestroyLaser = false;
                                    nearestDistance = Mathf.Min(nearestDistance, distance);
                                }
                            }
                        }
                    }
                }

                if (shouldDestroyLaser)
                {
                    Destroy(qrManager.currentLaser.gameObject);
                    qrManager.currentLaser = null;
                }
            }

            // Toggle scanning state with E key (with cooldown)
            if (Input.GetKeyDown(KeyCode.E) && Time.time - lastScanToggleTime > SCAN_TOGGLE_COOLDOWN)
            {
                lastScanToggleTime = Time.time;
                if (qrManager != null)
                {
                    qrManager.isScanning = !qrManager.isScanning;
                    Debug.Log($"QR scanning {(qrManager.isScanning ? "started" : "stopped")} (E pressed)");
                }
            }

            // Update map and UI if needed
            if (map != null)
            {
                // Get visible bounds of the map
                double tlx, tly, brx, bry;
                map.GetTileCorners(out tlx, out tly, out brx, out bry);

                var marker3DManager = OnlineMapsMarker3DManager.instance;
                if (marker3DManager != null && marker3DManager.items != null)
                {
                    bool markersChanged = false;
                    tempMarkerList.Clear();
                    
                    foreach (var marker3D in marker3DManager.items)
                    {
                        if (marker3D != null && marker3D.enabled && !string.IsNullOrEmpty(marker3D.label))
                        {
                            // Convert marker position to tile coordinates
                            double mx, my;
                            OnlineMaps.instance.projection.CoordinatesToTile(marker3D.position.x, marker3D.position.y, map.zoom, out mx, out my);

                            // Only process if marker is within visible bounds
                            if (mx >= tlx && mx <= brx && my >= tly && my <= bry)
                            {
                                string[] parts = marker3D.label.Split('-');
                                if (parts.Length >= 2)
                                {
                                    var markerData = new MarkerData
                                    {
                                        publicKey = parts[0].Trim(),
                                        blockchain = parts[1].Trim(),
                                        label = marker3D.label
                                    };
                                    tempMarkerList.Add(markerData);
                                    
                                    // Check if this marker was not previously visible
                                    if (!currentVisibleMarkers.Any(m => m.publicKey == markerData.publicKey))
                                    {
                                        markersChanged = true;
                                    }
                                }

                                // Update marker rotation if needed
                                if (marker3D.instance != null && mainCamera != null)
                                {
                                    float distance = Vector3.Distance(mainCamera.transform.position, marker3D.instance.transform.position);
                                    if (distance < 5f)
                                    {
                                        Vector3 directionToPlayer = (mainCamera.transform.position - marker3D.instance.transform.position).normalized;
                                        marker3D.instance.transform.rotation = Quaternion.Lerp(
                                            marker3D.instance.transform.rotation,
                                            Quaternion.LookRotation(directionToPlayer),
                                            Time.deltaTime * 5f
                                        );
                                    }
                                    else if (marker3D.instance.transform.rotation != Quaternion.Euler(defaultRotation))
                                    {
                                        marker3D.instance.transform.rotation = Quaternion.Lerp(
                                            marker3D.instance.transform.rotation,
                                            Quaternion.Euler(defaultRotation),
                                            Time.deltaTime * 5f
                                        );
                                    }
                                }
                            }
                        }
                    }

                    // Check if markers have changed
                    if (markersChanged || tempMarkerList.Count != currentVisibleMarkers.Count)
                    {
                        // Swap lists to avoid allocation
                        var temp = currentVisibleMarkers;
                        currentVisibleMarkers = tempMarkerList;
                        tempMarkerList = temp;
                        
                        UpdateUI(currentVisibleMarkers);
                    }
                    else if (contentParent != null && curvedUISettings != null)
                    {
                        // Minimal UI update when no markers changed
                        Canvas.ForceUpdateCanvases();
                        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
                        curvedUISettings.enabled = false;
                        curvedUISettings.enabled = true;
                    }
                }
            }
        }

        private bool MarkersListsEqual(List<MarkerData> list1, List<MarkerData> list2)
        {
            if (list1.Count != list2.Count) return false;
            
            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].publicKey != list2[i].publicKey ||
                    list1[i].blockchain != list2[i].blockchain ||
                    list1[i].label != list2[i].label)
                {
                    return false;
                }
            }
            
            return true;
        }

        IEnumerator FetchMarkers()
        {
            Debug.Log($"Fetching markers from: {apiUrl}");
            UnityWebRequest request = UnityWebRequest.Get(apiUrl);
            
            // Add required headers
            request.SetRequestHeader("Authorization", "Bearer " + apiConfig.BearerToken);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");

            #if UNITY_WEBGL && !UNITY_EDITOR
            // Add specific headers for WebGL builds
            request.SetRequestHeader("Origin", "http://localhost:51847");
            #endif

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"Received JSON: {jsonResponse}");
                
                try 
                {
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
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing JSON response: {e.Message}");
                    Debug.LogError($"Raw response: {jsonResponse}");
                }
            }
            else
            {
                Debug.LogError($"Error fetching markers: {request.error}");
                Debug.LogError($"Response Code: {request.responseCode}");
                var headers = request.GetResponseHeaders();
                if (headers != null)
                {
                    Debug.LogError("Response Headers:");
                    foreach (var header in headers)
                    {
                        Debug.LogError($"{header.Key}: {header.Value}");
                    }
                }
                string responseBody = request.downloadHandler?.text ?? "No response body";
                Debug.LogError($"Response Body: {responseBody}");
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

            try
            {
                OnlineMapsMarker3D marker3D = OnlineMapsMarker3DManager.CreateItem(longitude, latitude, prefabToUse);
                marker3D.label = label;
                marker3D.enabled = true;
                
                // Adjust settings based on blockchain type
                if (blockchain.ToLower() == "circle" || blockchain.ToLower() == "usdc")
                {
                    marker3D.scale = 100f;
                    marker3D.rotation = Quaternion.Euler(0, 180, 0);
                    marker3D.altitude = 120;
                    Debug.Log($"USDC Marker created - Position: {marker3D.transform.position}, Scale: {marker3D.scale}, Rotation: {marker3D.rotation}");
                }
                else
                {
                    marker3D.scale = 10f;
                    marker3D.rotation = Quaternion.Euler(-90, 180, 0);
                    marker3D.altitude = 52;
                }

                // Create ground-to-marker laser
                GameObject laserObj = new GameObject($"GroundLaser_{label}");
                laserObj.transform.SetParent(marker3D.instance.transform);
                
                LineRenderer laser = laserObj.AddComponent<LineRenderer>();
                laser.material = new Material(Shader.Find("Sprites/Default"));
                laser.material.color = laserColor;
                laser.startWidth = laserWidth;
                laser.endWidth = laserWidth;
                laser.positionCount = 2;
                laser.useWorldSpace = true;

                // Set ground laser positions
                Vector3 markerPos = marker3D.instance.transform.position;
                Vector3 groundPos = new Vector3(markerPos.x, 0, markerPos.z);
                laser.SetPosition(0, groundPos);
                laser.SetPosition(1, markerPos);

                // Set color with alpha
                Color laserColorWithAlpha = new Color(laserColor.r, laserColor.g, laserColor.b, 0.8f);
                laser.startColor = laserColorWithAlpha;
                laser.endColor = laserColorWithAlpha;

                activeLasers.Add(laser);

                // Add QR code for Stellar markers
                if (blockchain.ToLower() == "stellar" && qrManager != null)
                {
                    string stellarUri = GenerateStellarUri(label);
                    qrManager.AttachQRCodeToMarker(marker3D.instance, label, stellarUri);
                    Debug.Log($"Added QR code to Stellar marker: {label}");
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
            StartCoroutine(HandleStellarUriCoroutine(qrData.Split('|')[1]));
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

                #if !UNITY_EDITOR && UNITY_WEBGL
                // Use PassKey Kit for WebGL builds
                var passkeyManager = PasskeyManager.Instance;
                if (passkeyManager == null)
                {
                    throw new InvalidOperationException("PasskeyManager not found in scene");
                }

                // Check if user is authenticated
                if (!passkeyManager.IsAuthenticated())
                {
                    Debug.Log("User not authenticated, showing authentication UI...");
                    if (passkeyUIManager != null)
                    {
                        passkeyUIManager.Show();
                        // Wait for authentication
                        while (!passkeyManager.IsAuthenticated())
                        {
                            await Task.Delay(100);
                            // Add timeout logic if needed
                        }
                    }
                    else
                    {
                        Debug.LogWarning("PasskeyUIManager not found, attempting direct authentication...");
                        bool success = await passkeyManager.Authenticate("user");
                        if (!success)
                        {
                            return new StellarTransactionResult 
                            { 
                                Success = false, 
                                ErrorMessage = "Please authenticate first" 
                            };
                        }
                    }
                }

                // Sign the transaction
                string signedTx = await passkeyManager.SignTransaction(uri);
                
                // Hide the UI after successful transaction
                if (passkeyUIManager != null)
                {
                    passkeyUIManager.Hide();
                }
                
                return new StellarTransactionResult
                {
                    Success = signedTx != null,
                    TransactionHash = signedTx ?? "Transaction signing failed",
                    ErrorMessage = signedTx == null ? "Failed to sign transaction" : null
                };
                #else
                // Default response for non-WebGL builds
                return new StellarTransactionResult
                {
                    Success = false,
                    ErrorMessage = "PassKey Kit is only available in WebGL builds"
                };
                #endif
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

                // Remove " - Stellar" suffix if present
                publicKey = publicKey.Replace(" - Stellar", "");

                // Build a basic SEP-0007 URI without using KeyPair
                return $"web+stellar:pay?destination={publicKey}&amount=100";
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating Stellar URI: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return string.Empty;
            }
        }

        private void OnMapZoomChanged()
        {
            if (map == null) return;
            map.width = 12288;
            map.height = 12288;
            map.Redraw();
        }

        public bool IsQRCodeScanned(string publicKey)
        {
            return qrManager != null && qrManager.IsQRCodeScanned(publicKey);
        }
    }

    public class StellarQRManager : MonoBehaviour
    {
        [Header("Map References")]
        [SerializeField] private OnlineMaps map;
        [SerializeField] private OnlineMapsControlBase control;
        [SerializeField] private OnlineMapsMarkerManager markerMGR;

        [Header("Stellar Config")]
        [SerializeField] private StellarConfig stellarConfig;

        private Texture2D qrCodeTexture;
        private Material qrCodeMaterial;
        public bool isScanning = false;
        public float scanDistance = 3000f;
        private Material laserMaterial;
        public LineRenderer currentLaser;
        public Camera scanningCamera;
        private Dictionary<string, bool> scannedQRCodesByPublicKey = new Dictionary<string, bool>();
        
        public delegate void QRScannedHandler(string qrData);
        public event QRScannedHandler OnQRScanned;
        
        private Vector3 lastPosition;
        private bool isHovering = false;
        private float hoverThreshold = 0.1f;
        private float hoverCheckDelay = 0.1f;
        private LineRenderer pointerLine;
        private GameObject focusedQRCode;

        void Start()
        {
            Debug.Log("=== Checking Stellar SDK Setup ===");
            
            #if !UNITY_EDITOR && UNITY_WEBGL
            try
            {
                // WebGL-specific initialization
                laserMaterial = new Material(Shader.Find("Particles/Standard Unlit"))
                {
                    color = Color.red,
                    renderQueue = 3000
                };
            }
            catch (Exception e)
            {
                Debug.LogError($"WebGL initialization error in StellarQRManager: {e.Message}");
            }
            #endif

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

            if (map == null) map = OnlineMaps.instance;
            if (control == null) control = OnlineMapsControlBase.instance;
            if (markerMGR == null) markerMGR = OnlineMapsMarkerManager.instance;

            if (map == null || control == null || markerMGR == null)
            {
                Debug.LogError("One or more required components are missing. Please check the inspector.");
                return;
            }

            // Set up map zoom settings
            map.zoomRange = new OnlineMapsRange(2, 20);  // Set min and max zoom levels
            map.zoom = 17;  // Set initial zoom level
            map.notInteractUnderGUI = true;
            map.width = 24576;  // Set large tileset width
            map.height = 24576;  // Set large tileset height
            map.redrawOnPlay = true;

            // Add zoom change listener
            map.OnChangeZoom += OnMapZoomChanged;

            laserMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
            laserMaterial.color = Color.red;
            lastPosition = transform.position;
            StartCoroutine(CheckHovering());
            OnQRScanned += HandleQRScanned;
        }

        public bool IsQRCodeScanned(string publicKey)
        {
            return scannedQRCodesByPublicKey.ContainsKey(publicKey) && scannedQRCodesByPublicKey[publicKey];
        }

        private void HandleQRScanned(string qrData)
        {
            Debug.Log($"QR Code scanned: {qrData}");
            string[] parts = qrData.Split('|');
            if (parts.Length == 2)
            {
                string publicKey = parts[0];
                Debug.Log($"Marking QR code as scanned for public key: {publicKey}");
                scannedQRCodesByPublicKey[publicKey] = true;
            }
        }

        void Update()
        {
            if (isScanning && Camera.main != null)
            {
                GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
                if (qrCodes.Length > 0)
                {
                    // Find nearest unscanned QR code
                    GameObject nearestQR = null;
                    float nearestDistance = float.MaxValue;
                    foreach (GameObject qr in qrCodes)
                    {
                        string qrId = qr.GetInstanceID().ToString();
                        if (scannedQRCodesByPublicKey.ContainsKey(qrId) && scannedQRCodesByPublicKey[qrId]) continue;
                        
                        float dist = Vector3.Distance(Camera.main.transform.position, qr.transform.position);
                        if (dist < nearestDistance && dist <= scanDistance)
                        {
                            nearestDistance = dist;
                            nearestQR = qr;
                        }
                    }

                    if (nearestQR != null)
                    {
                        // Update or create pointer line
                        if (pointerLine == null)
                        {
                            GameObject pointerObj = new GameObject("QROutlineLaser");
                            pointerLine = pointerObj.AddComponent<LineRenderer>();
                            
                            // Create material with emissive properties
                            Material pointerMat = new Material(Shader.Find("Particles/Standard Unlit"));
                            pointerMat.color = new Color(1, 0, 0, 1f); // Fully opaque red
                            pointerLine.material = pointerMat;
                            
                            pointerLine.startWidth = 15.0f;  // Much thicker line
                            pointerLine.endWidth = 15.0f;    // Much thicker line
                            pointerLine.positionCount = 5;
                            pointerLine.loop = true;
                            pointerLine.useWorldSpace = false; // Use local space for better positioning
                            
                            // Ensure it renders on top
                            pointerLine.sortingOrder = 100;
                            pointerLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                            pointerLine.receiveShadows = false;
                            pointerLine.material.renderQueue = 4000;
                        }

                        // Update outline position to match QR code
                        float markerHeight = nearestQR.transform.parent.name.ToLower().Contains("usdc") ? 120f : 52f;
                        float qrHeight = markerHeight + 15f; // Same height as QR code
                        Vector3 qrPosition = nearestQR.transform.position;
                        pointerLine.transform.position = qrPosition;
                        pointerLine.transform.rotation = nearestQR.transform.rotation;

                        // Create square outline slightly bigger than QR code
                        float size = 7.0f;  // Increased outline size
                        float zOffset = -0.1f; // Offset the line slightly in front of the QR code
                        Vector3[] points = new Vector3[5]
                        {
                            new Vector3(-size, -size, zOffset),
                            new Vector3(size, -size, zOffset),
                            new Vector3(size, size, zOffset),
                            new Vector3(-size, size, zOffset),
                            new Vector3(-size, -size, zOffset)
                        };

                        for (int i = 0; i < points.Length; i++)
                        {
                            pointerLine.SetPosition(i, points[i]);
                        }

                        focusedQRCode = nearestQR;
                    }
                }
                else if (pointerLine != null)
                {
                    Destroy(pointerLine.gameObject);
                    pointerLine = null;
                    focusedQRCode = null;
                }
            }
            else if (pointerLine != null)
            {
                Destroy(pointerLine.gameObject);
                pointerLine = null;
                focusedQRCode = null;
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

        void OnDestroy()
        {
            if (laserMaterial != null)
                Destroy(laserMaterial);
            if (currentLaser != null)
                Destroy(currentLaser.gameObject);
            if (pointerLine != null)
                Destroy(pointerLine.gameObject);
        }

        public void ResetScannedStatus()
        {
            scannedQRCodesByPublicKey.Clear();
        }

        public void ScanQRCode(Camera camera, Vector3 markerPosition)
        {
            if (!isScanning || !isHovering) return;

            GameObject[] qrCodes = GameObject.FindGameObjectsWithTag("QRCode");
            if (qrCodes.Length == 0) return;

            // Only scan the nearest unscanned QR code
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

                            #if !UNITY_EDITOR && UNITY_WEBGL
                            // In WebGL, we'll simulate a successful scan
                            string simulatedData = $"{markerName}|web+stellar:pay?destination={markerName}&amount=100";
                            Debug.Log($"WebGL: Simulated QR scan: {simulatedData}");
                            scannedQRCodesByPublicKey[qrId] = true;
                            OnQRScanned?.Invoke(simulatedData);
                            #else
                            var meshRenderer = hit.collider.GetComponent<MeshRenderer>();
                            if (meshRenderer != null && meshRenderer.material != null && meshRenderer.material.mainTexture != null)
                            {
                                Texture2D qrTexture = meshRenderer.material.mainTexture as Texture2D;
                                var reader = new BarcodeReader();
                                var result = reader.Decode(qrTexture.GetPixels32(), qrTexture.width, qrTexture.height);
                                
                                if (result != null)
                                {
                                    Debug.Log($"Successfully decoded QR code for {markerName}: {result.Text}");
                                    scannedQRCodesByPublicKey[qrId] = true;
                                    OnQRScanned?.Invoke(result.Text);
                                }
                            }
                            #endif
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error scanning QR code: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        private IEnumerator CheckHovering()
        {
            while (true)
            {
                float movement = Vector3.Distance(transform.position, lastPosition);
                isHovering = movement < hoverThreshold;
                
                if (!isHovering && isScanning)
                {
                    isScanning = false;
                    if (currentLaser != null)
                    {
                        Destroy(currentLaser.gameObject);
                        currentLaser = null;
                    }
                }
                
                lastPosition = transform.position;
                yield return new WaitForSeconds(hoverCheckDelay);
            }
        }

        public void AttachQRCodeToMarker(GameObject marker, string publicKey, string uriData)
        {
            try
            {
                Debug.Log($"Attaching QR code to marker: {marker.name}, PublicKey: {publicKey}");
                
                // Create QR code plane above the marker
                GameObject qrPlane = GameObject.CreatePrimitive(PrimitiveType.Quad);
                qrPlane.name = $"QRCode_{publicKey}";
                qrPlane.transform.SetParent(marker.transform, false);
                
                // Position the QR code just above the marker
                float markerHeight = marker.transform.parent.name.ToLower().Contains("usdc") ? 120f : 52f;
                float qrHeight = markerHeight + 15f;
                qrPlane.transform.localPosition = new Vector3(0, qrHeight, 0);
                
                // Rotate to face camera and scale appropriately
                qrPlane.transform.localRotation = Quaternion.Euler(0, 180, 0);
                qrPlane.transform.localScale = new Vector3(10f, 10f, 10f);
                qrPlane.tag = "QRCode";
                
                qrPlane.layer = 8;

                BoxCollider collider = qrPlane.GetComponent<BoxCollider>();
                if (collider == null)
                {
                    collider = qrPlane.AddComponent<BoxCollider>();
                }
                collider.isTrigger = true;
                collider.size = new Vector3(1f, 1f, 0.1f);

                // Generate and apply QR code texture
                Texture2D qrTexture = GenerateQRCode(publicKey, uriData);
                if (qrTexture != null)
                {
                    Material qrMaterial = null;
                    
                    // Try different shaders in order of preference
                    string[] shaderNames = new string[] 
                    {
                        "Unlit/Texture",
                        "Sprites/Default",
                        "Mobile/Unlit (Supports Lightmap)",
                        "Unlit/Color",
                        "UI/Default"
                    };

                    foreach (string shaderName in shaderNames)
                    {
                        Shader shader = Shader.Find(shaderName);
                        if (shader != null)
                        {
                            qrMaterial = new Material(shader);
                            Debug.Log($"Successfully created material with shader: {shaderName}");
                            break;
                        }
                    }

                    if (qrMaterial == null)
                    {
                        Debug.LogError("Failed to create material with any shader. Using default material.");
                        qrMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
                    }
                    
                    qrMaterial.mainTexture = qrTexture;
                    qrMaterial.renderQueue = 3000;
                    
                    MeshRenderer meshRenderer = qrPlane.GetComponent<MeshRenderer>();
                    meshRenderer.material = qrMaterial;
                    meshRenderer.receiveShadows = false;
                    meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    
                    Debug.Log($"Successfully created QR code for {publicKey} with dimensions: {qrTexture.width}x{qrTexture.height}");
                }
                else
                {
                    Debug.LogError($"Failed to generate QR texture for {publicKey}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error attaching QR code to marker: {e.Message}\nStack trace: {e.StackTrace}");
            }
        }

        public Texture2D GenerateQRCode(string publicKey, string uriData)
        {
            try
            {
                Debug.Log($"Generating QR code for PublicKey: {publicKey}");
                
                #if !UNITY_EDITOR && UNITY_WEBGL
                // Create a simple pattern for WebGL
                var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                texture.filterMode = FilterMode.Point;
                
                Color32[] colors = new Color32[256 * 256];
                for (int y = 0; y < 256; y++)
                {
                    for (int x = 0; x < 256; x++)
                    {
                        // Create a QR-like pattern that's more visible
                        bool isQRPixel = false;
                        
                        // Create border
                        if (x < 24 || x > 232 || y < 24 || y > 232)
                        {
                            isQRPixel = false;
                        }
                        // Create positioning squares in corners
                        else if ((x < 64 && y < 64) || // Top-left
                                (x > 192 && y < 64) || // Top-right
                                (x < 64 && y > 192))   // Bottom-left
                        {
                            isQRPixel = true;
                        }
                        // Create data pattern
                        else
                        {
                            isQRPixel = ((x + y) % 32 < 16);
                        }
                        
                        colors[y * 256 + x] = isQRPixel ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                    }
                }
                
                texture.SetPixels32(colors);
                texture.Apply(false);
                return texture;
                #else
                var writer = new BarcodeWriter
                {
                    Format = BarcodeFormat.QR_CODE,
                    Options = new QrCodeEncodingOptions
                    {
                        Height = 256,
                        Width = 256,
                        Margin = 1,
                        CharacterSet = "UTF-8"
                    }
                };

                string combinedData = $"{publicKey}|{uriData}";
                Debug.Log($"Combined QR data: {combinedData}");
                
                var color32 = writer.Write(combinedData);
                var texture = new Texture2D(256, 256, TextureFormat.RGB24, false);
                texture.filterMode = FilterMode.Point;
                
                for (int i = 0; i < color32.Length; i++)
                {
                    color32[i] = color32[i].r < 128 ? new Color32(0, 0, 0, 255) : new Color32(255, 255, 255, 255);
                }
                
                texture.SetPixels32(color32);
                texture.Apply(false);
                return texture;
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating QR code: {e.Message}\nStack trace: {e.StackTrace}");
                return null;
            }
        }

        private void OnMapZoomChanged()
        {
            if (map == null) return;
            map.width = 12288;
            map.height = 12288;
            map.Redraw();
        }
    }
}