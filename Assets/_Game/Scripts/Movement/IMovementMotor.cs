using UnityEngine;

public interface IMovementMotor
{
    void SetSimulationEnabled(bool isEnabled);
    void Simulate(MovementInput movementInput, float deltaTime);
    MovementState GetState();
    void SetState(MovementState movementState);
    void SetSpeedMultiplier(float speedMultiplier);
}
