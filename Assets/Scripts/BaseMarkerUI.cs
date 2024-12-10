using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using CurvedUI;
using TMPro;

public class BaseMarkerUI : MonoBehaviour
{
    [SerializeField] private GameObject markerKeyPrefab;
    [SerializeField] private Transform contentParent;
    [SerializeField] private CurvedUISettings curvedUISettings;
    [SerializeField] private GetBlockchainMarkers blockchainMarkers;

    private void Start()
    {
        Debug.Log("BaseMarkerUI Start");
        
        if (blockchainMarkers == null)
        {
            blockchainMarkers = FindAnyObjectByType<GetBlockchainMarkers>();
            Debug.Log($"Found blockchainMarkers: {blockchainMarkers != null}");
        }

        if (blockchainMarkers != null)
        {
            Debug.Log("Subscribing to OnMarkersLoaded");
            blockchainMarkers.OnMarkersLoaded += DisplayBaseMarkerKeys;
        }
        else
        {
            Debug.LogError("Could not find GetBlockchainMarkers component!");
        }
    }

    private void DisplayBaseMarkerKeys(List<GetBlockchainMarkers.MarkerData> markers)
    {
        Debug.Log("=== DisplayBaseMarkerKeys ===");
        if (contentParent == null)
        {
            Debug.LogError("contentParent is null!");
            return;
        }

        Debug.Log($"DisplayBaseMarkerKeys called with {markers?.Count ?? 0} markers");
        if (markers == null || markers.Count == 0)
        {
            Debug.LogWarning("No markers to display!");
            return;
        }

        // Clear existing items
        int childCount = contentParent.childCount;
        Debug.Log($"Clearing {childCount} existing items");
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Create new items
        foreach (GetBlockchainMarkers.MarkerData marker in markers)
        {
            Debug.Log($"Processing marker - PublicKey: {marker.publicKey}, Blockchain: {marker.blockchain}");
            GameObject keyItem = Instantiate(markerKeyPrefab, contentParent);
            
            // Try TextMeshProUGUI first
            TextMeshProUGUI tmpText = keyItem.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpText != null)
            {
                string displayText = $"{marker.publicKey} - {marker.blockchain}";
                tmpText.text = displayText;
                tmpText.enabled = true;
                Debug.Log($"Set TMPro text: '{displayText}' on {GetGameObjectHierarchy(keyItem)}");
            }
            else
            {
                // Fall back to legacy Text
                Text legacyText = keyItem.GetComponentInChildren<Text>(true);
                if (legacyText != null)
                {
                    string displayText = $"{marker.publicKey} - {marker.blockchain}";
                    legacyText.text = displayText;
                    legacyText.enabled = true;
                    Debug.Log($"Set legacy text: '{displayText}' on {GetGameObjectHierarchy(keyItem)}");
                }
                else
                {
                    Debug.LogError($"No text components found on {GetGameObjectHierarchy(keyItem)}");
                }
            }
        }

        // Force layout update
        Debug.Log("Forcing layout rebuild");
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);
    }

    private string GetGameObjectHierarchy(GameObject obj)
    {
        string hierarchy = obj.name;
        Transform parent = obj.transform.parent;
        while (parent != null)
        {
            hierarchy = parent.name + "/" + hierarchy;
            parent = parent.parent;
        }
        return hierarchy;
    }

    private void OnDestroy()
    {
        if (blockchainMarkers != null)
        {
            blockchainMarkers.OnMarkersLoaded -= DisplayBaseMarkerKeys;
        }
    }
} 