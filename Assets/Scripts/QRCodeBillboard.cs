using UnityEngine;

namespace BlockchainMaps
{
    public class QRCodeBillboard : MonoBehaviour
    {
        private Camera mainCamera;
        
        void Start()
        {
            mainCamera = Camera.main;
        }
        
        void Update()
        {
            if (mainCamera != null)
            {
                // Make the QR code always face the camera
                transform.LookAt(mainCamera.transform);
                
                // Keep the QR code upright (don't rotate on Y axis)
                Vector3 eulerAngles = transform.rotation.eulerAngles;
                eulerAngles.z = 0; // Keep Z rotation at 0 to stay upright
                transform.rotation = Quaternion.Euler(eulerAngles);
            }
        }
    }
} 