using Unity.Netcode;
using UnityEngine;

public class NetworkSpawnLogger : NetworkBehaviour
{
    [SerializeField] private bool _isLoggingEnabled = true;

    public override void OnNetworkSpawn()
    {
        if(!_isLoggingEnabled) { return; }

        LogManager.Log(this, 
            $"Spawned | NetworkObjectId: {NetworkObjectId}, OwnerClientId: {OwnerClientId} | " +
            $"IsServer: {IsServer}, IsHost: {IsHost}, IsClient: {IsClient} | " + 
            $"IsOwner: {IsOwner}, IsLocalPlayer: {IsLocalPlayer}, IsOwnedByServer: {IsOwnedByServer}.");
    }

    public override void OnNetworkDespawn()
    {
        if(!_isLoggingEnabled) { return; }

        LogManager.Log(this, 
            $"Despawned | NetworkObjectId: {NetworkObjectId}, OwnerClientId: {OwnerClientId}");
    }
}
