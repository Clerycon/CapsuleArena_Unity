using UnityEngine;

[CreateAssetMenu(fileName = "Prediction Data", menuName = "Scriptable Objects/Prediction Data")]
public class PredictionDataSO : ScriptableObject
{
    [Header("Client")]

    [Tooltip("How many seconds of unacknowledged inputs the client keeps. Buffer size = duration x tick rate. Prediction pauses when the buffer is full.")]
    [Min(0.1f)][SerializeField] private float _inputBufferDurationSeconds = 1f;

    [Tooltip("How many of the latest inputs each packet carries. Higher values survive more consecutive packet losses but cost more bandwidth.")]
    [Range(1, 10)][SerializeField] private int _redundantInputCount = 3;

    [Header("Server")]

    [Tooltip("Maximum inputs the server queues per player. Oldest inputs are dropped beyond this; also limits input spam.")]
    [Min(1)][SerializeField] private int _maxServerQueueSize = 6;

    [Tooltip("What the server does when no input is queued for a tick: repeat the last input or stop.")]
    [SerializeField] private EmptyInputQueueStrategy _emptyQueueStrategy = EmptyInputQueueStrategy.RepeatLastInput;

    [Tooltip("Maximum ticks the last input is repeated while the queue is empty. After this the input is treated as stop. Used only with RepeatLastInput.")]
    [Min(0)][SerializeField] private int _maxRepeatedInputTicks = 6;

    [Header("Reconciliation")]

    [Tooltip("Maximum distance (m) between predicted and server positions that still counts as a match.")]
    [Min(0f)][SerializeField] private float _reconciliationTolerance = 0.01f;

    [Tooltip("Seconds the visual takes to blend into a corrected position. Not used for teleports.")]
    [Min(0f)][SerializeField] private float _correctionSmoothingDuration = 0.1f;

    public float InputBufferDurationSeconds => _inputBufferDurationSeconds;
    public int RedundantInputCount => _redundantInputCount;
    public int MaxServerQueueSize => _maxServerQueueSize;
    public EmptyInputQueueStrategy EmptyQueueStrategy => _emptyQueueStrategy;
    public int MaxRepeatedInputTicks => _maxRepeatedInputTicks;
    public float ReconciliationTolerance => _reconciliationTolerance;
    public float CorrectionSmoothingDuration => _correctionSmoothingDuration;

}
