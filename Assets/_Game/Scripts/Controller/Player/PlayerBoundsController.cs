using System;
using UnityEngine;

public class PlayerBoundsController : MonoBehaviour
{
    public event Action OnPlayerLeftArena;
    [SerializeField] private float _maxAllowedDistanceFromCenter = 15f;

    private bool _isPlayerLeftArena;

    private void Update()
    {
        if(Vector3.Distance(Vector3.zero, transform.position) > _maxAllowedDistanceFromCenter)
        {
            if (!_isPlayerLeftArena)
            {
                OnPlayerLeftArena?.Invoke();
                _isPlayerLeftArena = true;
            }
        }
        else
        {
            if (_isPlayerLeftArena)
            {
                _isPlayerLeftArena = false;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        UnityEditor.Handles.color = Color.green;
        UnityEditor.Handles.DrawWireDisc(Vector3.zero, Vector3.up, _maxAllowedDistanceFromCenter);
    }
#endif
}
