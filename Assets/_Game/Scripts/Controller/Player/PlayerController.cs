using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private Transform _playerCameraTarget;
    [SerializeField] private PlayerInputController _playerInputController;
    [SerializeField] private PlayerCanvasController _playerCanvasController;

    public override void OnNetworkSpawn()
    {
        string nameText = IsOwner ? $"Player {OwnerClientId} (You)" : $"Player {OwnerClientId}";
        _playerCanvasController.SetPlayerName(nameText);

        if(!IsOwner) { return; }

        _playerInputController.enabled = true;
        if(CameraController.Instance == null)
        {
            LogManager.LogWarning(this, "CameraController not found. Camera will not follow the local player.");
        }
        else
        {
            CameraController.Instance.SetTarget(_playerCameraTarget);
        }

        LogManager.Log(this, "Local player setup completed.");
    }

    public override void OnNetworkDespawn()
    {
        if(!IsOwner) { return; }
        
        _playerInputController.enabled = false;

        if(CameraController.Instance != null)
        {
            CameraController.Instance.ClearTarget();
        }

        LogManager.Log(this, "Local player teardown completed.");
    }
}
