using UnityEngine;

[CreateAssetMenu(fileName = "Movement Data", menuName = "Scriptable Objects/Movement Data")]
public class MovementDataSO : ScriptableObject
{
    [SerializeField] private float _movementSpeed;
    public float MovementSpeed => _movementSpeed; 
}
