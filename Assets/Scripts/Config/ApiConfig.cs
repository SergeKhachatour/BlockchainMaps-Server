using UnityEngine;

[CreateAssetMenu(fileName = "ApiConfig", menuName = "Config/API Configuration")]
public class ApiConfig : ScriptableObject
{
    [SerializeField] private string bearerToken;
    public string BearerToken => bearerToken;
} 