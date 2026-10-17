using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public Vector3 MoveDirection { get; private set; }
    private void Update()
    {
        SetMoveDirection();
    }

    private void SetMoveDirection()
    {
        if(InputManager.Instance == null) { return; }

        Vector2 moveInput = InputManager.Instance.Inputs.Gameplay.Move.ReadValue<Vector2>();
        if(moveInput.sqrMagnitude < 0.0001f)
        {
            MoveDirection = Vector3.zero;
            return;
        }

        Vector3 camForward = Camera.main.transform.forward;
        Vector3 camRight = Camera.main.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        MoveDirection = camRight * moveInput.x + camForward * moveInput.y;
    }

    private void OnDisable()
    {
        MoveDirection = Vector3.zero;   
    }
}
