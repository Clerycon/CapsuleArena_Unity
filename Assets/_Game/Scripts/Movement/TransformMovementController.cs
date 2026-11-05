using UnityEngine;

public class TransformMovementController : MonoBehaviour, IMovementMotor
{
    [SerializeField] private MovementDataSO _movementDataSO;
    private float _speedMultiplier = 1f;
    private bool _isSimulationEnabled;

    private void OnEnable()
    {
        if(_movementDataSO == null)
        {
            LogManager.LogError(this, "MovementDataSO is not assigned.");
            enabled = false;
        }
    }

    #region Motor
    public void SetSimulationEnabled(bool isEnabled)
    {
        _isSimulationEnabled = isEnabled;
    }
    public void Simulate(MovementInput movementInput, float deltaTime)
    {
        if(!enabled || !_isSimulationEnabled) { return; }

        Vector3 clampedMoveDirection = Vector3.ClampMagnitude(movementInput.MoveDirection, 1f);
        transform.position += clampedMoveDirection * _movementDataSO.MovementSpeed * _speedMultiplier * deltaTime;
    }
    public MovementState GetState()
    {
        return new MovementState { Position = transform.position };
    }
    public void SetState(MovementState movementState)
    {
        transform.position = movementState.Position;
    }
    public void SetSpeedMultiplier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }
    #endregion
}
