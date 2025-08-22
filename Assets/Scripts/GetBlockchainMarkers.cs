using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CurvedUI;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Net.Http;
using System.Net;
using System.Runtime.InteropServices;
using BlockchainMaps.Authentication;
using BlockchainMaps.Soroban;
using BlockchainMaps;

#if !UNITY_WEBGL
using ZXing.Unity;
#endif

#if !UNITY_WEBGL
using StellarDotnetSdk;
using StellarDotnetSdk.Responses;
using StellarDotnetSdk.Requests;
using System.Web;
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
#endif

#if !UNITY_EDITOR && UNITY_WEBGL
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("stellar-dotnet-sdk")]
#endif

namespace BlockchainMaps
{
    [DefaultExecutionOrder(-100)] // Ensure this script initializes early
    public class GetBlockchainMarkers : MonoBehaviour
    {
        private static bool isInitialized = false;
        private PasskeyUIManager passkeyUIManager;
        
        [Header("Configuration")]
        [SerializeField] private SorobanConfig sorobanConfig;
        
        void Awake()
        {
            if (!isInitialized)
            {
                isInitialized = true;
                Debug.Log("Initializing GetBlockchainMarkers...");
                
                // Ensure AutoStellarSetup exists
                EnsureAutoStellarSetup();
                
                #if !UNITY_EDITOR && UNITY_WEBGL
                Debug.Log("WebGL build detected - using WaitForComponents");
                // Ensure all required components are available before proceeding
                StartCoroutine(WaitForComponents());
                #else
                Debug.Log("Non-WebGL build detected - using InitializeComponents");
                InitializeComponents();
                #endif

                // Initialize laser material
                if (laserMaterial != null)
                {
                    sharedLaserMaterial = new Material(laserMaterial);
                    sharedLaserMaterial.color = laserColor;
                }
                else
                {
                    Debug.LogWarning("Laser material not assigned, using default shader");
                    sharedLaserMaterial = new Material(Shader.Find("Sprites/Default"));
                    sharedLaserMaterial.color = laserColor;
                }
            }
            else
            {
                Debug.Log("GetBlockchainMarkers already initialized - skipping initialization");
            }
        }
        
        private void EnsureAutoStellarSetup()
        {
            // Check if AutoStellarSetup already exists using reflection
            var allMonoBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            bool setupFound = false;
            
            foreach (var mb in allMonoBehaviours)
            {
                if (mb.GetType().Name == "AutoStellarSetup")
                {
                    setupFound = true;
                    break;
                }
            }
            
            if (!setupFound)
            {
                Debug.Log("Creating AutoStellarSetup component...");
                var setupObj = new GameObject("AutoStellarSetup");
                
                // Use reflection to add the component
                var autoStellarSetupType = System.Type.GetType("BlockchainMaps.AutoStellarSetup");
                if (autoStellarSetupType != null)
                {
                    setupObj.AddComponent(autoStellarSetupType);
                    DontDestroyOnLoad(setupObj);
                    Debug.Log("AutoStellarSetup created successfully");
                }
                else
                {
                    Debug.LogError("AutoStellarSetup type not found");
                    Destroy(setupObj);
                }
            }
        }

        private IEnumerator WaitForComponents()
        {
            yield return new WaitForSeconds(0.5f); // Wait for other components to initialize
            
            try
            {
                // Initialize Stellar SDK
                LoadStellarSDK();
                
                // Initialize UI components
                InitializeUI();
                
                // Initialize map components
                if (map == null) map = GetComponent<OnlineMaps>();
                if (control == null) control = GetComponent<OnlineMapsControlBase>();
                if (markerMGR == null) markerMGR = GetComponent<OnlineMapsMarkerManager>();
                
                // Create laser container
                if (laserContainer == null)
                {
                    laserContainer = new GameObject("LaserContainer");
                    laserContainer.transform.SetParent(transform);
                }
                
                // Initialize PassKey UI Manager
                if (passkeyUIManager == null)
                {
                    passkeyUIManager = FindFirstObjectByType<PasskeyUIManager>();
                }
                
                // Initialize QR Manager
                InitializeQRManager();
                
                Debug.Log("All components initialized successfully");
                
                // Start fetching markers
                StartCoroutine(FetchMarkers());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during component initialization: {e.Message}");
            }
        }

        private void InitializeComponents()
        {
            try
            {
                // Initialize Stellar SDK
                LoadStellarSDK();
                
                // Initialize UI components
                InitializeUI();
                
                // Initialize map components
                if (map == null) map = GetComponent<OnlineMaps>();
                if (control == null) control = GetComponent<OnlineMapsControlBase>();
                if (markerMGR == null) markerMGR = GetComponent<OnlineMapsMarkerManager>();
                
                // Create laser container
                if (laserContainer == null)
                {
                    laserContainer = new GameObject("LaserContainer");
                    laserContainer.transform.SetParent(transform);
                }
                
                // Initialize QR Manager
                InitializeQRManager();
                
                Debug.Log("All components initialized successfully");
                
                // Start fetching markers
                StartCoroutine(FetchMarkers());
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during component initialization: {e.Message}");
            }
        }

