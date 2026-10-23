using UnityEngine;

[CreateAssetMenu(fileName = "Movement Validation Data", menuName = "Scriptable Objects/Movement Validation Data")]
public class MovementValidationDataSO : ScriptableObject
{
    [Header("Violation Settings")]
    [SerializeField] private float _percentageTolerance = 0.15f;
    [SerializeField] private float _fixedTolerance = 0.5f;
    [SerializeField] private float _maxAllowedTime = 0.5f;
    [SerializeField] private float _targetTolerance = 0.5f;
    [SerializeField] private float _correctionTimeout = 1f;
    [SerializeField] private float _timeWindow = 0.25f;
    [SerializeField] private float _jumpFactor = 2f;
    [SerializeField] private float _scoreThreshold = 10f;
    [SerializeField] private float _scoreDecreaseInSec = 1f;
    
    [Header("Violation Points")]
    [SerializeField] private float _speedExceedViolationPoint = 1f;
    [SerializeField] private float _unexpectedTeleportViolationPoint = 5f;
    [SerializeField] private float _largeJumpViolationPoint = 5f;
    [SerializeField] private float _correctionIgnoredViolationPoint = 3f;
    

    public float PercentageTolerance => _percentageTolerance;
    public float FixedTolerance => _fixedTolerance;
    public float MaxAllowedTime => _maxAllowedTime;
    public float TargetTolerance => _targetTolerance;
    public float CorrectionTimeout => _correctionTimeout;
    public float TimeWindow => _timeWindow;
    public float JumpFactor => _jumpFactor;
    public float ScoreThreshold => _scoreThreshold;
    public float ScoreDecreaseInSec => _scoreDecreaseInSec;

    public float SpeedExceedViolationPoint => _speedExceedViolationPoint;
    public float UnexpectedTeleportViolationPoint => _unexpectedTeleportViolationPoint;
    public float LargeJumpViolationPoint => _largeJumpViolationPoint;
    public float CorrectionIgnoredViolationPoint => _correctionIgnoredViolationPoint;
}
