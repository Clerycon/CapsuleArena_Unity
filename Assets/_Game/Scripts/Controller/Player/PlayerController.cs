using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerCameraTarget;
    [SerializeField] private PlayerInputController _playerInputController;
    [SerializeField] private PlayerCanvasController _playerCanvasController;
    [SerializeField] private PlayerMovementController _playerMovementController;

    [Header("Debug")]
    [SerializeField] private bool _isMoveRpcLoggingEnabled;

    private Vector3 _lastSentMoveDirection;
    public override void OnNetworkSpawn()
    {
        _playerMovementController.enabled = IsServer;

        string nameText = IsOwner ? $"<color=green>Player {OwnerClientId}</color>" : $"<color=red>Player {OwnerClientId}</color>";
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

    private void Update()
    {
        RequestMove();
    }

    private void RequestMove()
    {
        if(!IsOwner) { return; }

        Vector3 moveDirection = _playerInputController.MoveDirection;
        moveDirection.y = 0f;
        
        if((_lastSentMoveDirection - moveDirection).sqrMagnitude < 0.0001f) { return; }

        RequestMoveRpc(moveDirection);

        _lastSentMoveDirection = moveDirection;

        if (_isMoveRpcLoggingEnabled)
        {
            LogManager.Log(this, $"Move direction sent: {moveDirection}");
        }
    }

    [Rpc(SendTo.Server)]
    private void RequestMoveRpc(Vector3 moveDirection, RpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            LogManager.LogWarning(this, $"Rejected move direction from client {rpcParams.Receive.SenderClientId}, owner is {OwnerClientId}.");
            return;
        }

        if((float.IsNaN(moveDirection.x) || float.IsInfinity(moveDirection.x)) 
            || (float.IsNaN(moveDirection.z) || float.IsInfinity(moveDirection.z)))
        {
            LogManager.LogWarning(this, $"Rejected invalid move direction from client {rpcParams.Receive.SenderClientId}: {moveDirection}");
            return;
        }
        moveDirection.y = 0f;

        _playerMovementController.SetMoveDirection(moveDirection);

        if (_isMoveRpcLoggingEnabled)
        {
            LogManager.Log(this, $"Move direction received from client {rpcParams.Receive.SenderClientId}: {moveDirection}");
        }
    } 

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            _playerMovementController.SetMoveDirection(Vector3.zero);
            _playerMovementController.enabled = false;
        }

        if(!IsOwner) { return; }
        
        _playerInputController.enabled = false;

        if(CameraController.Instance != null)
        {
            CameraController.Instance.ClearTarget();
        }

        LogManager.Log(this, "Local player teardown completed.");
    }
}
