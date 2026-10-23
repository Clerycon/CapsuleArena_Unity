using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private MovementDataSO _movementDataSO;

    private Vector3 _moveDirection;
    private float _speedMultiplier = 1f;

    private void OnEnable()
    {
        if(_movementDataSO == null)
        {
            LogManager.LogError(this, "MovementDataSO is not assigned.");
            enabled = false;
        }
    }

    private void Update()
    {
        
        transform.position += _moveDirection * _movementDataSO.MovementSpeed * _speedMultiplier * Time.deltaTime;
    }

    public void SetMoveDirection(Vector3 moveDirection)
    {
        _moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }

    public void SetSpeedMultiplier(float speedMultiplier)
    {
        _speedMultiplier = speedMultiplier;
    }
}
