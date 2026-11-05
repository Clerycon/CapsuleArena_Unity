using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovementController : MonoBehaviour, IMovementMotor
{
    [SerializeField] private MovementDataSO _movementDataSO;
    private float _speedMultiplier = 1f;
    private bool _isSimulationEnabled;
    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if(_movementDataSO == null)
        {
            LogManager.LogError(this, "MovementDataSO is not assigned.");
            enabled = false;
        }
    }

    private void Reset()
    {
        CharacterController characterController = GetComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.5f;
        characterController.center = Vector3.zero;
        characterController.skinWidth = 0.05f;
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45f;
        characterController.minMoveDistance = 0f;
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
        _characterController.Move(clampedMoveDirection * _movementDataSO.MovementSpeed * _speedMultiplier * deltaTime);
    }
    public MovementState GetState()
    {
        return new MovementState { Position = transform.position };
    }
    public void SetState(MovementState movementState)
    {
        transform.position = movementState.Position;
        Physics.SyncTransforms();
    }
    public void SetSpeedMultiplier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }
    #endregion
}
