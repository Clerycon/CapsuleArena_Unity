using UnityEngine;

public class PlayerMovementController : MonoBehaviour
{
    [SerializeField] private PlayerInputController _playerInputController;

    [SerializeField] private float _moveSpeed = 5f;

    private void Update()
    {
        transform.position += _playerInputController.MoveDirection * _moveSpeed * Time.deltaTime;
    }
}
