using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    private Inputs _inputs;
    public Inputs Inputs => _inputs;
    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _inputs = new Inputs();

        if (Debug.isDebugBuild)
        {
            _inputs.Developer.Enable();
        }
    }

    private void OnDestroy()
    {
        if(Instance == this)
        {
            _inputs.Dispose();
            Instance = null;
        }
    }
}
