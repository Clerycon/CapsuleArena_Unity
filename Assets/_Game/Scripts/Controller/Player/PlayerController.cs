using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerCameraTarget;
    [SerializeField] private PlayerInputController _playerInputController;
    [SerializeField] private PlayerCanvasController _playerCanvasController;
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private PlayerBoundsController _playerBoundsController;
    [SerializeField] private NetworkTransform _networkTransform;
    [SerializeField] private MovementAuthorityMode _movementAuthorityMode = MovementAuthorityMode.Server;

    [Header("Debug")]
    [SerializeField] private bool _isMoveRpcLoggingEnabled;

    private Vector3 _lastSentMoveDirection;
    private MovementAuthorityMode _resolvedMovementAuthorityMode;
    private bool _isOwnerAuthorityMode => 
        (_resolvedMovementAuthorityMode == MovementAuthorityMode.Owner) || 
        (_resolvedMovementAuthorityMode == MovementAuthorityMode.OwnerValidated);

    private void Awake()
    {
        SetMovementAuthorityMode();
    }
    public override void OnNetworkSpawn()
    {
        _playerBoundsController.enabled = IsServer;

        if (IsServer)
        {
            _playerBoundsController.OnPlayerLeftArena += PlayerBoundsController_OnPlayerLeftArena;
        }

        if (_isOwnerAuthorityMode)
        {
            _playerMovementController.enabled = IsOwner;
        }
        else
        {
            _playerMovementController.enabled = IsServer;
        }

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
        Move();
    }

    private void SetMovementAuthorityMode()
    {
        switch (_movementAuthorityMode)
        {
            case MovementAuthorityMode.Server:
                _resolvedMovementAuthorityMode = _movementAuthorityMode;
                break;

            case MovementAuthorityMode.ServerPredicted:
                _resolvedMovementAuthorityMode = MovementAuthorityMode.Server;
                LogManager.LogWarning(this, $"{_movementAuthorityMode} is not implemented yet, falling back to {_resolvedMovementAuthorityMode}.");
                break;

            case MovementAuthorityMode.Owner:
                _resolvedMovementAuthorityMode = _movementAuthorityMode;
                break;

            case MovementAuthorityMode.OwnerValidated:
                _resolvedMovementAuthorityMode = MovementAuthorityMode.Owner;
                LogManager.LogWarning(this, $"{_movementAuthorityMode} is not implemented yet, falling back to {_resolvedMovementAuthorityMode}.");
                break;
        }

        _networkTransform.AuthorityMode = _isOwnerAuthorityMode ? NetworkTransform.AuthorityModes.Owner : NetworkTransform.AuthorityModes.Server;
        LogManager.Log(this, $"Movement authority: {_resolvedMovementAuthorityMode}, NetworkTransform authority: {_networkTransform.AuthorityMode}");
    }

    private void Move()
    {
        if(!IsOwner) { return; }

        Vector3 moveDirection = _playerInputController.MoveDirection;
        moveDirection.y = 0f;

        if (_isOwnerAuthorityMode)
        {
            ApplyMoveDirectionLocally(moveDirection);
        }
        else
        {
            SendMoveDirectionIfChanged(moveDirection);
        }
    }

    private void ApplyMoveDirectionLocally(Vector3 moveDirection)
    {
        _playerMovementController.SetMoveDirection(moveDirection);
    }

    private void SendMoveDirectionIfChanged(Vector3 moveDirection)
    {
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
        if (_isOwnerAuthorityMode)
        {
            LogManager.LogWarning(this, $"Rejected move direction from client {rpcParams.Receive.SenderClientId}, movement authority is {_resolvedMovementAuthorityMode}.");
            return;
        }

        if(rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            LogManager.LogWarning(this, $"Rejected move direction from client {rpcParams.Receive.SenderClientId}, owner is {OwnerClientId}.");
            return;
        }

        if(float.IsNaN(moveDirection.x) || float.IsInfinity(moveDirection.x) 
            || float.IsNaN(moveDirection.z) || float.IsInfinity(moveDirection.z))
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

    private void PlayerBoundsController_OnPlayerLeftArena()
    {
        if(!IsServer) { return; }

        Pose? spawnPose = SpawnManager.Instance?.GetRandomSpawnPose(OwnerClientId);

        if (spawnPose.HasValue)
        {
            LogManager.Log(this, $"Player {OwnerClientId} left the arena at {transform.position}, respawning at {spawnPose.Value.position}.");
            if (_isOwnerAuthorityMode)
            {
                TeleportOwnerRpc(spawnPose.Value.position,spawnPose.Value.rotation);   
            }
            else
            {
                _networkTransform.Teleport(spawnPose.Value.position, spawnPose.Value.rotation, transform.localScale);
                _playerMovementController.SetMoveDirection(Vector3.zero);
                ResetLastSentMoveDirectionRpc();
            }
        }
        else
        {
            LogManager.LogWarning(this, $"Player {OwnerClientId} left the arena but no spawn pose is available.");
        }

    }


    [Rpc(SendTo.Owner)]
    private void TeleportOwnerRpc(Vector3 spawnPosition, Quaternion spawnRotation, RpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
        {
            LogManager.LogWarning(this, $"Rejected teleport from client {rpcParams.Receive.SenderClientId}, only the server may send it.");
            return;
        }

        _networkTransform.Teleport(spawnPosition, spawnRotation, transform.localScale);
        _playerMovementController.SetMoveDirection(Vector3.zero);
        LogManager.Log(this, $"Teleported by server to {spawnPosition}.");
    }

    [Rpc(SendTo.Owner)]
    private void ResetLastSentMoveDirectionRpc(RpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
        {
            LogManager.LogWarning(this, $"Rejected move reset from client {rpcParams.Receive.SenderClientId}, only the server may send it.");
            return;
        }

        _lastSentMoveDirection = Vector3.zero;

        if (_isMoveRpcLoggingEnabled)
        {
            LogManager.Log(this, "Last sent move direction reset after respawn.");
        }
    }



    public override void OnNetworkDespawn()
    {
        if (_isOwnerAuthorityMode)
        {
            if (IsOwner)
            {
                _playerMovementController.SetMoveDirection(Vector3.zero);
                _playerMovementController.enabled = false;
            }
        }
        else
        {
            if (IsServer)
            {
                _playerMovementController.SetMoveDirection(Vector3.zero);
                _playerMovementController.enabled = false;
            }
        }

        if (IsServer)
        {
            
            _playerBoundsController.OnPlayerLeftArena -= PlayerBoundsController_OnPlayerLeftArena;
            _playerBoundsController.enabled = false;
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
