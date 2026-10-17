using Unity.Cinemachine;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get;  private set; }

    [SerializeField] private CinemachineCamera _cinemachineCamera;
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            LogManager.LogError(this, "Another CameraController already exists in the scene.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetTarget(Transform target)
    {
        if(target == null)
        {
            LogManager.LogWarning(this, "SetTarget called with a null target. Use ClearTarget instead.");
            return;
        }

        _cinemachineCamera.Target.TrackingTarget = target;
        _cinemachineCamera.PreviousStateIsValid = false;
        LogManager.Log(this, $"Camera target set: {target.root.name}/{target.name}");
    }

    public void ClearTarget()
    {
        _cinemachineCamera.Target.TrackingTarget = null;
        LogManager.Log(this, "Camera target cleared.");
    }
    private void OnDestroy()
    {
        if(Instance == this)
        {
            Instance = null;
        }
    }
}
