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

    public struct MarkerData
    {
        public string publicKey;
        public string blockchain;
        public string label;
    }

    public delegate void MarkersLoadedHandler(List<MarkerData> markers);
    public event MarkersLoadedHandler OnMarkersLoaded;

    void Start()
    {
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
                    }
                }

                // Update UI with only visible markers
                UpdateUI(visibleMarkers);
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

}
