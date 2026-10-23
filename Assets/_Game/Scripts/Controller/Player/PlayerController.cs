using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _playerCameraTarget;
    [SerializeField] private PlayerInputController _playerInputController;
    [SerializeField] private PlayerCanvasController _playerCanvasController;
    [SerializeField] private PlayerMovementController _playerMovementController;
    [SerializeField] private PlayerBoundsController _playerBoundsController;
    [SerializeField] private PlayerNetworkTransform _playerNetworkTransform;
    [SerializeField] private PlayerMovementValidator _playerMovementValidator;
    [SerializeField] private MovementAuthorityMode _movementAuthorityMode = MovementAuthorityMode.Server;

    [Header("Debug")]
    [SerializeField] private bool _isMoveRpcLoggingEnabled;

    private Vector3 _lastSentMoveDirection;
    private MovementAuthorityMode _resolvedMovementAuthorityMode;
    private bool _isCheatSpeedBoostActive;
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

        if(IsServer && _resolvedMovementAuthorityMode == MovementAuthorityMode.OwnerValidated)
        {
            _playerNetworkTransform.OnPositionChanged += PlayerNetworkTransform_OnPositionChanged;
            _playerMovementValidator.Initialize(NetworkManager.NetworkConfig.TickRate);
            _playerMovementValidator.enabled = true;
            _playerMovementValidator.OnViolationDetected += PlayerMovementValidator_OnViolationDetected;
            _playerMovementValidator.OnCorrectionTimeout += PlayerMovementValidator_OnCorrectionTimeOut;
            _playerMovementValidator.OnViolationThresholdCrossed += PlayerMovementValidator_OnViolationThresholdCrossed;
        }

        string nameText = IsOwner ? $"<color=green>Player {OwnerClientId}</color>" : $"<color=red>Player {OwnerClientId}</color>";
        _playerCanvasController.SetPlayerName(nameText);

        if(!IsOwner) { return; }

        _playerInputController.enabled = true;

        if(InputManager.Instance == null)
        {
            LogManager.LogWarning(this, "InputManager not found. Cheat keys are disabled.");
        }
        else
        {
            InputManager.Instance.Inputs.Developer.CheatTeleport.performed += InputManager_CheatTeleport;
            InputManager.Instance.Inputs.Developer.CheatSpeedBoost.performed += InputManager_CheatSpeedBoost;
        }
        

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
                _resolvedMovementAuthorityMode = _movementAuthorityMode;
                break;
        }

        _playerNetworkTransform.AuthorityMode = _isOwnerAuthorityMode ? NetworkTransform.AuthorityModes.Owner : NetworkTransform.AuthorityModes.Server;
        LogManager.Log(this, $"Movement authority: {_resolvedMovementAuthorityMode}, NetworkTransform authority: {_playerNetworkTransform.AuthorityMode}");
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
                SendOwnerTeleport(spawnPose.Value.position, spawnPose.Value.rotation); 
            }
            else
            {
                _playerNetworkTransform.Teleport(spawnPose.Value.position, spawnPose.Value.rotation, transform.localScale);
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
        
        _playerNetworkTransform.Teleport(spawnPosition, spawnRotation, transform.localScale);
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

    private void PlayerNetworkTransform_OnPositionChanged(Vector3 position, int networkTick, bool isTeleporting)
    {
        if (_isMoveRpcLoggingEnabled)
        {
            LogManager.Log(this, $"Player {OwnerClientId} state: tick {networkTick}, position {position}, teleport {isTeleporting}");
        }

        _playerMovementValidator.ValidateState(position, networkTick, isTeleporting);
    }


    private void PlayerMovementValidator_OnViolationDetected(MovementViolationType movementViolationType, Vector3 lastValidPosition, float movedDistance, float allowedDistance)
    {
        if(movementViolationType == MovementViolationType.SpeedExceeded)
        {
            LogManager.LogWarning(this, $"Player {OwnerClientId} speed violation: moved {movedDistance:F2} m, allowed {allowedDistance:F2} m. Score: {_playerMovementValidator.ViolationScore:F1}");
        }
        else if(movementViolationType == MovementViolationType.LargeJump)
        {
            LogManager.LogWarning(this, $"Player {OwnerClientId} large jump: moved {movedDistance:F2} m, allowed {allowedDistance:F2} m. Score: {_playerMovementValidator.ViolationScore:F1}");
        }
        else if (movementViolationType == MovementViolationType.UnexpectedTeleport)
        {
            LogManager.LogWarning(this, $"Player {OwnerClientId} unexpected teleport: jumped {movedDistance:F2} m. Score: {_playerMovementValidator.ViolationScore:F1}");
        }
        
        SendOwnerTeleport(lastValidPosition, transform.rotation);
    }

    private void PlayerMovementValidator_OnCorrectionTimeOut(Vector3 expectedPosition)
    {
        SendOwnerTeleport(expectedPosition, transform.rotation);
        LogManager.LogWarning(this, $"Player {OwnerClientId} ignored the correction, resending to {expectedPosition}. Score: {_playerMovementValidator.ViolationScore:F1}");
    }

    private void PlayerMovementValidator_OnViolationThresholdCrossed()
    {
        if(!IsServer) { return; }

        LogManager.LogWarning(this, $"Player {OwnerClientId} reached the violation threshold ({_playerMovementValidator.ViolationScore:F1}), kicking.");
        if(ConnectionManager.Instance == null)
        {
            LogManager.LogError(this, $"ConnectionManager not found. Player {OwnerClientId} could not be kicked.");
            return;
        }
        ConnectionManager.Instance.KickClient(OwnerClientId, "Kicked: movement violations.");
    }

    private void SendOwnerTeleport(Vector3 position, Quaternion rotation)
    {
        if(!IsServer) { return; }

        if(_resolvedMovementAuthorityMode == MovementAuthorityMode.OwnerValidated)
        {
            _playerMovementValidator.ExpectTeleport(position);
        }
        TeleportOwnerRpc(position, rotation);
    }

    private void InputManager_CheatSpeedBoost(InputAction.CallbackContext context)
    {
        _isCheatSpeedBoostActive = !_isCheatSpeedBoostActive;
        _playerMovementController.SetSpeedMultiplier(_isCheatSpeedBoostActive ? 3f : 1f);
        LogManager.LogWarning(this, $"[Cheat] Speed boost {(_isCheatSpeedBoostActive ? "on" : "off")}.");
    }

    private void InputManager_CheatTeleport(InputAction.CallbackContext context)
    {
        if (!_isOwnerAuthorityMode)
        {
            LogManager.LogWarning(this, $"[Cheat] Teleport only works with owner authority (current: {_resolvedMovementAuthorityMode}).");
            return;
        }

        Vector3 targetPosition = transform.position + Vector3.forward * 10f;
        _playerNetworkTransform.Teleport(targetPosition, transform.rotation, transform.localScale);
        LogManager.LogWarning(this, $"[Cheat] Teleported forward to {targetPosition}.");
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

        if(IsServer && _resolvedMovementAuthorityMode == MovementAuthorityMode.OwnerValidated)
        {
            _playerNetworkTransform.OnPositionChanged -= PlayerNetworkTransform_OnPositionChanged;
            _playerMovementValidator.OnViolationDetected -= PlayerMovementValidator_OnViolationDetected;
            _playerMovementValidator.enabled = false;
            _playerMovementValidator.OnCorrectionTimeout -= PlayerMovementValidator_OnCorrectionTimeOut;
            _playerMovementValidator.OnViolationThresholdCrossed += PlayerMovementValidator_OnViolationThresholdCrossed;
        }

        if(!IsOwner) { return; }
        
        _playerInputController.enabled = false;

        if(InputManager.Instance != null)
        {
            InputManager.Instance.Inputs.Developer.CheatTeleport.performed -= InputManager_CheatTeleport;
            InputManager.Instance.Inputs.Developer.CheatSpeedBoost.performed -= InputManager_CheatSpeedBoost;
        }
        

        if(CameraController.Instance != null)
        {
            CameraController.Instance.ClearTarget();
        }

        LogManager.Log(this, "Local player teardown completed.");
    }
}