        void Start()
        {
            Debug.Log("=== GetBlockchainMarkers Start ===");
            
            // Check QR Manager
            qrManager = FindAnyObjectByType<StellarQRManager>();
            Debug.Log($"Initial QR Manager check - Found: {qrManager != null}");
            
            if (qrManager == null)
            {
                Debug.LogError("StellarQRManager not found in scene! QR codes will not be created.");
            }
            
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

        void OnEnable()
        {
            Debug.Log("=== GetBlockchainMarkers OnEnable ===");
            // Check QR Manager again in case it was added after Start
            if (qrManager == null)
            {
                qrManager = FindAnyObjectByType<StellarQRManager>();
                Debug.Log($"OnEnable QR Manager check - Found: {qrManager != null}");
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
        // WebGL JavaScript functions are no longer available - using fallback approach
        private void SafeInitializeWebGL()
        {
            Debug.Log("[GetBlockchainMarkers] WebGL initialization - using fallback mode");
        }
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
            Debug.Log("=== Starting EnsureRequiredComponents ===");
            
            // Add OnlineMaps if missing
            if (map == null)
            {
                map = GetComponent<OnlineMaps>();
                if (map == null)
                {
                    Debug.Log("Adding OnlineMaps component");
                    map = gameObject.AddComponent<OnlineMaps>();
                    if (map == null)
                    {
                        Debug.LogError("Failed to add OnlineMaps component!");
                        return;
                    }
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
                    if (control == null)
                    {
                        Debug.LogError("Failed to add OnlineMapsControlBase component!");
                        return;
                    }
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
                    if (markerMGR == null)
                    {
                        Debug.LogError("Failed to add OnlineMapsMarkerManager component!");
                        return;
                    }
                }
            }

            // Add OnlineMapsMarker3DManager if using 3D markers
            if (use3DMarkers && GetComponent<OnlineMapsMarker3DManager>() == null)
            {
                Debug.Log("Adding OnlineMapsMarker3DManager component");
                var marker3DManager = gameObject.AddComponent<OnlineMapsMarker3DManager>();
                if (marker3DManager == null)
                {
                    Debug.LogError("Failed to add OnlineMapsMarker3DManager component!");
                    return;
                }
            }

            // Verify all components are present
            Debug.Log($"Component verification - Map: {map != null}, Control: {control != null}, MarkerMGR: {markerMGR != null}");
            if (map == null || control == null || markerMGR == null)
            {
                Debug.LogError("Failed to initialize required components. Please check the inspector.");
                enabled = false;
                return;
            }

            Debug.Log("All required components are present");
        }

        private void InitializeMapComponents()
        {
            Debug.Log("=== Starting InitializeMapComponents ===");
            
            // First ensure we have all required components
            EnsureRequiredComponents();
            
            // Verify control is assigned
            if (control == null)
            {
                Debug.LogError("OnlineMapsControlBase is null after initialization!");
                return;
            }

            // Check if we have a 3D control and try to get the correct type if not
            OnlineMapsControlBase3D control3D = control as OnlineMapsControlBase3D;
            if (control3D == null)
            {
                // Try to find a 3D control on the GameObject
                control3D = GetComponent<OnlineMapsControlBase3D>();
                if (control3D == null)
                {
                    // If no 3D control exists, try to add OnlineMapsTileSetControl
                    Debug.LogWarning("No 3D control found. Adding OnlineMapsTileSetControl...");
                    control3D = gameObject.AddComponent<OnlineMapsTileSetControl>();
                    if (control3D == null)
                    {
                        Debug.LogError("Failed to create 3D control. Please add a 3D control type manually (Tileset or Mesh).");
                        return;
                    }
                    control = control3D;
                }
            }

            Debug.Log($"Map component status: Map={map != null}, Control={control != null}, Control3D={control3D != null}, MarkerMGR={markerMGR != null}");
            Debug.Log($"Marker setup status: Texture={markerTexture != null}, Prefab={markerPrefab != null}, Use3D={use3DMarkers}");
            
            // Set up map zoom settings
            if (map != null)
            {
                Debug.Log("Configuring map settings...");
                map.zoomRange = new OnlineMapsRange(2, 20);
                map.zoom = 15;
                map.notInteractUnderGUI = true;
                map.width = 24576;  // Increased for better quality
                map.height = 24576; // Increased for better quality
                map.redrawOnPlay = true;
                map.countParentLevels = 1; // Use this instead of useCurrentZoomTiles
                map.renderInThread = false; // Disable threaded rendering for better quality
                Debug.Log($"Map settings configured - Zoom: {map.zoom}, Size: {map.width}x{map.height}");
            }
            else
            {
                Debug.LogError("Map component is null!");
                return;
            }

            // Add elevation manager if not present
            OnlineMapsElevationManagerBase elevationManager = GetComponent<OnlineMapsElevationManagerBase>();
            if (elevationManager == null)
            {
                Debug.Log("Adding elevation manager");
                elevationManager = gameObject.AddComponent<OnlineMapsElevationManagerBase>();
                if (elevationManager == null)
                {
                    Debug.LogError("Failed to add elevation manager!");
                    return;
                }
            }

            // Configure elevation settings
            elevationManager.zoomRange = new OnlineMapsRange(12, 20);
            elevationManager.scale = 1f;
            elevationManager.enabled = true;
            control3D.elevationManager = elevationManager;
            Debug.Log("Elevation manager configured");

            // Add buildings manager if not present
            OnlineMapsBuildings buildingManager = GetComponent<OnlineMapsBuildings>();
            if (buildingManager == null)
            {
                Debug.Log("Adding buildings manager");
                buildingManager = gameObject.AddComponent<OnlineMapsBuildings>();
                if (buildingManager == null)
                {
                    Debug.LogError("Failed to add buildings manager!");
                    return;
                }
            }

            // Configure buildings settings
            buildingManager.enabled = true;
            buildingManager.zoomRange = new OnlineMapsRange(15, 20);
            buildingManager.heightScale = 1f;
            Debug.Log("Buildings manager configured");

            // Enable terrain
            if (control3D is OnlineMapsControlBaseDynamicMesh meshControl)
            {
                Debug.Log("Configuring terrain settings");
                meshControl.elevationResolution = 32;
                meshControl.sizeInScene = new Vector2(1024, 1024);
            }

            // Add zoom change listener
            map.OnChangeZoom += OnMapZoomChanged;

            // Enable relief shader if available
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                renderer.sharedMaterial.EnableKeyword("TERRAIN");
                Debug.Log("Relief shader enabled");
            }

            if (use3DMarkers)
            {
                Debug.Log("Loading marker prefabs...");
                
                // Load USDC marker
                if (usdcMarkerPrefab == null)
                {
                    Debug.Log("Attempting to load USDC marker prefab from Resources");
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
                    Debug.Log("Using default marker prefab for Stellar markers");
                    stellarMarkerPrefab = markerPrefab;
                }

                Debug.Log($"Marker prefab status - USDC: {usdcMarkerPrefab != null}, Stellar: {stellarMarkerPrefab != null}");
            }

            Debug.Log("Map component initialization completed");
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
            Debug.Log("=== InitializeQRManager called ===");
            if (qrManager == null)
            {
                Debug.Log("Adding StellarQRManager component...");
                qrManager = gameObject.AddComponent<StellarQRManager>();
                if (qrManager != null)
                {
                    Debug.Log("StellarQRManager component added successfully, subscribing to OnQRScanned event");
                    qrManager.OnQRScanned += HandleQRScanned;
                    Debug.Log("OnQRScanned event subscription successful");
                }
                else
                {
                    Debug.LogError("Failed to add StellarQRManager component");
                }
            }
            else
            {
                Debug.Log("StellarQRManager already exists, subscribing to OnQRScanned event");
                qrManager.OnQRScanned += HandleQRScanned;
                Debug.Log("OnQRScanned event subscription successful");
            }
        }

        private void LoadStellarSDK()
        {
            #if !UNITY_EDITOR && UNITY_WEBGL
            Debug.Log("Loading Stellar SDK for WebGL...");
            // WebGL implementation uses the JavaScript bridge
            StartCoroutine(InitializeWebGLWithDelay());
            #else
            Debug.Log("Loading Stellar SDK for standalone...");
            try
            {
                // Initialize Stellar SDK for standalone builds
                if (stellarConfig != null)
                {
                    #if !UNITY_WEBGL
                    var server = new StellarDotnetSdk.Server(stellarConfig.HorizonUrl);
                    StellarDotnetSdk.Network.UseTestNetwork();
                    #endif
                    Debug.Log("Stellar SDK initialized successfully");
                }
                else
                {
                    Debug.LogWarning("StellarConfig not assigned!");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error initializing Stellar SDK: {e.Message}");
            }
            #endif
        }

        private IEnumerator InitializeWebGLWithDelay()
        {
            yield return new WaitForSeconds(0.5f);
            try
            {
                #if !UNITY_EDITOR && UNITY_WEBGL
                // Add an additional safety check
                if (Application.platform == RuntimePlatform.WebGLPlayer)
                {
                    Debug.Log("WebGL initialization - using fallback mode");
                }
                else
                {
                    Debug.LogWarning("Not in WebGL platform, skipping WebGL initialization");
                }
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"WebGL initialization failed: {e.Message}. Using fallback mode.");
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
            try
            {
                // Update lasers with memory management
                for (int i = activeLasers.Count - 1; i >= 0; i--)
                {
                    LineRenderer laser = activeLasers[i];
                    if (laser == null)
                    {
                        activeLasers.RemoveAt(i);
                        continue;
                    }
                    
                    if (laser.transform != null && laser.transform.parent != null)
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
                    else
                    {
                        // Clean up orphaned lasers
                        activeLasers.RemoveAt(i);
                        if (laser != null && laser.gameObject != null)
                        {
                            Destroy(laser.gameObject);
                        }
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
                        if (qrCodes != null && qrCodes.Length > 0)
                        {
                            float nearestDistance = float.MaxValue;
                            foreach (var qrCode in qrCodes)
                            {
                                if (qrCode != null && qrCode.transform != null)
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

                    if (shouldDestroyLaser && qrManager.currentLaser != null && qrManager.currentLaser.gameObject != null)
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

                // Debug key bindings
                if (Input.GetKeyDown(KeyCode.F4))
                {
                    ManuallyCreateQRCodes();
                }

                // Update map and UI if needed (optimized)
                if (map != null && Time.frameCount % 30 == 0) // Only update every 30 frames
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
                                // Check if marker is within visible bounds
                                double markerLon = marker3D.position.x;
                                double markerLat = marker3D.position.y;
                                
                                if (markerLon >= tlx && markerLon <= brx && markerLat <= tly && markerLat >= bry)
                                {
                                    // Extract public key from label
                                    string publicKey = marker3D.label;
                                    string[] parts = marker3D.label.Split('-');
                                    if (parts.Length >= 2)
                                    {
                                        publicKey = parts[0].Trim();
                                    }
                                    
                                    tempMarkerList.Add(new MarkerData
                                    {
                                        publicKey = publicKey,
                                        blockchain = parts.Length >= 2 ? parts[1].Trim() : "Unknown",
                                        label = marker3D.label
                                    });
                                }
                            }
                        }
                        
                        // Check if visible markers have changed
                        if (!MarkersListsEqual(tempMarkerList, currentVisibleMarkers))
                        {
                            currentVisibleMarkers.Clear();
                            currentVisibleMarkers.AddRange(tempMarkerList);
                            markersChanged = true;
                        }
                        
                        // Update UI if markers changed
                        if (markersChanged)
                        {
                            UpdateUI(currentVisibleMarkers);
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in Update method: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
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
            Debug.Log("=== Starting FetchMarkers ===");
            
            // Log component status
            Debug.Log($"API Config present: {apiConfig != null}");
            Debug.Log($"MarkerMGR present: {markerMGR != null}");
            Debug.Log($"Map present: {map != null}");
            Debug.Log($"Control present: {control != null}");
            Debug.Log($"Marker prefabs - Default: {markerPrefab != null}, USDC: {usdcMarkerPrefab != null}, Stellar: {stellarMarkerPrefab != null}");

            if (apiConfig == null)
            {
                Debug.LogError("ApiConfig is missing!");
                yield break;
            }

            if (string.IsNullOrEmpty(apiConfig.BearerToken))
            {
                Debug.LogError("Bearer token is missing in ApiConfig!");
                yield break;
            }

            Debug.Log($"Attempting API request to: {apiUrl}");
            
            UnityWebRequest request = UnityWebRequest.Get(apiUrl);
            request.SetRequestHeader("Authorization", $"Bearer {apiConfig.BearerToken}");
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            
            Debug.Log("Sending API request...");
            yield return request.SendWebRequest();

            Debug.Log($"API Response Status: {request.responseCode}");
            Debug.Log($"API Error Status: {request.error}");
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"API Response Length: {jsonResponse?.Length ?? 0} characters");
                Debug.Log($"API Response Content: {jsonResponse}");

                try
                {
                    ParseJson(jsonResponse);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error parsing JSON response: {e.Message}\nStack trace: {e.StackTrace}");
                }
            }
            else
            {
                Debug.LogError($"API request failed: {request.error}\nResponse Code: {request.responseCode}");
                if (request.downloadHandler != null)
                {
                    Debug.LogError($"Response Text: {request.downloadHandler.text}");
                }
            }

            request.Dispose();
            Debug.Log("=== FetchMarkers Complete ===");
        }

        private void ParseJson(string jsonString)
        {
            Debug.Log("=== Starting JSON Parse ===");
            try
            {
                JArray markersArray = JArray.Parse(jsonString);
                Debug.Log($"Successfully parsed JSON array with {markersArray.Count} items");

                visibleMarkers.Clear();
                foreach (JObject markerObj in markersArray)
                {
                    try
                    {
                        string publicKey = markerObj["publicKey"].ToString();
                        string blockchain = markerObj["blockchain"].ToString();
                        double latitude = markerObj["latitude"].ToObject<double>();
                        double longitude = markerObj["longitude"].ToObject<double>();
                        string label = $"{publicKey} - {blockchain}";

                        MarkerData marker = new MarkerData
                        {
                            publicKey = publicKey,
                            blockchain = blockchain,
                            label = label
                        };
                        visibleMarkers.Add(marker);
                        Debug.Log($"Added marker: {label} at ({latitude}, {longitude})");

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
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing individual marker: {e.Message}");
                    }
                }
                Debug.Log($"Total markers parsed: {visibleMarkers.Count}");

                // Update UI
                UpdateUI(visibleMarkers);
                
                // Notify subscribers
                if (OnMarkersLoaded != null)
                {
                    OnMarkersLoaded.Invoke(visibleMarkers);
                }
                
                // Redraw map
                if (map != null)
                {
                    map.Redraw();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in ParseJson: {e.Message}\nStack trace: {e.StackTrace}");
                throw;
            }
            Debug.Log("=== JSON Parse Complete ===");
        }

        private void Add2DMarker(double latitude, double longitude, string label)
        {
            Debug.Log($"=== Adding 2D Marker at ({latitude}, {longitude}) ===");
            
            if (markerMGR == null)
            {
                Debug.LogError("MarkerManager is null - cannot add 2D marker");
                return;
            }

            if (markerTexture == null)
            {
                Debug.LogError("Marker texture is null - cannot add 2D marker");
                return;
            }

            try
            {
                Debug.Log("Creating 2D marker with texture");
                var marker = markerMGR.Create(longitude, latitude, markerTexture);
                
                if (marker == null)
                {
                    Debug.LogError("Failed to create 2D marker");
                    return;
                }

                marker.label = label;
                marker.enabled = true;
                marker.scale = 10.0f;
                marker.align = OnlineMapsAlign.Bottom;
                
                Debug.Log($"2D Marker created successfully at ({marker.position.x}, {marker.position.y})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error adding 2D marker: {e.Message}\n{e.StackTrace}");
            }
        }

        private void Add3DMarker(double latitude, double longitude, string label, string blockchain)
        {
            Debug.Log($"=== Adding 3D Marker ===");
            Debug.Log($"Label: {label}, Blockchain: {blockchain}, Position: {latitude}, {longitude}");
            
            var marker3DManager = GetComponent<OnlineMapsMarker3DManager>();
            if (marker3DManager == null)
            {
                Debug.LogError("Marker3DManager component not found - adding it now");
                marker3DManager = gameObject.AddComponent<OnlineMapsMarker3DManager>();
            }

            GameObject prefabToUse;
            if (blockchain.Equals("USDC", StringComparison.OrdinalIgnoreCase) || 
                blockchain.Equals("Circle", StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log("Using USDC marker prefab");
                prefabToUse = usdcMarkerPrefab ?? markerPrefab;
            }
            else
            {
                Debug.Log("Using Stellar marker prefab");
                prefabToUse = stellarMarkerPrefab ?? markerPrefab;
            }

            if (prefabToUse == null)
            {
                Debug.LogError($"No prefab available for {blockchain} - cannot add 3D marker");
                return;
            }

            try
            {
                Debug.Log($"Creating 3D marker with prefab: {prefabToUse.name}");
                var marker = marker3DManager.Create(longitude, latitude, prefabToUse);
                
                if (marker == null)
                {
                    Debug.LogError("Failed to create 3D marker");
                    return;
                }

                marker.label = label;
                marker.enabled = true;

                // Adjust settings based on blockchain type
                if (blockchain.Equals("USDC", StringComparison.OrdinalIgnoreCase) || 
                    blockchain.Equals("Circle", StringComparison.OrdinalIgnoreCase))
                {
                    marker.scale = 100f;
                    marker.rotation = Quaternion.Euler(0, 180, 0);
                    marker.altitude = 120;
                    Debug.Log($"USDC Marker settings - Scale: {marker.scale}, Rotation: {marker.rotation}, Altitude: {marker.altitude}");
                }
                else
                {
                    marker.scale = 10f;
                    marker.rotation = Quaternion.Euler(-90, 180, 0);
                    marker.altitude = 52;
                    Debug.Log($"Stellar Marker settings - Scale: {marker.scale}, Rotation: {marker.rotation}, Altitude: {marker.altitude}");
                }

                // Create ground-to-marker laser with delay to ensure marker is properly positioned
                StartCoroutine(CreateLaserWithDelay(marker));

                // Add QR code for Stellar markers
                if (blockchain.Equals("Stellar", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.Log("Starting QR code creation for Stellar marker");
                    StartCoroutine(CreateQRCodeWithDelay(marker));
                }

                Debug.Log($"3D Marker created successfully at ({marker.position.x}, {marker.position.y}, {marker.instance.transform.position.y})");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error adding 3D marker: {e.Message}\n{e.StackTrace}");
            }
        }

        private IEnumerator CreateQRCodeWithDelay(OnlineMapsMarker3D marker)
        {
            Debug.Log($"=== Starting QR Code Creation for marker: {marker.label} ===");
            
            yield return new WaitForSeconds(0.2f); // Wait for marker to be properly positioned

            try
            {
                if (marker != null && marker.instance != null && qrManager != null)
                {
                    Debug.Log($"Generating Stellar URI for marker: {marker.label}");
                    string stellarUri = GenerateStellarUri(marker.label);
                    
                    Debug.Log("Calling AttachQRCodeToMarker...");
                    // Extract public key from label (format: "PUBLICKEY - Blockchain")
                    string publicKey = marker.label;
                    string[] parts = marker.label.Split('-');
                    if (parts.Length >= 2)
                    {
                        publicKey = parts[0].Trim();
                    }
                    qrManager.AttachQRCodeToMarker(marker.instance, publicKey, stellarUri);
                    Debug.Log($"QR code attached to marker: {publicKey}");
                }
                else
                {
                    Debug.LogError($"Invalid state for QR code creation - Marker null: {marker == null}, Instance null: {marker?.instance == null}, QR Manager null: {qrManager == null}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in CreateQRCodeWithDelay: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }

            // Verify QR code was created (outside try-catch to allow yield)
            yield return new WaitForSeconds(0.1f);
            if (marker != null && marker.instance != null)
            {
                // Look for QR code plane by the naming pattern used in AttachQRCodeToMarker
                Transform qrPlane = marker.instance.transform.Find("QRCodePlane");
                if (qrPlane == null)
                {
                    // Try to find by the actual naming pattern: QRCode_{publicKey}
                    string publicKey = marker.label;
                    string[] parts = marker.label.Split('-');
                    if (parts.Length >= 2)
                    {
                        publicKey = parts[0].Trim();
                    }
                    qrPlane = marker.instance.transform.Find($"QRCode_{publicKey}");
                }
                
                if (qrPlane != null)
                {
                    Debug.Log($"QR code plane verified: {qrPlane.name} at position {qrPlane.position}, tag: {qrPlane.tag}");
                }
                else
                {
                    Debug.LogError("QR code plane not found after creation!");
                    // List all children to help debug
                    Debug.LogError($"Marker children: {string.Join(", ", marker.instance.transform.Cast<Transform>().Select(t => t.name))}");
                }
            }

            Debug.Log("=== QR Code Creation Complete ===");
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
            // Clean up lasers
            foreach (var laser in activeLasers)
            {
                if (laser != null)
                {
                    Destroy(laser.gameObject);
                }
            }
            activeLasers.Clear();

            // Clean up materials
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

            // Force garbage collection
            System.GC.Collect();
        }

        private void HandleQRScanned(string qrData)
        {
            Debug.Log($"[GetBlockchainMarkers] QR code scanned: {qrData}");
            Debug.Log($"[GetBlockchainMarkers] Looking for QRPaymentProcessor...");
            
            try
            {
                // First try to use the dedicated QR Payment Processor
                var paymentProcessor = FindFirstObjectByType<QRPaymentProcessor>();
                Debug.Log($"[GetBlockchainMarkers] QRPaymentProcessor found: {paymentProcessor != null}");
                
                if (paymentProcessor != null)
                {
                    Debug.Log("[GetBlockchainMarkers] Forwarding to QRPaymentProcessor");
                    paymentProcessor.HandleQRCodeScanned(qrData);
                    Debug.Log("[GetBlockchainMarkers] Forwarding completed");
                    return;
                }
                
                // Fallback: Try QRPaymentManager if available
                var qrPaymentManager = FindFirstObjectByType<MonoBehaviour>();
                if (qrPaymentManager != null && qrPaymentManager.GetType().Name == "QRPaymentManager")
                {
                    var processMethod = qrPaymentManager.GetType().GetMethod("ProcessScannedQRCode");
                    if (processMethod != null)
                    {
                        Debug.Log("[GetBlockchainMarkers] Forwarding to QRPaymentManager");
                        StartCoroutine(ProcessQRWithManager(qrPaymentManager, processMethod, qrData));
                        return;
                    }
                }
                
                // Final fallback: Process directly using built-in methods
                Debug.Log("[GetBlockchainMarkers] Using built-in QR processing");
                ProcessQRDirectly(qrData);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetBlockchainMarkers] Error handling QR code: {e.Message}");
                ShowPaymentError($"QR processing error: {e.Message}");
            }
        }
        
        private IEnumerator ProcessQRWithManager(MonoBehaviour manager, System.Reflection.MethodInfo method, string qrData)
        {
            // Call the method asynchronously
            Task task = null;
            try
            {
                task = (Task)method.Invoke(manager, new object[] { qrData });
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetBlockchainMarkers] Error calling QRPaymentManager: {e.Message}");
                ShowPaymentError($"Payment manager error: {e.Message}");
                yield break;
            }
            
            if (task != null)
            {
                // Wait for task completion
                while (!task.IsCompleted)
                {
                    yield return null;
                }
                
                try
                {
                    if (task.Exception != null)
                    {
                        Debug.LogError($"[GetBlockchainMarkers] QRPaymentManager error: {task.Exception.Message}");
                        ShowPaymentError($"Payment processing error: {task.Exception.Message}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GetBlockchainMarkers] Error handling QRPaymentManager result: {e.Message}");
                    ShowPaymentError($"Payment result error: {e.Message}");
                }
            }
        }
        
        private void ProcessQRDirectly(string qrData)
        {
            try
            {
                // Parse QR data to determine type
                if (qrData.StartsWith("web+stellar:"))
                {
                    // Stellar URI format
                    StartCoroutine(HandleStellarUriCoroutine(qrData));
                }
                else if (qrData.Contains("|"))
                {
                    // Legacy format with separator
                    var parts = qrData.Split('|');
                    if (parts.Length > 1)
                    {
                        StartCoroutine(HandleStellarUriCoroutine(parts[1]));
                    }
                    else
                    {
                        ShowPaymentError("Invalid QR code format");
                    }
                }
                else if (qrData.StartsWith("G") && qrData.Length == 56)
                {
                    // Direct Stellar public key
                    var uri = GenerateStellarUri(qrData);
                    StartCoroutine(HandleStellarUriCoroutine(uri));
                }
                else
                {
                    // Try to extract Stellar address
                    var match = System.Text.RegularExpressions.Regex.Match(qrData, @"G[A-Z0-9]{55}");
                    if (match.Success)
                    {
                        var uri = GenerateStellarUri(match.Value);
                        StartCoroutine(HandleStellarUriCoroutine(uri));
                    }
                    else
                    {
                        ShowPaymentError("Unsupported QR code format");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetBlockchainMarkers] Error processing QR directly: {e.Message}");
                ShowPaymentError($"QR processing error: {e.Message}");
            }
        }

        private IEnumerator HandleStellarUriCoroutine(string uriData)
        {
            try
            {
                Debug.Log($"[GetBlockchainMarkers] Processing Stellar URI: {uriData}");
                
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
                    
                    // Extract payment details
                    if (parameters.ContainsKey("destination") && parameters.ContainsKey("amount"))
                    {
                        var destination = parameters["destination"];
                        var amountStr = parameters["amount"];
                        var memo = parameters.ContainsKey("memo") ? parameters["memo"] : DEFAULT_MEMO;
                        
                        if (decimal.TryParse(amountStr, out decimal amount))
                        {
                            // Show payment confirmation UI
                            ShowPaymentConfirmation(destination, amount, memo);
                        }
                        else
                        {
                            ShowPaymentError("Invalid amount in QR code");
                        }
                    }
                    else
                    {
                        ShowPaymentError("Missing payment details in QR code");
                    }
                }
                else
                {
                    // No query parameters, treat as address only
                    var destination = uriData;
                    ShowPaymentConfirmation(destination, 0, DEFAULT_MEMO);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetBlockchainMarkers] Error in HandleStellarUriCoroutine: {e.Message}");
                ShowPaymentError($"URI processing error: {e.Message}");
            }
            
            yield return null;
        }
        
        private void ShowPaymentConfirmation(string destination, decimal amount, string memo)
        {
            Debug.Log($"[GetBlockchainMarkers] Showing payment confirmation for {destination}, amount: {amount}, memo: {memo}");
            
            // Try to find and use existing payment UI components
            var paymentUI = FindFirstObjectByType<MonoBehaviour>();
            if (paymentUI != null)
            {
                var showMethod = paymentUI.GetType().GetMethod("ShowPaymentUI");
                if (showMethod != null)
                {
                    // Create payment request object
                    var paymentRequest = new PaymentRequest
                    {
                        RecipientAddress = destination,
                        AssetCode = "XLM",
                        IssuerPublicKey = "native",
                        Amount = amount,
                        Memo = memo
                    };
                    
                    showMethod.Invoke(paymentUI, new object[] { paymentRequest });
                    return;
                }
            }
            
            // Fallback: Show simple confirmation dialog
            ShowSimplePaymentDialog(destination, amount, memo);
        }
        
        private void ShowSimplePaymentDialog(string destination, decimal amount, string memo)
        {
            // Create a simple payment dialog if no existing UI is found
            Debug.Log($"[GetBlockchainMarkers] Creating simple payment dialog for {destination}");
            
            // This would typically create a UI dialog
            // For now, we'll just log the payment request
            Debug.Log($"=== Payment Request ===");
            Debug.Log($"To: {destination}");
            Debug.Log($"Amount: {amount} XLM");
            Debug.Log($"Memo: {memo}");
            Debug.Log($"=====================");
            
            // Try to process the payment directly
            StartCoroutine(ProcessPaymentDirectly(destination, amount, memo));
        }
        
        private IEnumerator ProcessPaymentDirectly(string destination, decimal amount, string memo)
        {
            Debug.Log($"[GetBlockchainMarkers] Processing payment directly...");
            
            // Check if we have the required components
            if (sorobanConfig == null)
            {
                EnsureBackendComponents();
                yield return new WaitForSeconds(0.5f);
            }
            
            // First, ensure we have a wallet from the authentication system
            var walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager == null)
            {
                Debug.LogError("[GetBlockchainMarkers] StellarWalletManager not found!");
                ShowPaymentError("Wallet manager not available");
                yield break;
            }
            
            // Check if we have a wallet
            if (!walletManager.HasWallet())
            {
                Debug.LogWarning("[GetBlockchainMarkers] No wallet found, attempting to create one...");
                ShowPaymentError("No wallet found. Please authenticate with passkey first to create a wallet.");
                yield break;
            }
            
            var wallet = walletManager.GetCurrentWallet();
            if (wallet == null || string.IsNullOrEmpty(wallet.publicKey))
            {
                Debug.LogError("[GetBlockchainMarkers] Wallet data is invalid!");
                ShowPaymentError("Invalid wallet data. Please re-authenticate.");
                yield break;
            }
            
            Debug.Log($"[GetBlockchainMarkers] Using wallet: {wallet.publicKey}");
            
            // Try to use SorobanManager for payment
            var sorobanManager = SorobanManager.Instance;
            if (sorobanManager != null && sorobanManager.IsInitialized())
            {
                Debug.Log("[GetBlockchainMarkers] Using SorobanManager for payment");
                
                // Start async operation and wait for completion
                var task = ProcessStellarUri($"web+stellar:pay?destination={destination}&amount={amount}&memo={memo}");
                
                while (!task.IsCompleted)
                {
                    yield return null;
                }
                
                try
                {
                    var result = task.Result;
                    
                    if (result.Success)
                    {
                        ShowPaymentSuccess($"Payment successful! Transaction: {result.TransactionHash}");
                    }
                    else
                    {
                        ShowPaymentError($"Payment failed: {result.ErrorMessage}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GetBlockchainMarkers] Error getting payment result: {e.Message}");
                    ShowPaymentError($"Payment result error: {e.Message}");
                }
            }
            else
            {
                // Fallback to API-based payment
                Debug.Log("[GetBlockchainMarkers] Using API-based payment fallback");
                var apiClient = FindFirstObjectByType<StellarApiClient>();
                if (apiClient != null)
                {
                    // Create transfer request using the authenticated wallet
                    var transferRequest = new TransferAssetRequest
                    {
                        senderSecret = wallet.secretKey,
                        recipientPublicKey = destination,
                        assetCode = "XLM",
                        issuerPublicKey = "native",
                        amount = amount.ToString(),
                        memo = memo
                    };
                    
                    Debug.Log($"[GetBlockchainMarkers] Sending transfer request from {wallet.publicKey} to {destination}");
                    
                    // Use coroutine to handle async API call
                    StartCoroutine(ProcessTransferRequest(apiClient, transferRequest));
                }
                else
                {
                    ShowPaymentError("Payment system not available");
                }
            }
        }
        
        private IEnumerator ProcessTransferRequest(StellarApiClient apiClient, BlockchainMaps.TransferAssetRequest transferRequest)
        {
            var task = apiClient.TransferAsset(transferRequest);
            
            while (!task.IsCompleted)
            {
                yield return null;
            }
            
            try
            {
                var result = task.Result;
                
                if (result != null && !string.IsNullOrEmpty(result.hash))
                {
                    ShowPaymentSuccess($"Payment successful! Transaction: {result.hash}");
                }
                else
                {
                    ShowPaymentError("Payment failed - no transaction hash received");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[GetBlockchainMarkers] API payment error: {e.Message}");
                ShowPaymentError($"Payment failed: {e.Message}");
            }
        }
        
        private void ShowPaymentSuccess(string message)
        {
            Debug.Log($"[GetBlockchainMarkers] Payment Success: {message}");
            // In a real implementation, this would show a success UI
            // For now, we'll just log it
        }
        
        private void ShowPaymentError(string message)
        {
            Debug.LogError($"[GetBlockchainMarkers] Payment Error: {message}");
            // In a real implementation, this would show an error UI
            // For now, we'll just log it
        }
        
        // Add PaymentRequest class for compatibility
        [System.Serializable]
        public class PaymentRequest
        {
            public string RecipientAddress;
            public string AssetCode;
            public string IssuerPublicKey;
            public decimal Amount;
            public string Memo;
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

                // Get Soroban manager with safety check
                var sorobanManager = SorobanManager.Instance;
                if (sorobanManager == null)
                {
                    throw new InvalidOperationException("SorobanManager not found");
                }

                // Add a small delay to ensure JavaScript bridge is ready
                await Task.Delay(100);

                // Parse URI parameters
                var uriParams = ParseStellarUri(uri);
                if (uriParams == null)
                {
                    throw new ArgumentException("Failed to parse Stellar URI parameters");
                }
                
                // Execute payment through Soroban contract with additional safety
                try
                {
                    var response = await sorobanManager.ExecuteContract(
                        sorobanConfig.tokenContractId,
                        "transfer",
                        new object[] {
                            uriParams.destination,
                            uriParams.amount
                        }
                    );

                    // Parse response
                    var result = JsonUtility.FromJson<StellarTransactionResult>(response);
                    return result;
                }
                catch (Exception sorobanError)
                {
                    Debug.LogError($"[GetBlockchainMarkers] Soroban contract execution failed: {sorobanError.Message}");
                    return new StellarTransactionResult
                    {
                        Success = false,
                        ErrorMessage = $"Soroban execution failed: {sorobanError.Message}",
                        OperationErrors = new List<string> { sorobanError.StackTrace }
                    };
                }
                #else
                // Simulate transaction in editor
                await Task.Delay(100);
                return new StellarTransactionResult
                {
                    Success = true,
                    TransactionHash = "simulated_hash",
                    ErrorMessage = null,
                    OperationErrors = null
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
                    ErrorMessage = e.Message,
                    OperationErrors = new List<string> { e.StackTrace }
                };
            }
        }

        private class StellarUriParams
        {
            public string destination;
            public decimal amount;
            public string memo;
        }

        private StellarUriParams ParseStellarUri(string uri)
        {
            try
            {
                // Remove prefix
                string query = uri.Substring(STELLAR_URI_PREFIX.Length);
                
                #if !UNITY_WEBGL
                // Parse query parameters using System.Web.HttpUtility
                var parameters = System.Web.HttpUtility.ParseQueryString(query);
                return new StellarUriParams
                {
                    destination = parameters["destination"],
                    amount = decimal.Parse(parameters["amount"] ?? "0"),
                    memo = parameters["memo"] ?? DEFAULT_MEMO
                };
                #else
                // Manual parsing for WebGL builds
                var parameters = new Dictionary<string, string>();
                string[] pairs = query.Split('&');
                foreach (string pair in pairs)
                {
                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length == 2)
                    {
                        parameters[keyValue[0]] = UnityWebRequest.UnEscapeURL(keyValue[1]);
                    }
                }
                
                return new StellarUriParams
                {
                    destination = parameters.ContainsKey("destination") ? parameters["destination"] : null,
                    amount = parameters.ContainsKey("amount") ? decimal.Parse(parameters["amount"]) : 0,
                    memo = parameters.ContainsKey("memo") ? parameters["memo"] : DEFAULT_MEMO
                };
                #endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing Stellar URI: {e.Message}");
                throw;
            }
        }

        private string GenerateStellarUri(string publicKey)
        {
            try
            {
                Debug.Log("Generating Stellar URI...");
                
                if (sorobanConfig == null)
                {
                    Debug.LogWarning("SorobanConfig not found, loading from Resources...");
                    sorobanConfig = Resources.Load<SorobanConfig>("SorobanConfig");
                    
                    if (sorobanConfig == null)
                    {
                        Debug.LogError("Failed to load SorobanConfig from Resources. Please ensure it exists at Resources/SorobanConfig");
                        return $"web+stellar:pay?destination={publicKey}&amount=100";
                    }
                }

                // Remove " - Stellar" suffix if present
                publicKey = publicKey.Replace(" - Stellar", "");

                // Build a basic SEP-0007 URI
                string uri = $"web+stellar:pay?destination={publicKey}&amount=100";
                Debug.Log($"Generated Stellar URI: {uri}");
                return uri;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error generating Stellar URI: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
                return $"web+stellar:pay?destination={publicKey}&amount=100";
            }
        }

        private void OnMapZoomChanged()
        {
            if (map == null) return;
            
            // Adjust quality based on zoom level
            float zoomFactor = Mathf.Pow(2, map.zoom - 10);
            map.width = Mathf.RoundToInt(12288 * zoomFactor);
            map.height = Mathf.RoundToInt(12288 * zoomFactor);
            
            // Force immediate redraw for better quality
            map.needRedraw = true;
            map.Redraw();
        }

        public bool IsQRCodeScanned(string publicKey)
        {
            return qrManager != null && qrManager.IsQRCodeScanned(publicKey);
        }
        
        /// <summary>
        /// Method that can be called from JavaScript to ensure backend components are created
        /// </summary>
        public void EnsureBackendComponents()
        {
            Debug.Log("[GetBlockchainMarkers] Ensuring backend components are available...");
            
            // Load SorobanConfig if not already loaded
            if (sorobanConfig == null)
            {
                Debug.Log("[GetBlockchainMarkers] Loading SorobanConfig from Resources...");
                sorobanConfig = Resources.Load<SorobanConfig>("SorobanConfig");
                
                if (sorobanConfig == null)
                {
                    Debug.LogError("[GetBlockchainMarkers] Failed to load SorobanConfig from Resources. Creating default config...");
                    // Create a default config to prevent errors
                    sorobanConfig = ScriptableObject.CreateInstance<SorobanConfig>();
                }
                else
                {
                    Debug.Log($"[GetBlockchainMarkers] SorobanConfig loaded successfully. RPC URL: {sorobanConfig.rpcUrl}");
                }
            }
            
            // Ensure SorobanManager is available
            if (SorobanManager.Instance == null)
            {
                Debug.LogWarning("[GetBlockchainMarkers] SorobanManager not found, creating one...");
                var sorobanManagerObj = new GameObject("SorobanManager");
                sorobanManagerObj.AddComponent<SorobanManager>();
                DontDestroyOnLoad(sorobanManagerObj);
            }
            
            // Ensure StellarWalletManager is available
            var walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager == null)
            {
                Debug.LogWarning("[GetBlockchainMarkers] StellarWalletManager not found, creating one...");
                var walletManagerObj = new GameObject("StellarWalletManager");
                walletManagerObj.AddComponent<StellarWalletManager>();
                DontDestroyOnLoad(walletManagerObj);
            }
            
            // Ensure QRPaymentProcessor is available and connected to the wallet
            var qrProcessor = FindFirstObjectByType<QRPaymentProcessor>();
            if (qrProcessor == null)
            {
                Debug.LogWarning("[GetBlockchainMarkers] QRPaymentProcessor not found, creating one...");
                var qrProcessorObj = new GameObject("QRPaymentProcessor");
                qrProcessorObj.AddComponent<QRPaymentProcessor>();
                DontDestroyOnLoad(qrProcessorObj);
            }
            
            Debug.Log("[GetBlockchainMarkers] Backend components ensured successfully");
        }
        
        /// <summary>
        /// Ensures the authenticated wallet is available for payments
        /// </summary>
        public bool EnsureAuthenticatedWallet()
        {
            Debug.Log("[GetBlockchainMarkers] Ensuring authenticated wallet is available...");
            
            var walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager == null)
            {
                Debug.LogError("[GetBlockchainMarkers] StellarWalletManager not found!");
                return false;
            }
            
            if (!walletManager.HasWallet())
            {
                Debug.LogWarning("[GetBlockchainMarkers] No wallet found. User needs to authenticate with passkey first.");
                return false;
            }
            
            var wallet = walletManager.GetCurrentWallet();
            if (wallet == null || string.IsNullOrEmpty(wallet.publicKey))
            {
                Debug.LogError("[GetBlockchainMarkers] Wallet data is invalid!");
                return false;
            }
            
            Debug.Log($"[GetBlockchainMarkers] Authenticated wallet available: {wallet.publicKey}");
            return true;
        }
        
        private IEnumerator CreateLaserWithDelay(OnlineMapsMarker3D marker)
        {
            yield return new WaitForSeconds(0.2f); // Wait for marker to be properly positioned

            if (marker != null && marker.instance != null && activeLasers.Count < MAX_LASERS)
            {
                GameObject laserObj = new GameObject($"GroundLaser_{marker.label}");
                laserObj.transform.SetParent(marker.instance.transform);
                
                LineRenderer laser = laserObj.AddComponent<LineRenderer>();
                laser.material = sharedLaserMaterial ?? new Material(Shader.Find("Sprites/Default"));
                laser.startColor = laserColor;
                laser.endColor = laserColor;
                laser.startWidth = laserWidth;
                laser.endWidth = 0.1f; // Thinner at the top for better effect
                laser.positionCount = 2;
                laser.useWorldSpace = true;

                // Set ground laser positions
                Vector3 markerPos = marker.instance.transform.position;
                Vector3 groundPos = new Vector3(markerPos.x, 0, markerPos.z);
                laser.SetPosition(0, groundPos);
                laser.SetPosition(1, markerPos);

                activeLasers.Add(laser);
                Debug.Log($"Added laser effect to marker: {marker.label} (Total lasers: {activeLasers.Count})");
            }
            else if (activeLasers.Count >= MAX_LASERS)
            {
                Debug.LogWarning($"Maximum laser count reached ({MAX_LASERS}). Skipping laser creation for marker: {marker?.label}");
            }
        }

        // Public method to manually trigger QR code creation for testing
        public void ManuallyCreateQRCodes()
        {
            Debug.Log("[GetBlockchainMarkers] Manually creating QR codes for all markers");
            if (currentVisibleMarkers != null && currentVisibleMarkers.Count > 0)
            {
                Debug.Log($"[GetBlockchainMarkers] Found {currentVisibleMarkers.Count} markers in currentVisibleMarkers");
                foreach (var markerData in currentVisibleMarkers)
                {
                    // Find the corresponding 3D marker instance
                    if (markerMGR != null)
                    {
                        // This is a simplified approach - in practice, you might need to store references to the actual marker instances
                        Debug.Log($"[GetBlockchainMarkers] Marker data: {markerData.label} at {markerData.publicKey}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GetBlockchainMarkers] No visible markers found. Try loading markers first.");
            }
        }
        
        // Public method to test wallet connection and status
        [ContextMenu("Test Wallet Connection")]
        public void TestWalletConnection()
        {
            Debug.Log("=== Testing Wallet Connection ===");
            
            var walletManager = FindFirstObjectByType<StellarWalletManager>();
            if (walletManager == null)
            {
                Debug.LogError("❌ StellarWalletManager not found!");
                return;
            }
            
            Debug.Log("✅ StellarWalletManager found");
            
            if (!walletManager.HasWallet())
            {
                Debug.LogWarning("⚠️ No wallet found. User needs to authenticate with passkey first.");
                return;
            }
            
            Debug.Log("✅ Wallet exists");
            
            var wallet = walletManager.GetCurrentWallet();
            if (wallet == null)
            {
                Debug.LogError("❌ GetCurrentWallet() returned null!");
                return;
            }
            
            Debug.Log($"✅ Wallet data retrieved");
            Debug.Log($"   Public Key: {wallet.publicKey}");
            Debug.Log($"   Has Secret: {!string.IsNullOrEmpty(wallet.secretKey)}");
            Debug.Log($"   Created: {wallet.createdAt}");
            Debug.Log($"   Network: {wallet.network}");
            Debug.Log($"   Is Funded: {wallet.isFunded}");
            
            // Test QRPaymentProcessor connection
            var qrProcessor = FindFirstObjectByType<QRPaymentProcessor>();
            if (qrProcessor == null)
            {
                Debug.LogWarning("⚠️ QRPaymentProcessor not found!");
            }
            else
            {
                Debug.Log("✅ QRPaymentProcessor found");
            }
            
            Debug.Log("=== Wallet Connection Test Complete ===");
        }
    }
}