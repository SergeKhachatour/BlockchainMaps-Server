using UnityEngine;

[CreateAssetMenu(fileName = "StellarConfig", menuName = "Config/StellarConfig")]
public class StellarConfig : ScriptableObject
{
    [Header("Network Settings")]
    public bool UseTestNetwork = true;
    public string NetworkPassphrase = "Test SDF Network ; September 2015";
    public string HorizonUrl = "https://horizon-testnet.stellar.org";

    [Header("Account Settings")]
    [SerializeField] private string secretKey;
    public string SecretKey => secretKey;

    [Header("Transaction Settings")]
    public decimal MaximumTransactionAmount = 100;
} 