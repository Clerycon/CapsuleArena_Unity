using System;
using UnityEngine;

public class PlayerMovementValidator : MonoBehaviour
{
    public event Action<MovementViolationType, Vector3, float, float> OnViolationDetected;
    public event Action<Vector3> OnCorrectionTimeout;
    public event Action OnViolationThresholdCrossed;
    [SerializeField] private MovementDataSO _movementDataSO;
    [SerializeField] private MovementValidationDataSO _movementValidationDataSO;
    private float _tickRate;
    private bool _hasLastValidState;
    private Vector3 _lastValidPosition;
    private int _lastValidTick;
    private bool _isWaitingForTeleport;
    private Vector3 _expectedPositionToTeleport;
    private float _timeWaitingStarted;

    private float _violationScore;
    public float ViolationScore => _violationScore;

    private bool _isViolationThresholdEventInvoked;

    private void Update()
    {
        if (_isWaitingForTeleport)
        {
            if((Time.time - _timeWaitingStarted) > _movementValidationDataSO.CorrectionTimeout)
            {
                _timeWaitingStarted = Time.time;
                AddViolationScore(MovementViolationType.CorrectionIgnored);
                OnCorrectionTimeout?.Invoke(_expectedPositionToTeleport); 
                CheckViolationThreshold();
            }
        }
        else
        {
            _violationScore = Mathf.Max(0f, _violationScore - _movementValidationDataSO.ScoreDecreaseInSec * Time.deltaTime);
        }
    }
    public void Initialize(uint tickRate)
    {
        _tickRate = tickRate;
        _hasLastValidState = false;
        _violationScore = 0f;
        _isViolationThresholdEventInvoked = false;
    }

    public void ValidateState(Vector3 position, int networkTick, bool isTeleporting)
    {
        if (!_hasLastValidState)
        {
            _lastValidPosition = position;
            _lastValidTick = networkTick;
            _hasLastValidState = true;
            return;
        }

        if(networkTick <= _lastValidTick) { return; }

        if (_isWaitingForTeleport)
        {
            if (isTeleporting && Vector2.Distance(
                new Vector2(_expectedPositionToTeleport.x, _expectedPositionToTeleport.z),
                new Vector2(position.x, position.z)) <= _movementValidationDataSO.TargetTolerance)
            {
                _lastValidPosition = position;
                _lastValidTick = networkTick;
                _isWaitingForTeleport = false;
                LogManager.Log(this, $"Expected teleport applied at {position}.");
            }
            return;
        }

        if (isTeleporting)
        {
            AddViolationScore(MovementViolationType.UnexpectedTeleport);
            OnViolationDetected?.Invoke(MovementViolationType.UnexpectedTeleport, _lastValidPosition, 
                Vector2.Distance(new Vector2(_lastValidPosition.x,_lastValidPosition.z), new Vector2(position.x, position.z)), 0f);
            CheckViolationThreshold();
            return;
        }

        float elapsedTime = (networkTick - _lastValidTick) / _tickRate;

        float cappedElapsedTime = Mathf.Min(elapsedTime, _movementValidationDataSO.MaxAllowedTime);

        float movedDistance = Vector2.Distance(
            new Vector2(_lastValidPosition.x,_lastValidPosition.z),
            new Vector2(position.x, position.z));
        float allowedDistance = _movementDataSO.MovementSpeed * cappedElapsedTime 
            * (1 + _movementValidationDataSO.PercentageTolerance) 
            + _movementValidationDataSO.FixedTolerance;

        if(movedDistance <= allowedDistance)
        {
            if(elapsedTime >= _movementValidationDataSO.TimeWindow)
            {
                _lastValidPosition = position;
                _lastValidTick = networkTick;
            }
        }
        else
        {
            if(movedDistance > allowedDistance * _movementValidationDataSO.JumpFactor)
            {
                AddViolationScore(MovementViolationType.LargeJump);
                OnViolationDetected?.Invoke(MovementViolationType.LargeJump, _lastValidPosition, movedDistance, allowedDistance);
            }
            else
            {
                AddViolationScore(MovementViolationType.SpeedExceeded);
                OnViolationDetected?.Invoke(MovementViolationType.SpeedExceeded, _lastValidPosition, movedDistance, allowedDistance);
            }
            CheckViolationThreshold();
        }
    }


    public void ExpectTeleport(Vector3 targetPosition)
    {
        _isWaitingForTeleport = true;
        _expectedPositionToTeleport = targetPosition;
        _timeWaitingStarted = Time.time;
    }

    private void AddViolationScore(MovementViolationType movementViolationType)
    {
        _violationScore += movementViolationType switch
        {
            MovementViolationType.SpeedExceeded => _movementValidationDataSO.SpeedExceedViolationPoint,
            MovementViolationType.UnexpectedTeleport => _movementValidationDataSO.UnexpectedTeleportViolationPoint,
            MovementViolationType.LargeJump => _movementValidationDataSO.LargeJumpViolationPoint,
            MovementViolationType.CorrectionIgnored => _movementValidationDataSO.CorrectionIgnoredViolationPoint,
            _ => 0f
        };
    }

    private void CheckViolationThreshold()
    {
        if(_violationScore >= _movementValidationDataSO.ScoreThreshold && !_isViolationThresholdEventInvoked)
        {
            _isViolationThresholdEventInvoked = true;
            OnViolationThresholdCrossed?.Invoke();
        }
    }
}
