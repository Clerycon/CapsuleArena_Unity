using Unity.Multiplayer.Tools.NetStatsMonitor;
using UnityEngine;
using UnityEngine.InputSystem;

public class NetStatsManager : MonoBehaviour
{   
    private RuntimeNetStatsMonitor _runtimeNetworkStatsMonitor;

    private void Awake()
    {
        _runtimeNetworkStatsMonitor = GetComponent<RuntimeNetStatsMonitor>();
        _runtimeNetworkStatsMonitor.Visible = false;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if(InputManager.Instance == null)
        {
            LogManager.LogError(this, "InputManager instance not found.");
            return;
        }

        InputManager.Instance.Inputs.Developer.NetStatsToggle.performed += InputManager_NetStatsToggle;
    }

    private void InputManager_NetStatsToggle(InputAction.CallbackContext context)
    {
        _runtimeNetworkStatsMonitor.Visible = !_runtimeNetworkStatsMonitor.Visible;
    }

    private void OnDestroy()
    {
        if (InputManager.Instance == null)
        {
            return;
        }

        InputManager.Instance.Inputs.Developer.NetStatsToggle.performed -= InputManager_NetStatsToggle;
    }
}
