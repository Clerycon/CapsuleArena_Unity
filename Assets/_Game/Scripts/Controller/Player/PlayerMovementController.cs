using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private Vector3 _moveDirection;

    private void Update()
    {
        transform.position += _moveDirection * _moveSpeed * Time.deltaTime;
    }

    public void SetMoveDirection(Vector3 moveDirection)
    {
        _moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
    }
}
