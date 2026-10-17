using UnityEngine;

public class InputManager : MonoBehaviour
{
    // Singleton Pattern
    public static InputManager Instance { get; private set; }

    // Inputs
    private Inputs _inputs;

    // Context
    private InputContext _inputContext;

    // Get Properties 
    public Inputs Inputs => _inputs;
    public InputContext InputContext => _inputContext;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            LogManager.LogError(this, "Another InputManager already exists in the scene.");
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

        SetContext(InputContext.UI);
    }

    public void SetContext(InputContext inputContext)
    {   
        if(_inputContext == inputContext)
        {
            return;
        }
        InputContext previousInputContext = _inputContext;
        _inputContext = inputContext;

        switch (inputContext)
        {
            case InputContext.None:
                _inputs.Gameplay.Disable();
                _inputs.UI.Disable();
                break;

            case InputContext.Gameplay:
                _inputs.UI.Disable();
                _inputs.Gameplay.Enable();
                break;

            case InputContext.UI:
                _inputs.Gameplay.Disable();
                _inputs.UI.Enable();
                break;
        }

        LogManager.Log(this, $"Input context changed: {previousInputContext} -> {_inputContext}");
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
