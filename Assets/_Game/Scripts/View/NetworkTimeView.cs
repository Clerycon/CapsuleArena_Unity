using TMPro;
using Unity.Netcode;
using UnityEngine;

public class NetworkTimeView : MonoBehaviour
{
    [SerializeField] private TMP_Text _networkTimeText;
    private void OnEnable()
    {
        if(_networkTimeText == null)
        {
            LogManager.LogError(this, "NetworkTimeText is not assigned.");
            enabled = false;
        }
    }

    private void Update()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if(networkManager == null || !networkManager.IsListening)
        {
            _networkTimeText.text = "Not Connected";
            return;
        }

        NetworkTime localTime = networkManager.LocalTime;
        NetworkTime serverTime = networkManager.ServerTime;
        
        double localServerDifferenceMs = (localTime.Time - serverTime.Time) * 1000;
        ulong roundTripTimeMs = networkManager.IsServer ? 0UL 
            : networkManager.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);
        uint tickRate = networkManager.NetworkConfig.TickRate;

        _networkTimeText.text = 
            $"Tick Rate: {tickRate}\nLocal: tick {localTime.Tick} {localTime.Time:F3}s\nServer: tick {serverTime.Tick} {serverTime.Time:F3}s\nLocal - Server: {localServerDifferenceMs:F0} ms\nRTT: {roundTripTimeMs} ms\nTime.time: {Time.timeAsDouble:F3}s";
    }
}
