using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerPredictionController : NetworkBehaviour
{
    [SerializeField] private PredictionDataSO _predictionDataSO;
    
    private IMovementMotor _movementMotor;
    private PredictionHistoryBuffer _predictionHistoryBuffer;
    private float _tickDeltaTime;
    private Vector3 _latestMoveDirection;
    private uint _lastInputSequence;
    private bool _isPredictionActive;

    [Header("Debug")] [SerializeField] private bool _isInputLoggingEnabled;

    private void OnEnable()
    {
        if(_predictionDataSO == null)
        {
            LogManager.LogError(this, "PredictionDataSO is not assigned.");
            enabled = false;
        }
    }

    public void StartPrediction(IMovementMotor movementMotor)
    {
        if (!enabled)
        {
            LogManager.LogError(this, "Cannot start prediction: component is disabled (check PredictionDataSO).");
            return;
        }

        if(_isPredictionActive) { return; }

        if(movementMotor == null)
        {
            LogManager.LogError(this, "Cannot start prediction: movement motor is null.");
            return;
        }

        _movementMotor = movementMotor;
        uint tickRate = NetworkManager.NetworkConfig.TickRate;
        _tickDeltaTime = 1f / tickRate;
        int capacity = Mathf.CeilToInt(_predictionDataSO.InputBufferDurationSeconds * tickRate);
        capacity = Mathf.Max(capacity, _predictionDataSO.RedundantInputCount);
        _predictionHistoryBuffer = new PredictionHistoryBuffer(capacity);
        _lastInputSequence = 0;
        _latestMoveDirection = Vector3.zero;
        NetworkManager.NetworkTickSystem.Tick += NetworkTickSystem_Tick;
        _isPredictionActive = true;
        LogManager.Log(this, $"Prediction started. Tick delta: {_tickDeltaTime:F4}s, buffer capacity: {capacity}, owner: {IsOwner}, server: {IsServer}.");
    }

    private void NetworkTickSystem_Tick()
    {
        if(IsOwner && !IsServer)
        {
            SimulateOwnerTick();
        }
    }

    private void SimulateOwnerTick()
    {
        _lastInputSequence++;

        PlayerInputData inputData = new PlayerInputData{ 
            Sequence = _lastInputSequence, 
            MovementInput = new MovementInput { MoveDirection = _latestMoveDirection}
        };

        _movementMotor.Simulate(inputData.MovementInput, _tickDeltaTime);

        PredictionRecord predictionRecord = new PredictionRecord
        {
            Sequence = inputData.Sequence,
            MovementInput = inputData.MovementInput,
            PredictedState = _movementMotor.GetState()  
        };

        _predictionHistoryBuffer.Add(predictionRecord);
        if (_isInputLoggingEnabled) LogManager.Log(this, $"Predicted input {inputData.Sequence}: direction {inputData.MovementInput.MoveDirection}, position {predictionRecord.PredictedState.Position}");
        SendRecentInputs();
    }

    private void SendRecentInputs()
    {
        int inputCount = Mathf.Min(_predictionDataSO.RedundantInputCount, (int)_lastInputSequence);

        PlayerInputData[] inputs = new PlayerInputData[inputCount];

        for(int i = 0; i < inputCount; i++)
        {
            uint sequence = _lastInputSequence - (uint)(inputCount - 1 - i);

            bool hasRecord = _predictionHistoryBuffer.TryGet(sequence, out PredictionRecord record);

            if (hasRecord)
            {
                inputs[i] = new PlayerInputData
                {
                    Sequence = record.Sequence,
                    MovementInput = record.MovementInput,  
                };
            }
        }

        SubmitInputsRpc(inputs);
    }


    [Rpc(SendTo.Server, Delivery = RpcDelivery.Unreliable)]
    private void SubmitInputsRpc(PlayerInputData[] inputs, RpcParams rpcParams = default)
    {
        if(rpcParams.Receive.SenderClientId != OwnerClientId)
        {
            LogManager.LogWarning(this, $"Rejected inputs from client {rpcParams.Receive.SenderClientId}, owner is {OwnerClientId}.");
            return;
        }

        if(inputs == null || inputs.Length == 0)
        {
            return;
        }

        if (_isInputLoggingEnabled)
        {
            LogManager.Log(this, $"Received inputs {inputs[0].Sequence}-{inputs[inputs.Length - 1].Sequence} from client {rpcParams.Receive.SenderClientId}.");
        }
    }

    public void StopPrediction()
    {
        if(!_isPredictionActive) { return; }
        if(NetworkManager != null && NetworkManager.NetworkTickSystem != null)
        {
            NetworkManager.NetworkTickSystem.Tick -= NetworkTickSystem_Tick;
        }
        _predictionHistoryBuffer?.Clear();
        _latestMoveDirection = Vector3.zero;
        _isPredictionActive = false;
        _movementMotor = null;
        LogManager.Log(this, "Prediction stopped.");
    }

    public void SetMoveDirection(Vector3 moveDirection)
    {
        _latestMoveDirection = moveDirection;
    }

    public override void OnNetworkDespawn()
    {
        StopPrediction();
    }
}
