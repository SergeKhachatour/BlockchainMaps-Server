using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BlockchainMaps.Soroban
{
    public class MarkerContract : MonoBehaviour
    {
        [SerializeField] private SorobanConfig sorobanConfig;
        private SorobanManager sorobanManager;

        private void Start()
        {
            sorobanManager = SorobanManager.Instance;
            if (sorobanConfig == null)
            {
                Debug.LogError("[MarkerContract] SorobanConfig is not assigned!");
            }
        }

        public async Task<string> CreateMarker(string publicKey, double latitude, double longitude, string label)
        {
            try
            {
                var args = new object[]
                {
                    publicKey,
                    latitude,
                    longitude,
                    label
                };

                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerFactoryContractId,
                    "create_marker",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error creating marker: {e.Message}");
                throw;
            }
        }

        public async Task<string> GetMarkerInfo(string markerId)
        {
            try
            {
                var args = new object[] { markerId };
                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerRegistryContractId,
                    "get_marker",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error getting marker info: {e.Message}");
                throw;
            }
        }

        public async Task<string> UpdateMarkerLocation(string markerId, double latitude, double longitude)
        {
            try
            {
                var args = new object[]
                {
                    markerId,
                    latitude,
                    longitude
                };

                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerRegistryContractId,
                    "update_location",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error updating marker location: {e.Message}");
                throw;
            }
        }

        public async Task<string> GetMarkersInRadius(double centerLat, double centerLon, double radiusKm)
        {
            try
            {
                var args = new object[]
                {
                    centerLat,
                    centerLon,
                    radiusKm
                };

                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerRegistryContractId,
                    "get_markers_in_radius",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error getting markers in radius: {e.Message}");
                throw;
            }
        }

        public async Task<string> AttachTokenToMarker(string markerId, string tokenId, decimal amount)
        {
            try
            {
                var args = new object[]
                {
                    markerId,
                    tokenId,
                    amount.ToString()
                };

                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerRegistryContractId,
                    "attach_token",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error attaching token to marker: {e.Message}");
                throw;
            }
        }

        public async Task<string> GetMarkerTokens(string markerId)
        {
            try
            {
                var args = new object[] { markerId };
                string response = await sorobanManager.ExecuteContract(
                    sorobanConfig.markerRegistryContractId,
                    "get_marker_tokens",
                    args
                );

                return response;
            }
            catch (Exception e)
            {
                Debug.LogError($"[MarkerContract] Error getting marker tokens: {e.Message}");
                throw;
            }
        }
    }
} 